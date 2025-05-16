using KurrentDB.Client.Model;

namespace KurrentDB.Client.SchemaRegistry.Serialization;

public record struct SchemaSerializationContext(string Stream, Metadata Metadata, SchemaDataFormat DataFormat);

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

public abstract class SchemaSerializer(SchemaSerializerOptions options, KurrentRegistryClient schemaRegistry, MessageTypeRegistry typeRegistry, ISchemaExporter schemaExporter) : ISchemaSerializer {
	SchemaSerializerOptions Options        { get; } = options;
	KurrentRegistryClient   SchemaRegistry { get; } = schemaRegistry;
	MessageTypeRegistry     TypeRegistry   { get; } = typeRegistry;
	ISchemaExporter         SchemaExporter { get; } = schemaExporter;

	public abstract SchemaDataFormat DataFormat { get; }

	async ValueTask<ReadOnlyMemory<byte>> ISchemaSerializer.Serialize(object? value, SchemaSerializationContext context, CancellationToken cancellationToken) {
		if (value is null)
			return null;

		// we don't really care about verifying or registering the schema
		// for bytes because we gave control to the developer/user
		// the schema name must always be set when passing bytes
		if (value is ReadOnlyMemory<byte> or Memory<byte> or byte[])
			return (dynamic)value;

		var messageType = value.GetType();
		var schemaName  = SchemaName.From(Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream));
		var definition  = SchemaExporter.ExportSchemaDefinition(messageType);

		if (Options.AutoRegister) {
			if (TypeRegistry.TryGetMessageType(schemaName, out var registeredType)) {
				if (registeredType != messageType)
					throw new Exception($"Message schema '{schemaName}' is already registered with a different type: {registeredType}");
			}
			else {
				// var getOrRegisterSchemaResult = await SchemaRegistry
				// 	.GetOrRegisterSchema(schemaName, definition, DataFormat, cancellationToken)
				// 	.ConfigureAwait(false);
				//
				// getOrRegisterSchemaResult.Switch(
				// 	schema => {
				// 		context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, schema.SchemaVersionId);
				// 		TypeRegistry.Register(schemaName, messageType);
				// 	},
				// 	error  => throw new Exception($"Failed to auto register schema: {schemaName}", error.Value)
				// );
			}
		}
		else {
			throw new Exception($"The message schema for {messageType.FullName} is not registered and auto registration is disabled.");

			// var validateSchemaResult = await SchemaRegistry
			// 	.CheckSchemaCompatibility(schemaName, definition, context.DataFormat, cancellationToken)
			// 	.ConfigureAwait(false);
			//
			// validateSchemaResult.Switch(
			// 	versionId  => {
			// 		context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, versionId);
			// 		TypeRegistry.TryRegister(schemaName, messageType);
			// 	},
			// 	errors   => throw new Exception($"Invalid schema: {schemaName} - {errors.Select(x => x.ToString()).ToArray()}"),
			// 	notfound => throw new Exception($"Schema not found: {schemaName}")
			// );
		}

