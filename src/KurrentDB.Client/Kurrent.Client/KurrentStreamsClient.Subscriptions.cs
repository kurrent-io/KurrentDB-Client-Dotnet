#pragma warning disable CS8509

using System.Threading.Channels;
using EventStore.Client;
using EventStore.Client.Streams;
using Grpc.Core;
using Kurrent.Client.Model;
using KurrentDB.Client;
using static EventStore.Client.Streams.ReadResp.ContentOneofCase;
using ErrorDetails = Kurrent.Client.Model.ErrorDetails;

namespace Kurrent.Client;

public partial class KurrentStreamsClient {
    public ValueTask<Result<Subscription, SubscriptionError>> Subscribe(SubscriptionOptions options) =>
        SubscribeInternal(options.CreateSubscriptionRequest(), options.BufferSize, options.SubscriptionTimeout, options.StoppingToken);

    public ValueTask<Result<Subscription, SubscriptionError>> Subscribe(StreamSubscriptionOptions options) =>
        SubscribeInternal(options.CreateStreamSubscriptionRequest(), options.BufferSize, options.SubscriptionTimeout, options.StoppingToken);

    // public async Task<Result<Subscription, StreamSubscriptionError>> Subscribe(
    //     SubscriptionOptions options,
    //     int bufferSize = 100
    // ) {
    //     // Create a linked cancellation token source for background task control
    //     var cancellator = CancellationTokenSource.CreateLinkedTokenSource(options.StoppingToken);
    //
    //     var stoppingToken = cancellator.Token;
    //
    //     var session = LegacyStreamsClient.Read(
    //         StreamsV1Converter.CreateSubscriptionRequest(options),
    //         cancellationToken: stoppingToken);
    //
    //     // Check for access denied. the legacy exception mapper throws this exception....
    //     try {
    //         await session.ResponseStream.MoveNext(stoppingToken).ConfigureAwait(false);
    //     }
    //     catch (AccessDeniedException) {
    //         return Result.Failure<Subscription, StreamSubscriptionError>(new ErrorDetails.AccessDenied());
    //     }
    //
    //     if (session.ResponseStream.Current.ContentCase == StreamNotFound)
    //         return Result.Failure<Subscription, StreamSubscriptionError>(new ErrorDetails.StreamNotFound());
    //
    //     if (session.ResponseStream.Current.ContentCase != Confirmation)
    //         throw KurrentClientException.CreateUnknown(
    //             nameof(SubscribeToStream), new Exception($"Expected confirmation message but got {session.ResponseStream.Current.ContentCase}"));
    //
    //     var subscriptionId = session.ResponseStream.Current.Confirmation.SubscriptionId;
    //
    //     // Create a bounded channel for backpressure control
    //     var channel = Channel.CreateBounded<SubscriptionMessage>(
    //         new BoundedChannelOptions(bufferSize) {
    //             FullMode     = BoundedChannelFullMode.Wait,
    //             SingleReader = true,
    //             SingleWriter = true
    //         }
    //     );
    //
    //     // Start a background task to pump remaining messages through the channel
    //     _ = Task.Run(
    //         async () => {
    //             try {
    //                 var messages = session.ResponseStream.ReadAllAsync(stoppingToken)
    //                     .Where(x => x.ContentCase is Event or Checkpoint or CaughtUp or FellBehind)
    //                     .SelectAwait<ReadResp, SubscriptionMessage>(async x => x.ContentCase switch {
    //                         Event      => await LegacyConverter.ConvertToRecord(x.Event, stoppingToken).ConfigureAwait(false),
    //                         Checkpoint => StreamsV1Converter.ConvertToHeartbeat(x.Checkpoint),
    //                         CaughtUp   => StreamsV1Converter.ConvertToHeartbeat(x.CaughtUp),
    //                         FellBehind => StreamsV1Converter.ConvertToHeartbeat(x.FellBehind),
    //                     });
    //
    //                 await foreach (var message in messages.ConfigureAwait(false))
    //                     await channel.Writer.WriteAsync(message, stoppingToken).ConfigureAwait(false);
    //
    //                 channel.Writer.Complete();
    //             } catch (OperationCanceledException) {
    //                 channel.Writer.Complete();
    //             } catch (Exception ex) {
    //                 channel.Writer.TryComplete(ex);
    //             } finally {
    //                 cancellator.Dispose();
    //                 session.Dispose();
    //             }
    //         }, stoppingToken
    //     );
    //
    //     return new Subscription(subscriptionId, channel.Reader.ReadAllAsync(stoppingToken));
    //
    // }
    //
    // public async Task<Result<Subscription, StreamSubscriptionError>> Subscribe(
    //     StreamName streamName,
    //     StreamSubscriptionOptions options,
    //     int bufferSize = 100
    // ) {
    //     // Create a linked cancellation token source for background task control
    //     var cancellator = CancellationTokenSource.CreateLinkedTokenSource(options.CancellationToken);
    //
    //     var stoppingToken = cancellator.Token;
    //
    //     var session = LegacyStreamsClient.Read(
    //         StreamsV1Converter.CreateStreamSubscriptionRequest(streamName, options),
    //         cancellationToken: stoppingToken);
    //
    //     // Check for access denied. the legacy exception mapper throws this exception....
    //     try {
    //         await session.ResponseStream.MoveNext(stoppingToken).ConfigureAwait(false);
    //     }
    //     catch (AccessDeniedException) {
    //         return Result.Failure<Subscription, StreamSubscriptionError>(new ErrorDetails.AccessDenied());
    //     }
    //
    //     if (session.ResponseStream.Current.ContentCase == StreamNotFound)
    //         return Result.Failure<Subscription, StreamSubscriptionError>(new ErrorDetails.StreamNotFound());
    //
    //     if (session.ResponseStream.Current.ContentCase != Confirmation)
    //         throw KurrentClientException.CreateUnknown(
    //             nameof(SubscribeToStream), new Exception($"Expected confirmation message but got {session.ResponseStream.Current.ContentCase}"));
    //
    //     var subscriptionId = session.ResponseStream.Current.Confirmation.SubscriptionId;
    //
    //     // Create a bounded channel for backpressure control
    //     var channel = Channel.CreateBounded<SubscriptionMessage>(
    //         new BoundedChannelOptions(bufferSize) {
    //             FullMode     = BoundedChannelFullMode.Wait,
    //             SingleReader = true,
    //             SingleWriter = true
    //         }
    //     );
    //
    //     // Start a background task to pump remaining messages through the channel
    //     _ = Task.Run(
    //         async () => {
    //             try {
    //                 var messages = session.ResponseStream.ReadAllAsync(stoppingToken)
    //                     .Where(x => x.ContentCase is Event or Checkpoint or CaughtUp or FellBehind)
    //                     .SelectAwait<ReadResp, SubscriptionMessage>(async x => x.ContentCase switch {
    //                         Event      => await LegacyConverter.ConvertToRecord(x.Event, stoppingToken).ConfigureAwait(false),
    //                         Checkpoint => StreamsV1Converter.ConvertToHeartbeat(x.Checkpoint),
    //                         CaughtUp   => StreamsV1Converter.ConvertToHeartbeat(x.CaughtUp),
    //                         FellBehind => StreamsV1Converter.ConvertToHeartbeat(x.FellBehind),
    //                     });
    //
    //                 await foreach (var message in messages.ConfigureAwait(false))
    //                     await channel.Writer.WriteAsync(message, stoppingToken).ConfigureAwait(false);
    //
    //                 channel.Writer.Complete();
    //             } catch (OperationCanceledException) {
    //                 channel.Writer.Complete();
    //             } catch (Exception ex) {
    //                 channel.Writer.TryComplete(ex);
    //             } finally {
    //                 cancellator.Dispose();
    //                 session.Dispose();
    //             }
    //         }, stoppingToken
    //     );
    //
    //     return new Subscription(subscriptionId, channel.Reader.ReadAllAsync(stoppingToken));
    // }
}

