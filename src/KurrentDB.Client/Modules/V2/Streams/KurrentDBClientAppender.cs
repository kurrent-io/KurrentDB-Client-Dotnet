#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

using System.Runtime.CompilerServices;
using Google.Protobuf;
using Grpc.Net.Client;
using KurrentDB.Client.Model;
using KurrentDB.Client.Schema.Serialization;
using KurrentDB.Protocol.Streams.V2;

using static KurrentDB.Protocol.Streams.V2.StreamsService;

using AppendStreamFailure = KurrentDB.Client.Model.AppendStreamFailure;
using AppendStreamSuccess = KurrentDB.Client.Model.AppendStreamSuccess;
using AppendStreamRequest = KurrentDB.Client.Model.AppendStreamRequest;

namespace KurrentDB.Client;

public record LegacyAppendOptions {
	/// <summary>
	/// Whether or not to immediately throw a <see cref="WrongExpectedVersionException"/> when an append fails.
	/// </summary>
	public bool ThrowOnAppendFailure { get; init; }

	/// <summary>
	/// The batch size, in bytes.
	/// </summary>
	public int BatchAppendSize { get; init; }

	/// <summary>
	/// A callback function to extract the authorize header value from the <see cref="UserCredentials"/> used in the operation.
	/// </summary>
	public Func<UserCredentials, CancellationToken, ValueTask<string>> GetAuthenticationHeaderValue { get; init; } = null!;

	public TimeSpan?        Deadline        { get; init; } = null;
	public UserCredentials? UserCredentials { get; init; } = null;
}

public static class KurrentDBClientAppender {
	static LegacyProtocolMapper LegacyMapper { get; set; } = null!;
	static ISchemaSerializer        SchemaSerializer { get; set; } = null!;
	static IMetadataDecoder         MetadataDecoder  { get; set; } = null!;

	public static async Task<AppendStreamResult> AppendStream(
		this KurrentDBClient client,
		string streamName,
		StreamState expectedState,
		IEnumerable<Message> messages,
		Func<LegacyAppendOptions, LegacyAppendOptions>? configureOptions = null,
		CancellationToken cancellationToken = default
	) {
		var legacyOptions = configureOptions?.Invoke(new LegacyAppendOptions());

		var eventData = await LegacyMapper.ConvertMessagesToEventDataAsync(
			streamName,
			messages,
			metadata => {
				// metadata.Set(HeaderKeys.ProducerId, "");
				// metadata.Set(HeaderKeys.ProducerRequestId, "");
			},
			cancellationToken
		).ToArrayAsync(cancellationToken);

		var result = await client
			.AppendToStreamAsync(streamName, expectedState, eventData, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		// use oneof

		return new AppendStreamSuccess {
			Stream         = streamName,
			Position       = (long)result.LogPosition.CommitPosition,
			StreamRevision = result.NextExpectedStreamState.ToInt64()
		};
	}

	public static async ValueTask<MultiStreamAppendResult> MultiStreamAppend(
		this KurrentDBClient client,
		IEnumerable<AppendStreamRequest> requests,
		CancellationToken cancellationToken = default
	) {
		var streamsClient = new StreamsServiceClient(GrpcChannel.ForAddress(""));

		Action<Metadata> configureMetadata = metadata => {
			// metadata.Set(HeaderKeys.ProducerId, "");
		};

		var reqs = new List<KurrentDB.Protocol.Streams.V2.AppendStreamRequest>();

		foreach (var request in requests) {
			var records = await ConvertMessages(request.Stream, request.Messages, configureMetadata, cancellationToken)
				.ToArrayAsync(cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			reqs.Add(new KurrentDB.Protocol.Streams.V2.AppendStreamRequest {
				Stream           = request.Stream,
				ExpectedRevision = request.ExpectedRevision,
				Records          = { records }
			});
		}

		var result = await streamsClient.MultiStreamAppendAsync(new() { Input = { reqs } }, cancellationToken: cancellationToken);

		return result.ResultCase switch {
			MultiStreamAppendResponse.ResultOneofCase.Success => new AppendStreamSuccesses(
				result.Success.Output.Select(x => new AppendStreamSuccess {
					Stream         = x.Stream,
					Position       = x.Position,
					StreamRevision = x.StreamRevision
				})
			),
			MultiStreamAppendResponse.ResultOneofCase.Failure => new AppendStreamFailures(
				result.Failure.Output.Select(x => new AppendStreamFailure {
					Stream = x.Stream,
					Error  = new Exception(x.ErrorCase.ToString()) // lol just to test it.
				})
			)
		};

		async IAsyncEnumerable<AppendRecord> ConvertMessages(string stream, IEnumerable<Message> messages, Action<Metadata> prepareMetadata, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
			foreach (var message in messages) {
				prepareMetadata(message.Metadata);
				yield return await ConvertMessage(message, stream, cancellationToken);
			}

			yield break;

			async ValueTask<AppendRecord> ConvertMessage(Message message, string stream, CancellationToken cancellationToken) {
				var data = await SchemaSerializer
					.Serialize(message.Value, new(message.Metadata, stream, cancellationToken))
					.ConfigureAwait(false);

				// after serialization, all the schema info should already be in the metadata
				// - SchemaName
				// - SchemaDataFormat
				// - SchemaVersionId

				return new KurrentDB.Protocol.Streams.V2.AppendRecord {
					RecordId   = Uuid.FromGuid(message.RecordId).ToString(),
					Data       = ByteString.CopyFrom(data.Span),
					Properties = { message.Metadata.MapToDynamicMapField() }
				};
			}
		}
	}
}
