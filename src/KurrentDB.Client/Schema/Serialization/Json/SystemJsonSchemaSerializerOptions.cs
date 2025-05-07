using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

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