public partial class KurrentStreamsClient {
    async ValueTask<Result<Subscription, SubscriptionError>> SubscribeInternal(
        ReadReq request, int bufferSize, TimeSpan subscriptionTimeout, CancellationToken cancellationToken
    ) {
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
    Channel<SubscriptionMessage> StartMessageRelay(
        AsyncServerStreamingCall<ReadResp> session, int bufferSize, TimeSpan subscriptionTimeout, CancellationTokenSource cancellator
    ) {
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
                            Checkpoint => x.Checkpoint.ConvertCheckpointToHeartbeat(),
                            CaughtUp   => x.CaughtUp.ConvertCaughtUpToHeartbeat(),
                            FellBehind => x.FellBehind.ConvertFellBehindToHeartbeat()
                        });

                    await foreach (var message in messages.ConfigureAwait(false)) {
                        // try to write immediately without blocking
                        if (channel.Writer.TryWrite(message))
                            continue; // no timeout needed

                        // the channel is full, now we need timeout protection
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

    // async Task<Result<Subscription, StreamSubscriptionError>> SubscribeToStreamOriginalInternal(
    //     ReadReq request,
    //     int bufferSize,
    //     TimeSpan subscriptionTimeout,
    //     CancellationToken cancellationToken
    // ) {
    //     // Create a linked cancellation token source for background task control
    //     var cancellator = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    //
    //     var stoppingToken = cancellator.Token;
    //
    //     var session = LegacyStreamsClient.Read(request, cancellationToken: stoppingToken);
    //
    //     // Check for access denied. the legacy exception mapper throws this exception...
    //     // we could skip it for this operation... requires refactoring the interceptor
    //     // or we could execute this logic outside by doing another call.
    //     try {
    //         await session.ResponseStream.MoveNext(stoppingToken).ConfigureAwait(false);
    //     }
    //     catch (AccessDeniedException) {
    //         return Result.Failure<Subscription, StreamSubscriptionError>(new ErrorDetails.AccessDenied());
    //     }
    //
    //     if (session.ResponseStream.Current.ContentCase == StreamNotFound)
    //         return Result.Failure<Subscription, StreamSubscriptionError>(new ErrorDetails.StreamNotFound());
    //
    //     if (session.ResponseStream.Current.ContentCase != Confirmation)
    //         throw KurrentClientException.CreateUnknown(
    //             nameof(SubscribeToStream), new Exception($"Expected confirmation message but got {session.ResponseStream.Current.ContentCase}"));
    //
    //     var subscriptionId = session.ResponseStream.Current.Confirmation.SubscriptionId;
    //
    //     var channel = StartMessageRelay(session, bufferSize, subscriptionTimeout,  cancellator);
    //
    //     return new Subscription(subscriptionId, channel);
    // }
}

#region . subscription processor .
/// <summary>
/// Configuration options for processor-based subscriptions.
/// </summary>
[PublicAPI]
public record SubscriptionProcessorOptions {
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
public interface ISubscriptionProcessor {
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
public enum SubscriptionShutdownReason {
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

static class StreamsV1Converter {
    static readonly ReadReq.Types.Options.Types.SubscriptionOptions DefaultSubscriptionOptions = new();
    static readonly ReadReq.Types.Options.Types.UUIDOption          DefaultUuidOptions         = new();
    static readonly Empty                                           DefaultEmpty               = new();

