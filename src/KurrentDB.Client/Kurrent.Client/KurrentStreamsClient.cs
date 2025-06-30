#pragma warning disable CS8509

using System.Runtime.CompilerServices;
using System.Text.Json;
using EventStore.Client;
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
using ErrorDetails = Kurrent.Client.Model.ErrorDetails;
using JsonSerializer = Kurrent.Client.SchemaRegistry.Serialization.Json.JsonSerializer;
using StreamMetadata = Kurrent.Client.Model.StreamMetadata;
using StreamMetadataJsonConverter = KurrentDB.Client.StreamMetadataJsonConverter;

namespace Kurrent.Client;

public partial class KurrentStreamsClient {
    internal KurrentStreamsClient(CallInvoker callInvoker, KurrentClientOptions options) {
        Options  = options;

        options.Mapper.Map<StreamMetadata>("$metadata");

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
            Position  : options.Start.ConvertToLegacyPosition(),
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
            return new ReadError(new ErrorDetails.AccessDenied(x => x.With("reason", "Access denied while reading all streams.")));
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

    public async ValueTask<Result<Messages, ReadError>> ReadStream(ReadStreamOptions options) {
        options.EnsureValid();

        var session = LegacyClient.ReadStreamAsync(
            (Direction) options.Direction, options.Stream,
            options.Start.ConvertToLegacyStreamPosition(),
            options.Limit,
            cancellationToken: options.CancellationToken
        );

        try {
            if (await session.ReadState == ReadState.StreamNotFound)
                return new ReadError(new StreamNotFound(x => x.With("stream", options.Stream)));
        }
        catch (AccessDeniedException) {
            return new ReadError(new ErrorDetails.AccessDenied(x => x.With("stream", options.Stream)));
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

    public ValueTask<Result<Messages, ReadError>> ReadStream(StreamName stream, CancellationToken cancellationToken = default) =>
        ReadStream(new ReadStreamOptions {  Stream = stream, CancellationToken = cancellationToken});

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

    public async ValueTask<Result<LogPosition, DeleteStreamError>> Delete(StreamName stream, ExpectedStreamState expectedState, CancellationToken cancellationToken = default) {
        var request = StreamsV1Mapper.CreateDeleteRequest(stream, expectedState);

        try {
            var resp = await LegacyStreamsClient
                .DeleteAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return resp?.Position?.MapToLogPosition() ?? LogPosition.Unset;
        }
        catch (WrongExpectedVersionException ex) {
            var info = await GetStreamInfo(stream, cancellationToken).ConfigureAwait(false);
            return Result.Failure<LogPosition, DeleteStreamError>(
                info is { IsFailure: true, Error.IsStreamNotFound: true }
                    ? info.Error.AsStreamNotFound
                    : ex.AsStreamRevisionConflict());
        }
        catch (Exception ex) {
            return Result.Failure<LogPosition, DeleteStreamError>(ex switch {
                StreamNotFoundException => ex.AsStreamNotFoundError(),
                AccessDeniedException   => ex.AsAccessDeniedError(stream),
                _                       => throw KurrentClientException.CreateUnknown(nameof(Delete), ex)
            });
        }
    }

    public async ValueTask<Result<LogPosition, TombstoneError>> Tombstone(StreamName stream, ExpectedStreamState expectedState, CancellationToken cancellationToken = default) {
        var request = StreamsV1Mapper.CreateTombstoneRequest(stream, expectedState);

        try {
            var resp = await LegacyStreamsClient
                .TombstoneAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return resp?.Position?.MapToLogPosition() ?? LogPosition.Unset;
        }
        catch (WrongExpectedVersionException ex) {
            var info = await GetStreamInfo(stream, cancellationToken).ConfigureAwait(false);
            return Result.Failure<LogPosition, TombstoneError>(
                info is { IsFailure: true, Error.IsStreamNotFound: true }
                    ? info.Error.AsStreamNotFound
                    : ex.AsStreamRevisionConflict());
        }
        catch (Exception ex) {
            return Result.Failure<LogPosition, TombstoneError>(ex switch {
                StreamNotFoundException       => ex.AsStreamNotFoundError(),
                AccessDeniedException         => ex.AsAccessDeniedError(stream),
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
        var metaStream = SystemStreams.MetastreamOf(stream);

        // read the stream metadata from the last record of the metastream
        var metastreamReadResult = await this.ReadLastStreamRecord(metaStream, cancellationToken)
            .MatchAsync(
                rec => new StreamInfo {
                    Metadata         = (StreamMetadata)rec.Value,
                    MetadataRevision = rec.StreamRevision
                },
                err => err.IsAccessDenied
                    ? Result.Failure<StreamInfo, GetStreamInfoError>(err.AsAccessDenied)
                    : new StreamInfo()
            )
            .ConfigureAwait(false);

        // access denied
        if (metastreamReadResult.IsFailure)
            return metastreamReadResult;

        // check if the stream is actually deleted and if not,
        // sets the latest position and revision
        return await this.ReadLastStreamRecord(stream, cancellationToken)
            .MatchAsync(
                rec => metastreamReadResult.Value with {
                    LastStreamPosition = rec.Position,
                    LastStreamRevision = rec.StreamRevision
                },
                err => err.IsStreamDeleted
                    ? metastreamReadResult.Value with { IsDeleted = true }
                    : Result.Failure<StreamInfo, GetStreamInfoError>(err.Value switch {
                        StreamNotFound notFound          => notFound,
                        ErrorDetails.AccessDenied denied => denied
                    })
            )
            .ConfigureAwait(false);
    }

    public async ValueTask<Result<StreamMetadata, GetStreamInfoError>> GetStreamMetadata(StreamName stream, CancellationToken cancellationToken = default) {
        var metaStream = SystemStreams.MetastreamOf(stream);

        var result = await this.ReadLastStreamRecord(metaStream, cancellationToken)
            .MatchAsync(
                rec => rec == Record.None ? StreamMetadata.None : (StreamMetadata)rec.Value,
                err => Result.Failure<StreamMetadata, GetStreamInfoError>(err.Value switch {
                    StreamNotFound notFound          => notFound,
                    ErrorDetails.AccessDenied denied => denied
                })
            )
            .ConfigureAwait(false);

        return result;
    }

	public ValueTask<Result<StreamRevision, SetStreamMetadataError>> SetStreamMetadata(StreamName stream, StreamMetadata metadata, ExpectedStreamState expectedState, CancellationToken cancellationToken = default) {

        var metaStream = SystemStreams.MetastreamOf(stream);

        // var data = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(metadata, StreamMetadataJsonSerializerOptions);

        return this.Append(metaStream, expectedState, Message.New.WithValue(metadata).Build(), cancellationToken)
            .MatchAsync(
                ok => ok.StreamRevision,
                ko => Result.Failure<StreamRevision, SetStreamMetadataError>(ko.Value switch {
                    StreamNotFound err             => err,
                    ErrorDetails.StreamDeleted err => err,
                    ErrorDetails.AccessDenied err  => err,
                    StreamRevisionConflict err     => err
                }));
        // .MatchAsync(
            //     ok => ok.StreamRevision,
            //     ko => Result.Failure<StreamRevision, SetStreamMetadataError>(ko.Case switch {
            //         AppendStreamFailure.AppendStreamFailureCase.StreamNotFound         => ko.AsStreamNotFound,
            //         AppendStreamFailure.AppendStreamFailureCase.StreamDeleted          => ko.AsStreamDeleted,
            //         AppendStreamFailure.AppendStreamFailureCase.AccessDenied           => ko.AsAccessDenied,
            //         AppendStreamFailure.AppendStreamFailureCase.StreamRevisionConflict => ko.AsStreamRevisionConflict
            //     }));
    }

    #endregion
}
