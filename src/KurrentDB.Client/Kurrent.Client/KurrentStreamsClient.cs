#pragma warning disable CS8509

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry;
using Kurrent.Client.SchemaRegistry.Serialization;
using Kurrent.Client.SchemaRegistry.Serialization.Bytes;
using Kurrent.Client.SchemaRegistry.Serialization.Json;
using Kurrent.Client.SchemaRegistry.Serialization.Protobuf;
using KurrentDB.Client;
using static KurrentDB.Protocol.Streams.V2.StreamsService;
using Contracts = KurrentDB.Protocol.Streams.V2;
using ErrorDetails = Kurrent.Client.Model.ErrorDetails;
using StreamMetadata = Kurrent.Client.Model.StreamMetadata;

namespace Kurrent.Client;

public partial class KurrentStreamsClient {
    internal KurrentStreamsClient(CallInvoker callInvoker, KurrentClientOptions options) {
        Options  = options;

        ServiceClient = new StreamsServiceClient(callInvoker);
        Registry      = new KurrentRegistryClient(callInvoker);

        var typeMapper     = new MessageTypeMapper();
        var schemaExporter = new SchemaExporter();
        var schemaManager  = new SchemaManager(Registry, schemaExporter, typeMapper);

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

        LegacySettings = options.ConvertToLegacySettings();
        LegacyClient = new KurrentDBClient(LegacySettings);

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

    internal KurrentDBClientSettings  LegacySettings  { get; }
    internal KurrentDBClient          LegacyClient    { get; }
    internal KurrentDBLegacyConverter LegacyConverter { get; }

    #region . Append .

    public async ValueTask<Result<AppendStreamSuccesses, AppendStreamFailures>> Append(IAsyncEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default) {
        try {
            using var session = ServiceClient.MultiStreamAppendSession(cancellationToken: cancellationToken);

            // var temp = await requests.ToListAsync();

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
                // If you need to cancel the operation, you should cancel the entire session.
                await session.RequestStream
                    .WriteAsync(serviceRequest, CancellationToken.None)
                    .ConfigureAwait(false);
            }

            await session.RequestStream.CompleteAsync();

            // var responseResult = await session.ResponseAsync
            //     .ToResultAsync()
            //     .ThrowOnErrorAsync(ex => new KurrentClientException(
            //         nameof(Append),
            //         $"Error while appending stream: {ex.Message}",
            //         ex
            //     ));

            // var result = await responseResult
            //         .ThenAsync( async response =>
            //             response.ResultCase switch {
            //                 Contracts.MultiStreamAppendResponse.ResultOneofCase.Success => response.Success.Map(),
            //                 Contracts.MultiStreamAppendResponse.ResultOneofCase.Failure => response.Failure.Map(),
            //                 // _                                                           => throw KurrentClientException.CreateUnknown(nameof(Append), new UnreachableException($"Unreachable error while appending stream: {response.ResultCase}"))
            //             });
            //
            //     .ThenAsync( async response =>
            //         response.ResultCase switch {
            //             Contracts.MultiStreamAppendResponse.ResultOneofCase.Success => Result<AppendStreamSuccesses, AppendStreamFailures>.Success(response.Success.Map()),
            //             Contracts.MultiStreamAppendResponse.ResultOneofCase.Failure => Result<AppendStreamSuccesses, AppendStreamFailures>.Error(response.Failure.Map()),
            //             _                                                           => throw KurrentClientException.CreateUnknown(nameof(Append), new UnreachableException($"Unreachable error while appending stream: {response.ResultCase}"))
            //         });
            //     // .MapErrorAsync(ex => throw ex);

            var response = await session.ResponseAsync;

            return response.ResultCase switch {
                Contracts.MultiStreamAppendResponse.ResultOneofCase.Success => Result.Success<AppendStreamSuccesses, AppendStreamFailures>(response.Success.Map()),
                Contracts.MultiStreamAppendResponse.ResultOneofCase.Failure => Result.Failure<AppendStreamSuccesses, AppendStreamFailures>(response.Failure.Map()),
                _                                                           => throw KurrentClientException.CreateUnknown(nameof(Append), new UnreachableException($"Unreachable error while appending stream: {response.ResultCase}"))
            };
        }
        catch (Exception ex) {
            throw ex;
        }
    }

    #endregion

    #region . Read .