    public static ReadReq CreateSubscriptionRequest(this SubscriptionOptions options) {
        return NewSubscriptionRequest()
            .With(x => x.Options.All = ConvertToAllOptions(options.Start))
            .With(x => x.Options.Filter = ConvertToFilterOptions(options.Filter, options.Heartbeat));

        // return new() {
        //     Options = new ReadReq.Types.Options {
        //         ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
        //         All           = ConvertToAllOptions(options.StartPosition),
        //         Subscription  = DefaultSubscriptionOptions,
        //         Filter        = GetFilterOptions(options.Filter, options.Heartbeat)!,
        //         UuidOption    = DefaultUuidOptions
        //     }
        // };
    }

    public static ReadReq CreateStreamSubscriptionRequest(this StreamSubscriptionOptions options) {
        return NewSubscriptionRequest()
            .With(x => x.Options.Stream = ConvertToStreamOptions(options.Stream, options.Start));

        // return NewSubscriptionRequest()
        //     .With(x => x.Options.Stream = ConvertToStreamOptions(options.Stream, options.Start))
        //     .With(x => x.Options.Filter = ConvertToFilterOptions(options.Filter, options.Heartbeat)); // not sure if it works and we will do it locally.


        // return new() {
        //     Options = new() {
        //         ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
        //         Stream        = ConvertToStreamOptions(stream, options.StartRevision),
        //         Subscription  = DefaultSubscriptionOptions,
        //         UuidOption    = DefaultUuidOptions,
        //         NoFilter      = DefaultEmpty
        //     }
        // };
    }

