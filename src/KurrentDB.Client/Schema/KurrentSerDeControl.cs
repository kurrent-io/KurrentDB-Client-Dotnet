using KurrentDB.Client.Model;
using KurrentDB.Client.Schema.Serialization;

namespace KurrentDB.Client.Schema;

public class KurrentSerDeControl : ISchemaSerializer {
    public KurrentSerDeControl(IEnumerable<ISchemaSerializer> serializers) =>
        Serializers = serializers.ToDictionary(s => s.DataFormat);

    Dictionary<SchemaDataFormat, ISchemaSerializer> Serializers { get; }

    public SchemaDataFormat DataFormat => SchemaDataFormat.Unspecified;

    public ValueTask<ReadOnlyMemory<byte>> Serialize(object? value, SerializationContext context) =>
        Serializers.TryGetValue(context.SchemaInfo.DataFormat, out var serializer)
            ? serializer.Serialize(value, context)
            : throw new SerializerNotFoundException(context.SchemaInfo.DataFormat, Serializers.Keys.ToArray());

    public ValueTask<object?> Deserialize(ReadOnlyMemory<byte> data, SerializationContext context) =>
        Serializers.TryGetValue(context.SchemaInfo.DataFormat, out var serializer)
            ? serializer.Deserialize(data, context)
            : throw new SerializerNotFoundException(context.SchemaInfo.DataFormat, Serializers.Keys.ToArray());

    public bool SupportsDataFormat(SchemaDataFormat dataFormat) =>
        Serializers.ContainsKey(dataFormat);

    public ISchemaSerializer GetSchemaSerializer(SchemaDataFormat dataFormat) =>
        Serializers.TryGetValue(dataFormat, out var serializer)
            ? serializer
            : throw new SerializerNotFoundException(dataFormat, Serializers.Keys.ToArray());

    public ISchemaSerializer GetSchemaSerializer(SerializationContext context) =>
        GetSchemaSerializer(context.SchemaInfo.DataFormat);
}