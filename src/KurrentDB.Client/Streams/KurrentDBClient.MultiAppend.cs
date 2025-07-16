using Google.Protobuf;
using KurrentDB.Client.Core.Serialization;
using KurrentDB.Protocol.V2;

namespace KurrentDB.Client;

public partial class KurrentDBClient {
	/// <summary>
	/// Appends events asynchronously to a stream. Messages are serialized using default or custom serialization configured through <see cref="KurrentDBClientSettings"/>
	/// </summary>
	/// <param name="requests">Messages to append to the stream.</param>
	/// <param name="options">Optional settings for the append operation, e.g. deadline, user credentials etc.</param>
	/// <param name="token">The optional <see cref="System.Threading.CancellationToken"/>.</param>
	/// <returns></returns>
	public Task<MultiStreamWriteResult> MultiAppend(
		IEnumerable<AppendRequest> requests,
		AppendToStreamOptions? options = null,
		CancellationToken token = default
	) {
		return MultiAppendInternal(options, requests, token).AsTask();
	}

	async ValueTask<MultiStreamWriteResult> MultiAppendInternal(
		AppendToStreamOptions? options, IEnumerable<AppendRequest> requests, CancellationToken token
	) {
		var channelInfo = await GetChannelInfo(token).ConfigureAwait(false);
		var client = new StreamsService.StreamsServiceClient(channelInfo.CallInvoker).MultiStreamAppendSession(
			KurrentDBCallOptions.CreateStreaming(
				Settings,
				userCredentials: Settings.DefaultCredentials,
				cancellationToken: token
			)
		);

		foreach (var request in GetRequests(options, requests, token))
			await client.RequestStream.WriteAsync(request).ConfigureAwait(false);

		await client.RequestStream.CompleteAsync().ConfigureAwait(false);
		return new MultiStreamWriteResult(await client.ResponseAsync);
	}

	IEnumerable<AppendStreamRequest> GetRequests(
		AppendToStreamOptions? options,
		IEnumerable<AppendRequest> requests,
		CancellationToken token
	) {
		foreach (var request in requests) {
			token.ThrowIfCancellationRequested();

			var serializationContext =
				new MessageSerializationContext(MessageTypeNamingResolutionContext.FromStreamName(request.StreamName));

			var messages = _messageSerializer.With(options?.SerializationSettings)
				.Serialize(request.Messages, serializationContext);


			yield return new AppendStreamRequest {
				Stream           = request.StreamName,
				ExpectedRevision = request.ExpectedState.ToInt64(),
				Records = {
					messages.Select(x => new AppendRecord {
							RecordId = x.MessageId.ToString(),
							Data     = ByteString.CopyFrom(x.Data.Span),
						}
					)
				}
			};
		}
	}
}
