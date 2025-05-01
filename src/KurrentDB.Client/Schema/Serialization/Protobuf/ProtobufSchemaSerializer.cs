using Google.Protobuf;
using KurrentDB.Client.Model;

namespace KurrentDB.Client.Schema.Serialization.Protobuf;

public class ProtobufSchemaSerializer(KurrentSchemaControl schemaControl) : SerializerBase(schemaControl) {
    public override SchemaDataFormat DataFormat => SchemaDataFormat.Protobuf;

    protected override ValueTask<ReadOnlyMemory<byte>> Serialize(object? value, SerializationContext context) =>
        new(new ReadOnlyMemory<byte>(value.EnsureValueIsProtoMessage().ToByteArray()));

    protected override ValueTask<object?> Deserialize(ReadOnlyMemory<byte> data, Type resolvedType, SerializationContext context) =>
        new(resolvedType.EnsureTypeIsProtoMessage().GetProtoMessageParser().ParseFrom(data.Span));

    protected ValueTask<object?> Deserialize(Stream data, Type resolvedType, SerializationContext context) =>
        new(resolvedType.EnsureTypeIsProtoMessage().GetProtoMessageParser().ParseFrom(data));
}