#pragma warning disable CS8509

using System.Runtime.CompilerServices;
using EventStore.Client.Streams;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry;
using Kurrent.Client.SchemaRegistry.Serialization;
using Kurrent.Client.SchemaRegistry.Serialization.Bytes;
using Kurrent.Client.SchemaRegistry.Serialization.Json;
using Kurrent.Client.SchemaRegistry.Serialization.Protobuf;
using KurrentDB.Client;
using static Kurrent.Client.Model.ErrorDetails;
using static KurrentDB.Protocol.Streams.V2.MultiStreamAppendResponse;
using static KurrentDB.Protocol.Streams.V2.StreamsService;
using Contracts = KurrentDB.Protocol.Streams.V2;
using StreamMetadata = Kurrent.Client.Model.StreamMetadata;

namespace Kurrent.Client;

public partial class KurrentStreamsClient {
    internal KurrentStreamsClient(CallInvoker callInvoker, KurrentClientOptions options) {
        Options  = options;

        ServiceClient = new StreamsServiceClient(callInvoker);
        Registry      = new KurrentRegistryClient(callInvoker);

        var schemaExporter = new SchemaExporter();
        var schemaManager  = new SchemaManager(Registry, schemaExporter, options.Mapper);

        SerializerProvider = new SchemaSerializerProvider([
            new BytesPassthroughSerializer(),
            new JsonSchemaSerializer(
                new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
                schemaManager
            ),
            new ProtobufSchemaSerializer(
                new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
                schemaManager
            )
        ]);

        LegacyStreamsClient = new Streams.StreamsClient(callInvoker);
        LegacySettings      = options.ConvertToLegacySettings();
        LegacyClient        = new KurrentDBClient(LegacySettings);

        LegacyConverter = new KurrentDBLegacyConverter(
            SerializerProvider,
            options.MetadataDecoder,
            SchemaRegistryPolicy.NoRequirements
        );
    }

    internal KurrentClientOptions      Options            { get; }
    internal StreamsServiceClient      ServiceClient      { get; }
    internal KurrentRegistryClient     Registry           { get; }
    internal ISchemaSerializerProvider SerializerProvider { get; }

    internal KurrentDBClientSettings  LegacySettings      { get; }
    internal KurrentDBClient          LegacyClient        { get; }
    internal KurrentDBLegacyConverter LegacyConverter     { get; }
    internal Streams.StreamsClient    LegacyStreamsClient { get; }

    #region . Append .

    public async ValueTask<Result<AppendStreamSuccesses, AppendStreamFailures>> Append(IAsyncEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default) {
        try {
            using var session = ServiceClient.MultiStreamAppendSession(cancellationToken: cancellationToken);

            await foreach (var request in requests.WithCancellation(cancellationToken)) {
                var records = await request.Messages
                    .Map(request.Stream, SerializerProvider, cancellationToken)
                    .ToArrayAsync(cancellationToken)
                    .ConfigureAwait(false);

                var serviceRequest = new Contracts.AppendStreamRequest {
                    Stream           = request.Stream,
                    ExpectedRevision = request.ExpectedState,
                    Records          = { records }
                };

                // Cancellation of stream writes is not supported by this gRPC implementation.
                // To cancel the operation, we should cancel the entire session.
                await session.RequestStream
                    .WriteAsync(serviceRequest, CancellationToken.None)
                    .ConfigureAwait(false);
            }

            await session.RequestStream.CompleteAsync();

            var response = await session.ResponseAsync;

            return response.ResultCase switch {
                ResultOneofCase.Success => response.Success.Map(),
                ResultOneofCase.Failure => response.Failure.Map(),
            };
        }
        catch (Exception ex) {
            throw KurrentClientException.CreateUnknown(nameof(Append), ex);
        }
    }

    #endregion

    #region . Read .

