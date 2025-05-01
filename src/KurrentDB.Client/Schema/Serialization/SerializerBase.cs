using KurrentDB.Client.Model;

namespace KurrentDB.Client.Schema.Serialization;

public abstract class SerializerBase(KurrentSchemaControl schemaControl) : ISchemaSerializer {
    KurrentSchemaControl SchemaControl { get; } = schemaControl;

	public abstract SchemaDataFormat DataFormat { get; }

	async ValueTask<ReadOnlyMemory<byte>> ISchemaSerializer.Serialize(object? value, SerializationContext context) {
		if (context.SchemaInfo.DataFormat != DataFormat)
			throw new UnsupportedSchemaException(DataFormat, context.SchemaInfo.DataFormat);

		if (value is null)
			return ReadOnlyMemory<byte>.Empty;

		var messageType = value.GetType();

		// we don't really care about verifying or registering the schema for bytes because we gave control to the developer/user
		// the schema name is always set when passing bytes
		if (messageType == typeof(byte[]) || messageType == typeof(ReadOnlyMemory<byte>) || messageType == typeof(Memory<byte>))
			return (ReadOnlyMemory<byte>)value;

        var registeredSchema = await SchemaControl.GetOrRegisterSchema(context.SchemaInfo, messageType).ConfigureAwait(false);

        if (context.SchemaInfo.SchemaNameMissing)
            context.Metadata.Set(SystemMetadataKeys.SchemaName, registeredSchema.SchemaName);

		try {
			return await Serialize(value, context).ConfigureAwait(false);
		}
		catch (Exception ex) {
			throw new SerializationFailedException(DataFormat, registeredSchema.ToSchemaInfo(),  ex);
		}
	}

	async ValueTask<object?> ISchemaSerializer.Deserialize(ReadOnlyMemory<byte> data, SerializationContext context) {
        if (context.SchemaInfo.DataFormat != DataFormat)
            throw new UnsupportedSchemaException(DataFormat, context.SchemaInfo.DataFormat);

        if (data.IsEmpty)
			return null;

		if (context.SchemaInfo.DataFormat == SchemaDataFormat.Bytes)
			return data;

		var messageType = SchemaControl.ResolveMessageType(context.SchemaInfo.SchemaName, context.Stream, context.Metadata);

		try {
			return await Deserialize(data, messageType, context).ConfigureAwait(false);
		}
		catch (Exception ex) {
			throw new DeserializationFailedException(DataFormat, new SchemaInfo(context.SchemaInfo.SchemaName, context.SchemaInfo.DataFormat),  ex);
		}
	}

	protected abstract ValueTask<ReadOnlyMemory<byte>> Serialize(object? value, SerializationContext context);

	protected abstract ValueTask<object?> Deserialize(ReadOnlyMemory<byte> data, Type resolvedType, SerializationContext context);
}