    internal async IAsyncEnumerable<ReadMessage> Read(
        LogPosition startPosition,
        long limit,
        ReadFilter? filter = null,
        ReadDirection direction = ReadDirection.Forwards,
        HeartbeatOptions? heartbeatOptions = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        var channel = Channel.CreateUnbounded<ReadMessage>(
            new() {
                SingleReader = true,
                SingleWriter = true
            }
        );

        var request = new Contracts.ReadRequest {
            Filter        = filter?.Map(),
            StartPosition = startPosition,
            Limit         = limit,
            Direction     = direction.Map(),
            Heartbeats    = heartbeatOptions?.Map()
        };

        // Start a task to read from gRPC and write to the channel
        using var readTask = Task.Run(
            async () => {
                try {
                    using var session = ServiceClient.ReadSession(request, cancellationToken: cancellationToken);

                    while (await session.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                        var response = session.ResponseStream.Current;

                        await (response.ResultCase switch {
                            Contracts.ReadResponse.ResultOneofCase.Success   => HandleSuccess(response),
                            Contracts.ReadResponse.ResultOneofCase.Heartbeat => HandleHeartbeat(response),
                            Contracts.ReadResponse.ResultOneofCase.Failure   => HandleFailure(response),
                            _                                                => throw KurrentClientException.CreateUnknown(
                                nameof(Read), new UnreachableException($"Unreachable error while reading stream: {response.ResultCase}"))
                        });
                    }

                    channel.Writer.TryComplete();
                }
                catch (Exception ex) {
                    channel.Writer.Complete(new Exception($"Error while reading stream: {ex.Message}", ex));
                }
            },
            cancellationToken
        );

        await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            yield return result;

        yield break;

        async ValueTask HandleSuccess(Contracts.ReadResponse response) {
            foreach (var record in response.Success.Records) {
                var mappedRecord = await record.Map(SerializerProvider, cancellationToken).ConfigureAwait(false);
                await channel.Writer.WriteAsync(mappedRecord, cancellationToken).ConfigureAwait(false);
            }
        }

        async ValueTask HandleHeartbeat(Contracts.ReadResponse response) => await channel.Writer.WriteAsync(response.Heartbeat.Map(), cancellationToken).ConfigureAwait(false);

        ValueTask HandleFailure(Contracts.ReadResponse response) {
            Exception ex = response.Failure.ErrorCase switch {
                Contracts.ReadFailure.ErrorOneofCase.AccessDenied  => new AccessDeniedException(),
                Contracts.ReadFailure.ErrorOneofCase.StreamDeleted => new StreamDeletedException(response.Failure.StreamDeleted.Stream),
                _                                                  => throw KurrentClientException.CreateUnknown(
                    nameof(Read), new UnreachableException($"Unreachable error while reading stream: {response.Failure.ErrorCase}"))

            };

            channel.Writer.TryComplete(ex);

            return ValueTask.CompletedTask;
        }
    }

    public IAsyncEnumerable<ReadMessage> Read(
        LogPosition startPosition,
        long limit,
        ReadFilter filter,
        Direction direction,
        HeartbeatOptions heartbeatOptions,
        CancellationToken cancellationToken = default
    ) {
        var session = filter.IsStreamNameFilter
            ? ReadStream(
                filter.Expression,
                startPosition, limit, direction,
                cancellationToken
            )
            : ReadAll(
                startPosition, limit, direction,
                filter, heartbeatOptions,
                cancellationToken
            );

        return session;
    }

    public async IAsyncEnumerable<ReadMessage> ReadAll(
        LogPosition startPosition, long limit, Direction direction,
        ReadFilter filter, HeartbeatOptions heartbeatOptions,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        var legacyPosition = startPosition.ConvertToLegacyPosition();
        var eventFilter    = filter.ConvertToEventFilter(heartbeatOptions.RecordsThreshold);

        var session = LegacyClient.ReadAllAsync(
            direction, legacyPosition, eventFilter,
            limit,
            cancellationToken: cancellationToken
        );

        // what about checkpoints (aka heartbeats), only with new protocol?
        await foreach (var re in session.ConfigureAwait(false)) {
            var record = await LegacyConverter
                .ConvertToRecord(re, cancellationToken)
                .ConfigureAwait(false);

            yield return record;
        }
    }