    // internal async IAsyncEnumerable<ReadMessage> NewRead(
    //     LogPosition startPosition,
    //     long limit,
    //     ReadFilter? filter = null,
    //     ReadDirection direction = ReadDirection.Forwards,
    //     HeartbeatOptions? heartbeatOptions = null,
    //     [EnumeratorCancellation] CancellationToken cancellationToken = default
    // ) {
    //     var channel = Channel.CreateUnbounded<ReadMessage>(new() {
    //         SingleReader = true,
    //         SingleWriter = true
    //     });
    //
    //     var request = new Contracts.ReadRequest {
    //         Filter        = filter?.Map(),
    //         StartPosition = startPosition,
    //         Limit         = limit,
    //         Direction     = direction.Map(),
    //         Heartbeats    = heartbeatOptions?.Map()
    //     };
    //
    //     // Start a task to read from gRPC and write to the channel
    //     using var readTask = Task.Run(
    //         async () => {
    //             try {
    //                 using var session = ServiceClient.ReadSession(request, cancellationToken: cancellationToken);
    //
    //                 while (await session.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
    //                     var response = session.ResponseStream.Current;
    //
    //                     await (response.ResultCase switch {
    //                         Contracts.ReadResponse.ResultOneofCase.Success   => HandleSuccess(response),
    //                         Contracts.ReadResponse.ResultOneofCase.Heartbeat => HandleHeartbeat(response),
    //                         Contracts.ReadResponse.ResultOneofCase.Failure   => HandleFailure(response),
    //                         _                                                => throw KurrentClientException.CreateUnknown(
    //                             nameof(Read), new UnreachableException($"Unreachable error while reading stream: {response.ResultCase}"))
    //                     });
    //                 }
    //
    //                 channel.Writer.TryComplete();
    //             }
    //             catch (Exception ex) {
    //                 channel.Writer.Complete(new Exception($"Error while reading stream: {ex.Message}", ex));
    //             }
    //         },
    //         cancellationToken
    //     );
    //
    //     await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
    //         yield return result;
    //
    //     yield break;
    //
    //     async ValueTask HandleSuccess(Contracts.ReadResponse response) {
    //         foreach (var record in response.Success.Records) {
    //             var mappedRecord = await record.Map(SerializerProvider, cancellationToken).ConfigureAwait(false);
    //             await channel.Writer.WriteAsync(mappedRecord, cancellationToken).ConfigureAwait(false);
    //         }
    //     }
    //
    //     ValueTask HandleHeartbeat(Contracts.ReadResponse response) =>
    //          channel.Writer.WriteAsync(response.Heartbeat.Map(), cancellationToken);
    //
    //     ValueTask HandleFailure(Contracts.ReadResponse response) {
    //         IResultError err = response.Failure.ErrorCase switch {
    //             Contracts.ReadFailure.ErrorOneofCase.AccessDenied   => new AccessDenied(),
    //             Contracts.ReadFailure.ErrorOneofCase.StreamDeleted  => new StreamDeleted(x => x.With("stream",response.Failure.StreamDeleted.Stream)),
    //             Contracts.ReadFailure.ErrorOneofCase.StreamNotFound => new StreamNotFound(x => x.With("stream",response.Failure.StreamDeleted.Stream)),
    //         };
    //
    //         channel.Writer.TryComplete(new Exception($"Read operation failed: {err}"));
    //
    //         return ValueTask.CompletedTask;
    //     }
    // }

    // public IAsyncEnumerable<ReadMessage> Read(
    //     LogPosition startPosition, long limit,
    //     ReadFilter filter, Direction direction,
    //     HeartbeatOptions heartbeatOptions,
    //     CancellationToken cancellationToken = default
    // ) {
    //     var session = filter.IsStreamNameFilter
    //         ? ReadStream(
    //             filter.Expression, startPosition,
    //             limit, direction, cancellationToken
    //         )
    //         : ReadAll(
    //             startPosition, limit, direction, filter,
    //             heartbeatOptions, cancellationToken
    //         );
    //
    //     return session;
    // }

    // public async IAsyncEnumerable<ReadMessage> ReadAll(
    //     LogPosition startPosition, long limit, ReadDirection direction,
    //     ReadFilter filter, HeartbeatOptions heartbeatOptions,
    //     [EnumeratorCancellation] CancellationToken cancellationToken = default
    // ) {
    //     var legacyPosition = startPosition.ConvertToLegacyPosition();
    //     var eventFilter    = filter.ConvertToEventFilter(heartbeatOptions.RecordsThreshold);
    //
    //     var session = LegacyClient.ReadAllAsync(
    //         direction, legacyPosition, eventFilter, limit,
    //         cancellationToken: cancellationToken
    //     );
    //
    //     // what about checkpoints (aka heartbeats), only with new protocol?
    //     await foreach (var re in session.ConfigureAwait(false)) {
    //         var record = await LegacyConverter
    //             .ConvertToRecord(re, cancellationToken)
    //             .ConfigureAwait(false);
    //
    //         yield return record;
    //     }
    // }

