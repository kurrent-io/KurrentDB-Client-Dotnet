using Google.Protobuf;
using Kurrent.Client.Model;

namespace Kurrent.Client.SchemaRegistry.Serialization.Protobuf;

public record ProtobufSchemaSerializerOptions : SchemaSerializerOptions {
	public static readonly ProtobufSchemaSerializerOptions Default = new();
}

public class ProtobufSchemaSerializer(ProtobufSchemaSerializerOptions options, SchemaManager schemaManager)
	: SchemaSerializerBase(options, schemaManager) {
	public override SchemaDataFormat DataFormat => SchemaDataFormat.Protobuf;

	protected override ReadOnlyMemory<byte> Serialize(object? value) =>
		value.EnsureValueIsProtoMessage().ToByteArray();

	protected override object Deserialize(ReadOnlyMemory<byte> data, Type resolvedType) =>
		resolvedType.EnsureTypeIsProtoMessage().GetProtoMessageParser().ParseFrom(data.Span);

	protected object Deserialize(Stream data, Type resolvedType) =>
		resolvedType.EnsureTypeIsProtoMessage().GetProtoMessageParser().ParseFrom(data);
}
