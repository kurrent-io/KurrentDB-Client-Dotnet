using System.Diagnostics;
using Google.Protobuf;
using KurrentDB.Client.Diagnostics;
using KurrentDB.Protocol.V2.Streams;
using static KurrentDB.Client.Constants.Metadata;
using SchemaFormat = KurrentDB.Protocol.V2.Streams.SchemaFormat;
using Contracts = KurrentDB.Protocol.V2.Streams;

namespace KurrentDB.Client;

static class StreamsClientMapper {
	public static async IAsyncEnumerable<Contracts.AppendRecord> Map(this IEnumerable<EventData> source) {
		foreach (var message in source)
			yield return await message
				.Map()
				.ConfigureAwait(false);
	}

	public static async ValueTask<Contracts.AppendRecord> Map(this AppendRecord source) {
		var record = await source.Record.Map().ConfigureAwait(false);
		record.Stream = source.Stream;
		return record;
	}

	public static ValueTask<Contracts.AppendRecord> Map(this EventData source) {
		Dictionary<string, string> metadata;

		try {
			metadata = source.Metadata.Decode() ?? new Dictionary<string, string>();
		} catch (Exception ex) {
			throw new ArgumentException(
				"Failed to decode event metadata. The metadata may be missing, malformed, or not in valid JSON format. Please verify the event's metadata structure.",
				ex
			);
		}

		metadata.InjectTracingContext(Activity.Current);

		var record = new Contracts.AppendRecord {
			RecordId = source.EventId.ToString(),
			Data     = ByteString.CopyFrom(source.Data.Span),
			Schema = new SchemaInfo {
				Format = source.ContentType is ContentTypes.ApplicationJson
					? SchemaFormat.Json
					: SchemaFormat.Bytes,
				Name = source.Type
			},
			Properties = { metadata.MapToMapValue() }
		};

		return new ValueTask<Contracts.AppendRecord>(record);
	}
}
