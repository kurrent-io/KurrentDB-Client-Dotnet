using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Google.Protobuf;
using Kurrent.Client.SchemaRegistry.Serialization.Protobuf;

namespace Kurrent.Client.SchemaRegistry.Serialization.Json;

/// <summary>
/// A serializer class that supports serialization and deserialization of objects
/// with optional use of a protobuf-based formatter.
/// </summary>
public class JsonSerializer(JsonSerializerOptions? options = null, bool useProtoFormatter = true) {
	static readonly JsonParser ProtoJsonParser = new(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

	JsonSerializerOptions Options           { get; } = options ?? JsonSchemaSerializerOptions.DefaultJsonSerializerOptions;
	bool                  UseProtoFormatter { get; } = useProtoFormatter;

	public ReadOnlyMemory<byte> Serialize(object? value) {
		var bytes = value is not IMessage protoMessage || !UseProtoFormatter
			? System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(value, Options)
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
                var result   = new StringBuilder(json.Length);

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
			? !type.IsProtoMessage() || !UseProtoFormatter
				? System.Text.Json.JsonSerializer.Deserialize(data.Span, type, Options)
				: ProtoJsonParser.Parse(Encoding.UTF8.GetString(data.Span.ToArray()), type.GetProtoMessageDescriptor())
			: System.Text.Json.JsonSerializer.Deserialize<JsonNode>(data.Span, Options);

		return value;
	}

	public async ValueTask<object?> Deserialize(Stream data, Type type, CancellationToken cancellationToken = default) {
		var value = !type.IsMissing()
			? !type.IsProtoMessage() || !UseProtoFormatter
				? await System.Text.Json.JsonSerializer.DeserializeAsync(data, type, Options, cancellationToken).ConfigureAwait(false)
				: ProtoJsonParser.Parse(await DeserializeToJson(data, cancellationToken).ConfigureAwait(false), type.GetProtoMessageDescriptor())
			: await System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(data, Options, cancellationToken).ConfigureAwait(false);

		return value;

		static async Task<string> DeserializeToJson(Stream stream, CancellationToken cancellationToken) {
			using var reader = new StreamReader(stream, Encoding.UTF8);
			return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
		}
	}
}
