// #pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
//
// using System.Runtime.CompilerServices;
// using Google.Protobuf;
// using JetBrains.Annotations;
// using KurrentDB.Client.Model;
//
// using Contracts = KurrentDB.Protocol.Streams.V2;
//
// namespace KurrentDB.Client;
//
// [PublicAPI]
// public static class KurrentDBAppender {
// 	// static Action<Metadata> SetSystemProperties(KurrentDBClient client, string stream) =>
// 	// 	metadata => {
// 	// 		metadata.Set(SystemMetadataKeys.ClientName, "KurrentDB .NET Client");
// 	// 		metadata.Set(SystemMetadataKeys.ClientVersion, AppVersionInfo.Current.FileVersion);
// 	// 		metadata.Set(SystemMetadataKeys.ConnectionName, client..ConnectionName);
// 	// 		metadata.Set(SystemMetadataKeys.Stream, stream);
// 	// 	};
//
// 	public static async Task<AppendStreamResult> AppendStream(this KurrentDBClient client, AppendStreamRequest request, CancellationToken cancellationToken) {
// 		var eventData = await request.Messages
// 			.ToEventDataAsync(SetSystemProperties(client, request.Stream), client.SerializerProvider, cancellationToken)
// 			.ToArrayAsync(cancellationToken);
//
// 		var result = await client
// 			.AppendToStreamAsync(request.Stream, request.ExpectedState, eventData, cancellationToken: cancellationToken)
// 			.ConfigureAwait(false);
//
// 		return new AppendStreamSuccess {
// 			Stream         = request.Stream,
// 			Position       = (long)result.LogPosition.CommitPosition,
// 			StreamRevision = result.NextExpectedStreamState.ToInt64()
// 		};
// 	}
//
// 	public static async ValueTask<MultiStreamAppendResult> MultiStreamAppend(
// 		this KurrentDBClient client, MultiStreamAppendRequest request, CancellationToken cancellationToken = default
// 	) {
// 		var streamsClient = await client
// 			.CreateServiceClient<Contracts.StreamsService.StreamsServiceClient>(cancellationToken)
// 			.ConfigureAwait(false);
//
// 		var reqs = new List<Contracts.AppendStreamRequest>();
//
// 		foreach (var appendStreamRequest in request.Requests) {
// 			var records = await ConvertMessages(appendStreamRequest.Messages, SetSystemProperties(client, appendStreamRequest.Stream), cancellationToken)
// 				.ToArrayAsync(cancellationToken: cancellationToken)
// 				.ConfigureAwait(false);
//
// 			reqs.Add(new Contracts.AppendStreamRequest {
// 				Stream           = appendStreamRequest.Stream,
// 				ExpectedRevision = appendStreamRequest.ExpectedState.ToInt64(),
// 				Records          = { records }
// 			});
// 		}
//
// 		var result = await streamsClient.MultiStreamAppendAsync(
// 			new Contracts.MultiStreamAppendRequest { Input = { reqs } },
// 			cancellationToken: cancellationToken
// 		);
//
// 		return result.ResultCase switch {
// 			Contracts.MultiStreamAppendResponse.ResultOneofCase.Success => new AppendStreamSuccesses(
// 				result.Success.Output.Select(x => new AppendStreamSuccess {
// 					Stream         = x.Stream,
// 					Position       = x.Position,
// 					StreamRevision = x.StreamRevision
// 				})
// 			),
// 			Contracts.MultiStreamAppendResponse.ResultOneofCase.Failure => new AppendStreamFailures(
// 				result.Failure.Output.Select(x => new AppendStreamFailure {
// 					Stream = x.Stream,
// 					Error  = new Exception(x.ErrorCase.ToString()) // lol just to test it.
// 				})
// 			)
// 		};
//
// 		async IAsyncEnumerable<Contracts.AppendRecord> ConvertMessages(IEnumerable<Message> messages, Action<Metadata> updateMetadata, [EnumeratorCancellation] CancellationToken ct) {
// 			foreach (var message in messages) {
// 				updateMetadata(message.Metadata);
// 				yield return await ConvertMessage(message);
// 			}
//
// 			yield break;
//
// 			async ValueTask<Contracts.AppendRecord> ConvertMessage(Message message) {
// 				var data = await client.SerializerProvider
// 					.GetSerializer(message.DataFormat)
// 					.Serialize(message, ct)
// 					.ConfigureAwait(false);
//
// 				// we need to remove the schema name from the
// 				// metadata as it is not required in the end.
// 				message.Metadata.Remove(SystemMetadataKeys.SchemaName);
//
// 				return new Contracts.AppendRecord {
// 					RecordId   = Uuid.FromGuid(message.RecordId).ToString(),
// 					Data       = ByteString.CopyFrom(data.Span),
// 					Properties = { message.Metadata.MapToDynamicMapField() }
// 				};
// 			}
// 		}
// 	}
//
// 	public static ValueTask<MultiStreamAppendResult> MultiStreamAppend(
// 		this KurrentDBClient client, IEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default
// 	) => client.MultiStreamAppend(new MultiStreamAppendRequest { Requests = requests.ToList() }, cancellationToken);
//
// 	public static Task<AppendStreamResult> AppendStream(
// 		this KurrentDBClient client, string stream, StreamState expectedState, IEnumerable<Message> messages, CancellationToken cancellationToken
// 	) => client.AppendStream(new AppendStreamRequest(stream, expectedState, messages), cancellationToken);
// }
