// #pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
// #pragma warning disable CS8509
//
// // ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
// // ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
//
// using System.Diagnostics;
// using System.Runtime.CompilerServices;
// using System.Threading.Channels;
// using KurrentDB.Client.Model;
// using OneOf;
//
// using static KurrentDB.Protocol.Streams.V2.StreamsService;
//
// using Contracts = KurrentDB.Protocol.Streams.V2;
//
// namespace KurrentDB.Client;
//
// public static class KurrentDBReaderExtensions {
// 	// internal static async IAsyncEnumerable<ReadResult> UnifiedReadWithNewProtocol(
// 	// 	this KurrentDBClient client,
// 	// 	LogPosition startPosition,
// 	// 	long limit,
// 	// 	ReadFilter? filter = null,
// 	// 	Direction direction = Direction.Forwards,
// 	// 	HeartbeatOptions? heartbeatOptions = null,
// 	// 	[EnumeratorCancellation] CancellationToken cancellationToken = default
// 	// ) {
// 	// 	var channel = Channel.CreateUnbounded<ReadResult>(new() {
// 	// 		SingleReader = true,
// 	// 		SingleWriter = true,
// 	// 	});
// 	//
// 	// 	var (serviceClient, _) = await client.Connect<StreamsServiceClient>(cancellationToken).ConfigureAwait(false);
// 	//
// 	// 	var request = new Contracts.ReadRequest {
// 	// 		Filter        = filter?.Map(),
// 	// 		StartPosition = startPosition,
// 	// 		Limit         = limit,
// 	// 		Direction     = direction.Map(),
// 	// 		Heartbeats    = heartbeatOptions?.Map()
// 	// 	};
// 	//
// 	// 	// Start a task to read from gRPC and write to the channel
// 	// 	using var readTask = Task.Run(
// 	// 		async () => {
// 	// 			try {
// 	// 				using var session = serviceClient.ReadSession(request, cancellationToken: cancellationToken);
// 	//
// 	// 				while (await session.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
// 	// 					var response = session.ResponseStream.Current;
// 	//
// 	// 					await (response.ResultCase switch {
// 	// 						Contracts.ReadResponse.ResultOneofCase.Success   => HandleSuccess(response),
// 	// 						Contracts.ReadResponse.ResultOneofCase.Heartbeat => HandleHeartbeat(response),
// 	// 						Contracts.ReadResponse.ResultOneofCase.Failure   => HandleFailure(response),
// 	// 						_ => throw new UnreachableException($"Unexpected result while reading stream: {response.ResultCase}")
// 	// 					});
// 	// 				}
// 	//
// 	// 				channel.Writer.TryComplete();
// 	// 			}
// 	// 			catch (Exception ex) {
// 	// 				channel.Writer.Complete(new Exception($"Error while reading stream: {ex.Message}", ex));
// 	// 			}
// 	// 		},
// 	// 		cancellationToken
// 	// 	);
// 	//
// 	// 	await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
// 	// 		yield return result;
// 	//
// 	// 	yield break;
// 	//
// 	// 	async ValueTask HandleSuccess(Contracts.ReadResponse response) {
// 	// 		foreach (var record in response.Success.Records) {
// 	// 			var mappedRecord = await record.Map(client.SerializerProvider, cancellationToken).ConfigureAwait(false);
// 	// 			await channel.Writer.WriteAsync(mappedRecord, cancellationToken).ConfigureAwait(false);
// 	// 		}
// 	// 	}
// 	//
// 	// 	async ValueTask HandleHeartbeat(Contracts.ReadResponse response) =>
// 	// 		await channel.Writer.WriteAsync(response.Heartbeat.Map(), cancellationToken).ConfigureAwait(false);
// 	//
// 	// 	ValueTask HandleFailure(Contracts.ReadResponse response) {
// 	// 		Exception ex = response.Failure.ErrorCase switch {
// 	// 			Contracts.ReadFailure.ErrorOneofCase.AccessDenied  => new AccessDeniedException(),
// 	// 			Contracts.ReadFailure.ErrorOneofCase.StreamDeleted => new StreamDeletedException(response.Failure.StreamDeleted.Stream),
// 	// 			_                                                  => new UnreachableException($"Unexpected error while reading stream: {response.Failure.ErrorCase}")
// 	// 		};
// 	//
// 	// 		channel.Writer.TryComplete(ex);
// 	//
// 	// 		return ValueTask.CompletedTask;
// 	// 	}
// 	// }
//
// 	public static IAsyncEnumerable<ReadResult> Read(
// 		this KurrentDBClient client,
// 		LogPosition startPosition,
// 		long limit,
// 		ReadFilter filter,
// 		Direction direction,
// 		HeartbeatOptions heartbeatOptions,
// 		CancellationToken cancellationToken = default
// 	) {
// 		var session = filter.IsStreamNameFilter
// 			? client.ReadStream(
// 				filter.Expression,
// 				startPosition, limit, direction,
// 				cancellationToken: cancellationToken
// 			)
// 			: client.ReadAll(
// 				startPosition, limit, direction,
// 				filter, heartbeatOptions,
// 				cancellationToken: cancellationToken
// 			);
//
// 		return session;
// 	}
//
// 	public static async IAsyncEnumerable<ReadResult> ReadAll(
// 		this KurrentDBClient client,
// 		LogPosition startPosition, long limit, Direction direction,
// 		ReadFilter filter, HeartbeatOptions heartbeatOptions,
// 		[EnumeratorCancellation] CancellationToken cancellationToken = default
// 	) {
// 		var legacyPosition = startPosition.ConvertToLegacyPosition();
// 		var eventFilter    = filter.ConvertToEventFilter(heartbeatOptions.RecordsThreshold);
//
// 		var session = client.ReadAllAsync(
// 			direction, legacyPosition, eventFilter, limit,
// 			cancellationToken: cancellationToken
// 		);
//
// 		// what about checkpoints (aka heartbeats), only with new protocol?
// 		await foreach (var re in session.ConfigureAwait(false)) {
// 			var record = await client.DataConverter
// 				.ConvertToRecord(re, cancellationToken)
// 				.ConfigureAwait(false);
//
// 			yield return record;
// 		}
// 	}
//
// 	public static async IAsyncEnumerable<ReadResult> ReadStream(
// 		this KurrentDBClient client,
// 		string stream, StreamRevision revision, long limit, Direction direction,
// 		[EnumeratorCancellation] CancellationToken cancellationToken = default
// 	) {
// 		// will throw if stream is not found or deleted
// 		// and ignores all other message types.
// 		var session = client.ReadStreamAsync(
// 			direction, stream,
// 			revision.ConvertToLegacyStreamPosition(), limit,
// 			cancellationToken: cancellationToken
// 		);
//
// 		// what about checkpoints (aka heartbeats), only with new protocol?
// 		await foreach (var re in session.ConfigureAwait(false)) {
// 			var record = await client.DataConverter
// 				.ConvertToRecord(re, cancellationToken)
// 				.ConfigureAwait(false);
//
// 			yield return record;
// 		}
// 	}
//
// 	public static async IAsyncEnumerable<ReadResult> ReadStream(
// 		this KurrentDBClient client,
// 		string stream, LogPosition startPosition, long limit, Direction direction,
// 		[EnumeratorCancellation] CancellationToken cancellationToken = default
// 	) {
// 		var revision = startPosition switch {
// 			_ when startPosition == LogPosition.Unset    => StreamRevision.Min,
// 			_ when startPosition == LogPosition.Earliest => StreamRevision.Min,
// 			_ when startPosition == LogPosition.Latest   => StreamRevision.Max,
// 			_                                            => await client.GetStreamRevision(startPosition, cancellationToken).ConfigureAwait(false)
// 		};
//
// 		var session = client.ReadStream(stream, revision, limit, direction, cancellationToken);
// 		await foreach (var record in session.ConfigureAwait(false))
// 			yield return record;
// 	}
//
// 	public static async ValueTask<Record> ReadFirstStreamRecord(this KurrentDBClient client, string stream, CancellationToken cancellationToken = default) {
// 		try {
// 			var result = client.ReadStreamAsync(
// 				direction: Direction.Forwards,
// 				streamName: stream,
// 				revision: StreamPosition.Start,
// 				maxCount: 1,
// 				cancellationToken: cancellationToken
// 			);
//
// 			ResolvedEvent? re = await result
// 				.FirstOrDefaultAsync(cancellationToken: cancellationToken)
// 				.ConfigureAwait(false);
//
// 			return re?.Event is not null
// 				? await client.DataConverter
// 					.ConvertToRecord(re.Value, cancellationToken)
// 					.ConfigureAwait(false)
// 				: Record.None;
// 		}
// 		catch (StreamNotFoundException) {
// 			return Record.None;
// 		}
// 		catch (StreamDeletedException) { // tombstoned
// 			return Record.None;
// 		}
// 	}
//
// 	public static async ValueTask<Record> ReadLastStreamRecord(this KurrentDBClient client, string stream, CancellationToken cancellationToken = default) {
// 		try {
// 			var result = client.ReadStreamAsync(
// 				direction: Direction.Backwards,
// 				streamName: stream,
// 				revision: StreamPosition.End,
// 				maxCount: 1,
// 				cancellationToken: cancellationToken
// 			);
//
// 			ResolvedEvent? re = await result
// 				.FirstOrDefaultAsync(cancellationToken)
// 				.ConfigureAwait(false);
//
// 			return re?.Event is not null
// 				? await client.DataConverter
// 					.ConvertToRecord(re.Value, cancellationToken)
// 					.ConfigureAwait(false)
// 				: Record.None;
// 		}
// 		catch (StreamNotFoundException) {
// 			return Record.None;
// 		}
// 		catch (StreamDeletedException) { // tombstoned
// 			return Record.None;
// 		}
// 	}
//
// 	public static async ValueTask<Record> ReadSingleRecord(this KurrentDBClient client, LogPosition position, CancellationToken cancellationToken = default) {
// 		try {
// 			ResolvedEvent? re = await client
// 				.ReadAllAsync(Direction.Forwards, position.ConvertToLegacyPosition(), maxCount: 1, cancellationToken: cancellationToken)
// 				.FirstOrDefaultAsync(cancellationToken);
//
// 			return re?.Event is not null
// 				? await client.DataConverter
// 					.ConvertToRecord(re.Value, cancellationToken)
// 					.ConfigureAwait(false)
// 				: Record.None;
// 		}
// 		catch (StreamNotFoundException) {
// 			return Record.None;
// 		}
// 		catch (StreamDeletedException) { // tombstoned
// 			return Record.None;
// 		}
// 	}
//
// 	internal static async ValueTask<StreamRevision> GetStreamRevision(this KurrentDBClient client, LogPosition position, CancellationToken cancellationToken = default) {
// 		if (position == LogPosition.Latest)
// 			return StreamRevision.Max;
//
// 		if (position == LogPosition.Unset || position == LogPosition.Earliest)
// 			return StreamRevision.Min;
//
// 		var re = await client
// 			.ReadAllAsync(Direction.Forwards, position.ConvertToLegacyPosition(), maxCount: 1, cancellationToken: cancellationToken)
// 			.SingleAsync(cancellationToken)
// 			.ConfigureAwait(false);
//
// 		return re.OriginalEventNumber.ConvertToStreamRevision();
// 	}
// }
