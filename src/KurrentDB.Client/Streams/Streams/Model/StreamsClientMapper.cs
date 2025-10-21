using System.Diagnostics;
using Google.Protobuf;
using KurrentDB.Client.Diagnostics;
using KurrentDB.Protocol.V2.Streams;
using static KurrentDB.Client.Constants.Metadata;
using SchemaFormat = KurrentDB.Protocol.V2.Streams.SchemaFormat;

namespace KurrentDB.Client;

static class StreamsClientMapper {
	public static async IAsyncEnumerable<AppendRecord> Map(this IEnumerable<EventData> source) {
		foreach (var message in source)
			yield return await message
				.Map()
				.ConfigureAwait(false);
	}

	public static ValueTask<AppendRecord> Map(this EventData source) {
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

		var record = new AppendRecord {
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

		return new ValueTask<AppendRecord>(record);
	}
}
