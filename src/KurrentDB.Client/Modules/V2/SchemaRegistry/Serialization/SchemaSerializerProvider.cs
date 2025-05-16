using KurrentDB.Client.Model;

namespace KurrentDB.Client.SchemaRegistry.Serialization;

public interface ISchemaSerializerProvider {
	bool SupportsDataFormat(SchemaDataFormat dataFormat);

	ISchemaSerializer GetSerializer(SchemaDataFormat dataFormat);
}

public sealed class SchemaSerializerProvider : ISchemaSerializerProvider {
	public SchemaSerializerProvider(IEnumerable<ISchemaSerializer> serializers) =>
		Serializers = serializers.ToDictionary(x => x.DataFormat);

	Dictionary<SchemaDataFormat, ISchemaSerializer> Serializers { get; }

	public bool SupportsDataFormat(SchemaDataFormat dataFormat) =>
		Serializers.ContainsKey(dataFormat);

	public ISchemaSerializer GetSerializer(SchemaDataFormat dataFormat) =>
		Serializers.TryGetValue(dataFormat, out var serializer)
			? serializer
			: throw new SerializerNotFoundException(dataFormat, Serializers.Keys.ToArray());
}
