#pragma warning disable CS8509

using System.Diagnostics;
using System.Threading.Channels;
using Grpc.Core;
using KurrentDB.Diagnostics;
using KurrentDB.Diagnostics.Tracing;
using KurrentDB.Protocol.Streams.V1;
using static KurrentDB.Protocol.Streams.V1.ReadResp.ContentOneofCase;
using AsyncStreamReaderExtensions = Kurrent.Grpc.AsyncStreamReaderExtensions;

namespace Kurrent.Client.Streams;

public partial class StreamsClient {
    public ValueTask<Result<Subscription, ReadError>> Subscribe(AllSubscriptionOptions options) =>
        SubscribeCore(
            StreamsClientV1Mapper.Requests.CreateSubscriptionRequest(options), options.BufferSize, options.Timeout,
            options.SkipDecoding, options.CancellationToken
        );

    public ValueTask<Result<Subscription, ReadError>> Subscribe(StreamSubscriptionOptions options) =>
        SubscribeCore(
            StreamsClientV1Mapper.Requests.CreateStreamSubscriptionRequest(options), options.BufferSize, options.Timeout,
            options.SkipDecoding, options.CancellationToken
        );

    async ValueTask<Result<Subscription, ReadError>> SubscribeCore(
        ReadReq request, int bufferSize, TimeSpan subscriptionTimeout, bool skipDecoding,
        CancellationToken cancellationToken
    ) {
        // Create a linked cancellation token source for background task control
        var cancellator = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var stoppingToken = cancellator.Token;

        var session = LegacyServiceClient.Read(request, cancellationToken: stoppingToken);

        // Check for access denied. the legacy exception mapper throws this exception...
        // we could skip it for this operation... requires refactoring the interceptor
        try {
            await session.ResponseStream.MoveNext(stoppingToken).ConfigureAwait(false);
        }
        catch (RpcException rex) when (rex.StatusCode == StatusCode.PermissionDenied) {
            return Result.Failure<Subscription, ReadError>(new ErrorDetails.AccessDenied());
        }

        // why would this even happen? seems like an unreachable state...
        if (session.ResponseStream.Current.ContentCase != Confirmation)
            throw new UnreachableException($"Expected confirmation message but got {session.ResponseStream.Current.ContentCase}");

        var subscriptionId = session.ResponseStream.Current.Confirmation.SubscriptionId;

        // Create a factory function instead of starting immediately
        var channelFactory = new Func<Channel<ReadMessage>>(() => StartMessageRelay(
                session, bufferSize, subscriptionTimeout,
                skipDecoding, cancellator
            )
        );

        // var channel = StartMessageRelay(session, bufferSize, subscriptionTimeout,  cancellator);

		var tags = Tags
			.WithRequiredTag(TraceConstants.Tags.DatabaseOperationName, TraceConstants.Operations.Subscribe)
			.WithRequiredTag(TraceConstants.Tags.DatabaseStream, request.Options.Stream.StreamIdentifier.StreamName)
			.WithRequiredTag(TraceConstants.Tags.DatabaseSubscriptionId, subscriptionId);

        return new Subscription(subscriptionId, channelFactory, tags);
    }

    /// <summary>
    /// Initiates a message relay process to supply messages via a channel.
    /// </summary>
    /// <param name="session">An asynchronous server streaming call providing subscription responses.</param>
    /// <param name="bufferSize">The maximum number of messages the channel can buffer before applying backpressure.</param>
    /// <param name="subscriptionTimeout">The maximum time to wait for a subscription message before timing out.</param>
    /// <param name="skipDecoding"></param>
    /// <param name="cancellator">A cancellation token source used to signal termination of the message relay process.</param>
    /// <returns>A bounded channel that streams subscription messages.</returns>
    Channel<ReadMessage> StartMessageRelay(
        AsyncServerStreamingCall<ReadResp> session, int bufferSize, TimeSpan subscriptionTimeout, bool skipDecoding,
        CancellationTokenSource cancellator
    ) {
        // create a bounded channel for backpressure control
        var channel = Channel.CreateBounded<ReadMessage>(
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
                    var messages = AsyncStreamReaderExtensions.ReadAllAsync(session.ResponseStream, stoppingToken)
                        .Where(x => x.ContentCase is Event or Checkpoint or CaughtUp or FellBehind)
                        .SelectAwait<ReadResp, ReadMessage>(async x => x.ContentCase switch {
                                Event => await x.Event.MapToRecord(
                                    SerializerProvider, MetadataDecoder, skipDecoding,
                                    stoppingToken
                                ).ConfigureAwait(false),
                                Checkpoint => x.Checkpoint.MapToHeartbeat(),
                                CaughtUp   => x.CaughtUp.MapToHeartbeat(),
                                FellBehind => x.FellBehind.MapToHeartbeat()
                            }
                        );

                    await foreach (var message in messages.ConfigureAwait(false)) {
                        // try to write immediately without blocking
                        if (channel.Writer.TryWrite(message))
                            continue; // no timeout needed

                        // the channel is full, we must add timeout protection
                        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken)
                            .With(x => x.CancelAfter(subscriptionTimeout));

                        await channel.Writer
                            .WriteAsync(message, timeout.Token)
                            .ConfigureAwait(false);
                    }

                    // it might already have been marked for
                    // completion manually or on dispose.
                    channel.Writer.TryComplete();
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken != stoppingToken) {
                    channel.Writer.TryComplete(
                        new TimeoutException($"Timed out after {subscriptionTimeout}. The application may not be reading messages fast enough.")
                    );
                }
                catch (OperationCanceledException) {
                    channel.Writer.TryComplete();
                }
                catch (Exception ex) {
                    channel.Writer.TryComplete(ex);
                }
                finally {
                    session.Dispose();
                    cancellator.Dispose();
                }
            }, stoppingToken
        );

        return channel;
    }
}
