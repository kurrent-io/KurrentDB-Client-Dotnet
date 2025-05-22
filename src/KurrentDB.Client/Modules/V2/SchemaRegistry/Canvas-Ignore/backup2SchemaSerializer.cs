// using System.Diagnostics;
// using KurrentDB.Client.Model;
// using SchemaDataFormat = KurrentDB.Client.Model.SchemaDataFormat;
//
// namespace KurrentDB.Client.SchemaRegistry.Serialization;
//
// public record struct SchemaSerializationContext(string Stream, Metadata Metadata, SchemaRegistryServerConfiguration ServerConfiguration);
//
// public interface ISchemaSerializer {
// 	SchemaDataFormat DataFormat { get; }
//
// 	ValueTask<ReadOnlyMemory<byte>> Serialize(object? value, SchemaSerializationContext context, CancellationToken cancellationToken);
//
// 	ValueTask<object?> Deserialize(ReadOnlyMemory<byte> data, SchemaSerializationContext context, CancellationToken cancellationToken);
// }
//
// public record SchemaSerializerOptions {
// 	/// <summary>
// 	/// Indicates whether schemas should be automatically registered when they are encountered
// 	/// during the serialization process. When set to <c>true</c>, any new schemas not
// 	/// already registered in the schema registry will be automatically registered.
// 	/// Disabling it prevents auto-registration and assumes that all required schemas
// 	/// are explicitly pre-registered.
// 	/// </summary>
// 	public bool AutoRegister { get; init; } = true;
//
// 	/// <summary>
// 	/// Specifies whether schemas should be validated during the serialization or deserialization
// 	/// process. When set to <c>true</c>, the schema validation logic will enforce that the data
// 	/// being processed adheres to the expected schema definitions. Disabling it bypasses this
// 	/// validation step and may be useful in scenarios where schema adherence is not strictly
// 	/// required or can be guaranteed externally.
// 	/// </summary>
// 	public bool Validate { get; init; } = true;
//
// 	/// <summary>
// 	/// Specifies whether only schemas explicitly mapped to a .NET type should be consumed during
// 	/// deserialization. When set to <c>true</c>, the deserialization process will restrict itself
// 	/// to schemas that are registered and have a corresponding mapped type in the application,
// 	/// skipping any unmapped schemas. This is useful for ensuring type safety and consistency.
// 	/// Setting it to <c>false</c> allows for the consumption of any schema, regardless of whether
// 	/// it has a mapped type, which may be necessary in scenarios where dynamic or unknown schemas
// 	/// are expected.
// 	/// </summary>
// 	public bool ConsumeMappedOnly { get; set; } = true;
//
// 	/// <summary>
// 	/// Specifies the strategy used for generating schema names during serialization and deserialization.
// 	/// The schema naming strategy determines how the name of a schema is derived based on the message type
// 	/// and other possible context, ensuring consistent and clear identification of schemas across different systems.
// 	/// </summary>
// 	public ISchemaNameStrategy SchemaNameStrategy { get; init; } = new MessageSchemaNameStrategy();
// }
//
// public abstract class SchemaSerializer(SchemaSerializerOptions options, SchemaManager schemaManager, MessageTypeMapper typeMapper) : ISchemaSerializer {
// 	SchemaSerializerOptions Options       { get; } = options;
// 	SchemaManager           SchemaManager { get; } = schemaManager;
// 	MessageTypeMapper       TypeMapper    { get; } = typeMapper;
//
// 	public abstract SchemaDataFormat DataFormat { get; }
//
// 	async ValueTask<ReadOnlyMemory<byte>> ISchemaSerializer.Serialize(object? value, SchemaSerializationContext context, CancellationToken cancellationToken) {
// 		// -------------------------------------------------------------------------------------------------
// 		// these debug asserts are to make sure the required info is set during the development process
// 		// -------------------------------------------------------------------------------------------------
// 		Debug.Assert(!string.IsNullOrWhiteSpace(context.Stream) || context.Metadata.Get<string>(SystemMetadataKeys.Stream) != null, "Stream name is missing in the metadata");
// 		// it should be impossible to try to serialize data with the wrong format
// 		Debug.Assert(context.Metadata.GetSchemaInfo().DataFormat == DataFormat, "Schema data format does not match the serializer data format");
// 		// -------------------------------------------------------------------------------------------------
//
// 		if (value is null)
// 			return ReadOnlyMemory<byte>.Empty;
//
// 		var messageType = value.GetType();
//
// 		try {
// 			if (context.ServerConfiguration.SchemaRegistryEnabled || context.ServerConfiguration.AutoRegistration.Enforced || context.ServerConfiguration.Validation.Enforced) {
// 				context.ServerConfiguration.EnsureDataFormatAllowed(DataFormat);
//
// 				var schemaName = TypeMapper
// 					.GetSchemaNameOrDefault(messageType, Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream));
//
// 				if (Options.AutoRegister || context.ServerConfiguration.AutoRegistration.Enforced) {
// 					var versionInfo = await SchemaManager
// 						.TryRegisterSchema(schemaName, messageType, DataFormat, cancellationToken)
// 						.ConfigureAwait(false);
//
// 					context.Metadata
// 						.Set(SystemMetadataKeys.SchemaName, schemaName)
// 						.Set(SystemMetadataKeys.SchemaVersionId, versionInfo.VersionId)
// 						.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
// 				}
// 				else if (Options.Validate || context.ServerConfiguration.Validation.Enforced) {
// 					var lastSchemaVersionId = await SchemaManager
// 						.EnsureSchemaCompatibility(schemaName, messageType, DataFormat, cancellationToken)
// 						.ConfigureAwait(false);
//
// 					context.Metadata
// 						.Set(SystemMetadataKeys.SchemaName, schemaName)
// 						.Set(SystemMetadataKeys.SchemaVersionId, lastSchemaVersionId)
// 						.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
// 				}
// 				else {
// 					// if neither auto-register nor validate is enabled, nothing to do?
// 					// I think we must check the default behaviour now and if auto register is enabled
// 					// we do it and only if not we throw.
//
// 				}
// 			}
// 			else {
// 				await HandleSchemaRegistryDisabled(messageType, context).ConfigureAwait(false);
// 			}
//
// 			return Serialize(value);
// 		}
// 		catch (Exception ex) {
// 			throw new SerializationFailedException(DataFormat, messageType,  ex);
// 		}
// 	}
//
// 	async ValueTask HandleSchemaRegistryEnabled(Type messageType, SchemaSerializationContext context, CancellationToken cancellationToken) {
// 		context.ServerConfiguration.EnsureDataFormatAllowed(DataFormat);
//
// 		var schemaName = TypeMapper
// 			.GetSchemaNameOrDefault(messageType, Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream));
//
// 		if (Options.AutoRegister || context.ServerConfiguration.AutoRegistration.Enforced) {
// 			var versionInfo = await SchemaManager
// 				.TryRegisterSchema(schemaName, messageType, DataFormat, cancellationToken)
// 				.ConfigureAwait(false);
//
// 			context.Metadata
// 				.Set(SystemMetadataKeys.SchemaName, schemaName)
// 				.Set(SystemMetadataKeys.SchemaVersionId, versionInfo.VersionId)
// 				.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
// 		}
// 		else if (Options.Validate || context.ServerConfiguration.Validation.Enforced) {
// 			var lastSchemaVersionId = await SchemaManager
// 				.EnsureSchemaCompatibility(schemaName, messageType, DataFormat, cancellationToken)
// 				.ConfigureAwait(false);
//
// 			context.Metadata
// 				.Set(SystemMetadataKeys.SchemaName, schemaName)
// 				.Set(SystemMetadataKeys.SchemaVersionId, lastSchemaVersionId)
// 				.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
// 		}
// 		else {
// 			// if neither auto-register nor validate is enabled, nothing to do?
// 			// I think we must check the default behaviour now and if auto register is enabled
// 			// we do it and only if not we throw.
//
// 		}
//
// 		if (Options.AutoRegister || context.ServerConfiguration.AutoRegistration.Enforced) {
// 			await RegisterProducerSchema(messageType, schemaName, context, cancellationToken);
// 		}
// 		else if (Options.Validate || context.ServerConfiguration.Validation.Enforced) {
// 			await ValidateProducerSchema(messageType, schemaName, context, cancellationToken);
// 		}
//
// 		return;
//
// 		async ValueTask RegisterProducerSchema(Type messageType, SchemaName schemaName, SchemaSerializationContext context, CancellationToken cancellationToken) {
// 			var versionInfo = await SchemaManager
// 				.TryRegisterSchema(schemaName, messageType, DataFormat, cancellationToken)
// 				.ConfigureAwait(false);
//
// 			context.Metadata
// 				.Set(SystemMetadataKeys.SchemaName, schemaName)
// 				.Set(SystemMetadataKeys.SchemaVersionId, versionInfo.VersionId)
// 				.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
// 		}
//
// 		async ValueTask ValidateProducerSchema(Type messageType, SchemaName schemaName, SchemaSerializationContext context, CancellationToken cancellationToken) {
// 			var lastSchemaVersionId = await SchemaManager
// 				.EnsureSchemaCompatibility(schemaName, messageType, DataFormat, cancellationToken)
// 				.ConfigureAwait(false);
//
// 			context.Metadata
// 				.Set(SystemMetadataKeys.SchemaName, schemaName)
// 				.Set(SystemMetadataKeys.SchemaVersionId, lastSchemaVersionId)
// 				.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
// 		}
// 	}
//
//
// 	ValueTask HandleSchemaRegistryDisabled(Type messageType, SchemaSerializationContext context) {
// 		if(Options.AutoRegister) {
// 			var schemaName = TypeMapper
// 				.GetSchemaNameOrDefault(messageType, Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream));
//
// 			context.Metadata
// 				.Set(SystemMetadataKeys.SchemaName, schemaName)
// 				.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
// 		}
// 		else if(TypeMapper.TryGetSchemaName(messageType, out var schemaName)) {
// 			context.Metadata
// 				.Set(SystemMetadataKeys.SchemaName, schemaName)
// 				.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
// 		}
// 		else
// 			throw new InvalidOperationException($"The message type '{messageType.FullName}' is not mapped and auto-registration is disabled.");
//
// 		return new ValueTask();
// 	}
//
// 	async ValueTask<object?> ISchemaSerializer.Deserialize(ReadOnlyMemory<byte> data, SchemaSerializationContext context, CancellationToken cancellationToken) {
// 		var schemaInfo = context.Metadata.GetSchemaInfo();
//
// 		// -------------------------------------------------------------------------------------------------
// 		// these debug asserts are to make sure the required info is set during the development process
// 		// -------------------------------------------------------------------------------------------------
// 		// the consumers of the serializer must always set these values
// 		Debug.Assert(schemaInfo.HasSchemaName, "Schema name is missing in the metadata");
// 		Debug.Assert(schemaInfo.HasDataFormat, "Schema data format is missing in the metadata");
// 		// it should be impossible to try to deserialize data with the wrong format
// 		Debug.Assert(schemaInfo.DataFormat == DataFormat, "Schema data format does not match the serializer data format");
// 		// -------------------------------------------------------------------------------------------------
//
// 		if (data.IsEmpty)
// 			return null;
//
// 		try {
// 			var (messageType, schemaVersionId) = await SchemaManager
// 				.CanDeserialize(schemaInfo, Options.Validate, cancellationToken)
// 				.ConfigureAwait(false);
//
// 			if (schemaVersionId != SchemaVersionId.None)
// 				context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, schemaInfo.SchemaVersionId);
//
// 			return Deserialize(data, messageType);
// 		}
// 		catch (Exception ex) {
// 			throw new DeserializationFailedException(DataFormat, schemaInfo.SchemaName,  ex);
// 		}
// 	}
//
// 	// looks good but it is still confusing...
// 	// async ValueTask<ReadOnlyMemory<byte>> ISchemaSerializer.Serialize(object? value, SchemaSerializationContext context, CancellationToken cancellationToken) {
// 	// 	if (value is null)
// 	// 		return ReadOnlyMemory<byte>.Empty;
// 	//
// 	// 	var messageType = value.GetType();
// 	// 	// var schemaInfo  = context.Metadata.GetSchemaInfo();
// 	// 	//
// 	// 	// // -------------------------------------------------------------------------------------------------
// 	// 	// // these debug asserts are to make sure the metadata is correctly set during the development process
// 	// 	// // -------------------------------------------------------------------------------------------------
// 	// 	// // the consumers of the serializer must always set these values
// 	// 	// Debug.Assert(schemaInfo.HasDataFormat, "Schema data format is missing in the metadata");
// 	// 	// // it should be impossible to try to deserialize data with the wrong format
// 	// 	// Debug.Assert(schemaInfo.DataFormat == DataFormat, "Schema data format does not match the serializer data format");
// 	// 	// // -------------------------------------------------------------------------------------------------
// 	// 	//
// 	// 	// // if (schemaInfo.SchemaName == SchemaName.None) {
// 	// 	// // 	schemaInfo = schemaInfo with {
// 	// 	// // 		SchemaName = TypeMapper.GetSchemaNameOrDefault(messageType, SchemaName.From(Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream)))
// 	// 	// // 	};
// 	// 	// // }
// 	//
// 	// 	try {
// 	// 		if (context.ServerConfiguration.SchemaRegistryEnabled || context.ServerConfiguration.AutoRegistration.Enforced || context.ServerConfiguration.Validation.Enforced) {
// 	// 			context.ServerConfiguration.EnsureDataFormatAllowed(DataFormat);
// 	//
// 	// 			var schemaName = TypeMapper
// 	// 				.GetSchemaNameOrDefault(messageType, Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream));
// 	//
// 	// 			if (Options.AutoRegister || context.ServerConfiguration.AutoRegistration.Enforced) {
// 	// 				var versionInfo = await SchemaManager
// 	// 					.TryRegisterSchema(schemaName, messageType, DataFormat, cancellationToken)
// 	// 					.ConfigureAwait(false);
// 	//
// 	// 				context.Metadata
// 	// 					.Set(SystemMetadataKeys.SchemaName, schemaName)
// 	// 					.Set(SystemMetadataKeys.SchemaVersionId, versionInfo.VersionId)
// 	// 					.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
// 	// 			}
// 	// 			else if (Options.Validate || context.ServerConfiguration.Validation.Enforced) {
// 	// 				var lastSchemaVersionId = await SchemaManager
// 	// 					.EnsureSchemaCompatibility(schemaName, messageType, DataFormat, cancellationToken)
// 	// 					.ConfigureAwait(false);
// 	//
// 	// 				context.Metadata
// 	// 					.Set(SystemMetadataKeys.SchemaName, schemaName)
// 	// 					.Set(SystemMetadataKeys.SchemaVersionId, lastSchemaVersionId)
// 	// 					.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
// 	// 			}
// 	// 			else {
// 	// 				// what happens here? im a bit lost...
// 	// 			}
// 	// 		}
// 	// 		else {
// 	// 			var schemaName = TypeMapper
// 	// 				.GetSchemaNameOrDefault(messageType, Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream));
// 	//
// 	// 			context.Metadata
// 	// 				.Set(SystemMetadataKeys.SchemaName, schemaName)
// 	// 				.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
// 	// 		}
// 	//
// 	// 		// if (Options.AutoRegister) {
// 	// 		// 	var schemaName = TypeMapper.GetSchemaNameOrDefault(messageType, Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream));
// 	// 		//
// 	// 		// 	if (context.ServerConfiguration.SchemaRegistryEnabled) {
// 	// 		// 		var versionInfo = await SchemaManager
// 	// 		// 			.TryRegisterSchema(schemaName, messageType, DataFormat, cancellationToken)
// 	// 		// 			.ConfigureAwait(false);
// 	// 		//
// 	// 		// 		context.Metadata
// 	// 		// 			.Set(SystemMetadataKeys.SchemaName, schemaName)
// 	// 		// 			.Set(SystemMetadataKeys.SchemaVersionId, versionInfo.VersionId)
// 	// 		// 			.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
// 	// 		// 	}
// 	// 		// 	else {
// 	// 		// 		context.Metadata
// 	// 		// 			.Set(SystemMetadataKeys.SchemaName, schemaName)
// 	// 		// 			.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
// 	// 		// 	}
// 	// 		// }
// 	// 		// else {
// 	// 		// 	if (context.ServerConfiguration.SchemaRegistryEnabled) {
// 	// 		// 		if (Options.Validate) {
// 	// 		// 			var schemaName = TypeMapper.GetSchemaNameOrDefault(messageType, Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream));
// 	// 		//
// 	// 		// 			var lastSchemaVersionId = await SchemaManager
// 	// 		// 				.EnsureSchemaCompatibility(schemaName, messageType, DataFormat, cancellationToken)
// 	// 		// 				.ConfigureAwait(false);
// 	// 		//
// 	// 		// 			context.Metadata
// 	// 		// 				.Set(SystemMetadataKeys.SchemaName, schemaName)
// 	// 		// 				.Set(SystemMetadataKeys.SchemaVersionId, lastSchemaVersionId)
// 	// 		// 				.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
// 	// 		// 		}
// 	// 		// 		else {
// 	// 		// 			if(!TypeMapper.TryGetSchemaName(messageType, out var schemaNameFound))
// 	// 		// 				throw new AutoRegistrationDisabledException(DataFormat, schemaNameFound, messageType);
// 	// 		// 		}
// 	// 		// 	}
// 	// 		// 	else {
// 	// 		// 		if(!TypeMapper.TryGetSchemaName(messageType, out var schemaNameFound))
// 	// 		// 			throw new AutoRegistrationDisabledException(DataFormat, schemaNameFound, messageType);
// 	// 		// 	}
// 	// 		// }
// 	//
// 	// 		return Serialize(value);
// 	// 	}
// 	// 	catch (Exception ex) {
// 	// 		throw new SerializationFailedException(DataFormat, messageType,  ex);
// 	// 	}
// 	// }
//
// 	// async ValueTask<object?> ISchemaSerializer.Deserialize(ReadOnlyMemory<byte> data, SchemaSerializationContext context, CancellationToken cancellationToken) {
// 	// 	if (data.IsEmpty)
// 	// 		return null;
// 	//
// 	// 	var schemaInfo = context.Metadata.GetSchemaInfo();
// 	//
// 	// 	// -------------------------------------------------------------------------------------------------
// 	// 	// these debug asserts are to make sure the metadata is correctly set during the development process
// 	// 	// -------------------------------------------------------------------------------------------------
// 	// 	// the consumers of the serializer must always set these values
// 	// 	Debug.Assert(schemaInfo.HasSchemaName, "Schema name is missing in the metadata");
// 	// 	Debug.Assert(schemaInfo.HasDataFormat, "Schema data format is missing in the metadata");
// 	// 	// it should be impossible to try to deserialize data with the wrong format
// 	// 	Debug.Assert(schemaInfo.DataFormat == DataFormat, "Schema data format does not match the serializer data format");
// 	// 	// -------------------------------------------------------------------------------------------------
// 	//
// 	// 	try {
// 	// 		// check if the message type was mapped or try to resolve it "magically"
// 	// 		if (!TypeMapper.TryGetMessageType(schemaInfo.SchemaName, out var messageType)) {
// 	// 			// it will only work if the name strategy was used was MessageSchemaNameStrategy
// 	// 			// (or the old event type property contains the full clr type name)
// 	// 			if (!SystemTypes.TryResolveType(schemaInfo.SchemaName, out messageType))
// 	// 				throw new Exception("Schema name does not match any known type");
// 	// 		}
// 	//
// 	// 		if (Options.Validate) {
// 	// 			if (schemaInfo.HasSchemaVersionId) {
// 	// 				await SchemaManager
// 	// 					.EnsureSchemaCompatibility(schemaInfo.SchemaVersionId, messageType, DataFormat, cancellationToken)
// 	// 					.ConfigureAwait(false);
// 	// 			}
// 	// 			else {
// 	// 				// fallback behaviour for backwards compatibility
// 	// 				var lastSchemaVersionId = await SchemaManager
// 	// 					.EnsureSchemaCompatibility(schemaInfo.SchemaName, messageType, DataFormat, cancellationToken)
// 	// 					.ConfigureAwait(false);
// 	//
// 	// 				// set the schema version id in the metadata now that it is known
// 	// 				context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, lastSchemaVersionId);
// 	// 			}
// 	// 		}
// 	//
// 	// 		return Deserialize(data, messageType);
// 	// 	}
// 	// 	catch (Exception ex) {
// 	// 		throw new DeserializationFailedException(DataFormat, schemaInfo.SchemaName,  ex);
// 	// 	}
// 	// }
//
// 	protected abstract ReadOnlyMemory<byte> Serialize(object? value);
//
// 	protected abstract object Deserialize(ReadOnlyMemory<byte> data, Type resolvedType);
// }
