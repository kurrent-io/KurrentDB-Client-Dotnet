#pragma warning disable CS8509

using System.Threading.Channels;
using EventStore.Client.Streams;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Model;
using KurrentDB.Client;
using static EventStore.Client.Streams.ReadResp.ContentOneofCase;
using ErrorDetails = Kurrent.Client.Model.ErrorDetails;

namespace Kurrent.Client;

public partial class KurrentStreamsClient {
    public ValueTask<Result<Subscription, SubscriptionError>> Subscribe(SubscriptionOptions options) =>
        SubscribeCore(StreamsV1Mapper.CreateSubscriptionRequest(options), options.BufferSize, options.SubscriptionTimeout, options.StoppingToken);

    public ValueTask<Result<Subscription, SubscriptionError>> Subscribe(StreamSubscriptionOptions options) =>
        SubscribeCore(StreamsV1Mapper.CreateStreamSubscriptionRequest(options), options.BufferSize, options.SubscriptionTimeout, options.StoppingToken);

    async ValueTask<Result<Subscription, SubscriptionError>> SubscribeCore(ReadReq request, int bufferSize, TimeSpan subscriptionTimeout, CancellationToken cancellationToken) {
        // Create a linked cancellation token source for background task control
        var cancellator = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var stoppingToken = cancellator.Token;

        var session = LegacyStreamsClient.Read(request, cancellationToken: stoppingToken);

        // Check for access denied. the legacy exception mapper throws this exception...
        // we could skip it for this operation... requires refactoring the interceptor
        try {
            await session.ResponseStream.MoveNext(stoppingToken).ConfigureAwait(false);
        }
        catch (AccessDeniedException) {
            return Result.Failure<Subscription, SubscriptionError>(new ErrorDetails.AccessDenied());
        }

        // why would this even happen? seems like an unreachable state...
        if (session.ResponseStream.Current.ContentCase != Confirmation)
            throw KurrentClientException.CreateUnknown(
                nameof(Subscription), new Exception($"Expected confirmation message but got {session.ResponseStream.Current.ContentCase}"));

        var subscriptionId = session.ResponseStream.Current.Confirmation.SubscriptionId;

        var channel = StartMessageRelay(session, bufferSize, subscriptionTimeout,  cancellator);

        return new Subscription(subscriptionId, channel);
    }

    /// <summary>
    /// Initiates a message relay process to supply messages via a channel.
    /// </summary>
    /// <param name="session">An asynchronous server streaming call providing subscription responses.</param>
    /// <param name="bufferSize">The maximum number of messages the channel can buffer before applying backpressure.</param>
    /// <param name="subscriptionTimeout">The maximum time to wait for a subscription message before timing out.</param>
    /// <param name="cancellator">A cancellation token source used to signal termination of the message relay process.</param>
    /// <returns>A bounded channel that streams subscription messages.</returns>
    Channel<SubscriptionMessage> StartMessageRelay(AsyncServerStreamingCall<ReadResp> session, int bufferSize, TimeSpan subscriptionTimeout, CancellationTokenSource cancellator) {
        // create a bounded channel for backpressure control
        var channel = Channel.CreateBounded<SubscriptionMessage>(
            new BoundedChannelOptions(bufferSize) {
                FullMode     = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true
            }
        );

        var stoppingToken = cancellator.Token;

        // start the background task to pump the messages through the channel
        _ = Task.Run(
            async () => {
                try {
                    var messages = session.ResponseStream.ReadAllAsync(stoppingToken)
                        .Where(x => x.ContentCase is Event or Checkpoint or CaughtUp or FellBehind)
                        .SelectAwait<ReadResp, SubscriptionMessage>(async x => x.ContentCase switch {
                            Event      => await LegacyConverter.ConvertToRecord(x.Event, stoppingToken).ConfigureAwait(false),
                            Checkpoint => x.Checkpoint.MapToHeartbeat(),
                            CaughtUp   => x.CaughtUp.MapToHeartbeat(),
                            FellBehind => x.FellBehind.MapToHeartbeat()
                        });

                    await foreach (var message in messages.ConfigureAwait(false)) {
                        // try to write immediately without blocking
                        if (channel.Writer.TryWrite(message))
                            continue; // no timeout needed

                        // the channel is full, we must add timeout protection
                        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        timeout.CancelAfter(subscriptionTimeout);

                        await channel.Writer
                            .WriteAsync(message, timeout.Token)
                            .ConfigureAwait(false);
                    }

                    // it might already have been marked for
                    // completion manually or on dispose.
                    channel.Writer.TryComplete();
                }  catch (OperationCanceledException ex) when (ex.CancellationToken != stoppingToken) {
                    channel.Writer.TryComplete(new TimeoutException($"Subscription timed out after {subscriptionTimeout}. The application may not be reading messages fast enough."));
                } catch (OperationCanceledException) {
                    channel.Writer.TryComplete();
                } catch (Exception ex) {
                    channel.Writer.TryComplete(ex);
                } finally {
                    session.Dispose();
                    cancellator.Dispose();
                }
            }, stoppingToken
        );

        return channel;
    }
}

