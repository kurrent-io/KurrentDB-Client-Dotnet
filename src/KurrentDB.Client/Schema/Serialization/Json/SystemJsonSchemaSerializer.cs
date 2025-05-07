using KurrentDB.Client.Model;

namespace KurrentDB.Client.Schema.Serialization.Json;

/// <summary>
/// A serializer class that supports serialization and deserialization of objects
/// with optional use of a protobuf-based formatter.
/// </summary>
public class SystemJsonSchemaSerializer : SerializerBase {
    public override SchemaDataFormat DataFormat => SchemaDataFormat.Json;

    public SystemJsonSchemaSerializer(IKurrentSchemaManager schemaManager, SystemJsonSchemaSerializerOptions? options = null) : base(schemaManager) {
        options ??= new SystemJsonSchemaSerializerOptions();
        Serializer = new SystemJsonSerializer(options.JsonSerializerOptions, options.UseProtobufFormatter);
    }

    SystemJsonSerializer Serializer { get; }

    protected override ValueTask<ReadOnlyMemory<byte>> Serialize(object? value, SerializationContext context) =>
        new ValueTask<ReadOnlyMemory<byte>>(Serializer.Serialize(value));

    protected override ValueTask<object?> Deserialize(ReadOnlyMemory<byte> data, Type resolvedType, SerializationContext context) =>
	    new ValueTask<object?>(Serializer.Deserialize(data, resolvedType));
}
