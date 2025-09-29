// ReSharper disable InconsistentNaming

using System.Buffers;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using System.Text;
using KurrentDB.Client.Core.Internal.Exceptions;
using static KurrentDB.Client.Constants;
using Enum = System.Enum;
using Type = System.Type;

namespace KurrentDB.Client;

/// <summary>
/// Provides methods to decode metadata.
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
				JsonTokenType.Number => reader.GetDouble(),
				_                    => throw new JsonException($"Unsupported metadata value type ({reader.TokenType}) for property '{propertyName}'")
			};

			metadata[propertyName] = value;
		}

		return metadata;

		static object? ParseString(Utf8JsonReader reader, string propertyName) {
			var value = reader.GetString();

			if (propertyName.Equals(Metadata.SchemaName, StringComparison.OrdinalIgnoreCase))
				return string.IsNullOrWhiteSpace(value) ? "" : value;

			if (propertyName.Equals(Metadata.SchemaFormat, StringComparison.OrdinalIgnoreCase))
				return Enum.TryParse<SchemaDataFormat>(value, ignoreCase: true, out var format)
					? format
					: SchemaDataFormat.Unspecified;

			if (reader.TryGetGuid(out var guid)) return guid;
			if (reader.TryGetDateTime(out var dateTime)) return dateTime;
			if (reader.TryGetTimeSpan(out var timeSpan)) return timeSpan;
			if (reader.TryGetBytesFromBase64(out var bytes)) return new ReadOnlyMemory<byte>(bytes);

			if (DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out var isoDateTime))
				return isoDateTime;

			return value;
		}
	}

	public override void Write(Utf8JsonWriter writer, Dictionary<string, object?> value, JsonSerializerOptions options) =>
		JsonSerializer.Serialize(writer, value, options);
}
