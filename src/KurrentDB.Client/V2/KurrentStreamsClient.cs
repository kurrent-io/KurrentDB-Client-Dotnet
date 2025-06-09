#pragma warning disable CS8509

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Grpc.Core;
using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry;
using Kurrent.Client.SchemaRegistry.Serialization;
using Kurrent.Client.SchemaRegistry.Serialization.Bytes;
using Kurrent.Client.SchemaRegistry.Serialization.Json;
using Kurrent.Client.SchemaRegistry.Serialization.Protobuf;
using KurrentDB.Client;
using KurrentDB.Client.Legacy;
using OneOf;
using OneOf.Types;
using static KurrentDB.Protocol.Streams.V2.StreamsService;
using Contracts = KurrentDB.Protocol.Streams.V2;
using ErrorDetails = Kurrent.Client.Model.ErrorDetails;

namespace Kurrent.Client;

public class KurrentStreamsClient {
	internal KurrentStreamsClient(CallInvoker callInvoker, KurrentDBClientSettings settings) {
		Settings = settings;

		ServiceClient = new StreamsServiceClient(callInvoker);
		Registry      = new KurrentRegistryClient(callInvoker);

		var typeMapper     = new MessageTypeMapper();
		var schemaExporter = new SchemaExporter();
		var schemaManager  = new SchemaManager(Registry, schemaExporter, typeMapper);

		SerializerProvider = new SchemaSerializerProvider([
			new BytesPassthroughSerializer(),
			new JsonSchemaSerializer(
				options: new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
				schemaManager: schemaManager
			),
			new ProtobufSchemaSerializer(
				options: new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
				schemaManager: schemaManager
			)
		]);

        LegacyClient        = new KurrentDBClient(settings);

		LegacyConverter = new KurrentDBLegacyConverter(
			SerializerProvider,
			new MetadataDecoder(),
			SchemaRegistryPolicy.NoRequirements
		);
	}

    internal KurrentStreamsClient(CallInvoker callInvoker, KurrentClientOptions options) {
        Settings = options.ConvertToLegacySettings();

        ServiceClient = new StreamsServiceClient(callInvoker);
        Registry      = new KurrentRegistryClient(callInvoker);

        var typeMapper     = new MessageTypeMapper();
        var schemaExporter = new SchemaExporter();
        var schemaManager  = new SchemaManager(Registry, schemaExporter, typeMapper);

        SerializerProvider = new SchemaSerializerProvider([
            new BytesPassthroughSerializer(),
            new JsonSchemaSerializer(
                options: new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
                schemaManager: schemaManager
            ),
            new ProtobufSchemaSerializer(
                options: new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
                schemaManager: schemaManager
            )
        ]);

        LegacyClient = new KurrentDBClient(Settings);

        LegacyConverter = new KurrentDBLegacyConverter(
            SerializerProvider,
            options.MetadataDecoder,
            SchemaRegistryPolicy.NoRequirements
        );
    }

    KurrentDBClientSettings   Settings           { get; }
    StreamsServiceClient      ServiceClient      { get; }
    KurrentRegistryClient     Registry           { get; }
    ISchemaSerializerProvider SerializerProvider { get; }

    KurrentDBClient          LegacyClient    { get; }
    KurrentDBLegacyConverter LegacyConverter { get; }

	#region . Append .

	public async ValueTask<MultiStreamAppendResult> Append(IAsyncEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default) {
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

			cancellationToken.ThrowIfCancellationRequested();

			await session.RequestStream
				.WriteAsync(serviceRequest, cancellationToken)
				.ConfigureAwait(false);
		}

		await session.RequestStream.CompleteAsync();

		var response = await session.ResponseAsync;

