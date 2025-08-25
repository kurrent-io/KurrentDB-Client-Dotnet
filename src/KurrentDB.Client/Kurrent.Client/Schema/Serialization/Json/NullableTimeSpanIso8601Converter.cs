using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace Kurrent.Client.Schema.Serialization.Json;

public class NullableTimeSpanIso8601Converter : JsonConverter<TimeSpan?> {
    public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Failed to convert ISO8601 TimeSpan from JSON. Expected a string value.");

        var value = reader.GetString();

        return string.IsNullOrEmpty(value) ? null : XmlConvert.ToTimeSpan(value);
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options) {
        if (value is not { } duration) {
            if (options.DefaultIgnoreCondition is not (JsonIgnoreCondition.WhenWritingNull or JsonIgnoreCondition.Always))
                writer.WriteNullValue();
        }
        else if (duration != TimeSpan.Zero || options.DefaultIgnoreCondition is not (JsonIgnoreCondition.WhenWritingDefault or JsonIgnoreCondition.Always))
            writer.WriteStringValue(XmlConvert.ToString(duration));
    }
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
