using Google.Protobuf;
using KurrentDB.Client.Model;

namespace KurrentDB.Client.SchemaRegistry.Serialization.Protobuf;

public class ProtobufSchemaSerializer(SchemaSerializerOptions options, SchemaManager schemaManager, MessageTypeRegistry typeRegistry, ITypeResolver typeResolver)
	: SchemaSerializer(options, schemaManager, typeRegistry, typeResolver) {
	public override SchemaDataFormat DataFormat => SchemaDataFormat.Protobuf;

	protected override ReadOnlyMemory<byte> Serialize(object? value) =>
		value.EnsureValueIsProtoMessage().ToByteArray();

	protected override object Deserialize(ReadOnlyMemory<byte> data, Type resolvedType) =>
		resolvedType.EnsureTypeIsProtoMessage().GetProtoMessageParser().ParseFrom(data.Span);

	protected object Deserialize(Stream data, Type resolvedType) =>
		resolvedType.EnsureTypeIsProtoMessage().GetProtoMessageParser().ParseFrom(data);
}