		return response.ResultCase switch {
			Contracts.MultiStreamAppendResponse.ResultOneofCase.Success => response.Success.Map(),
			Contracts.MultiStreamAppendResponse.ResultOneofCase.Failure => response.Failure.Map()
		};
	}

	public ValueTask<MultiStreamAppendResult> Append(IEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default) =>
		 Append(requests.ToAsyncEnumerable(), cancellationToken);

	public ValueTask<MultiStreamAppendResult> Append(MultiStreamAppendRequest request, CancellationToken cancellationToken = default) =>
		Append(request.Requests.ToAsyncEnumerable(), cancellationToken);

	/// <summary>
	/// Appends a series of messages to a specified stream in KurrentDB.
	/// </summary>
	/// <param name="request">The request object that specifies the stream, expected state, and messages to append.</param>
	/// <param name="cancellationToken">A token to cancel the operation if needed.</param>
	/// <returns>An <see cref="AppendStreamResult"/> representing the result of the append operation.</returns>
	public async ValueTask<AppendStreamResult> Append(AppendStreamRequest request, CancellationToken cancellationToken) {
		var result = await Append([request], cancellationToken).ConfigureAwait(false);

		return result.Match<AppendStreamResult>(
			success => success.First(),
			failure => failure.First()
		);
	}

	/// <summary>
	/// Appends a series of messages to a specified stream while specifying the expected stream state.
	/// </summary>
	/// <param name="stream">The name of the stream to which the messages will be appended.</param>
	/// <param name="expectedState">The expected state of the stream to ensure consistency during the append operation.</param>
	/// <param name="messages">A collection of messages to be appended to the stream.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the operation to complete, allowing for cancellation if needed.</param>
	/// <returns>An <see cref="AppendStreamResult"/> containing the outcome of the append operation, including success or failure details.</returns>
	public ValueTask<AppendStreamResult> Append(string stream, ExpectedStreamState expectedState, IEnumerable<Message> messages, CancellationToken cancellationToken) =>
		Append(new AppendStreamRequest(stream, expectedState, messages), cancellationToken);

    public ValueTask<AppendStreamResult> Append(string stream, ExpectedStreamState expectedState, Message message, CancellationToken cancellationToken) =>
        Append(new AppendStreamRequest(stream, expectedState, [message]), cancellationToken);

    public ValueTask<AppendStreamResult> Append<T>(string stream, ExpectedStreamState expectedState, T message, CancellationToken cancellationToken) =>
        Append(new AppendStreamRequest(stream, expectedState, [new Message{ Value = message ?? throw new ArgumentNullException(nameof(message))}]), cancellationToken);

    public ValueTask<AppendStreamResult> Append<T>(string stream, T message, CancellationToken cancellationToken) =>
        Append(new AppendStreamRequest(stream, ExpectedStreamState.Any, [new Message{ Value = message ?? throw new ArgumentNullException(nameof(message))}]), cancellationToken);


	#endregion

	#region . Read .

	internal async IAsyncEnumerable<ReadResult> Read(
		LogPosition startPosition,
		long limit,
		ReadFilter? filter = null,
		ReadDirection direction = ReadDirection.Forwards,
		HeartbeatOptions? heartbeatOptions = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default
	) {
		var channel = Channel.CreateUnbounded<ReadResult>(new() {
			SingleReader = true,
			SingleWriter = true,
		});

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
							_ => throw new UnreachableException($"Unexpected result while reading stream: {response.ResultCase}")
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

		async ValueTask HandleHeartbeat(Contracts.ReadResponse response) =>
			await channel.Writer.WriteAsync(response.Heartbeat.Map(), cancellationToken).ConfigureAwait(false);

		ValueTask HandleFailure(Contracts.ReadResponse response) {
			Exception ex = response.Failure.ErrorCase switch {
				Contracts.ReadFailure.ErrorOneofCase.AccessDenied  => new AccessDeniedException(),
				Contracts.ReadFailure.ErrorOneofCase.StreamDeleted => new StreamDeletedException(response.Failure.StreamDeleted.Stream),
				_                                                  => new UnreachableException($"Unexpected error while reading stream: {response.Failure.ErrorCase}")
			};

			channel.Writer.TryComplete(ex);

			return ValueTask.CompletedTask;
		}
	}

	public IAsyncEnumerable<ReadResult> Read(
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
				cancellationToken: cancellationToken
			)
			: ReadAll(
				startPosition, limit, direction,
				filter, heartbeatOptions,
				cancellationToken: cancellationToken
			);

		return session;
	}

	public async IAsyncEnumerable<ReadResult> ReadAll(
		LogPosition startPosition, long limit, Direction direction,
		ReadFilter filter, HeartbeatOptions heartbeatOptions,
		[EnumeratorCancellation] CancellationToken cancellationToken = default
	) {
		var legacyPosition = startPosition.ConvertToLegacyPosition();
		var eventFilter    = filter.ConvertToEventFilter(heartbeatOptions.RecordsThreshold);

		var session = LegacyClient.ReadAllAsync(
			direction, legacyPosition, eventFilter, limit,
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

	public async IAsyncEnumerable<ReadResult> ReadStream(
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

	public async IAsyncEnumerable<ReadResult> ReadStream(
		string stream, LogPosition startPosition, long limit, Direction direction,
		[EnumeratorCancellation] CancellationToken cancellationToken = default
	) {
		var revision = startPosition switch {
			_ when startPosition == LogPosition.Unset    => StreamRevision.Min,
			_ when startPosition == LogPosition.Earliest => StreamRevision.Min,
			_ when startPosition == LogPosition.Latest   => StreamRevision.Max,
			_                                            => await GetStreamRevision(startPosition, cancellationToken).ConfigureAwait(false)
		};

		var session = ReadStream(stream, revision, limit, direction, cancellationToken);
		await foreach (var record in session.ConfigureAwait(false))
			yield return record;
	}

	public async ValueTask<Record> ReadFirstStreamRecord(string stream, CancellationToken cancellationToken = default) {
		try {
			var result = LegacyClient.ReadStreamAsync(
				direction: Direction.Forwards,
				streamName: stream,
				revision: StreamPosition.Start,
				maxCount: 1,
				cancellationToken: cancellationToken
			);

			ResolvedEvent? re = await result
				.FirstOrDefaultAsync(cancellationToken: cancellationToken)
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
				direction: Direction.Backwards,
				streamName: stream,
				revision: StreamPosition.End,
				maxCount: 1,
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
				.ReadAllAsync(Direction.Forwards, position.ConvertToLegacyPosition(), maxCount: 1, cancellationToken: cancellationToken)
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
			.ReadAllAsync(Direction.Forwards, position.ConvertToLegacyPosition(), maxCount: 1, cancellationToken: cancellationToken)
			.SingleAsync(cancellationToken)
			.ConfigureAwait(false);

		return re.OriginalEventNumber.ConvertToStreamRevision();
	}

	#endregion

	#region . Subscribe .

	public IAsyncEnumerable<SubscribeResult> UnifiedSubscribe(
		LogPosition startPosition, ReadFilter filter, HeartbeatOptions heartbeatOptions,
		CancellationToken cancellationToken = default
	) {
		var session = filter.IsStreamNameFilter
			? SubscribeToStream(
				filter.Expression, startPosition, filter,
				cancellationToken: cancellationToken
			)
			: SubscribeToAll(
				startPosition, filter, heartbeatOptions,
				cancellationToken: cancellationToken
			);

		return session;
	}

	public async IAsyncEnumerable<SubscribeResult> SubscribeToAll(

		LogPosition startPosition, ReadFilter filter, HeartbeatOptions heartbeatOptions,
		[EnumeratorCancellation] CancellationToken cancellationToken = default
	) {
		var start       = startPosition.ConvertToLegacyFromAll();
		var eventFilter = filter.ConvertToEventFilter(heartbeatOptions.RecordsThreshold);

		// wth?!?... is SubscriptionFilterOptions.CheckpointInterval != IEventFilter.MaxSearchWindow ?!?!?!
		var filterOptions = new SubscriptionFilterOptions(eventFilter, (uint)heartbeatOptions.RecordsThreshold);

		await using var session = LegacyClient.SubscribeToAll(
			start: start,
			filterOptions: filterOptions,
			cancellationToken: cancellationToken
		);

		await foreach (var msg in session.Messages.WithCancellation(cancellationToken).ConfigureAwait(false)) {
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
						checkpoint.Timestamp);

					yield return heartbeat;
					break;
				}

				case StreamMessage.CaughtUp caughtUp: {
					var heartbeat = Heartbeat.CreateCaughtUp(
						caughtUp.Position.ConvertToLogPosition(),
						caughtUp.Timestamp);

					yield return heartbeat;
					break;
				}

				// new protocol, new model and this? this is just noise
				// case StreamMessage.FellBehind fellBehind:
				// case StreamMessage.LastAllStreamPosition lastAllStreamPosition:
				// case StreamMessage.SubscriptionConfirmation subscriptionConfirmation:
				// 	break;
			}
		}
	}

	public async IAsyncEnumerable<SubscribeResult> SubscribeToStream(
		string stream, StreamRevision startRevision, ReadFilter filter,
		[EnumeratorCancellation] CancellationToken cancellationToken = default
	) {
		var start = startRevision.ConvertToLegacyFromStream();

		await using var session = LegacyClient.SubscribeToStream(
			streamName: stream,
			start: start,
			cancellationToken: cancellationToken
		);

		await foreach (var msg in session.Messages.WithCancellation(cancellationToken).ConfigureAwait(false)) {
			switch (msg) {
				case StreamMessage.Event { ResolvedEvent: var re }:
					var record = await LegacyConverter
						.ConvertToRecord(re, cancellationToken)
						.ConfigureAwait(false);

					yield return record;

					// FILTER ALERT!
					// for now we could apply the filter locally until we refactor the server operation.

					// if (filter.IsEmptyFilter)
					// 	yield return record;
					// else {
					// 	switch (filter.Scope) {
					// 		case ReadFilterScope.Stream:
					// 			if (filter.IsMatch(record.Stream))
					// 				yield return record;
					// 			break;
					//
					// 		case ReadFilterScope.SchemaName:
					// 			if (filter.IsMatch(record.Schema.SchemaName))
					// 				yield return record;
					// 			break;
					//
					// 		// case ReadFilterScope.Properties:
					// 		// 	if (filter.IsMatch(record.Metadata))
					// 		// 		yield return record;
					// 		// 	break;
					//
					// 		// case ReadFilterScope.Record:
					// 		// 	if (filter.IsMatch(record.Schema.SchemaName))
					// 		// 		yield return record;
					// 		// 	break;
					//
					// 		// default:
					// 		// 	// if no scope is specified, we assume the filter applies to both stream and record
					// 		// 	if (filter.IsStreamNameFilter && filter.IsMatch(record.Stream) ||
					// 		// 	    filter.IsRecordFilter && filter.IsMatch(record.Data.Span))
					// 		// 		yield return record;
					// 		// 	break;
					//
					// 	}
					// }

					break;

				// its the same message as in SubscribeToAll, still need to test it...
				case StreamMessage.AllStreamCheckpointReached checkpoint: {
					var heartbeat = Heartbeat.CreateCheckpoint(
						checkpoint.Position.ConvertToLogPosition(),
						checkpoint.Timestamp);

					yield return heartbeat;
					break;
				}

				case StreamMessage.CaughtUp caughtUp: {
					var heartbeat = Heartbeat.CreateCaughtUp(
						caughtUp.Position.ConvertToLogPosition(),
						caughtUp.Timestamp);

					yield return heartbeat;
					break;
				}

				case StreamMessage.NotFound:
					throw new StreamNotFoundException(stream);

				// new protocol, new model and this? thi is just noise
				// case StreamMessage.FellBehind fellBehind:
				// case StreamMessage.LastAllStreamPosition lastAllStreamPosition:
				// case StreamMessage.SubscriptionConfirmation subscriptionConfirmation:
				// 	break;
			}
		}
	}

	public async IAsyncEnumerable<SubscribeResult> SubscribeToStream(
		string stream, LogPosition startPosition, ReadFilter filter,
		[EnumeratorCancellation] CancellationToken cancellationToken = default
	) {
		var revision = startPosition switch {
			_ when startPosition == LogPosition.Unset    => StreamRevision.Min,
			_ when startPosition == LogPosition.Earliest => StreamRevision.Min,
			_ when startPosition == LogPosition.Latest   => StreamRevision.Max,
			_                                            => await GetStreamRevision(startPosition, cancellationToken).ConfigureAwait(false)
		};

		var session = SubscribeToStream(stream, revision, filter, cancellationToken);
		await foreach (var record in session.ConfigureAwait(false))
			yield return record;
	}

	#endregion

    #region . Delete .

    /// <summary>
    /// Deletes a stream asynchronously.
    /// </summary>
    /// <param name="stream">The name of the stream to delete.</param>
    /// <param name="expectedState">The expected <see cref="ExpectedStreamState"/> of the stream to be deleted.</param>
    /// <param name="cancellationToken">
    /// The optional <see cref="CancellationToken"/> to observe while waiting for the operation to complete.
    /// </param>
    /// <returns></returns>
    public ValueTask<Result<DeleteError, LogPosition>> Delete(string stream, ExpectedStreamState expectedState, CancellationToken cancellationToken = default) =>
         DeleteCore(stream, expectedState, hardDelete: false, cancellationToken);

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
    public ValueTask<Result<DeleteError, LogPosition>> Tombstone(string stream, ExpectedStreamState expectedState, CancellationToken cancellationToken = default) =>
         DeleteCore(stream, expectedState, hardDelete: true, cancellationToken);

    async ValueTask<Result<DeleteError, LogPosition>> DeleteCore(string stream, ExpectedStreamState expectedState, bool hardDelete = false, CancellationToken cancellationToken = default) {
        var legacyExpectedState = expectedState switch {
            _ when expectedState == ExpectedStreamState.Any          => StreamState.Any,
            _ when expectedState == ExpectedStreamState.NoStream     => StreamState.NoStream,
            _ when expectedState == ExpectedStreamState.StreamExists => StreamState.StreamExists,
            _                                                        => StreamState.StreamRevision(expectedState),
        };

        try {
            var result = hardDelete switch {
                true => await LegacyClient
                    .TombstoneAsync(stream, legacyExpectedState, cancellationToken: cancellationToken)
                    .ConfigureAwait(false),
                false => await LegacyClient
                    .DeleteAsync(stream, legacyExpectedState, cancellationToken: cancellationToken)
                    .ConfigureAwait(false)
            };

            return result.LogPosition.ConvertToLogPosition();
        }
        catch (Exception ex) {
            return Result<DeleteError, LogPosition>.Error(ex switch {
                StreamNotFoundException => new ErrorDetails.StreamNotFound(stream),
                StreamDeletedException  => new ErrorDetails.StreamDeleted(stream),
                AccessDeniedException   => new ErrorDetails.AccessDenied(),
                _                       => throw new Exception($"Unexpected error while deleting stream {stream}: {ex.Message}", ex)
            });
        }
    }


    // [PublicAPI]
    // [GenerateOneOf]
    // public partial class DeleteResult : OneOfBase<LogPosition, DeleteError>;
    //

    public class DeleteResult : Result<DeleteError, LogPosition> {
        protected DeleteResult(OneOf<Error<DeleteError>, Success<LogPosition>> _) : base(_) { }
        protected DeleteResult(object value, bool isSuccess) : base(value, isSuccess) { }
    }


    #endregion

    #region . Meta .

    // public async ValueTask<Result<StreamMetadataError, StreamMetadata>> GetStreamMetadata(string stream, CancellationToken cancellationToken = default) {
    //     try {
    //         var result = await LegacyClient
    //             .GetStreamMetadataAsync(stream, cancellationToken: cancellationToken)
    //             .ConfigureAwait(false);
    //
    //         StreamMetadata metadata = new() {
    //             MaxAge           = result.Metadata.MaxAge,
    //             TruncateBefore   = result.Metadata.TruncateBefore?.ConvertToStreamRevision(),
    //             CacheControl     = result.Metadata.CacheControl,
    //             MaxCount         = result.Metadata.MaxCount,
    //             CustomMetadata   = result.Metadata.CustomMetadata,
    //             MetadataRevision = result.MetastreamRevision?.ConvertToStreamRevision(),
    //             IsDeleted        = result.StreamDeleted
    //         };
    //
    //         return metadata;
    //     }
    //     catch (Exception ex) {
    //         return Result<StreamMetadataError, StreamMetadata>.Error(ex switch {
    //             StreamNotFoundException => new ErrorDetails.StreamNotFound(stream),
    //             StreamDeletedException  => new ErrorDetails.StreamDeleted(stream),
    //             AccessDeniedException   => new ErrorDetails.AccessDenied(),
    //             _                       => throw new Exception($"Unexpected error {stream}: {ex.Message}", ex)
    //         });
    //     }
    // }

    // public async ValueTask<Result<StreamInfoOperationError, StreamInfo>> GetStreamInfo(string stream, CancellationToken cancellationToken = default) {
    //     try {
    //         var result = await LegacyClient
    //             .GetStreamMetadataAsync(stream, cancellationToken: cancellationToken)
    //             .ConfigureAwait(false);
    //
    //         var record = await ReadLastStreamRecord(stream, cancellationToken).ConfigureAwait(false);
    //
    //         StreamInfo metadata = new() {
    //             MaxAge             = result.Metadata.MaxAge,
    //             TruncateBefore     = result.Metadata.TruncateBefore?.ConvertToStreamRevision(),
    //             CacheControl       = result.Metadata.CacheControl,
    //             MaxCount           = result.Metadata.MaxCount,
    //             CustomMetadata     = result.Metadata.CustomMetadata,
    //             MetadataRevision   = result.MetastreamRevision?.ConvertToStreamRevision(),
    //             IsDeleted          = result.StreamDeleted,
    //             LastStreamRevision = record.StreamRevision,
    //             LastStreamPosition = record.Position,
    //         };
    //
    //         return metadata;
    //     }
    //     catch (Exception ex) {
    //         return Result<StreamInfoOperationError, StreamMetadata>.Error(ex switch {
    //             StreamNotFoundException => new ErrorDetails.StreamNotFound(stream),
    //             StreamDeletedException  => new ErrorDetails.StreamDeleted(stream),
    //             AccessDeniedException   => new ErrorDetails.AccessDenied(),
    //             _                       => throw new Exception($"Unexpected error {stream}: {ex.Message}", ex)
    //         });
    //     }
    // }

    // public async ValueTask<Result<StreamMetadataError, StreamMetadata>> SetStreamMetadata(string stream, ExpectedStreamState expectedState, CancellationToken cancellationToken = default) {
    //     var legacyExpectedState = expectedState.Match(
    //         streamRevision => StreamState.StreamRevision((ulong)streamRevision.Value),
    //         _ => StreamState.Any,
    //         _ => StreamState.NoStream,
    //         _ => StreamState.StreamExists
    //     );
    //
    //     var legacyMetadata = new KurrentDB.Client.StreamMetadata(
    //         maxAge:           null,
    //         truncateBefore:   null,
    //         cacheControl:     null,
    //         maxCount:         null,
    //         customMetadata:   null
    //     );
    //
    //     /// <summary>
    //     /// Asynchronously sets the metadata for a stream.
    //     /// </summary>
    //     /// <param name="streamName">The name of the stream to set metadata for.</param>
    //     /// <param name="expectedState">The <see cref="StreamState"/> of the stream to append to.</param>
    //     /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
    //     /// <param name="configureOperationOptions">An <see cref="Action{KurrentDBClientOperationOptions}"/> to configure the operation's options.</param>
    //     /// <param name="deadline"></param>
    //     /// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
    //     /// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
    //     /// <returns></returns>
    //     // public Task<IWriteResult> SetStreamMetadataAsync(string streamName, StreamState expectedState,
    //                                                  //    StreamMetadata metadata,
    //
    //     try {
    //         var result = await LegacyClient
    //             .SetStreamMetadataAsync(stream, legacyExpectedState, cancellationToken: cancellationToken)
    //             .ConfigureAwait(false);
    //
    //         StreamMetadata metadata = new() {
    //             MaxAge           = result.Metadata.MaxAge,
    //             TruncateBefore   = result.Metadata.TruncateBefore?.ConvertToStreamRevision(),
    //             CacheControl     = result.Metadata.CacheControl,
    //             MaxCount         = result.Metadata.MaxCount,
    //             CustomMetadata   = result.Metadata.CustomMetadata,
    //             MetadataRevision = result.MetastreamRevision?.ConvertToStreamRevision(),
    //             IsDeleted        = result.StreamDeleted
    //         };
    //
    //         return metadata;
    //     }
    //     catch (Exception ex) {
    //         return Result<StreamMetadataError, StreamMetadata>.Error(ex switch {
    //             StreamNotFoundException => new ErrorDetails.StreamNotFound(stream),
    //             StreamDeletedException  => new ErrorDetails.StreamDeleted(stream),
    //             AccessDeniedException   => new ErrorDetails.AccessDenied(),
    //             _                       => throw new Exception($"Unexpected error {stream}: {ex.Message}", ex)
    //         });
    //     }
    // }

    #endregion
}

