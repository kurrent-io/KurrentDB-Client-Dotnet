using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace KurrentDB.Client.SchemaRegistry.Serialization.Json;

[PublicAPI]
public record JsonSchemaSerializerOptions : SchemaSerializerOptions {
	public static readonly JsonSchemaSerializerOptions Default = new();

	public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new(JsonSerializerOptions.Default) {
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

	public JsonSerializerOptions JsonSerializerOptions { get; init; } = DefaultJsonSerializerOptions;
	public bool                  UseProtobufFormatter  { get; init; } = true;
}