    public async IAsyncEnumerable<ReadMessage> ReadStream(
        string stream, StreamRevision revision, long limit, Direction direction,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        // will throw if stream is not found or deleted
        // and ignores all other message types.
        var session = LegacyClient.ReadStreamAsync(
            direction, stream,
            revision.ConvertToLegacyStreamPosition(), limit,
            cancellationToken: cancellationToken
        );

        // what about checkpoints (aka heartbeats), only with new protocol?
        await foreach (var re in session.ConfigureAwait(false)) {
            var record = await LegacyConverter
                .ConvertToRecord(re, cancellationToken)
                .ConfigureAwait(false);

            yield return record;
        }
    }

    public async IAsyncEnumerable<ReadMessage> ReadStream(
        string stream, LogPosition startPosition, long limit, Direction direction,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        var revision = startPosition switch {
            _ when startPosition == LogPosition.Unset    => StreamRevision.Min,
            _ when startPosition == LogPosition.Earliest => StreamRevision.Min,
            _ when startPosition == LogPosition.Latest   => StreamRevision.Max,
            _                                            => await GetStreamRevision(startPosition, cancellationToken).ConfigureAwait(false)
        };

        var session = ReadStream(
            stream, revision, limit,
            direction, cancellationToken
        );

        await foreach (var record in session.ConfigureAwait(false))
            yield return record;
    }

    public async ValueTask<Record> ReadFirstStreamRecord(string stream, CancellationToken cancellationToken = default) {
        try {
            var result = LegacyClient.ReadStreamAsync(
                Direction.Forwards,
                stream,
                StreamPosition.Start,
                1,
                cancellationToken: cancellationToken
            );

            ResolvedEvent? re = await result
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return re?.Event is not null
                ? await LegacyConverter
                    .ConvertToRecord(re.Value, cancellationToken)
                    .ConfigureAwait(false)
                : Record.None;
        }
        catch (StreamNotFoundException) {
            return Record.None;
        }
        catch (StreamDeletedException) { // tombstoned
            return Record.None;
        }
    }

    public async ValueTask<Record> ReadLastStreamRecord(string stream, CancellationToken cancellationToken = default) {
        try {
            var result = LegacyClient.ReadStreamAsync(
                Direction.Backwards,
                stream,
                StreamPosition.End,
                1,
                cancellationToken: cancellationToken
            );

            ResolvedEvent? re = await result
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return re?.Event is not null
                ? await LegacyConverter
                    .ConvertToRecord(re.Value, cancellationToken)
                    .ConfigureAwait(false)
                : Record.None;
        }
        catch (StreamNotFoundException) {
            return Record.None;
        }
        catch (StreamDeletedException) { // tombstoned
            return Record.None;
        }
    }

    public async ValueTask<Record> ReadSingleRecord(LogPosition position, CancellationToken cancellationToken = default) {
        try {
            ResolvedEvent? re = await LegacyClient
                .ReadAllAsync(
                    Direction.Forwards, position.ConvertToLegacyPosition(), 1,
                    cancellationToken: cancellationToken
                )
                .FirstOrDefaultAsync(cancellationToken);

            return re?.Event is not null
                ? await LegacyConverter
                    .ConvertToRecord(re.Value, cancellationToken)
                    .ConfigureAwait(false)
                : Record.None;
        }
        catch (StreamNotFoundException) {
            return Record.None;
        }
        catch (StreamDeletedException) { // tombstoned
            return Record.None;
        }
    }

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

    public IAsyncEnumerable<SubscribeMessage> UnifiedSubscribe(
        LogPosition startPosition, ReadFilter filter, HeartbeatOptions heartbeatOptions,
        CancellationToken cancellationToken = default
    ) {
        var session = filter.IsStreamNameFilter
            ? SubscribeToStream(
                filter.Expression, startPosition, filter,
                cancellationToken
            )
            : SubscribeToAll(
                startPosition, filter, heartbeatOptions,
                cancellationToken
            );

        return session;
    }