    public async ValueTask<Result<Messages, ReadError>> ReadAll(ReadAllOptions options) {
        options.EnsureValid();

        var legacyOptions = (
            Direction : (Direction)options.Direction,
            Position  : options.StartPosition.ConvertToLegacyPosition(),
            Filter    : options.Filter.ConvertToEventFilter(options.Heartbeat.RecordsThreshold),
            MaxCount  : options.Limit
        );

        // so to check AccessDenied, I must call the api. However, once I call it, it will throw an exception,
        // and if it doesn't, I must now do another call because I cannot continue reading... cause InvalidOperationException("Messages may only be enumerated once."))
        // need to get read v2 immediately
        try {
            await LegacyClient
                .ReadAllAsync(Direction.Forwards, Position.Start, 1, cancellationToken: options.CancellationToken).Messages
                .AnyAsync().ConfigureAwait(false);
        } catch (AccessDeniedException) {
            return new ReadError(new AccessDenied(x => x.With("reason", "Access denied while reading all streams.")));
        }
        catch (Exception ex) when (ex is not KurrentClientException) {
            throw KurrentClientException.CreateUnknown(nameof(ReadAll), ex);
        }

        var messages =  LegacyClient.ReadAllAsync(
            legacyOptions.Direction,
            legacyOptions.Position,
            legacyOptions.Filter,
            legacyOptions.MaxCount,
            cancellationToken: options.CancellationToken
        ).Messages;

        var source = messages.SelectAwait<StreamMessage, ReadMessage>(async se => se switch {
            StreamMessage.Event message         => await LegacyConverter.ConvertToRecord(message.ResolvedEvent, options.CancellationToken).ConfigureAwait(false),
            StreamMessage.CaughtUp caughtUp     => Heartbeat.CreateCaughtUp(caughtUp.Position.ConvertToLogPosition(), caughtUp.Timestamp),
            StreamMessage.FellBehind fellBehind => Heartbeat.CreateFellBehind(fellBehind.Position.ConvertToLogPosition(), fellBehind.Timestamp),
        });

        return new Messages(source);
    }

    public async ValueTask<Result<Messages, ReadError>> ReadStream(StreamName stream, ReadStreamOptions options) {
        options.EnsureValid();

        var session = LegacyClient.ReadStreamAsync(
            (Direction) options.Direction, stream,
            options.StartRevision.ConvertToLegacyStreamPosition(),
            options.Limit,
            cancellationToken: options.CancellationToken
        );

        try {
            if (await session.ReadState == ReadState.StreamNotFound)
                return new ReadError(new StreamNotFound(x => x.With("stream", stream)));
        }
        catch (AccessDeniedException) {
            return new ReadError(new AccessDenied(x => x.With("stream", stream)));
        }
        catch (Exception ex) when (ex is not KurrentClientException) {
            throw KurrentClientException.CreateUnknown(nameof(ReadStream), ex);
        }

        var source = session.Messages
            .Where(se => se is StreamMessage.Event or StreamMessage.CaughtUp)
            .SelectAwait<StreamMessage, ReadMessage>(async se => se switch {
                StreamMessage.Event message     => await LegacyConverter.ConvertToRecord(message.ResolvedEvent, options.CancellationToken).ConfigureAwait(false),
                StreamMessage.CaughtUp caughtUp => Heartbeat.CreateCaughtUp(caughtUp.Position.ConvertToLogPosition(), caughtUp.Timestamp)
            });

        return new Messages(source);
    }

    // public async ValueTask<Result<Record, ReadError>> ReadSingleRecord(LogPosition position, CancellationToken cancellationToken = default) {
    //     try {
    //         ResolvedEvent? re = await LegacyClient
    //             .ReadAllAsync(
    //                 Direction.Forwards, position.ConvertToLegacyPosition(), 1,
    //                 cancellationToken: cancellationToken
    //             )
    //             .FirstOrDefaultAsync(cancellationToken);
    //
    //         return re?.Event is not null
    //             ? await LegacyConverter
    //                 .ConvertToRecord(re.Value, cancellationToken)
    //                 .ConfigureAwait(false)
    //             : Record.None;
    //     }
    //     catch (AccessDeniedException ex) {
    //         return new ReadError(ex.AsAccessDeniedError());
    //     }
    // }

