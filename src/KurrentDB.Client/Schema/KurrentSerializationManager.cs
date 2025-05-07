using KurrentDB.Client.Model;
using KurrentDB.Client.Schema.Serialization;

namespace KurrentDB.Client.Schema;

public interface IKurrentSerializationManager : ISchemaSerializer {
	bool              SupportsDataFormat(SchemaDataFormat dataFormat);
	ISchemaSerializer GetSerializer(SchemaDataFormat dataFormat);
	ISchemaSerializer GetSerializer(SerializationContext context);
}

public sealed class KurrentSerializationManager : IKurrentSerializationManager {
	public KurrentSerializationManager(IEnumerable<ISchemaSerializer> serializers) =>
		Serializers = serializers.ToDictionary(x => x.DataFormat);

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

	public ISchemaSerializer GetSerializer(SchemaDataFormat dataFormat) =>
		Serializers.TryGetValue(dataFormat, out var serializer)
			? serializer
			: throw new SerializerNotFoundException(dataFormat, Serializers.Keys.ToArray());

	public ISchemaSerializer GetSerializer(SerializationContext context) =>
		GetSerializer(context.SchemaInfo.DataFormat);
}