    public async IAsyncEnumerable<SubscribeMessage> SubscribeToAll(
        LogPosition startPosition, ReadFilter filter, HeartbeatOptions heartbeatOptions,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        var start       = startPosition.ConvertToLegacyFromAll();
        var eventFilter = filter.ConvertToEventFilter(heartbeatOptions.RecordsThreshold);

// wth?!?... is SubscriptionFilterOptions.CheckpointInterval != IEventFilter.MaxSearchWindow ?!?!?!
        var filterOptions = new SubscriptionFilterOptions(eventFilter, (uint)heartbeatOptions.RecordsThreshold);

        await using var session = LegacyClient.SubscribeToAll(
            start,
            filterOptions: filterOptions,
            cancellationToken: cancellationToken
        );

        await foreach (var msg in session.Messages.WithCancellation(cancellationToken).ConfigureAwait(false))
            switch (msg) {
                case StreamMessage.Event { ResolvedEvent: var re }:
                    var record = await LegacyConverter
                        .ConvertToRecord(re, cancellationToken)
                        .ConfigureAwait(false);

                    yield return record;

                    break;

                case StreamMessage.AllStreamCheckpointReached checkpoint: {
                    var heartbeat = Heartbeat.CreateCheckpoint(
                        checkpoint.Position.ConvertToLogPosition(),
                        checkpoint.Timestamp
                    );

                    yield return heartbeat;

                    break;
                }

                case StreamMessage.CaughtUp caughtUp: {
                    var heartbeat = Heartbeat.CreateCaughtUp(
                        caughtUp.Position.ConvertToLogPosition(),
                        caughtUp.Timestamp
                    );

                    yield return heartbeat;

                    break;
                }
                // new protocol, new model and this? this is just noise
                // case StreamMessage.FellBehind fellBehind:
                // case StreamMessage.LastAllStreamPosition lastAllStreamPosition:
                // case StreamMessage.SubscriptionConfirmation subscriptionConfirmation:
                // break;
            }
    }

    public async IAsyncEnumerable<SubscribeMessage> SubscribeToStream(
        string stream, StreamRevision startRevision, ReadFilter filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        var start = startRevision.ConvertToLegacyFromStream();

        await using var session = LegacyClient.SubscribeToStream(
            stream,
            start,
            cancellationToken: cancellationToken
        );

        await foreach (var msg in session.Messages.WithCancellation(cancellationToken).ConfigureAwait(false))
            switch (msg) {
                case StreamMessage.Event { ResolvedEvent: var re }:
                    var record = await LegacyConverter
                        .ConvertToRecord(re, cancellationToken)
                        .ConfigureAwait(false);

                    yield return record;

                    // FILTER ALERT!
                    // for now we could apply the filter locally until we refactor the server operation.
                    // if (filter.IsEmptyFilter)
                    // yield return record;
                    // else {
                    // switch (filter.Scope) {
                    // case ReadFilterScope.Stream:
                    // if (filter.IsMatch(record.Stream))
                    // yield return record;
                    // break;
                    //
                    // case ReadFilterScope.SchemaName:
                    // if (filter.IsMatch(record.Schema.SchemaName))
                    // yield return record;
                    // break;
                    //
                    // // case ReadFilterScope.Properties:
                    // // if (filter.IsMatch(record.Metadata))
                    // // yield return record;
                    // // break;
                    //
                    // // case ReadFilterScope.Record:
                    // // if (filter.IsMatch(record.Schema.SchemaName))
                    // // yield return record;
                    // // break;
                    //
                    // // default:
                    // // // if no scope is specified, we assume the filter applies to both stream and record
                    // // if (filter.IsStreamNameFilter && filter.IsMatch(record.Stream) ||
                    // //     filter.IsRecordFilter && filter.IsMatch(record.Data.Span))
                    // // yield return record;
                    // // break;
                    //
                    // }
                    // }
                    break;

                // its the same message as in SubscribeToAll, still need to test it...
                case StreamMessage.AllStreamCheckpointReached checkpoint: {
                    var heartbeat = Heartbeat.CreateCheckpoint(
                        checkpoint.Position.ConvertToLogPosition(),
                        checkpoint.Timestamp
                    );

                    yield return heartbeat;

                    break;
                }

                case StreamMessage.CaughtUp caughtUp: {
                    var heartbeat = Heartbeat.CreateCaughtUp(
                        caughtUp.Position.ConvertToLogPosition(),
                        caughtUp.Timestamp
                    );

                    yield return heartbeat;

                    break;
                }

                case StreamMessage.NotFound:
                    throw new StreamNotFoundException(stream);
                // new protocol, new model and this? thi is just noise
                // case StreamMessage.FellBehind fellBehind:
                // case StreamMessage.LastAllStreamPosition lastAllStreamPosition:
                // case StreamMessage.SubscriptionConfirmation subscriptionConfirmation:
                // break;
            }
    }