public record StreamMetadata {
    /// <summary>
    /// The optional maximum age of events allowed in the stream.
    /// </summary>
    public TimeSpan? MaxAge { get; init; }

    /// <summary>
    /// The optional <see cref="StreamRevision"/> from which previous events can be scavenged.
    /// This is used to implement soft-deletion of streams.
    /// </summary>
    public StreamRevision? TruncateBefore { get; init; }

    /// <summary>
    /// The optional amount of time for which the stream head is cacheable.
    /// </summary>
    public TimeSpan? CacheControl { get; init; }

    /// <summary>
    /// The optional maximum number of events allowed in the stream.
    /// </summary>
    public int? MaxCount { get; init; }

    /// <summary>
    /// The optional <see cref="JsonDocument"/> of user provided metadata.
    /// </summary>
    public JsonDocument? CustomMetadata { get; init; }

    public StreamRevision? MetadataRevision { get; init; }

    public bool IsDeleted { get; init; }

    // /// <summary>
    // /// The optional <see cref="StreamAcl"/> for the stream.
    // /// </summary>
    // public StreamAcl? Acl { get; init; }

    public bool HasMaxAge           => MaxAge.HasValue && MaxAge > TimeSpan.Zero;
    public bool HasTruncateBefore   => TruncateBefore is not null;
    public bool HasCacheControl     => CacheControl.HasValue && CacheControl.Value > TimeSpan.Zero;
    public bool HasMaxCount         => MaxCount is > 0;
    public bool HasCustomMetadata   => CustomMetadata is not null && CustomMetadata.RootElement.ValueKind != JsonValueKind.Undefined;
    public bool HasMetadataRevision => MetadataRevision is not null;
}

