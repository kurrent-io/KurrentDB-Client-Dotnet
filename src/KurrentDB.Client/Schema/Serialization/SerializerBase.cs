using System.Diagnostics;
using KurrentDB.Client.Model;

namespace KurrentDB.Client.Schema.Serialization;

public interface ISchemaExporter {
	string ExportSchemaDefinition(Type messageType);
	string ExportSchemaForValidation(Type messageType);
}

public abstract class NewSerializerBase : ISchemaSerializer {
	public NewSerializerBase(IKurrentSchemaRegistry schemaRegistry, ISchemaNameStrategy nameStrategy, MessageTypeRegistry typeRegistry, ISchemaExporter schemaExporter) {
		SchemaRegistry      = schemaRegistry;
		NameStrategy        = nameStrategy;
		TypeRegistry        = typeRegistry;
		SchemaExporter = schemaExporter;
	}

	IKurrentSchemaRegistry SchemaRegistry { get; }
	ISchemaNameStrategy    NameStrategy   { get; }
	MessageTypeRegistry    TypeRegistry   { get; }
	ISchemaExporter        SchemaExporter { get; }

	public abstract SchemaDataFormat DataFormat { get; }

	async ValueTask<ReadOnlyMemory<byte>> ISchemaSerializer.Serialize(Message message, string stream, CancellationToken cancellationToken) {
		Debug.Assert(message.DataFormat != DataFormat, "DataFormat should be set to the serializer's DataFormat");

		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (message.Value is null)
			throw new InvalidOperationException("Message value cannot be null");

		var messageType = message.Value.GetType();

		// we don't really care about verifying or registering the schema for bytes because we gave control to the developer/user
		// the schema name is always set when passing bytes
		if (messageType == typeof(byte[]) || messageType == typeof(ReadOnlyMemory<byte>) || messageType == typeof(Memory<byte>))
			return (ReadOnlyMemory<byte>)message.Value;

		var dataFormat = message.DataFormat;
		var schemaName = NameStrategy.GenerateSchemaName(messageType, message.Stream);
		var definition = SchemaExporter.ExportSchemaDefinition(messageType);

		context.Metadata.Set(SystemMetadataKeys.SchemaName, schemaName);
		context.Metadata.Set(SystemMetadataKeys.SchemaDataFormat, dataFormat);

		var result = await SchemaRegistry.ValidateSchema(schemaName, definition, context.CancellationToken).ConfigureAwait(false);

		await result.Match(
			success => {



				//
				// var s = await SchemaRegistry.ValidateSchema(schemaInfo, context.CancellationToken).ConfigureAwait(false);
				//
				//
				// var schemaInfo = new SchemaInfo(schemaName, context.SchemaInfo.DataFormat);
				//
				// var s = await SchemaRegistry.GetOrRegisterSchema(schemaInfo, context.CancellationToken).ConfigureAwait(false);
				//
				//       var registeredSchema = await SchemaManager.GetOrRegisterSchema(context.SchemaInfo, messageType).ConfigureAwait(false);

			},
			failure => throw new Exception($"Failed to validate schema:"),
			async notFound => {
				var schema = await SchemaRegistry
					.RegisterSchema(schemaName, definition, dataFormat, context.CancellationToken)
					.ConfigureAwait(false);
				// log some shit?
			}
		);

        if (context.SchemaInfo.SchemaNameMissing)
            context.Metadata.Set(SystemMetadataKeys.SchemaName, registeredSchema.SchemaName);

		try {
			return await Serialize(value, context).ConfigureAwait(false);
		}
		catch (Exception ex) {
			throw new SerializationFailedException(DataFormat, registeredSchema.ToSchemaInfo(),  ex);
		}
	}

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

		var dataFormat = context.SchemaInfo.DataFormat;
		var schemaName = NameStrategy.GenerateSchemaName(messageType, context.Stream);
		var definition = SchemaExporter.ExportSchemaDefinition(messageType);

		context.Metadata.Set(SystemMetadataKeys.SchemaName, schemaName);
		context.Metadata.Set(SystemMetadataKeys.SchemaDataFormat, dataFormat);

		var result = await SchemaRegistry.ValidateSchema(schemaName, definition, context.CancellationToken).ConfigureAwait(false);

		await result.Match(
			success => {



				//
				// var s = await SchemaRegistry.ValidateSchema(schemaInfo, context.CancellationToken).ConfigureAwait(false);
				//
				//
				// var schemaInfo = new SchemaInfo(schemaName, context.SchemaInfo.DataFormat);
				//
				// var s = await SchemaRegistry.GetOrRegisterSchema(schemaInfo, context.CancellationToken).ConfigureAwait(false);
				//
				//       var registeredSchema = await SchemaManager.GetOrRegisterSchema(context.SchemaInfo, messageType).ConfigureAwait(false);

			},
			failure => throw new Exception($"Failed to validate schema:"),
			async notFound => {
				var schema = await SchemaRegistry
					.RegisterSchema(schemaName, definition, dataFormat, context.CancellationToken)
					.ConfigureAwait(false);
				// log some shit?
			}
		);

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

		var messageType = SchemaManager.ResolveMessageType(context.SchemaInfo.SchemaName, context.Stream, context.Metadata);

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



public abstract class SerializerBase : ISchemaSerializer {

	protected SerializerBase(IKurrentSchemaManager schemaManager) {
		SchemaManager = schemaManager;
	}


	IKurrentSchemaManager SchemaManager { get; }

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

        var registeredSchema = await SchemaManager.GetOrRegisterSchema(context.SchemaInfo, messageType).ConfigureAwait(false);

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

		var messageType = SchemaManager.ResolveMessageType(context.SchemaInfo.SchemaName, context.Stream, context.Metadata);

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
