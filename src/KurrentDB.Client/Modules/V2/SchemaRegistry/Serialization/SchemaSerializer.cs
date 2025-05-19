using KurrentDB.Client.Model;
using SchemaDataFormat = KurrentDB.Client.Model.SchemaDataFormat;

namespace KurrentDB.Client.SchemaRegistry.Serialization;

public record struct SchemaSerializationContext(string Stream, Metadata Metadata);

public interface ISchemaSerializer {
	SchemaDataFormat DataFormat { get; }

	ValueTask<ReadOnlyMemory<byte>> Serialize(object? value, SchemaSerializationContext context, CancellationToken cancellationToken);

	ValueTask<object?> Deserialize(ReadOnlyMemory<byte> data, SchemaSerializationContext context, CancellationToken cancellationToken);
}

public abstract record SchemaSerializerOptions {
	/// <summary>
	/// Indicates whether schemas should be automatically registered when they are encountered during
	/// the serialization or deserialization process. When set to <c>true</c>, any new schemas not
	/// already registered in the schema registry will be automatically registered. Disabling it
	/// prevents auto-registration and assumes that all required schemas are explicitly pre-registered.
	/// </summary>
	public bool AutoRegister { get; init; } = true;

	/// <summary>
	/// Specifies whether schemas should be validated during the serialization or deserialization
	/// process. When set to <c>true</c>, the schema validation logic will enforce that the data
	/// being processed adheres to the expected schema definitions. Disabling it bypasses this
	/// validation step and may be useful in scenarios where schema adherence is not strictly
	/// required or can be guaranteed externally.
	/// </summary>
	public bool Validate { get; init; } = true;

	/// <summary>
	/// Indicates whether <c>StrictMode</c> is enabled in the schema serializer options.
	/// When set to <c>true</c>, the serializer enforces stricter validation rules, ensuring schemas and data comply with
	/// defined specifications. Disabling it allows more leniency during serialization and deserialization processes.
	/// </summary>
	public bool StrictMode { get; init; } = false;

	/// <summary>
	/// Specifies the strategy used for generating schema names during serialization and deserialization.
	/// The schema naming strategy determines how the name of a schema is derived based on the message type
	/// and other possible context, ensuring consistent and clear identification of schemas across different systems.
	/// </summary>
	public ISchemaNameStrategy SchemaNameStrategy { get; init; } = new MessageSchemaNameStrategy();
}

public abstract class SchemaSerializer(SchemaSerializerOptions options, SchemaManager schemaManager, MessageTypeRegistry typeRegistry, ITypeResolver typeResolver) : ISchemaSerializer {
	SchemaSerializerOptions Options       { get; } = options;
	SchemaManager           SchemaManager { get; } = schemaManager;
	MessageTypeRegistry     TypeRegistry  { get; } = typeRegistry;
	ITypeResolver           TypeResolver  { get; } = typeResolver;

	public abstract SchemaDataFormat DataFormat { get; }

	async ValueTask<ReadOnlyMemory<byte>> ISchemaSerializer.Serialize(object? value, SchemaSerializationContext context, CancellationToken cancellationToken) {
		// TODO SS: should we? or just blow up?
		if (value is null)
			return ReadOnlyMemory<byte>.Empty;

		var messageType = value.GetType();

		// TODO SS: this should be in the BytesPassthroughSerializer
		// we don't really care about verifying or registering the schema
		// for raw bytes because we gave control to the developer/user
		// the schema name should be already set? or should we generate it here?
		if (value is ReadOnlyMemory<byte> or Memory<byte> or byte[])
			return HandleBytes(value);

		// if (value is ReadOnlyMemory<byte> readOnlyMemoryBytes) {
		// 	context.Metadata
		// 		.TrySet(SystemMetadataKeys.SchemaName, TypeRegistry
		// 			.GetSchemaNameOrDefault(messageType, SchemaName.From(Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream))))
		// 		.Set(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Bytes);
		//
		// 	return readOnlyMemoryBytes;
		// }
		//
		// if (value is Memory<byte> memoryBytes) {
		// 	context.Metadata
		// 		.TrySet(SystemMetadataKeys.SchemaName, TypeRegistry
		// 			.GetSchemaNameOrDefault(messageType, SchemaName.From(Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream))))
		// 		.Set(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Bytes);
		//
		// 	return memoryBytes;
		// }
		//
		// if (value is byte[] byteArray) {
		// 	context.Metadata
		// 		.TrySet(SystemMetadataKeys.SchemaName, TypeRegistry
		// 			.GetSchemaNameOrDefault(messageType, SchemaName.From(Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream))))
		// 		.Set(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Bytes);
		//
		// 	return byteArray;
		// }

		var dataFormat = context.Metadata.Get<SchemaDataFormat>(SystemMetadataKeys.SchemaDataFormat);
		var schemaName = TypeRegistry.GetSchemaNameOrDefault(messageType, SchemaName.From(Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream)));

