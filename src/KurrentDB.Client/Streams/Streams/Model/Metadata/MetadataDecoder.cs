// ReSharper disable InconsistentNaming

using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using JetBrains.Annotations;
using System.Text;
using KurrentDB.Client.Core.Internal.Exceptions;
using static KurrentDB.Client.Constants;

namespace KurrentDB.Client;

/// <summary>
/// Provides methods to encode and decode metadata.
/// </summary>
[PublicAPI]
public static class MetadataDecoder {
	public static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerOptions.Default) {
		DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
		DictionaryKeyPolicy         = JsonNamingPolicy.CamelCase,
		NumberHandling              = JsonNumberHandling.AllowReadingFromString,
		PropertyNameCaseInsensitive = false,
		PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
		UnmappedMemberHandling      = JsonUnmappedMemberHandling.Skip,
		UnknownTypeHandling         = JsonUnknownTypeHandling.JsonNode,
		Converters = {
			new MetadataJsonConverter(),
			new TimeSpanIso8601Converter(),
			new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
		}
	};

	/// Decodes the provided metadata in a read-only byte memory structure into a dictionary
	/// containing key-value pairs of metadata properties.
	/// <param name="metadata">The read-only memory of bytes containing the metadata to decode.</param>
	/// <returns>A dictionary representing the decoded metadata, where keys are strings and values are objects,
	/// or null if the decoding process fails.</returns>
	/// <exception cref="ArgumentException">Thrown when the metadata is not valid JSON or contains unsupported property values.</exception>
	public static Dictionary<string, object?> Decode(ReadOnlyMemory<byte> metadata) {
		try {
			return JsonSerializer.Deserialize<Dictionary<string, object?>>(metadata.Span, JsonSerializerOptions) ?? throw new InvalidOperationException();
		} catch (Exception ex) {
			throw new ArgumentException(
				$"Event metadata must be valid JSON with property values limited to: null, boolean, number, string, Guid, DateTime, TimeSpan, or Base64-encoded byte arrays. " +
				$"Complex objects and arrays are not supported. This limitation will be removed in the next major release. " +
				$"Deserialization failed: {ex.Message}",
				ex
			);
		}
	}
}

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

public class TimeSpanIso8601Converter : JsonConverter<TimeSpan> {
	public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		if (reader.TokenType == JsonTokenType.Null)
			return TimeSpan.Zero;

		if (reader.TokenType != JsonTokenType.String)
			throw new JsonException("Failed to convert ISO8601 TimeSpan from JSON. Expected a string value.");

		var value = reader.GetString();

		return string.IsNullOrEmpty(value) ? TimeSpan.Zero : XmlConvert.ToTimeSpan(value);
	}

	public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) {
		if (value != TimeSpan.Zero || options.DefaultIgnoreCondition is not (JsonIgnoreCondition.WhenWritingDefault or JsonIgnoreCondition.Always))
			writer.WriteStringValue(XmlConvert.ToString(value));
	}
}
