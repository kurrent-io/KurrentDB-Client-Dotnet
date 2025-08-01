using Kurrent.Client.Schema.Serialization.Json;
using Kurrent.Client.Streams;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Kurrent.Client.Schema;

public interface ISchemaExporter {
	string Export(Type messageType, SchemaDataFormat dataFormat);
}

sealed class NJsonSchemaExporter(SystemTextJsonSchemaGeneratorSettings? settings = null) : ISchemaExporter {
	readonly SystemTextJsonSchemaGeneratorSettings _settings = settings ?? new() {
		SerializerOptions = JsonSchemaSerializerOptions.DefaultJsonSerializerOptions
	};

	public static ISchemaExporter Instance => new NJsonSchemaExporter();

	public string Export(Type messageType, SchemaDataFormat dataFormat) {
		if (dataFormat != SchemaDataFormat.Json)
			throw new NotSupportedException($"Unsupported schema data format. Expected {SchemaDataFormat.Json} but got {dataFormat}.");

		var schema = JsonSchema.FromType(messageType, _settings);

		return schema.ToJson();
	}
}