		if (Options.AutoRegister) {
			var versionInfo = await SchemaManager
				.TryRegisterSchema(schemaName, messageType, dataFormat, cancellationToken)
				.ConfigureAwait(false);

	sd

			context.Metadata
				.Set(SystemMetadataKeys.SchemaName, schemaName)
				.Set(SystemMetadataKeys.SchemaVersionId, versionInfo.VersionId);
		}
		else {
			context.Metadata
				.Set(SystemMetadataKeys.SchemaName, schemaName);

			if (Options.Validate) {
				var lastSchemaVersionId = await SchemaManager
					.EnsureSchemaCompatibility(schemaName, messageType, dataFormat, cancellationToken)
					.ConfigureAwait(false);

				context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, lastSchemaVersionId);
			}
			else {
				// TODO SS: if its not on the registry throw
				if(!TypeRegistry.TryGetSchemaName(messageType, out var schemaNameFound))
					// TODO SS: fix the exception to use the message type
					throw new AutoRegistrationDisabledException(DataFormat, schemaNameFound, messageType);
			}
		}

		try {
			return Serialize(value);
		}
		catch (Exception ex) {
			throw new SerializationFailedException(DataFormat, schemaName,  ex);
		}

		ReadOnlyMemory<byte> HandleBytes(dynamic bytes) {
			context.Metadata
				.TrySet(SystemMetadataKeys.SchemaName, TypeRegistry.GetSchemaNameOrDefault(messageType, Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream)))
				.Set(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Bytes);

			return bytes;
		}
	}

	async ValueTask<object?> ISchemaSerializer.Deserialize(ReadOnlyMemory<byte> data, SchemaSerializationContext context, CancellationToken cancellationToken) {
		if (data.IsEmpty)
			return null;

		var dataFormat = context.Metadata.Get<SchemaDataFormat>(SystemMetadataKeys.SchemaDataFormat);
		if (dataFormat == SchemaDataFormat.Bytes)
			return data;

		// debug assert cause we should never get here since the provider will give us the proper serializer.
		// if (dataFormat != DataFormat)
		// 	throw new UnsupportedSchemaDataFormatException(DataFormat, dataFormat);

		var schemaName = SchemaName.From(
			context.Metadata.Get<string>(SystemMetadataKeys.SchemaName)
			// this is more of a debug assert again because the converters will always add a schema name for backwards compatibility
		 ?? throw new Exception("Schema name is missing in the metadata")
		);

		if (TypeRegistry.TryGetMessageType(schemaName, out var messageType)) {
			if (Options.Validate) {
				// if a schema version id is present it means the new client was used
				if (context.Metadata.TryGet<string>(SystemMetadataKeys.SchemaVersionId, out var schemaVersionIdValue)) {
					var schemaVersionId = SchemaVersionId.From(schemaVersionIdValue!);

					await SchemaManager
						.EnsureSchemaCompatibility(schemaVersionId, messageType, dataFormat, cancellationToken)
						.ConfigureAwait(false);
				}
				else {
					// fallback behaviour to handle validation of messages
					// that were not appended using the new client

					var lastSchemaVersionId = await SchemaManager
						.EnsureSchemaCompatibility(schemaName, messageType, dataFormat, cancellationToken)
						.ConfigureAwait(false);

					context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, lastSchemaVersionId);
				}
			}
		}
		else {
			// try to resolve/discover the clr type directly like magic
			// it will only work if the schema name (old event type name)
			// is the full clr type name
			if (!TypeResolver.TryResolveType(schemaName, out var foundType))
				throw new Exception("Schema name does not match any known type");

			if (Options.Validate) {
				var lastSchemaVersionId = await SchemaManager
					.EnsureSchemaCompatibility(schemaName, messageType, dataFormat, cancellationToken)
					.ConfigureAwait(false);

				context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, lastSchemaVersionId);
			}

			TypeRegistry.TryRegister(schemaName, foundType);
		}

		try {
			return Deserialize(data, messageType);
		}
		catch (Exception ex) {
			throw new DeserializationFailedException(DataFormat, schemaName,  ex);
		}
	}

	protected abstract ReadOnlyMemory<byte> Serialize(object? value);

	protected abstract object Deserialize(ReadOnlyMemory<byte> data, Type resolvedType);
}