public record StreamInfo {
    /// <summary>
    /// The optional maximum age of events allowed in the stream.
    /// </summary>
    public TimeSpan? MaxAge { get; init; }

    /// <summary>
    /// The optional <see cref="StreamRevision"/> from which previous events can be scavenged.
    /// This is used to implement soft-deletion of streams.
    /// </summary>
    public StreamRevision? TruncateBefore { get; init; }

    /// <summary>
    /// The optional amount of time for which the stream head is cacheable.
    /// </summary>
    public TimeSpan? CacheControl { get; init; }

    /// <summary>
    /// The optional maximum number of events allowed in the stream.
    /// </summary>
    public int? MaxCount { get; init; }

    /// <summary>
    /// The optional <see cref="JsonDocument"/> of user provided metadata.
    /// </summary>
    public JsonDocument? CustomMetadata { get; init; }

    public StreamRevision? MetadataRevision { get; init; }

    public bool IsDeleted { get; init; }

    public StreamRevision LastStreamRevision { get; init; }
    public LogPosition    LastStreamPosition { get; init; }

    // /// <summary>
    // /// The optional <see cref="StreamAcl"/> for the stream.
    // /// </summary>
    // public StreamAcl? Acl { get; init; }

    public bool HasMaxAge           => MaxAge.HasValue && MaxAge > TimeSpan.Zero;
    public bool HasTruncateBefore   => TruncateBefore is not null;
    public bool HasCacheControl     => CacheControl.HasValue && CacheControl.Value > TimeSpan.Zero;
    public bool HasMaxCount         => MaxCount is > 0;
    public bool HasCustomMetadata   => CustomMetadata is not null && CustomMetadata.RootElement.ValueKind != JsonValueKind.Undefined;
    public bool HasMetadataRevision => MetadataRevision is not null;
}

// public class DeleteResult : Result<DeleteError, LogPosition> {
//     protected DeleteResult(OneOf<Error<DeleteError>, Success<LogPosition>> _) : base(_) { }
//     protected DeleteResult(object value, bool isSuccess) : base(value, isSuccess) { }
// }
