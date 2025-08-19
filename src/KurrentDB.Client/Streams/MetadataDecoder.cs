// ReSharper disable InconsistentNaming

using System.Text.Json;
using System.Text.Json.Serialization;

namespace KurrentDB.Client.Streams;

public static class MetadataDecoder {
	public static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerOptions.Default) {
		PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
		DictionaryKeyPolicy         = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = false,
		DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
		UnknownTypeHandling         = JsonUnknownTypeHandling.JsonNode,
		UnmappedMemberHandling      = JsonUnmappedMemberHandling.Skip,
		NumberHandling              = JsonNumberHandling.AllowReadingFromString,
		Converters = {
			new MetadataJsonConverter(),
			new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
		}
	};

	public static Dictionary<string, object?>? Decode(ReadOnlyMemory<byte> metadataBytes) {
		return JsonSerializer.Deserialize<Dictionary<string, object?>>(
			metadataBytes.Span,
			JsonSerializerOptions
		);
	}
}
