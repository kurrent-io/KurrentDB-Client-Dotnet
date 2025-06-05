#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
// ReSharper disable CheckNamespace

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Grpc.Core;
using Kurrent.Client.Features;
using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry.Serialization;
using KurrentDB.Client;
using static KurrentDB.Protocol.Streams.V2.StreamsService;
using Contracts = KurrentDB.Protocol.Streams.V2;

namespace Kurrent.Client;

[PublicAPI]
public class KurrentAppendOperations {
	// Custom empty request to avoid using the default one from the library.... sigh... -_-'
	static readonly EventStore.Client.Empty CustomEmptyRequest = new();

	public KurrentClient(KurrentDBClientSettings settings) : base(settings) {
		Connection = GetProxyConnection<StreamsServiceClient>();

		Connection = GetProxyConnection<StreamsServiceClient>();
	}

	ServiceProxy<StreamsServiceClient> Connection { get; }

	#region . Append .

	public static async ValueTask<MultiStreamAppendResult> Append(this KurrentClientIAsyncEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default) {
		using var session = Connection.ServiceClient.MultiStreamAppendSession(cancellationToken: cancellationToken);

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
					using var session = Connection.ServiceClient.ReadSession(request, cancellationToken: cancellationToken);

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
			? client.ReadStream(
				filter.Expression,
				startPosition, limit, direction,
				cancellationToken: cancellationToken
			)
			: client.ReadAll(
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

		var session = client.LegacyProxy.ReadAllAsync(
			direction, legacyPosition, eventFilter, limit,
			cancellationToken: cancellationToken
		);

		// what about checkpoints (aka heartbeats), only with new protocol?
		await foreach (var re in session.ConfigureAwait(false)) {
			var record = await client.DataConverter
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
			var record = await LegacyDataConverter
				.ConvertToRecord(re, cancellationToken)
				.ConfigureAwait(false);

			yield return record;
		}
	}

	public static async IAsyncEnumerable<ReadResult> ReadStream(
		this KurrentClient client,
		string stream, LogPosition startPosition, long limit, Direction direction,
		[EnumeratorCancellation] CancellationToken cancellationToken = default
	) {
		var revision = startPosition switch {
			_ when startPosition == LogPosition.Unset    => StreamRevision.Min,
			_ when startPosition == LogPosition.Earliest => StreamRevision.Min,
			_ when startPosition == LogPosition.Latest   => StreamRevision.Max,
			_                                            => await client.GetStreamRevision(startPosition, cancellationToken).ConfigureAwait(false)
		};

		var session = client.ReadStream(stream, revision, limit, direction, cancellationToken);
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
				? await LegacyDataConverter
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

	public async ValueTask<Record> ReadLastStreamRecord(this KurrentClient client, string stream, CancellationToken cancellationToken = default) {
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
				? await client.DataConverter
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

	public static async ValueTask<Record> ReadSingleRecord(this KurrentClient client, LogPosition position, CancellationToken cancellationToken = default) {
		try {
			ResolvedEvent? re = await client.LegacyProxy
				.ReadAllAsync(Direction.Forwards, position.ConvertToLegacyPosition(), maxCount: 1, cancellationToken: cancellationToken)
				.FirstOrDefaultAsync(cancellationToken);

			return re?.Event is not null
				? await client.DataConverter
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

	internal static async ValueTask<StreamRevision> GetStreamRevision(this KurrentClient client, LogPosition position, CancellationToken cancellationToken = default) {
		if (position == LogPosition.Latest)
			return StreamRevision.Max;

		if (position == LogPosition.Unset || position == LogPosition.Earliest)
			return StreamRevision.Min;

		var re = await client.LegacyProxy
			.ReadAllAsync(Direction.Forwards, position.ConvertToLegacyPosition(), maxCount: 1, cancellationToken: cancellationToken)
			.SingleAsync(cancellationToken)
			.ConfigureAwait(false);

		return re.OriginalEventNumber.ConvertToStreamRevision();
	}

	#endregion
}


// [PublicAPI]
// public partial class KurrentClient : KurrentClientBase {
// 	// Custom empty request to avoid using the default one from the library.... sigh... -_-'
// 	static readonly EventStore.Client.Empty CustomEmptyRequest = new();
//
// 	public KurrentClient(KurrentDBClientSettings settings) : base(settings) {
// 		Connection = GetProxyConnection<StreamsServiceClient>();
//
// 		Connection = GetProxyConnection<StreamsServiceClient>();
// 	}
//
// 	ThinClientConnection<StreamsServiceClient> Connection { get; }
//
// 	#region . Append .
//
// 	public async ValueTask<MultiStreamAppendResult> Append(IAsyncEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default) {
// 		using var session = Connection.ServiceClient.MultiStreamAppendSession(cancellationToken: cancellationToken);
//
// 		await foreach (var request in requests.WithCancellation(cancellationToken)) {
// 			var records = await request.Messages
// 				.Map(request.Stream, SerializerProvider, cancellationToken)
// 				.ToArrayAsync(cancellationToken)
// 				.ConfigureAwait(false);
//
// 			var serviceRequest = new Contracts.AppendStreamRequest {
// 				Stream           = request.Stream,
// 				ExpectedRevision = request.ExpectedState,
// 				Records          = { records }
// 			};
//
// 			cancellationToken.ThrowIfCancellationRequested();
//
// 			await session.RequestStream
// 				.WriteAsync(serviceRequest, cancellationToken)
// 				.ConfigureAwait(false);
// 		}
//
// 		await session.RequestStream.CompleteAsync();
//
// 		var response = await session.ResponseAsync;
//
// 		return response.ResultCase switch {
// 			Contracts.MultiStreamAppendResponse.ResultOneofCase.Success => response.Success.Map(),
// 			Contracts.MultiStreamAppendResponse.ResultOneofCase.Failure => response.Failure.Map()
// 		};
// 	}
//
// 	public ValueTask<MultiStreamAppendResult> Append(IEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default) =>
// 		 Append(requests.ToAsyncEnumerable(), cancellationToken);
//
// 	public ValueTask<MultiStreamAppendResult> Append(MultiStreamAppendRequest request, CancellationToken cancellationToken = default) =>
// 		Append(request.Requests.ToAsyncEnumerable(), cancellationToken);
//
// 	/// <summary>
// 	/// Appends a series of messages to a specified stream in KurrentDB.
// 	/// </summary>
// 	/// <param name="request">The request object that specifies the stream, expected state, and messages to append.</param>
// 	/// <param name="cancellationToken">A token to cancel the operation if needed.</param>
// 	/// <returns>An <see cref="AppendStreamResult"/> representing the result of the append operation.</returns>
// 	public async ValueTask<AppendStreamResult> Append(AppendStreamRequest request, CancellationToken cancellationToken) {
// 		var result = await Append([request], cancellationToken).ConfigureAwait(false);
//
// 		return result.Match<AppendStreamResult>(
// 			success => success.First(),
// 			failure => failure.First()
// 		);
// 	}
//
// 	/// <summary>
// 	/// Appends a series of messages to a specified stream while specifying the expected stream state.
// 	/// </summary>
// 	/// <param name="stream">The name of the stream to which the messages will be appended.</param>
// 	/// <param name="expectedState">The expected state of the stream to ensure consistency during the append operation.</param>
// 	/// <param name="messages">A collection of messages to be appended to the stream.</param>
// 	/// <param name="cancellationToken">A token to observe while waiting for the operation to complete, allowing for cancellation if needed.</param>
// 	/// <returns>An <see cref="AppendStreamResult"/> containing the outcome of the append operation, including success or failure details.</returns>
// 	public ValueTask<AppendStreamResult> Append(string stream, ExpectedStreamState expectedState, IEnumerable<Message> messages, CancellationToken cancellationToken) =>
// 		Append(new AppendStreamRequest(stream, expectedState, messages), cancellationToken);
//
// 	#endregion
//
// 	#region . Read .
//
// 	internal async IAsyncEnumerable<ReadResult> Read(
// 		LogPosition startPosition,
// 		long limit,
// 		ReadFilter? filter = null,
// 		ReadDirection direction = ReadDirection.Forwards,
// 		HeartbeatOptions? heartbeatOptions = null,
// 		[EnumeratorCancellation] CancellationToken cancellationToken = default
// 	) {
// 		var channel = Channel.CreateUnbounded<ReadResult>(new() {
// 			SingleReader = true,
// 			SingleWriter = true,
// 		});
//
// 		var request = new Contracts.ReadRequest {
// 			Filter        = filter?.Map(),
// 			StartPosition = startPosition,
// 			Limit         = limit,
// 			Direction     = direction.Map(),
// 			Heartbeats    = heartbeatOptions?.Map()
// 		};
//
// 		// Start a task to read from gRPC and write to the channel
// 		using var readTask = Task.Run(
// 			async () => {
// 				try {
// 					using var session = Connection.ServiceClient.ReadSession(request, cancellationToken: cancellationToken);
//
// 					while (await session.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
// 						var response = session.ResponseStream.Current;
//
// 						await (response.ResultCase switch {
// 							Contracts.ReadResponse.ResultOneofCase.Success   => HandleSuccess(response),
// 							Contracts.ReadResponse.ResultOneofCase.Heartbeat => HandleHeartbeat(response),
// 							Contracts.ReadResponse.ResultOneofCase.Failure   => HandleFailure(response),
// 							_ => throw new UnreachableException($"Unexpected result while reading stream: {response.ResultCase}")
// 						});
// 					}
//
// 					channel.Writer.TryComplete();
// 				}
// 				catch (Exception ex) {
// 					channel.Writer.Complete(new Exception($"Error while reading stream: {ex.Message}", ex));
// 				}
// 			},
// 			cancellationToken
// 		);
//
// 		await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
// 			yield return result;
//
// 		yield break;
//
// 		async ValueTask HandleSuccess(Contracts.ReadResponse response) {
// 			foreach (var record in response.Success.Records) {
// 				var mappedRecord = await record.Map(SerializerProvider, cancellationToken).ConfigureAwait(false);
// 				await channel.Writer.WriteAsync(mappedRecord, cancellationToken).ConfigureAwait(false);
// 			}
// 		}
//
// 		async ValueTask HandleHeartbeat(Contracts.ReadResponse response) =>
// 			await channel.Writer.WriteAsync(response.Heartbeat.Map(), cancellationToken).ConfigureAwait(false);
//
// 		ValueTask HandleFailure(Contracts.ReadResponse response) {
// 			Exception ex = response.Failure.ErrorCase switch {
// 				Contracts.ReadFailure.ErrorOneofCase.AccessDenied  => new AccessDeniedException(),
// 				Contracts.ReadFailure.ErrorOneofCase.StreamDeleted => new StreamDeletedException(response.Failure.StreamDeleted.Stream),
// 				_                                                  => new UnreachableException($"Unexpected error while reading stream: {response.Failure.ErrorCase}")
// 			};
//
// 			channel.Writer.TryComplete(ex);
//
// 			return ValueTask.CompletedTask;
// 		}
// 	}
//
// 	public IAsyncEnumerable<ReadResult> Read(
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
// 	public async IAsyncEnumerable<ReadResult> ReadAll(
// 		LogPosition startPosition, long limit, Direction direction,
// 		ReadFilter filter, HeartbeatOptions heartbeatOptions,
// 		[EnumeratorCancellation] CancellationToken cancellationToken = default
// 	) {
// 		var legacyPosition = startPosition.ConvertToLegacyPosition();
// 		var eventFilter    = filter.ConvertToEventFilter(heartbeatOptions.RecordsThreshold);
//
// 		var session = client.LegacyProxy.ReadAllAsync(
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
// 	public async IAsyncEnumerable<ReadResult> ReadStream(
// 		string stream, StreamRevision revision, long limit, Direction direction,
// 		[EnumeratorCancellation] CancellationToken cancellationToken = default
// 	) {
// 		// will throw if stream is not found or deleted
// 		// and ignores all other message types.
// 		var session = LegacyClient.ReadStreamAsync(
// 			direction, stream,
// 			revision.ConvertToLegacyStreamPosition(), limit,
// 			cancellationToken: cancellationToken
// 		);
//
// 		// what about checkpoints (aka heartbeats), only with new protocol?
// 		await foreach (var re in session.ConfigureAwait(false)) {
// 			var record = await LegacyDataConverter
// 				.ConvertToRecord(re, cancellationToken)
// 				.ConfigureAwait(false);
//
// 			yield return record;
// 		}
// 	}
//
// 	public static async IAsyncEnumerable<ReadResult> ReadStream(
// 		this KurrentClient client,
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
// 	public async ValueTask<Record> ReadFirstStreamRecord(string stream, CancellationToken cancellationToken = default) {
// 		try {
// 			var result = LegacyClient.ReadStreamAsync(
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
// 				? await LegacyDataConverter
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
// 	public async ValueTask<Record> ReadLastStreamRecord(this KurrentClient client, string stream, CancellationToken cancellationToken = default) {
// 		try {
// 			var result = LegacyClient.ReadStreamAsync(
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
// 	public static async ValueTask<Record> ReadSingleRecord(this KurrentClient client, LogPosition position, CancellationToken cancellationToken = default) {
// 		try {
// 			ResolvedEvent? re = await client.LegacyProxy
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
// 	internal static async ValueTask<StreamRevision> GetStreamRevision(this KurrentClient client, LogPosition position, CancellationToken cancellationToken = default) {
// 		if (position == LogPosition.Latest)
// 			return StreamRevision.Max;
//
// 		if (position == LogPosition.Unset || position == LogPosition.Earliest)
// 			return StreamRevision.Min;
//
// 		var re = await client.LegacyProxy
// 			.ReadAllAsync(Direction.Forwards, position.ConvertToLegacyPosition(), maxCount: 1, cancellationToken: cancellationToken)
// 			.SingleAsync(cancellationToken)
// 			.ConfigureAwait(false);
//
// 		return re.OriginalEventNumber.ConvertToStreamRevision();
// 	}
//
// 	#endregion
// }


// [PublicAPI]
// public static class KurrentAppender {
// 	public static async ValueTask<MultiStreamAppendResult> Append(this KurrentClient client, IAsyncEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default) {
// 		var (serviceClient, _) = await client.Connect<StreamsServiceClient>(cancellationToken).ConfigureAwait(false);
//
// 		using var session = serviceClient.MultiStreamAppendSession(cancellationToken: cancellationToken);
//
// 		await foreach (var request in requests.WithCancellation(cancellationToken)) {
// 			var records = await request.Messages
// 				.Map(request.Stream, client.SerializerProvider, cancellationToken)
// 				.ToArrayAsync(cancellationToken)
// 				.ConfigureAwait(false);
//
// 			var serviceRequest = new Contracts.AppendStreamRequest {
// 				Stream           = request.Stream,
// 				ExpectedRevision = request.ExpectedState,
// 				Records          = { records }
// 			};
//
// 			cancellationToken.ThrowIfCancellationRequested();
//
// 			await session.RequestStream
// 				.WriteAsync(serviceRequest, cancellationToken)
// 				.ConfigureAwait(false);
// 		}
//
// 		await session.RequestStream.CompleteAsync();
//
// 		var response = await session.ResponseAsync;
//
// 		return response.ResultCase switch {
// 			Contracts.MultiStreamAppendResponse.ResultOneofCase.Success => response.Success.Map(),
// 			Contracts.MultiStreamAppendResponse.ResultOneofCase.Failure => response.Failure.Map()
// 		};
// 	}
//
// 	public static ValueTask<MultiStreamAppendResult> Append(this KurrentClient client, IEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default) =>
// 		 Append(client, requests.ToAsyncEnumerable(), cancellationToken);
//
// 	public static ValueTask<MultiStreamAppendResult> Append(this KurrentClient client, MultiStreamAppendRequest request, CancellationToken cancellationToken = default) =>
// 		Append(client, request.Requests.ToAsyncEnumerable(), cancellationToken);
//
// 	/// <summary>
// 	/// Appends a series of messages to a specified stream in KurrentDB.
// 	/// </summary>
// 	/// <param name="client">The KurrentDBClient instance used to perform the operation.</param>
// 	/// <param name="request">The request object that specifies the stream, expected state, and messages to append.</param>
// 	/// <param name="cancellationToken">A token to cancel the operation if needed.</param>
// 	/// <returns>An <see cref="AppendStreamResult"/> representing the result of the append operation.</returns>
// 	public static async ValueTask<AppendStreamResult> Append(this KurrentClient client, AppendStreamRequest request, CancellationToken cancellationToken) {
// 		var result = await Append(client, [request], cancellationToken).ConfigureAwait(false);
//
// 		return result.Match<AppendStreamResult>(
// 			success => success.First(),
// 			failure => failure.First()
// 		);
// 	}
//
// 	/// <summary>
// 	/// Appends a series of messages to a specified stream while specifying the expected stream state.
// 	/// </summary>
// 	/// <param name="client">The instance of <see cref="KurrentDBClient"/> used to execute the append operation.</param>
// 	/// <param name="stream">The name of the stream to which the messages will be appended.</param>
// 	/// <param name="expectedState">The expected state of the stream to ensure consistency during the append operation.</param>
// 	/// <param name="messages">A collection of messages to be appended to the stream.</param>
// 	/// <param name="cancellationToken">A token to observe while waiting for the operation to complete, allowing for cancellation if needed.</param>
// 	/// <returns>An <see cref="AppendStreamResult"/> containing the outcome of the append operation, including success or failure details.</returns>
// 	public static ValueTask<AppendStreamResult> Append(this KurrentClient client, string stream, ExpectedStreamState expectedState, IEnumerable<Message> messages, CancellationToken cancellationToken) =>
// 		client.Append(new AppendStreamRequest(stream, expectedState, messages), cancellationToken);
// }
