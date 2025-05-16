using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace KurrentDB.Client.SchemaRegistry.Serialization.Json;

[PublicAPI]
public record JsonSchemaSerializerOptions : SchemaSerializerOptions {
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

	public JsonSerializerOptions JsonSerializerOptions { get; init; } = Default;
	public bool                  UseProtobufFormatter  { get; init; } = true;
}
