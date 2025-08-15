// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KurrentDB.Client.Core.Internal.Exceptions;
using static KurrentDB.Client.Constants;

namespace KurrentDB.Client;

public class MetadataJsonConverter : JsonConverter<Dictionary<string, object?>> {
	public override Dictionary<string, object?> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		var metadata = new Dictionary<string, object?>();

		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
			if (reader.GetString() is not { } propertyName || string.IsNullOrWhiteSpace(propertyName))
				throw new JsonException("Property name cannot be empty or whitespace");

			reader.Read();

			var value = reader.TokenType switch {
				JsonTokenType.None   => null,
				JsonTokenType.Null   => null,
				JsonTokenType.True   => true,
				JsonTokenType.False  => false,
				JsonTokenType.String => ParseString(reader, propertyName),
				JsonTokenType.Number => ParseNumber(reader),
				_                    => throw new JsonException($"Unsupported metadata value type ({reader.TokenType}) for property '{propertyName}'")
			};

			metadata[propertyName] = value;
		}

		return metadata;

		static object ParseNumber(Utf8JsonReader reader) {
			if (reader.TryGetInt32(out var intValue))
				return intValue;

			if (reader.TryGetInt64(out var longValue))
				return longValue;

			if (reader.TryGetDouble(out var doubleValue))
				return doubleValue;

#if NET48
			return Encoding.UTF8.GetString(reader.ValueSequence.ToArray());
#else
			return Encoding.UTF8.GetString(reader.ValueSpan);
#endif
		}

		static object? ParseString(Utf8JsonReader reader, string propertyName) {
			if (propertyName.Equals(Metadata.SchemaName, StringComparison.OrdinalIgnoreCase)) {
				var value = reader.GetString();
				return string.IsNullOrWhiteSpace(value) ? "" : value;
			}

			if (propertyName.Equals(Metadata.SchemaDataFormat, StringComparison.OrdinalIgnoreCase))
				return Enum.TryParse<SchemaDataFormat>(reader.GetString(), ignoreCase: true, out var format)
					? format
					: SchemaDataFormat.Unspecified;

			if (reader.TryGetGuid(out var guid)) return guid;
			if (reader.TryGetDateTime(out var dateTime)) return dateTime;
			if (reader.TryGetTimeSpan(out var timeSpan)) return timeSpan;
			if (reader.TryGetBytesFromBase64(out var bytes)) return new ReadOnlyMemory<byte>(bytes);

			return reader.GetString();
		}
	}

	public override void Write(Utf8JsonWriter writer, Dictionary<string, object?> value, JsonSerializerOptions options) =>
		JsonSerializer.Serialize(writer, value, options);
}