    public async IAsyncEnumerable<SubscribeMessage> SubscribeToStream(
        string stream, LogPosition startPosition, ReadFilter filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        var revision = startPosition switch {
            _ when startPosition == LogPosition.Unset    => StreamRevision.Min,
            _ when startPosition == LogPosition.Earliest => StreamRevision.Min,
            _ when startPosition == LogPosition.Latest   => StreamRevision.Max,
            _                                            => await GetStreamRevision(startPosition, cancellationToken).ConfigureAwait(false)
        };

        var session = SubscribeToStream(
            stream, revision, filter,
            cancellationToken
        );

        await foreach (var record in session.ConfigureAwait(false))
            yield return record;
    }

    #endregion

    #region . Delete / Truncate .

    public ValueTask<Result<StreamRevision, TruncateStreamError>> Truncate(StreamName stream, StreamRevision truncateRevision, ExpectedStreamState expectedState, CancellationToken cancellationToken) {
        return SetStreamMetadata(stream, new() { TruncateBefore = truncateRevision }, expectedState, cancellationToken)
            .MapErrorAsync(err =>
                err.Match<TruncateStreamError>(
                    notFound => notFound,
                    deleted => deleted,
                    denied => denied,
                    conflict => conflict
                )
            );

            // return result.Match(
            //     success => success,
            //     failure => Result.Failure<StreamRevision, TruncateStreamError>(
            //         failure.Value switch {
            //             ErrorDetails.StreamNotFound         => failure.AsStreamNotFound,
            //             ErrorDetails.StreamDeleted          => failure.AsStreamDeleted,
            //             ErrorDetails.AccessDenied           => failure.AsAccessDenied,
            //             ErrorDetails.StreamRevisionConflict => failure.AsStreamRevisionConflict
            //         }
            //     )
            // );

            // return result.Match<Result<StreamRevision, TruncateStreamError>>(
            //     success => success,
            //     failure => failure.Match<TruncateStreamError>(
            //         notFound => notFound,
            //         deleted => deleted,
            //         accessDenied => accessDenied,
            //         revisionConflict => revisionConflict
            //     )
            // );

            // return result.Match<Result<StreamRevision, TruncateStreamError>>(
            //     ok => ok,
            //     ko => ko.Match<TruncateStreamError>(
            //         StreamNotFound => StreamNotFound,
            //         StreamDeleted => StreamDeleted,
            //         AccessDenied => AccessDenied,
            //         StreamRevisionConflict => StreamRevisionConflict
            //     )
            // );

            // return result.MapError(err =>
            //     err.Match<TruncateStreamError>(
            //         notFound => notFound,
            //         deleted => deleted,
            //         denied => denied,
            //         conflict => conflict
            //     )
            // );
    }

    public ValueTask<Result<StreamRevision, TruncateStreamError>> Truncate(StreamName stream, StreamRevision truncateRevision, CancellationToken cancellationToken) =>
        GetStreamInfo(stream, cancellationToken).MatchAsync(
            success => Truncate(stream, truncateRevision, success.MetadataRevision, cancellationToken),
            failure => failure.Match<TruncateStreamError>(notFound => notFound, denied => denied));

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
        var legacyExpectedState = expectedState switch {
            _ when expectedState == ExpectedStreamState.Any          => StreamState.Any,
            _ when expectedState == ExpectedStreamState.NoStream     => StreamState.NoStream,
            _ when expectedState == ExpectedStreamState.StreamExists => StreamState.StreamExists,
            _                                                        => StreamState.StreamRevision(expectedState)
        };

