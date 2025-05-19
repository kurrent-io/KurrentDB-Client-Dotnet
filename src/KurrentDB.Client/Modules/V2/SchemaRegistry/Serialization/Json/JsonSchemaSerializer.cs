using KurrentDB.Client.Model;

namespace KurrentDB.Client.SchemaRegistry.Serialization.Json;

/// <summary>
/// A serializer class that supports serialization and deserialization of objects
/// with optional use of a protobuf-based formatter.
/// </summary>
public class JsonSchemaSerializer(JsonSchemaSerializerOptions options,  SchemaManager schemaManager, MessageTypeRegistry typeRegistry, ITypeResolver typeResolver)
	: SchemaSerializer(options, schemaManager, typeRegistry, typeResolver) {
	JsonSerializer Serializer { get; } = new(options.JsonSerializerOptions, options.UseProtobufFormatter);

	public override SchemaDataFormat DataFormat => SchemaDataFormat.Json;

	protected override ReadOnlyMemory<byte> Serialize(object? value) =>
		Serializer.Serialize(value);

	protected override object Deserialize(ReadOnlyMemory<byte> data, Type resolvedType) =>
		Serializer.Deserialize(data, resolvedType)!;
}
