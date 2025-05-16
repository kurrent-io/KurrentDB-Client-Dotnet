using Google.Protobuf;
using KurrentDB.Client.Model;

namespace KurrentDB.Client.SchemaRegistry.Serialization.Protobuf;

public class ProtobufSchemaSerializer(SchemaSerializerOptions options, KurrentRegistryClient schemaRegistry, MessageTypeRegistry typeRegistry, ISchemaExporter schemaExporter)
	: SchemaSerializer(options, schemaRegistry, typeRegistry, schemaExporter) {
	public override SchemaDataFormat DataFormat => SchemaDataFormat.Protobuf;

	protected override ReadOnlyMemory<byte> Serialize(object? value) =>
		value.EnsureValueIsProtoMessage().ToByteArray();

	protected override object Deserialize(ReadOnlyMemory<byte> data, Type resolvedType) =>
		resolvedType.EnsureTypeIsProtoMessage().GetProtoMessageParser().ParseFrom(data.Span);

	protected object Deserialize(Stream data, Type resolvedType) =>
		resolvedType.EnsureTypeIsProtoMessage().GetProtoMessageParser().ParseFrom(data);
}