    static ReadReq.Types.Options.Types.FilterOptions? ConvertToFilterOptions(this ReadFilter filter, HeartbeatOptions heartbeat) {
        if (filter == ReadFilter.None)
            return null;

        var options = filter.Scope switch {
            ReadFilterScope.Stream => new ReadReq.Types.Options.Types.FilterOptions {
                StreamIdentifier = new() { Regex = filter.Expression }
            },
            ReadFilterScope.Record => new ReadReq.Types.Options.Types.FilterOptions {
                EventType = new() { Regex = filter.Expression }
            },
        };

        // what is this?!
        //options.Count = DefaultEmpty;
        // if (filter.MaxSearchWindow.HasValue)
        //     options.Max = (uint)heartbeatRecordsThreshold;
        // else
        //     options.Count = DefaultEmpty;

        options.Max                          = (uint)heartbeat.RecordsThreshold;
        options.CheckpointIntervalMultiplier = 1;

        return options;
    }

    static ReadReq NewSubscriptionRequest() =>
        new() {
            Options = new() {
                ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
                Subscription  = DefaultSubscriptionOptions,
                UuidOption    = DefaultUuidOptions,
                NoFilter      = DefaultEmpty
            }
        };

    static ReadReq.Types.Options.Types.AllOptions ConvertToAllOptions(LogPosition start) =>
        start switch {
            _ when start == LogPosition.Latest   => new() { End      = DefaultEmpty },
            _ when start == LogPosition.Earliest => new() { Start    = DefaultEmpty },
            _                                    => new() { Position = new() { CommitPosition = start, PreparePosition = start } }
        };

    static ReadReq.Types.Options.Types.StreamOptions ConvertToStreamOptions(string stream, StreamRevision start) =>
        start switch {
            _ when start == StreamRevision.Max => new() { StreamIdentifier = stream, End      = DefaultEmpty },
            _ when start == StreamRevision.Min => new() { StreamIdentifier = stream, Start    = DefaultEmpty },
            _                                  => new() { StreamIdentifier = stream, Revision = (ulong)start.Value }
        };

    public static Heartbeat ConvertCheckpointToHeartbeat(this ReadResp.Types.Checkpoint checkpoint) {
        var position  = LogPosition.From((long)checkpoint.CommitPosition);
        var timestamp = checkpoint.Timestamp.ToDateTimeOffset();
        return Heartbeat.CreateCheckpoint(position, timestamp);
    }

    public static Heartbeat ConvertCaughtUpToHeartbeat(this ReadResp.Types.CaughtUp caughtUp) {
        var position  = caughtUp.Position is not null ? LogPosition.From((long)caughtUp.Position.CommitPosition) : LogPosition.Unset;
        var revision  = caughtUp.HasStreamRevision ? StreamRevision.From(caughtUp.StreamRevision) : StreamRevision.Unset;
        var timestamp = caughtUp.Timestamp.ToDateTimeOffset();
        return Heartbeat.CreateCaughtUp(position, revision, timestamp);
    }

    public static Heartbeat ConvertFellBehindToHeartbeat(this ReadResp.Types.FellBehind fellBehind) {
        var position  = fellBehind.Position is not null ? LogPosition.From((long)fellBehind.Position.CommitPosition) : LogPosition.Unset;
        var revision  = fellBehind.HasStreamRevision ? StreamRevision.From(fellBehind.StreamRevision) : StreamRevision.Unset;
        var timestamp = fellBehind.Timestamp.ToDateTimeOffset();
        return Heartbeat.CreateFellBehind(position, revision, timestamp);
    }
}