    internal async ValueTask<StreamRevision> GetStreamRevision(LogPosition position, CancellationToken cancellationToken = default) {
        if (position == LogPosition.Latest)
            return StreamRevision.Max;

        if (position == LogPosition.Unset || position == LogPosition.Earliest)
            return StreamRevision.Min;

        var re = await LegacyClient
            .ReadAllAsync(
                Direction.Forwards, position.ConvertToLegacyPosition(), 1,
                cancellationToken: cancellationToken
            )
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);

        return re.OriginalEventNumber.ConvertToStreamRevision();
    }

    #endregion

    #region . Subscribe .

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
    //     //         return new ReadError(new StreamNotFound(x => x.With("stream", stream)));
    //     // }
    //     // catch (AccessDeniedException) {
    //     //     return new ReadError(new AccessDenied(x => x.With("stream", stream)));
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

    #endregion

    #region . Delete / Truncate .

    /// <summary>
    /// Deletes a stream asynchronously.
    /// </summary>
    /// <param name="stream">The name of the stream to delete.</param>
    /// <param name="expectedState">The expected <see cref="ExpectedStreamState"/> of the stream to be deleted.</param>
    /// <param name="cancellationToken">
    /// The optional <see cref="CancellationToken"/> to observe while waiting for the operation to complete.
    /// </param>
    /// <returns></returns>
    public async ValueTask<Result<LogPosition, DeleteStreamError>> Delete(string stream, ExpectedStreamState expectedState, CancellationToken cancellationToken = default) {
        try {
            var result = await LegacyClient
                .DeleteAsync(stream, expectedState.MapToLegacyExpectedState(), cancellationToken: cancellationToken);

            return result.LogPosition.ConvertToLogPosition();
        }
        catch (Exception ex) {
           return MapToDeleteError(ex, stream);
        }

        // var temp = await LegacyClient.DeleteAsync(stream, expectedState.MapToLegacyExpectedState(), cancellationToken: cancellationToken)
        //     .ToResultAsync()
        //     .MatchAsync(
        //         ok => ok.LogPosition.ConvertToLogPosition(),
        //         ex => MapToDeleteError(ex, stream))
        //     .ConfigureAwait(false);
        //
        // return temp;

        static Result<LogPosition, DeleteStreamError> MapToDeleteError(Exception ex, string stream) {
            return Result.Failure<LogPosition, DeleteStreamError>(ex switch {
                StreamNotFoundException       => ex.AsStreamNotFoundError(),
                AccessDeniedException         => ex.AsAccessDeniedError(stream),
                WrongExpectedVersionException => ex.AsStreamRevisionConflict(),
                _                             => throw KurrentClientException.CreateUnknown(nameof(Delete), ex)
            });
        }
    }

    /// <summary>
    /// Tombstones a stream asynchronously.
    /// <remarks>
    /// Tombstoned streams can never be recreated.
    /// </remarks>
    /// </summary>
    /// <param name="stream">The name of the stream to Tombstone.</param>
    /// <param name="expectedState">The expected <see cref="ExpectedStreamState"/> of the stream to be Tombstoned.</param>
    /// <param name="cancellationToken">
    /// The optional <see cref="CancellationToken"/> to observe while waiting for the operation to complete.
    /// </param>
    /// <returns></returns>
    public ValueTask<Result<LogPosition, TombstoneError>> Tombstone(string stream, ExpectedStreamState expectedState, CancellationToken cancellationToken = default) {
        return LegacyClient.TombstoneAsync(stream, expectedState.MapToLegacyExpectedState(), cancellationToken: cancellationToken)
            .ToResultAsync().MatchAsync(
                ok => ok.LogPosition.ConvertToLogPosition(),
                ex => MapToError(ex, stream));

        static Result<LogPosition, TombstoneError> MapToError(Exception ex, string stream) {
            return Result.Failure<LogPosition, TombstoneError>(ex switch {
                StreamNotFoundException       => ex.AsStreamNotFoundError(),
                AccessDeniedException         => ex.AsAccessDeniedError(stream),
                WrongExpectedVersionException => ex.AsStreamRevisionConflict(),
                _                             => throw KurrentClientException.CreateUnknown(nameof(Tombstone), ex)
            });
        }
    }

    public ValueTask<Result<StreamRevision, TruncateStreamError>> Truncate(StreamName stream, StreamRevision truncateRevision, ExpectedStreamState expectedState, CancellationToken cancellationToken) {
        return SetStreamMetadata(stream, new() { TruncateBefore = truncateRevision }, expectedState, cancellationToken)
            .MapErrorAsync(err => err.Match<TruncateStreamError>(
                notFound => notFound,
                deleted  => deleted,
                denied   => denied,
                conflict => conflict
            ));
    }

    public ValueTask<Result<StreamRevision, TruncateStreamError>> Truncate(StreamName stream, StreamRevision truncateRevision, CancellationToken cancellationToken) =>
        GetStreamInfo(stream, cancellationToken)
            .MapErrorAsync(failure => failure.Match<TruncateStreamError>(notFound => notFound, denied => denied))
            .MatchAsync(
                 async streamInfo => await Truncate(stream, truncateRevision, streamInfo.MetadataRevision, cancellationToken),
                 failure => ValueTask.FromResult(Result.Failure<StreamRevision, TruncateStreamError>(failure))
            );

    #endregion

    #region . Stream Info & Metadata .

    public async ValueTask<Result<bool, GetStreamInfoError>> StreamExists(StreamName stream, CancellationToken cancellationToken = default) {
        try {
            using var readState = LegacyClient
                .ReadStreamAsync(Direction.Backwards, stream, StreamPosition.End, 1, cancellationToken: cancellationToken)
                .ReadState;

            return ReadState.Ok == await readState.ConfigureAwait(false) ;
        }
        catch (Exception ex) {
           throw KurrentClientException.CreateUnknown(nameof(StreamExists), ex);
        }
    }

    public async ValueTask<Result<StreamInfo, GetStreamInfoError>> GetStreamInfo(StreamName stream, CancellationToken cancellationToken = default) {

        throw new NotImplementedException("GetStreamInfo is not implemented yet. Use GetStreamMetadata instead.");

        // try {
        //     var result = await LegacyClient
        //         .GetStreamMetadataAsync(stream, cancellationToken: cancellationToken)
        //         .ConfigureAwait(false);
        //
        //     var readResult = await ReadLastStreamRecord(stream, cancellationToken)
        //         .MatchAsync(
        //             record => Result.Success<Record, GetStreamInfoError>(record),
        //             error => error.Match(
        //                 notfound => Result.Failure<Record, GetStreamInfoError>(new StreamDeleted(x => x.With("stream", stream))),
        //                 deleted  =>  Result.Success<Record, GetStreamInfoError>(Record.None),
        //                 denied   => Result.Failure<Record, GetStreamInfoError>(denied),
        //             )
        //         .ConfigureAwait(false);
        //
        //     if (readResult is { IsFailure: true, Error.IsStreamNotFound: false })
        //         return Result.Failure<StreamInfo, GetStreamInfoError>(readResult.Error.Value switch {
        //             StreamNotFound notFound => notFound,
        //             AccessDenied denied     => denied
        //         });
        //
        //     var record = readResult.IsSuccess
        //         ? readResult.Value
        //         : Record.None;
        //
        //     StreamMetadata metadata = new() {
        //         MaxAge         = result.Metadata.MaxAge,
        //         TruncateBefore = result.Metadata.TruncateBefore?.ConvertToStreamRevision(),
        //         CacheControl   = result.Metadata.CacheControl,
        //         MaxCount       = result.Metadata.MaxCount,
        //         CustomMetadata = result.Metadata.CustomMetadata
        //     };
        //
        //     StreamInfo info = new() {
        //         Metadata           = metadata,
        //         MetadataRevision   = result.MetastreamRevision?.ConvertToStreamRevision() ?? StreamRevision.Unset,
        //         IsDeleted          = result.StreamDeleted,
        //         LastStreamRevision = record.StreamRevision,
        //         LastStreamPosition = record.Position,
        //     };
        //
        //     return info;
        // }
        // catch (Exception ex) when (ex is not KurrentClientException) {
        //     return MapToError(ex, stream);
        // }
        //
        // static Result<StreamInfo, GetStreamInfoError> MapToError(Exception ex, StreamName stream) {
        //     return Result.Failure<StreamInfo, GetStreamInfoError>(ex switch {
        //         StreamNotFoundException notFound => notFound.AsStreamNotFoundError(),
        //         AccessDeniedException denied     => denied.AsAccessDeniedError(stream),
        //         _                                => throw KurrentClientException.CreateUnknown(nameof(GetStreamInfo), ex)
        //     });
        // }
    }

    public async ValueTask<Result<StreamMetadata, GetStreamInfoError>> GetStreamMetadata(StreamName stream, CancellationToken cancellationToken = default) {
        var metaStream = SystemStreams.MetastreamOf(stream);

        try {
            var result = await this.ReadLastStreamRecord(metaStream, cancellationToken)
                .MatchAsync(
                    rec => rec == Record.None ? StreamMetadata.None : (StreamMetadata)rec.Value,
                    err => Result.Failure<StreamMetadata, GetStreamInfoError>(err.Value switch {
                        StreamNotFound notFound => notFound,
                        AccessDenied denied     => denied
                    })
                )
                .ConfigureAwait(false);

            return result;

            // var result = await this.ReadLastStreamRecord(metaStream, cancellationToken);
            //
            // return result.Match(
            //     rec => rec == Record.None
            //         ? StreamMetadata.None
            //         : (StreamMetadata)rec.Value,
            //     err => Result.Failure<StreamMetadata, GetStreamInfoError>(err.Value switch {
            //         StreamNotFound notFound => notFound,
            //         AccessDenied denied     => denied
            //     })
            // );
        }
        catch (Exception ex) when (ex is not KurrentClientException) {
            throw KurrentClientException.CreateUnknown(nameof(GetStreamMetadata), ex);
        }

        //
        // try {
        //     var result = await LegacyClient
        //         .GetStreamMetadataAsync(stream, cancellationToken: cancellationToken)
        //         .ConfigureAwait(false);
        //
        //     StreamMetadata metadata = new() {
        //         MaxAge         = result.Metadata.MaxAge,
        //         TruncateBefore = result.Metadata.TruncateBefore?.ConvertToStreamRevision(),
        //         CacheControl   = result.Metadata.CacheControl,
        //         MaxCount       = result.Metadata.MaxCount,
        //         CustomMetadata = result.Metadata.CustomMetadata
        //     };
        //
        //     return metadata;
        // }
        // catch (Exception ex) {
        //     return MapToError(ex, stream);
        // }
        //
        // static Result<StreamMetadata, GetStreamInfoError> MapToError(Exception ex, StreamName stream) {
        //     return Result.Failure<StreamMetadata, GetStreamInfoError>(ex switch {
        //         StreamNotFoundException notFound => notFound.AsStreamNotFoundError(),
        //         AccessDeniedException denied     => denied.AsAccessDeniedError(stream),
        //         _                                => throw KurrentClientException.CreateUnknown(nameof(GetStreamMetadata), ex)
        //     });
        // }
    }


    public async ValueTask<Result<StreamRevision, SetStreamMetadataError>> SetStreamMetadata(StreamName stream, StreamMetadata metadata, ExpectedStreamState expectedState, CancellationToken cancellationToken = default) {
        try {
            var legacyMetadata = new KurrentDB.Client.StreamMetadata(
                maxAge:         metadata.MaxAge,
                truncateBefore: metadata.TruncateBefore?.ConvertToLegacyStreamPosition(),
                cacheControl:   metadata.CacheControl,
                maxCount:       metadata.MaxCount,
                customMetadata: metadata.CustomMetadata,
                acl: metadata.HasAcl ? new KurrentDB.Client.StreamAcl(
                    readRoles: metadata.Acl?.ReadRoles ?? [],
                    writeRoles: metadata.Acl?.WriteRoles ?? [],
                    deleteRoles: metadata.Acl?.DeleteRoles ?? [],
                    metaReadRoles: metadata.Acl?.MetaReadRoles ?? [],
                    metaWriteRoles: metadata.Acl?.MetaWriteRoles ?? []
                ) : null
            );

            var result = await LegacyClient
                .SetStreamMetadataAsync(
                    stream, expectedState.MapToLegacyExpectedState(),
                    legacyMetadata, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return StreamRevision.From(result.NextExpectedStreamState.ToInt64());
        }
        catch (Exception ex) {
            return MapToError(ex, stream);
        }

        static Result<StreamRevision, SetStreamMetadataError> MapToError(Exception ex, StreamName stream) {
            return Result.Failure<StreamRevision, SetStreamMetadataError>(ex switch {
                StreamNotFoundException dex       => dex.AsStreamNotFoundError(),
                StreamDeletedException dex        => dex.AsStreamDeletedError(),
                AccessDeniedException dex         => dex.AsAccessDeniedError(stream),
                WrongExpectedVersionException dex => dex.AsStreamRevisionConflict(),
                _                                 => throw KurrentClientException.CreateUnknown(nameof(SetStreamMetadata), ex)
            });
        }
    }

    #endregion
}
