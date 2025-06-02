// #pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
//
// using KurrentDB.Client.Model;
// using static KurrentDB.Protocol.Streams.V2.StreamsService;
// using Contracts = KurrentDB.Protocol.Streams.V2;
//
// namespace KurrentDB.Client;
//
// [PublicAPI]
// public static class KurrentDBAppender {
// 	public static async ValueTask<MultiStreamAppendResult> Append(this KurrentDBClient client, IAsyncEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default) {
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
// 				ExpectedRevision = request.ExpectedState.ToInt64(),
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
// 	public static ValueTask<MultiStreamAppendResult> Append(this KurrentDBClient client, IEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default) =>
// 		 Append(client, requests.ToAsyncEnumerable(), cancellationToken);
//
// 	public static ValueTask<MultiStreamAppendResult> Append(this KurrentDBClient client, MultiStreamAppendRequest request, CancellationToken cancellationToken = default) =>
// 		Append(client, request.Requests.ToAsyncEnumerable(), cancellationToken);
//
// 	/// <summary>
// 	/// Appends a series of messages to a specified stream in KurrentDB.
// 	/// </summary>
// 	/// <param name="client">The KurrentDBClient instance used to perform the operation.</param>
// 	/// <param name="request">The request object that specifies the stream, expected state, and messages to append.</param>
// 	/// <param name="cancellationToken">A token to cancel the operation if needed.</param>
// 	/// <returns>An <see cref="AppendStreamResult"/> representing the result of the append operation.</returns>
// 	public static async ValueTask<AppendStreamResult> Append(this KurrentDBClient client, AppendStreamRequest request, CancellationToken cancellationToken) {
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
// 	public static ValueTask<AppendStreamResult> Append(this KurrentDBClient client, string stream, StreamState expectedState, IEnumerable<Message> messages, CancellationToken cancellationToken) =>
// 		client.Append(new AppendStreamRequest(stream, expectedState, messages), cancellationToken);
// }