        try {
            var result = await LegacyClient
                .DeleteAsync(stream, legacyExpectedState, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Result.Success<LogPosition, DeleteStreamError>(result.LogPosition.ConvertToLogPosition());
        }
        catch (Exception ex) {
            return Result.Failure<LogPosition, DeleteStreamError>(ex switch {
                StreamNotFoundException       => new ErrorDetails.StreamNotFound(stream),
                AccessDeniedException         => new ErrorDetails.AccessDenied(stream),
                WrongExpectedVersionException => new ErrorDetails.StreamRevisionConflict(stream, expectedState),
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
    public async ValueTask<Result<LogPosition, TombstoneStreamError>> Tombstone(string stream, ExpectedStreamState expectedState, CancellationToken cancellationToken = default) {
        var legacyExpectedState = expectedState switch {
            _ when expectedState == ExpectedStreamState.Any          => StreamState.Any,
            _ when expectedState == ExpectedStreamState.NoStream     => StreamState.NoStream,
            _ when expectedState == ExpectedStreamState.StreamExists => StreamState.StreamExists,
            _                                                        => StreamState.StreamRevision(expectedState)
        };

        try {
            var result = await LegacyClient
                .TombstoneAsync(stream, legacyExpectedState, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Result.Success<LogPosition, TombstoneStreamError>(result.LogPosition.ConvertToLogPosition());
        }
        catch (Exception ex) {
            return Result.Failure<LogPosition, TombstoneStreamError>(ex switch {
                StreamNotFoundException       => new ErrorDetails.StreamNotFound(stream),
                AccessDeniedException         => new ErrorDetails.AccessDenied(stream),
                WrongExpectedVersionException => new ErrorDetails.StreamRevisionConflict(stream, expectedState),
                _                             => throw KurrentClientException.CreateUnknown(nameof(Tombstone), ex)
            });
        }
    }

    #endregion

    #region . Stream Info & Metadata .

    // public async Task<Result<bool, GetStreamInfoError>> StreamExists(StreamName stream, CancellationToken cancellationToken = default) {
    //     var read = LegacyClient.ReadStreamAsync(Direction.Backwards, stream, StreamPosition.End, 1, cancellationToken: cancellationToken);
    //     using var readState = read.ReadState;
    //     var state = await readState.ConfigureAwait(false);
    //     return state == ReadState.Ok;
    // }

    public async ValueTask<Result<StreamInfo, GetStreamInfoError>> GetStreamInfo(string stream, CancellationToken cancellationToken = default) {
        try {
            var result = await LegacyClient
                .GetStreamMetadataAsync(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var record = await ReadLastStreamRecord(stream, cancellationToken).ConfigureAwait(false);

            StreamMetadata metadata = new() {
                MaxAge           = result.Metadata.MaxAge,
                TruncateBefore   = result.Metadata.TruncateBefore?.ConvertToStreamRevision(),
                CacheControl     = result.Metadata.CacheControl,
                MaxCount         = result.Metadata.MaxCount,
                CustomMetadata   = result.Metadata.CustomMetadata
            };

            StreamInfo info = new() {
                Metadata           = metadata,
                MetadataRevision   = result.MetastreamRevision?.ConvertToStreamRevision() ?? StreamRevision.Unset,
                IsDeleted          = result.StreamDeleted,
                LastStreamRevision = record.StreamRevision,
                LastStreamPosition = record.Position,
            };

            return info;
        }
        catch (Exception ex) {
            return Result.Failure<StreamInfo, GetStreamInfoError>(ex switch {
                StreamNotFoundException => new ErrorDetails.StreamNotFound(stream),
                AccessDeniedException   => new ErrorDetails.AccessDenied(stream),
                _                       => throw KurrentClientException.CreateUnknown(nameof(GetStreamInfo), ex)
            });
        }
    }

    public async ValueTask<Result<StreamRevision, SetStreamMetadataError>> SetStreamMetadata(string stream, StreamMetadata metadata, ExpectedStreamState expectedState, CancellationToken cancellationToken = default) {
        var legacyExpectedState = expectedState switch {
            _ when expectedState == ExpectedStreamState.Any          => StreamState.Any,
            _ when expectedState == ExpectedStreamState.NoStream     => StreamState.NoStream,
            _ when expectedState == ExpectedStreamState.StreamExists => StreamState.StreamExists,
            _                                                        => StreamState.StreamRevision(expectedState)
        };

        var legacyMetadata = new KurrentDB.Client.StreamMetadata(
            maxAge:         metadata.MaxAge,
            truncateBefore: metadata.TruncateBefore?.ConvertToLegacyStreamPosition(),
            cacheControl:   metadata.CacheControl,
            maxCount:       metadata.MaxCount,
            customMetadata: metadata.CustomMetadata,
            acl:            null // check if this is still needed
        );

        try {
            var result = await LegacyClient
                .SetStreamMetadataAsync(
                    stream, legacyExpectedState, legacyMetadata,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);

            return StreamRevision.From(result.NextExpectedStreamState.ToInt64());
        }
        catch (Exception ex) {
            return Result.Failure<StreamRevision, SetStreamMetadataError>(ex switch {
                StreamNotFoundException       => new ErrorDetails.StreamNotFound(stream),
                StreamDeletedException        => new ErrorDetails.StreamDeleted(stream),
                AccessDeniedException         => new ErrorDetails.AccessDenied(stream),
                WrongExpectedVersionException => new ErrorDetails.StreamRevisionConflict(stream, expectedState),
                _                             => throw KurrentClientException.CreateUnknown(nameof(SetStreamMetadata), ex)
            });
        }
    }

    #endregion
}
