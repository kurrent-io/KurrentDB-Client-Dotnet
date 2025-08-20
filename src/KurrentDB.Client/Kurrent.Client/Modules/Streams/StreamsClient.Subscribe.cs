#pragma warning disable CS8509

using System.Diagnostics;
using System.Threading.Channels;
using Grpc.Core;
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

        return new Subscription(subscriptionId, channelFactory);
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

    // public IAsyncEnumerable<SubscribeMessage> UnifiedSubscribe(
    //     LogPosition startPosition, ReadFilter filter, HeartbeatOptions heartbeatOptions,
    //     CancellationToken cancellationToken = default
    // ) {
    //     var session = filter.IsStreamNameFilter
    //         ? SubscribeToStream(
    //             filter.Expression, startPosition, filter,
    //             cancellationToken
    //         )
    //         : SubscribeToAll(
    //             startPosition, filter, heartbeatOptions,
    //             cancellationToken
    //         );
    //
    //     return session;
    // }
    //
    // public async IAsyncEnumerable<SubscribeMessage> SubscribeToAll(
    //     LogPosition startPosition, ReadFilter filter, HeartbeatOptions heartbeatOptions,
    //     [EnumeratorCancellation] CancellationToken cancellationToken = default
    // ) {
    //     var start       = startPosition.ConvertToLegacyFromAll();
    //     var eventFilter = filter.ConvertToEventFilter(heartbeatOptions.RecordsThreshold);
    //
    //     // wth?!?... is SubscriptionFilterOptions.CheckpointInterval != IEventFilter.MaxSearchWindow ?!?!?!
    //     var filterOptions = new SubscriptionFilterOptions(eventFilter, (uint)heartbeatOptions.RecordsThreshold);
    //
    //     await using var session = LegacyClient.SubscribeToAll(
    //         start,
    //         filterOptions: filterOptions,
    //         cancellationToken: cancellationToken
    //     );
    //
    //     await foreach (var msg in session.Messages.WithCancellation(cancellationToken).ConfigureAwait(false))
    //         switch (msg) {
    //             case StreamMessage.Event { ResolvedEvent: var re }:
    //                 var record = await LegacyConverter
    //                     .ConvertToRecord(re, cancellationToken)
    //                     .ConfigureAwait(false);
    //
    //                 yield return record;
    //
    //                 break;
    //
    //             case StreamMessage.AllStreamCheckpointReached checkpoint: {
    //                 var heartbeat = Heartbeat.CreateCheckpoint(
    //                     checkpoint.Position.ConvertToLogPosition(),
    //                     checkpoint.Timestamp
    //                 );
    //
    //                 yield return heartbeat;
    //
    //                 break;
    //             }
    //
    //             case StreamMessage.CaughtUp caughtUp: {
    //                 var heartbeat = Heartbeat.CreateCaughtUp(
    //                     caughtUp.Position.ConvertToLogPosition(),
    //                     caughtUp.Timestamp
    //                 );
    //
    //                 yield return heartbeat;
    //
    //                 break;
    //             }
    //             // new protocol, new model and this? this is just noise
    //             // case StreamMessage.FellBehind fellBehind:
    //             // case StreamMessage.LastAllStreamPosition lastAllStreamPosition:
    //             // case StreamMessage.SubscriptionConfirmation subscriptionConfirmation:
    //             // break;
    //         }
    // }

    // public async IAsyncEnumerable<SubscribeMessage> SubscribeToStream(
    //     string stream, LogPosition startPosition, ReadFilter filter,
    //     [EnumeratorCancellation] CancellationToken cancellationToken = default
    // ) {
    //     var revision = startPosition switch {
    //         _ when startPosition == LogPosition.Unset    => StreamRevision.Min,
    //         _ when startPosition == LogPosition.Earliest => StreamRevision.Min,
    //         _ when startPosition == LogPosition.Latest   => StreamRevision.Max,
    //         _                                            => await GetStreamRevision(startPosition, cancellationToken).ConfigureAwait(false)
    //     };
    //
    //     var session = SubscribeToStream(
    //         stream, revision, filter,
    //         cancellationToken
    //     );
    //
    //     await foreach (var record in session.ConfigureAwait(false))
    //         yield return record;
    // }

    // public async IAsyncEnumerable<SubscriptionMessage> SubscribeToStream(StreamName stream, StreamSubscriptionOptions subscriptionOptions) {
    //     subscriptionOptions.EnsureValid();
    //
    //     var legacyOptions = (
    //         Start  : subscriptionOptions.Start.ConvertToLegacyFromStream(),
    //         Filter    : subscriptionOptions.Filter.ConvertToEventFilter(subscriptionOptions.Heartbeat.RecordsThreshold)
    //     );
    //
    //     var session = LegacyClient.SubscribeToStream(
    //         stream,
    //         legacyOptions.Start,
    //         cancellationToken: subscriptionOptions.CancellationToken
    //     );
    //
    //     // session.SubscriptionId
    //     //
    //     //
    //     // try {
    //     //     if (await session..ReadState == ReadState.StreamNotFound)
    //     //         return new ReadError(new StreamNotFound(x => x.WithStreamName(stream)));
    //     // }
    //     // catch (AccessDeniedException) {
    //     //     return new ReadError(new AccessDenied(x => x.WithStreamName(stream)));
    //     // }
    //     // catch (Exception ex) when (ex is not KurrentClientException) {
    //     //     throw KurrentClientException.CreateUnknown(nameof(ReadStream), ex);
    //     // }
    //
    //     // var messages =  LegacyClient.ReadAllAsync(
    //     //     legacyOptions.Direction,
    //     //     legacyOptions.Position,
    //     //     legacyOptions.Filter,
    //     //     legacyOptions.MaxCount,
    //     //     cancellationToken: options.CancellationToken
    //     // ).Messages;
    //
    //     await foreach (var msg in session.Messages.WithCancellation(subscriptionOptions.CancellationToken).ConfigureAwait(false))
    //         switch (msg) {
    //             case StreamMessage.SubscriptionConfirmation subscriptionConfirmation:
    //                 var temp = subscriptionConfirmation.SubscriptionId;
    //
    //                 break;
    //
    //             case StreamMessage.Event { ResolvedEvent: var re }:
    //                 if (!subscriptionOptions.Filter.IsRecordFilter || !subscriptionOptions.Filter.IsMatch(re.OriginalEvent.EventType))
    //                     continue;
    //
    //                 var record = await LegacyConverter
    //                     .ConvertToRecord(re, subscriptionOptions.CancellationToken)
    //                     .ConfigureAwait(false);
    //
    //                 yield return record;
    //
    //                 break;
    //
    //             case StreamMessage.AllStreamCheckpointReached checkpoint: {
    //                 var heartbeat = Heartbeat.CreateCheckpoint(
    //                     checkpoint.Position.ConvertToLogPosition(),
    //                     checkpoint.Timestamp
    //                 );
    //
    //                 yield return heartbeat;
    //
    //                 break;
    //             }
    //
    //             case StreamMessage.CaughtUp caughtUp: {
    //                 var heartbeat = Heartbeat.CreateCaughtUp(
    //                     caughtUp.Position.ConvertToLogPosition(),
    //                     caughtUp.Timestamp
    //                 );
    //
    //                 yield return heartbeat;
    //
    //                 break;
    //             }
    //
    //             case StreamMessage.FellBehind fellBehind: {
    //                 var heartbeat = Heartbeat.CreateFellBehind(
    //                     fellBehind.Position.ConvertToLogPosition(),
    //                     fellBehind.Timestamp
    //                 );
    //
    //                 yield return heartbeat;
    //
    //                 break;
    //             }
    //
    //             case StreamMessage.NotFound:
    //                 throw new StreamNotFoundException(stream);
    //             // new protocol, new model and this? thi is just noise
    //             // case StreamMessage.FellBehind fellBehind:
    //             // case StreamMessage.LastAllStreamPosition lastAllStreamPosition:
    //             // case StreamMessage.SubscriptionConfirmation subscriptionConfirmation:
    //             // break;
    //         }
    // }

    // public async IAsyncEnumerable<SubscriptionMessage> SubscribeToStream(
    //     string stream, StreamRevision startRevision, ReadFilter filter,
    //     [EnumeratorCancellation] CancellationToken cancellationToken = default
    // ) {
    //     await using var session = LegacyClient.SubscribeToStream(
    //         stream,
    //         startRevision.ConvertToLegacyFromStream(),
    //         cancellationToken: cancellationToken
    //     );
    //
    //     await foreach (var msg in session.Messages.WithCancellation(cancellationToken).ConfigureAwait(false))
    //         switch (msg) {
    //             case StreamMessage.Event { ResolvedEvent: var re }:
    //                 var record = await LegacyConverter
    //                     .ConvertToRecord(re, cancellationToken)
    //                     .ConfigureAwait(false);
    //
    //                 yield return record;
    //
    //                 // FILTER ALERT!
    //                 // for now we could apply the filter locally until we refactor the server operation.
    //                 // if (filter.IsEmptyFilter)
    //                 // yield return record;
    //                 // else {
    //                 // switch (filter.Scope) {
    //                 // case ReadFilterScope.Stream:
    //                 // if (filter.IsMatch(record.Stream))
    //                 // yield return record;
    //                 // break;
    //                 //
    //                 // case ReadFilterScope.SchemaName:
    //                 // if (filter.IsMatch(record.Schema.SchemaName))
    //                 // yield return record;
    //                 // break;
    //                 //
    //                 // // case ReadFilterScope.Properties:
    //                 // // if (filter.IsMatch(record.Metadata))
    //                 // // yield return record;
    //                 // // break;
    //                 //
    //                 // // case ReadFilterScope.Record:
    //                 // // if (filter.IsMatch(record.Schema.SchemaName))
    //                 // // yield return record;
    //                 // // break;
    //                 //
    //                 // // default:
    //                 // // // if no scope is specified, we assume the filter applies to both stream and record
    //                 // // if (filter.IsStreamNameFilter && filter.IsMatch(record.Stream) ||
    //                 // //     filter.IsRecordFilter && filter.IsMatch(record.Data.Span))
    //                 // // yield return record;
    //                 // // break;
    //                 //
    //                 // }
    //                 // }
    //                 break;
    //
    //             // its the same message as in SubscribeToAll, still need to test it...
    //             case StreamMessage.AllStreamCheckpointReached checkpoint: {
    //                 var heartbeat = Heartbeat.CreateCheckpoint(
    //                     checkpoint.Position.ConvertToLogPosition(),
    //                     checkpoint.Timestamp
    //                 );
    //
    //                 yield return heartbeat;
    //
    //                 break;
    //             }
    //
    //             case StreamMessage.CaughtUp caughtUp: {
    //                 var heartbeat = Heartbeat.CreateCaughtUp(
    //                     caughtUp.Position.ConvertToLogPosition(),
    //                     caughtUp.Timestamp
    //                 );
    //
    //                 yield return heartbeat;
    //
    //                 break;
    //             }
    //
    //             case StreamMessage.NotFound:
    //                 throw new StreamNotFoundException(stream);
    //             // new protocol, new model and this? thi is just noise
    //             // case StreamMessage.FellBehind fellBehind:
    //             // case StreamMessage.LastAllStreamPosition lastAllStreamPosition:
    //             // case StreamMessage.SubscriptionConfirmation subscriptionConfirmation:
    //             // break;
    //         }
    // }
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