		if (Options.Validate) {
			if (Options.AutoRegister) {
				if (TypeRegistry.TryGetMessageType(schemaName, out var registeredType)) {
					if (registeredType != messageType)
						throw new Exception($"Schema name '{schemaName}' is already registered with a different type: {registeredType}");
				}
				else {


					// var getOrRegisterSchemaResult = await SchemaRegistry
					// 	.GetOrRegisterSchema(schemaName, definition, DataFormat, cancellationToken)
					// 	.ConfigureAwait(false);
					//
					// getOrRegisterSchemaResult.Switch(
					// 	schema => {
					// 		context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, schema.SchemaVersionId);
					// 		TypeRegistry.Register(schemaName, messageType);
					// 	},
					// 	error  => throw new Exception($"Failed to auto register schema: {schemaName}", error.Value)
					// );
				}
			}
			else {
				var validateSchemaResult = await SchemaRegistry
					.CheckSchemaCompatibility(schemaName, definition, context.DataFormat, cancellationToken)
					.ConfigureAwait(false);

				validateSchemaResult.Switch(
					versionId  => {
						context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, versionId);
						TypeRegistry.TryRegister(schemaName, messageType);
					},
					errors   => throw new Exception($"Invalid schema: {schemaName} - {errors.Select(x => x.ToString()).ToArray()}"),
					notfound => throw new Exception($"Schema not found: {schemaName}")
				);
			}
		}
		else
			TypeRegistry.TryRegister(schemaName, messageType);

		// enrich the metadata with schema name and data format
		context.Metadata
			.Set(SystemMetadataKeys.SchemaName, schemaName)
			.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);

		try {
			return Serialize(value);
		}
		catch (Exception ex) {
			throw new SerializationFailedException(DataFormat, schemaName,  ex);
		}
	}

	async ValueTask<object?> ISchemaSerializer.Deserialize(ReadOnlyMemory<byte> data, SchemaSerializationContext context, CancellationToken cancellationToken) {
		if (context.DataFormat != DataFormat)
			throw new UnsupportedSchemaException(DataFormat, context.DataFormat);

		if (data.IsEmpty)
			return null;

		var dataFormat = context.Metadata.Get<SchemaDataFormat>(SystemMetadataKeys.SchemaDataFormat);
		if (dataFormat == SchemaDataFormat.Bytes)
			return data;

		var schemaName = SchemaName.From(
			context.Metadata.Get<string>(SystemMetadataKeys.SchemaName)
		 ?? throw new Exception("Schema name is missing in the metadata"));

		if (TypeRegistry.TryGetMessageType(schemaName, out var messageType)) {
			// try resolve directly like magic
			// must use component or helper to resolve the type,
			// so it can load the assemblies
			messageType = Type.GetType(schemaName);

			if (messageType is null)
				throw new Exception($"Schema name '{schemaName}' is not registered and message type could not be resolved");

			if (Options.Validate) {
				// because the type was indeed found,
				// we must validate the schema against the registry
				var definition = SchemaExporter.ExportSchemaDefinition(messageType);

				var validateSchemaResult = await SchemaRegistry
					.CheckSchemaCompatibility(schemaName, definition, context.DataFormat, cancellationToken)
					.ConfigureAwait(false);

				validateSchemaResult.Switch(
					versionId => context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, versionId),
					errors    => throw new Exception($"Invalid schema: {schemaName} - {errors.Select(x => x.ToString()).ToArray()}"),
					notfound  => throw new Exception($"Schema not found: {schemaName}")
				);
			}

			TypeRegistry.TryRegister(schemaName, messageType);
		}
		else {
			if (Options.Validate) {
				// because the type was indeed found,
				// we must validate the schema against the registry
				var definition = SchemaExporter.ExportSchemaDefinition(messageType);

				var validateSchemaResult = await SchemaRegistry
					.CheckSchemaCompatibility(schemaName, definition, context.DataFormat, cancellationToken)
					.ConfigureAwait(false);

				validateSchemaResult.Switch(
					success  => context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, success.Value),
					failure  => throw new Exception($"Invalid schema: {schemaName} - {failure.Errors.Select(x => x.ToString()).ToArray()}"),
					notfound => throw new Exception($"Schema not found: {schemaName}")
				);
			}
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


	// async ValueTask<ReadOnlyMemory<byte>> SerializeOnly(object? value, SchemaSerializationContext context, CancellationToken cancellationToken) {
	// 	if (value is null)
	// 		return null;
	//
	// 	// we don't really care about verifying or registering the schema
	// 	// for bytes because we gave control to the developer/user
	// 	// the schema name must always be set when passing bytes
	// 	if (value is ReadOnlyMemory<byte> or Memory<byte> or byte[])
	// 		return (dynamic)value;
	//
	// 	var messageType = value.GetType();
	// 	var schemaName  = SchemaName.From(Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream));
	// 	var definition  = SchemaExporter.ExportSchemaDefinition(messageType);
	//
	// 	if (Options.Validate) {
	// 		if (Options.AutoRegister) {
	// 			if (TypeRegistry.GetClrType(schemaName) is { } type) {
	// 				if (type != messageType)
	// 					throw new Exception($"Schema name '{schemaName}' is already registered with a different type: {type}");
	// 			}
	// 			else {
	//
	//
	// 				// var getOrRegisterSchemaResult = await SchemaRegistry
	// 				// 	.GetOrRegisterSchema(schemaName, definition, DataFormat, cancellationToken)
	// 				// 	.ConfigureAwait(false);
	// 				//
	// 				// getOrRegisterSchemaResult.Switch(
	// 				// 	schema => {
	// 				// 		context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, schema.SchemaVersionId);
	// 				// 		TypeRegistry.Register(schemaName, messageType);
	// 				// 	},
	// 				// 	error  => throw new Exception($"Failed to auto register schema: {schemaName}", error.Value)
	// 				// );
	// 			}
	// 		}
	// 		else {
	// 			var validateSchemaResult = await SchemaRegistry
	// 				.CheckSchemaCompatibility(schemaName, definition, context.DataFormat, cancellationToken)
	// 				.ConfigureAwait(false);
	//
	// 			validateSchemaResult.Switch(
	// 				versionId  => {
	// 					context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, versionId);
	// 					TypeRegistry.Register(schemaName, messageType);
	// 				},
	// 				errors   => throw new Exception($"Invalid schema: {schemaName} - {errors.Select(x => x.ToString()).ToArray()}"),
	// 				notfound => throw new Exception($"Schema not found: {schemaName}")
	// 			);
	// 		}
	// 	}
	// 	else
	// 		TypeRegistry.Register(schemaName, messageType);
	//
	// 	// enrich the metadata with schema name and data format
	// 	context.Metadata
	// 		.Set(SystemMetadataKeys.SchemaName, schemaName)
	// 		.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
	//
	// 	try {
	// 		return Serialize(value);
	// 	}
	// 	catch (Exception ex) {
	// 		throw new SerializationFailedException(DataFormat, schemaName,  ex);
	// 	}
	// }

}