#region . subscription processor - experimental .
/// <summary>
/// Configuration options for processor-based subscriptions.
/// </summary>
[PublicAPI]
record SubscriptionProcessorOptions {
    /// <summary>
    /// Maximum number of records to batch together for processing.
    /// Default: 100
    /// </summary>
    public int MaxBatchSize { get; init; } = 100;

    /// <summary>
    /// Maximum time to wait before processing a partial batch.
    /// Default: 1 second
    /// </summary>
    public TimeSpan MaxBatchWaitTime { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Whether to automatically handle heartbeats by calling OnHeartbeat.
    /// Default: true
    /// </summary>
    public bool ProcessHeartbeats { get; init; } = true;

    /// <summary>
    /// Whether to continue processing if OnHeartbeat throws an exception.
    /// Default: true
    /// </summary>
    public bool ContinueOnHeartbeatError { get; init; } = true;
}

/// <summary>
/// Defines the interface for processing subscription messages with methods for handling records and heartbeats.
/// Similar to AWS Kinesis IRecordProcessor pattern.
/// </summary>
[PublicAPI]
interface ISubscriptionProcessor {
    /// <summary>
    /// Called when records are available for processing.
    /// </summary>
    /// <param name="records">The batch of records to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous processing operation</returns>
    Task ProcessRecords(IReadOnlyList<Record> records, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when a heartbeat is received, indicating the subscription is alive
    /// and providing checkpoint information.
    /// </summary>
    /// <param name="heartbeat">The heartbeat containing position and timing information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous heartbeat processing operation</returns>
    Task OnHeartbeat(Heartbeat heartbeat, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the subscription is initialized and ready to start processing.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier for this subscription</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous initialization operation</returns>
    Task OnInitialize(string subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the subscription is being shut down or encounters an error.
    /// </summary>
    /// <param name="reason">The reason for shutdown</param>
    /// <param name="exception">The exception that caused shutdown, if any</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous shutdown operation</returns>
    Task OnShutdown(SubscriptionShutdownReason reason, Exception? exception = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the reason why a subscription was shut down.
/// </summary>
[PublicAPI]
enum SubscriptionShutdownReason {
    /// <summary>
    /// Subscription was explicitly stopped by the client
    /// (via StopAsync(), DisposeAsync(), or CancellationToken).
    /// </summary>
    ClientInitiated,

    /// <summary>
    /// Subscription was terminated due to application shutdown
    /// (e.g., host shutdown token, SIGTERM).
    /// </summary>
    ApplicationShutdown,

    /// <summary>
    /// Subscription was terminated due to a server error or connection issue.
    /// </summary>
    ServerError,

    /// <summary>
    /// Subscription was terminated due to an error in the processor implementation.
    /// </summary>
    ProcessorError,

    /// <summary>
    /// Subscription was terminated because the internal buffer was full
    /// and SubscriptionTimeout was exceeded waiting for the processor to catch up.
    /// </summary>
    ProcessingTimeout,

    /// <summary>
    /// Subscription was terminated due to network/connection timeout
    /// (no messages or heartbeats received from server within expected timeframe).
    /// </summary>
    ConnectionTimeout,

    /// <summary>
    /// Subscription was terminated due to access denial or stream deletion.
    /// </summary>
    AccessDenied,

    /// <summary>
    /// Subscription reached a natural end.
    /// </summary>
    Completed
}

#endregion
