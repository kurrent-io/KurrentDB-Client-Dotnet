using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kurrent.Client.Registry;
using KurrentDB.Diagnostics.Tracing;
using JsonSerializer = Kurrent.Client.Schema.Serialization.Json.JsonSerializer;

namespace Kurrent.Client.Streams;

/// <summary>
/// The default metadata decoder that deserializes metadata from JSON-encoded byte arrays.
/// This allows for backward compatibility with previous metadata formats.
/// Only booleans, Guids, DateTimes, TimeSpans, and byte arrays (from base64 encoded strings) will be typed, numbers will be deserialized as strings.
/// </summary>
[PublicAPI]
public sealed class DefaultMetadataDecoder : MetadataDecoder {
    static readonly JsonSerializer Serializer = new();

    protected override Metadata DecodeCore(ReadOnlyMemory<byte> bytes, MetadataDecoderContext context) =>
        Serializer.Deserialize<Metadata>(bytes) ?? new();
}

class MetadataJsonConverter : JsonConverter<Metadata> {
    public override Metadata Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var metadata = new Metadata();

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
                JsonTokenType.Number => Encoding.UTF8.GetString(reader.ValueSpan),
                _                    => throw new JsonException($"Unsupported metadata value type ({reader.TokenType}) for property '{propertyName}'")
            };

            metadata.With(propertyName, value);
        }

        return metadata;

        static object? ParseString(Utf8JsonReader reader, string propertyName) {
            if (propertyName.Equals(SystemMetadataKeys.SchemaName, StringComparison.OrdinalIgnoreCase)) {
                var value = reader.GetString();
                return string.IsNullOrWhiteSpace(value) ? SchemaName.None : SchemaName.From(value);
            }

            if (propertyName.Equals(SystemMetadataKeys.SchemaDataFormat, StringComparison.OrdinalIgnoreCase))
                return Enum.TryParse<SchemaDataFormat>(reader.GetString(), ignoreCase: true, out var format)
                    ? format : SchemaDataFormat.Unspecified;

            if (propertyName.Equals(SystemMetadataKeys.SchemaVersionId, StringComparison.OrdinalIgnoreCase))
                return reader.TryGetGuid(out var versionId) && versionId != Guid.Empty
                    ? SchemaVersionId.From(versionId) : SchemaVersionId.None;

            if (propertyName.Equals(TraceConstants.TraceId, StringComparison.OrdinalIgnoreCase))
	            return ActivityTraceId.CreateFromString(reader.GetString());

            if (propertyName.Equals(TraceConstants.SpanId, StringComparison.OrdinalIgnoreCase))
	            return ActivitySpanId.CreateFromString(reader.GetString());

            if (reader.TryGetGuid(out var guid)) return guid;
            if (reader.TryGetDateTime(out var dateTime)) return dateTime;
            if (reader.TryGetTimeSpan(out var timeSpan)) return timeSpan;
            if (reader.TryGetBytesFromBase64(out var bytes)) return new ReadOnlyMemory<byte>(bytes);

            return reader.GetString();
        }
    }

    public override void Write(Utf8JsonWriter writer, Metadata value, JsonSerializerOptions options) =>
        System.Text.Json.JsonSerializer.Serialize(writer, value.Dictionary, options);
}

// /// <summary>
// /// The default metadata decoder that deserializes metadata from JSON-encoded byte arrays.
// /// This allows for backward compatibility with previous metadata formats.
// /// </summary>
// [PublicAPI]
// public sealed class DefaultMetadataDecoder : MetadataDecoder {
//     static readonly SchemaRegistry.Serialization.Json.JsonSerializer Serializer = new();
//
//     protected override Metadata DecodeCore(ReadOnlyMemory<byte> bytes, MetadataDecoderContext context) {
//         return Serializer.Deserialize<Dictionary<string, object?>>(bytes) is { Count: > 0 } deserialized
//             ? new(deserialized.ToDictionary(x => x.Key, static kvp => EvolveValue(kvp)))
//             : new();
//
//         static object? EvolveValue(KeyValuePair<string, object?> kvp) => kvp switch {
//             { Key: SystemMetadataKeys.SchemaDataFormat, Value: not null } => Enum.Parse<SchemaDataFormat>(kvp.Value.ToString()!, ignoreCase: true),
//             { Key: SystemMetadataKeys.SchemaName,       Value: not null } => SchemaName.From(kvp.Value.ToString()!),
//             _                                                             => kvp.Value?.ToString()
//         };
//     }
// }
