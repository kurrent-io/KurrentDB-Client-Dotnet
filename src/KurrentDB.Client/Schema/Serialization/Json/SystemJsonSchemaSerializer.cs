using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Google.Protobuf;
using JetBrains.Annotations;
using KurrentDB.Client.Extensions;
using KurrentDB.Client.Model;
using KurrentDB.Client.Schema.Serialization.Protobuf;

namespace KurrentDB.Client.Schema.Serialization.Json;

[PublicAPI]
public record SystemJsonSchemaSerializerOptions {
    public static readonly JsonSerializerOptions Default = new(JsonSerializerOptions.Default) {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy         = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        UnknownTypeHandling         = JsonUnknownTypeHandling.JsonNode,
        UnmappedMemberHandling      = JsonUnmappedMemberHandling.Skip,
        NumberHandling              = JsonNumberHandling.AllowReadingFromString,
        Converters                  = {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public SystemJsonSchemaSerializerOptions(JsonSerializerOptions jsonSerializerOptions, bool useProtobufFormatter = true) {
        JsonSerializerOptions = jsonSerializerOptions;
        UseProtobufFormatter  = useProtobufFormatter;
    }

    public SystemJsonSchemaSerializerOptions(Action<JsonSerializerOptions> configure, bool useProtobufFormatter = true) {
	    JsonSerializerOptions = new JsonSerializerOptions(Default);
        configure(JsonSerializerOptions);

        UseProtobufFormatter  = useProtobufFormatter;
    }

    public SystemJsonSchemaSerializerOptions(bool useProtobufFormatter) {
        JsonSerializerOptions = Default;
        UseProtobufFormatter  = useProtobufFormatter;
    }

    public SystemJsonSchemaSerializerOptions() { }

    public JsonSerializerOptions JsonSerializerOptions { get; init; } = Default;
    public bool                  UseProtobufFormatter  { get; init; } = true;
}

/// <summary>
/// A serializer class that supports serialization and deserialization of objects
/// with optional use of a protobuf-based formatter.
/// </summary>
public class SystemJsonSchemaSerializer : SerializerBase {
    public override SchemaDataFormat DataFormat => SchemaDataFormat.Json;

    public SystemJsonSchemaSerializer(SystemJsonSchemaSerializerOptions? options = null, KurrentSchemaControl? schemaControl = null) : base(schemaControl ?? new KurrentSchemaControl()) {
        options ??= new SystemJsonSchemaSerializerOptions();
        Serializer = new SystemJsonSerializer(options.JsonSerializerOptions, options.UseProtobufFormatter);
    }

    SystemJsonSerializer Serializer { get; }

    protected override ValueTask<ReadOnlyMemory<byte>> Serialize(object? value, SerializationContext context) =>
        new ValueTask<ReadOnlyMemory<byte>>(Serializer.Serialize(value));

    protected override ValueTask<object?> Deserialize(ReadOnlyMemory<byte> data, Type resolvedType, SerializationContext context) =>
	    new ValueTask<object?>(Serializer.Deserialize(data, resolvedType));
}

/// <summary>
/// A serializer class that supports serialization and deserialization of objects
/// with optional use of a protobuf-based formatter.
/// </summary>
public class SystemJsonSerializer(JsonSerializerOptions? options = null, bool useProtobufFormatter = true) {
    static readonly JsonParser ProtoJsonParser = new(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

    JsonSerializerOptions Options              { get; } = options ?? SystemJsonSchemaSerializerOptions.Default;
    bool                  UseProtobufFormatter { get; } = useProtobufFormatter;

    public ReadOnlyMemory<byte> Serialize(object? value) {
        var bytes = value is not IMessage protoMessage || !UseProtobufFormatter
            ? JsonSerializer.SerializeToUtf8Bytes(value, Options)
            : ProtobufToUtf8JsonBytes(protoMessage);

        return bytes;

        static ReadOnlyMemory<byte> ProtobufToUtf8JsonBytes(IMessage message) {
            return Encoding.UTF8.GetBytes(
                RemoveWhitespacesExceptInQuotes(JsonFormatter.Default.Format(message))
            );

            // simply because protobuf is so stupid that it adds spaces
            // between property names and values. absurd...
            static string RemoveWhitespacesExceptInQuotes(string json) {
                var inQuotes = false;

                var result = new StringBuilder(json.Length);

                foreach (var c in json) {
                    if (c == '\"') {
                        inQuotes = !inQuotes;
                        result.Append(c); // Always include the quote characters
                    } else if (inQuotes || (!inQuotes && !char.IsWhiteSpace(c)))
                        result.Append(c);
                }

                return result.ToString();
            }
        }
    }

    public object? Deserialize(ReadOnlyMemory<byte> data, Type type) {
        var value = !type.IsMissing()
            ? !type.IsProtoMessage() || !UseProtobufFormatter
                ? JsonSerializer.Deserialize(data.Span, type, Options)
                : ProtoJsonParser.Parse(Encoding.UTF8.GetString(data.Span.ToArray()), type.GetProtoMessageDescriptor())
            : JsonSerializer.Deserialize<JsonNode>(data.Span, Options);

        return value;
    }

    public async ValueTask<object?> Deserialize(Stream data, Type type, CancellationToken cancellationToken = default) {
        var value = !type.IsMissing()
            ? !type.IsProtoMessage() || !UseProtobufFormatter
                ? await JsonSerializer.DeserializeAsync(data, type, Options, cancellationToken).ConfigureAwait(false)
                : ProtoJsonParser.Parse(await DeserializeToJson(data, cancellationToken).ConfigureAwait(false), type.GetProtoMessageDescriptor())
            : await JsonSerializer.DeserializeAsync<JsonNode>(data, Options, cancellationToken).ConfigureAwait(false);

        return value;

        static async Task<string> DeserializeToJson(Stream stream, CancellationToken cancellationToken) {
            using var reader = new StreamReader(stream, Encoding.UTF8);

#if NET48
	        return await reader.ReadToEndAsync().ConfigureAwait(false);
#else
            return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
#endif
        }
    }
}