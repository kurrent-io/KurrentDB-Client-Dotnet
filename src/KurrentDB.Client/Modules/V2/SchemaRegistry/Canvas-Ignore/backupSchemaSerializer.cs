// using System.Buffers;
// using System.Collections.Concurrent;
// using KurrentDB.Client.Model;
// using KurrentDB.Protocol.Registry.V2;
// using RegisteredSchema = KurrentDB.Client.Model.RegisteredSchema;
// using SchemaDataFormat = KurrentDB.Client.Model.SchemaDataFormat;
//
// namespace KurrentDB.Client.SchemaRegistry.Serialization;
//
// public record struct SchemaSerializationContext(string Stream, Metadata Metadata, SchemaDataFormat DataFormat);
//
// public interface ISchemaSerializer {
// 	SchemaDataFormat DataFormat { get; }
//
// 	ValueTask<ReadOnlyMemory<byte>> Serialize(object? value, SchemaSerializationContext context, CancellationToken cancellationToken);
//
// 	ValueTask<object?> Deserialize(ReadOnlyMemory<byte> data, SchemaSerializationContext context, CancellationToken cancellationToken);
// }
//
// public abstract record SchemaSerializerOptions {
// 	/// <summary>
// 	/// Indicates whether schemas should be automatically registered when they are encountered during
// 	/// the serialization or deserialization process. When set to <c>true</c>, any new schemas not
// 	/// already registered in the schema registry will be automatically registered. Disabling it
// 	/// prevents auto-registration and assumes that all required schemas are explicitly pre-registered.
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
// 	/// Indicates whether <c>StrictMode</c> is enabled in the schema serializer options.
// 	/// When set to <c>true</c>, the serializer enforces stricter validation rules, ensuring schemas and data comply with
// 	/// defined specifications. Disabling it allows more leniency during serialization and deserialization processes.
// 	/// </summary>
// 	public bool StrictMode { get; init; } = false;
//
// 	/// <summary>
// 	/// Specifies the strategy used for generating schema names during serialization and deserialization.
// 	/// The schema naming strategy determines how the name of a schema is derived based on the message type
// 	/// and other possible context, ensuring consistent and clear identification of schemas across different systems.
// 	/// </summary>
// 	public ISchemaNameStrategy SchemaNameStrategy { get; init; } = new MessageSchemaNameStrategy();
// }
//
// public abstract class SchemaSerializer(SchemaSerializerOptions options, SchemaManager schemaManager, MessageTypeRegistry typeRegistry, ITypeResolver typeResolver) : ISchemaSerializer {
// 	SchemaSerializerOptions Options       { get; } = options;
// 	SchemaManager           SchemaManager { get; } = schemaManager;
// 	MessageTypeRegistry     TypeRegistry  { get; } = typeRegistry;
// 	ITypeResolver           TypeResolver  { get; } = typeResolver;
//
// 	public abstract SchemaDataFormat DataFormat { get; }
//
// 	async ValueTask<ReadOnlyMemory<byte>> ISchemaSerializer.Serialize(object? value, SchemaSerializationContext context, CancellationToken cancellationToken) {
// 		// should we? or just blow up?
// 		if (value is null)
// 			return ReadOnlyMemory<byte>.Empty;
//
// 		var messageType = value.GetType();
//
// 		// we don't really care about verifying or registering the schema
// 		// for raw bytes because we gave control to the developer/user
// 		// the schema name should be already set? or should we generate it here?
// 		if (value is ReadOnlyMemory<byte> or Memory<byte> or byte[])
// 			return HandleBytes(value);
//
// 		// if (value is ReadOnlyMemory<byte> readOnlyMemoryBytes) {
// 		// 	context.Metadata
// 		// 		.TrySet(SystemMetadataKeys.SchemaName, TypeRegistry
// 		// 			.GetSchemaNameOrDefault(messageType, SchemaName.From(Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream))))
// 		// 		.Set(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Bytes);
// 		//
// 		// 	return readOnlyMemoryBytes;
// 		// }
// 		//
// 		// if (value is Memory<byte> memoryBytes) {
// 		// 	context.Metadata
// 		// 		.TrySet(SystemMetadataKeys.SchemaName, TypeRegistry
// 		// 			.GetSchemaNameOrDefault(messageType, SchemaName.From(Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream))))
// 		// 		.Set(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Bytes);
// 		//
// 		// 	return memoryBytes;
// 		// }
// 		//
// 		// if (value is byte[] byteArray) {
// 		// 	context.Metadata
// 		// 		.TrySet(SystemMetadataKeys.SchemaName, TypeRegistry
// 		// 			.GetSchemaNameOrDefault(messageType, SchemaName.From(Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream))))
// 		// 		.Set(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Bytes);
// 		//
// 		// 	return byteArray;
// 		// }
//
// 		var dataFormat = context.Metadata.Get<SchemaDataFormat>(SystemMetadataKeys.SchemaDataFormat);
// 		var schemaName = TypeRegistry.GetSchemaNameOrDefault(messageType, SchemaName.From(Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream)));
//
// 		if (Options.AutoRegister) {
// 			var versionInfo = await SchemaManager
// 				.TryRegisterSchema(schemaName, messageType, dataFormat, context.Stream, cancellationToken)
// 				.ConfigureAwait(false);
//
// 			TypeRegistry.TryRegister(schemaName, messageType);
//
// 			context.Metadata
// 				.Set(SystemMetadataKeys.SchemaName, schemaName)
// 				.Set(SystemMetadataKeys.SchemaVersionId, versionInfo.VersionId);
// 		}
// 		else {
// 			context.Metadata
// 				.Set(SystemMetadataKeys.SchemaName, schemaName);
//
// 			if (Options.Validate) {
// 				var lastSchemaVersionId = await SchemaManager
// 					.EnsureSchemaCompatibility(schemaName, messageType, dataFormat, cancellationToken)
// 					.ConfigureAwait(false);
//
// 				TypeRegistry.TryRegister(schemaName, messageType);
//
// 				context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, lastSchemaVersionId);
// 			}
// 			else {
// 				TypeRegistry.TryRegister(schemaName, messageType);
//
// 				// if (TypeRegistry.TryGetMessageType(schemaName, out var registeredType)) {
// 				// 	if (registeredType != messageType)
// 				// 		throw new Exception($"Message schema '{schemaName}' is already registered with a different type: {registeredType}");
// 				// }
// 				// else
// 				// 	TypeRegistry.TryRegister(schemaName, messageType);
// 			}
// 		}
//
// 		try {
// 			return Serialize(value);
// 		}
// 		catch (Exception ex) {
// 			throw new SerializationFailedException(DataFormat, schemaName,  ex);
// 		}
//
// 		ReadOnlyMemory<byte> HandleBytes(dynamic bytes) {
// 			context.Metadata
// 				.TrySet(SystemMetadataKeys.SchemaName, TypeRegistry
// 					.GetSchemaNameOrDefault(messageType, Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream)))
// 				.Set(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Bytes);
//
// 			return bytes;
// 		}
// 	}
//
// 	async ValueTask<object?> ISchemaSerializer.Deserialize(ReadOnlyMemory<byte> data, SchemaSerializationContext context, CancellationToken cancellationToken) {
// 		if (data.IsEmpty)
// 			return null;
//
// 		var dataFormat = context.Metadata.Get<SchemaDataFormat>(SystemMetadataKeys.SchemaDataFormat);
// 		if (dataFormat == SchemaDataFormat.Bytes)
// 			return data;
//
// 		if (dataFormat != DataFormat)
// 			throw new UnsupportedSchemaException(DataFormat, context.DataFormat);
//
// 		var schemaName = SchemaName.From(
// 			context.Metadata.Get<string>(SystemMetadataKeys.SchemaName) ?? throw new Exception("Schema name is missing in the metadata")
// 		);
//
// 		if (TypeRegistry.TryGetMessageType(schemaName, out var messageType)) {
// 			if (Options.Validate) {
// 				// if a schema version id is present it means the new client was used
// 				if (context.Metadata.TryGet<string>(SystemMetadataKeys.SchemaVersionId, out var schemaVersionIdValue)) {
// 					var schemaVersionId = SchemaVersionId.From(schemaVersionIdValue!);
//
// 					// do we throw from the schema manager or do we use the result?
// 					await SchemaManager
// 						.EnsureSchemaCompatibility(schemaVersionId, messageType, dataFormat, cancellationToken)
// 						.ConfigureAwait(false);
// 				}
// 				else {
// 					// fallback behaviour to handle validation of messages
// 					// that were not appended using the new client
//
// 					var lastSchemaVersionId = await SchemaManager
// 						.EnsureSchemaCompatibility(schemaName, messageType, dataFormat, cancellationToken)
// 						.ConfigureAwait(false);
//
// 					context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, lastSchemaVersionId);
// 				}
// 			}
// 		}
// 		else {
// 			// try to resolve/discover the clr type directly like magic
// 			// it will only work if the schema name (old event type name)
// 			// is the full clr type name
// 			if (!TypeResolver.TryResolveType(schemaName, out var foundType))
// 				throw new Exception($"Schema name '{schemaName}' is not registered and message type could not be resolved");
//
// 			TypeRegistry.TryRegister(schemaName, foundType);
//
// 			if (Options.Validate) {
// 				var lastSchemaVersionId = await SchemaManager
// 					.EnsureSchemaCompatibility(schemaName, messageType, dataFormat, cancellationToken)
// 					.ConfigureAwait(false);
//
// 				context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, lastSchemaVersionId);
// 			}
// 		}
//
// 		try {
// 			return Deserialize(data, messageType);
// 		}
// 		catch (Exception ex) {
// 			throw new DeserializationFailedException(DataFormat, schemaName,  ex);
// 		}
// 	}
//
// 	protected abstract ReadOnlyMemory<byte> Serialize(object? value);
//
// 	protected abstract object Deserialize(ReadOnlyMemory<byte> data, Type resolvedType);
//
// 	// 	async ValueTask<ReadOnlyMemory<byte>> ISchemaSerializer.Serialize(object? value, SchemaSerializationContext context, CancellationToken cancellationToken) {
// 	//
// 	// 	// should we? or just blow up?
// 	// 	if (value is null)
// 	// 		return ReadOnlyMemory<byte>.Empty;
// 	//
// 	// 	var messageType = value.GetType();
// 	// 	var schemaName  = SchemaName.From(Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream));
// 	//
// 	// 	// we don't really care about verifying or registering the schema
// 	// 	// for raw bytes because we gave control to the developer/user
// 	// 	// the schema name should be already set? or should we generate it here?
// 	// 	if (value is ReadOnlyMemory<byte> readOnlyBytes) {
// 	// 		context.Metadata
// 	// 			.TrySet(SystemMetadataKeys.SchemaName, schemaName)
// 	// 			.Set(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Bytes);
// 	//
// 	// 		return readOnlyBytes;
// 	// 	}
// 	//
// 	// 	if (value is Memory<byte> memoryBytes) {
// 	// 		context.Metadata
// 	// 			.TrySet(SystemMetadataKeys.SchemaName, schemaName)
// 	// 			.Set(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Bytes);
// 	// 		return memoryBytes;
// 	// 	}
// 	//
// 	// 	if (value is byte[] byteArray) {
// 	// 		context.Metadata
// 	// 			.TrySet(SystemMetadataKeys.SchemaName, schemaName)
// 	// 			.Set(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Bytes);
// 	// 		return byteArray;
// 	// 	}
// 	//
// 	// 	if (Options.AutoRegister) {
// 	// 		if (TypeRegistry.TryGetMessageType(schemaName, out var registeredType)) {
// 	// 			if (registeredType != messageType)
// 	// 				throw new Exception($"Message schema '{schemaName}' is already registered with a different type: {registeredType}");
// 	// 		}
// 	// 		else {
// 	// 			var versionInfo = await SchemaManager
// 	// 				.TryRegisterSchema(schemaName, messageType, DataFormat, context.Stream, cancellationToken)
// 	// 				.ConfigureAwait(false);
// 	//
// 	// 			context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, versionInfo.VersionId);
// 	// 			TypeRegistry.TryRegister(schemaName, messageType);
// 	// 		}
// 	// 	}
// 	// 	else {
// 	// 		throw new Exception($"The message schema for {messageType.FullName} is not registered and auto registration is disabled.");
// 	// 	}
// 	//
// 	// 	if (Options.Validate) {
// 	// 		if (Options.AutoRegister) {
// 	// 			if (TypeRegistry.TryGetMessageType(schemaName, out var registeredType)) {
// 	// 				if (registeredType != messageType)
// 	// 					throw new Exception($"Schema name '{schemaName}' is already registered with a different type: {registeredType}");
// 	// 			}
// 	// 			else {
// 	//
// 	//
// 	// 				// var getOrRegisterSchemaResult = await SchemaRegistry
// 	// 				// 	.GetOrRegisterSchema(schemaName, definition, DataFormat, cancellationToken)
// 	// 				// 	.ConfigureAwait(false);
// 	// 				//
// 	// 				// getOrRegisterSchemaResult.Switch(
// 	// 				// 	schema => {
// 	// 				// 		context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, schema.SchemaVersionId);
// 	// 				// 		TypeRegistry.Register(schemaName, messageType);
// 	// 				// 	},
// 	// 				// 	error  => throw new Exception($"Failed to auto register schema: {schemaName}", error.Value)
// 	// 				// );
// 	// 			}
// 	// 		}
// 	// 		else {
// 	// 			var validateSchemaResult = await SchemaRegistry
// 	// 				.CheckSchemaCompatibility(schemaName, definition, context.DataFormat, cancellationToken)
// 	// 				.ConfigureAwait(false);
// 	//
// 	// 			validateSchemaResult.Switch(
// 	// 				versionId  => {
// 	// 					context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, versionId);
// 	// 					TypeRegistry.TryRegister(schemaName, messageType);
// 	// 				},
// 	// 				errors   => throw new Exception($"Invalid schema: {schemaName} - {errors.Select(x => x.ToString()).ToArray()}"),
// 	// 				notfound => throw new Exception($"Schema not found: {schemaName}")
// 	// 			);
// 	// 		}
// 	// 	}
// 	// 	else
// 	// 		TypeRegistry.TryRegister(schemaName, messageType);
// 	//
// 	// 	// enrich the metadata with schema name and data format
// 	// 	context.Metadata
// 	// 		.Set(SystemMetadataKeys.SchemaName, schemaName)
// 	// 		.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
// 	//
// 	// 	try {
// 	// 		return Serialize(value);
// 	// 	}
// 	// 	catch (Exception ex) {
// 	// 		throw new SerializationFailedException(DataFormat, schemaName,  ex);
// 	// 	}
// 	// }
//
//
// 	// async ValueTask<ReadOnlyMemory<byte>> SerializeOnly(object? value, SchemaSerializationContext context, CancellationToken cancellationToken) {
// 	// 	if (value is null)
// 	// 		return null;
// 	//
// 	// 	// we don't really care about verifying or registering the schema
// 	// 	// for bytes because we gave control to the developer/user
// 	// 	// the schema name must always be set when passing bytes
// 	// 	if (value is ReadOnlyMemory<byte> or Memory<byte> or byte[])
// 	// 		return (dynamic)value;
// 	//
// 	// 	var messageType = value.GetType();
// 	// 	var schemaName  = SchemaName.From(Options.SchemaNameStrategy.GenerateSchemaName(messageType, context.Stream));
// 	// 	var definition  = SchemaExporter.ExportSchemaDefinition(messageType);
// 	//
// 	// 	if (Options.Validate) {
// 	// 		if (Options.AutoRegister) {
// 	// 			if (TypeRegistry.GetClrType(schemaName) is { } type) {
// 	// 				if (type != messageType)
// 	// 					throw new Exception($"Schema name '{schemaName}' is already registered with a different type: {type}");
// 	// 			}
// 	// 			else {
// 	//
// 	//
// 	// 				// var getOrRegisterSchemaResult = await SchemaRegistry
// 	// 				// 	.GetOrRegisterSchema(schemaName, definition, DataFormat, cancellationToken)
// 	// 				// 	.ConfigureAwait(false);
// 	// 				//
// 	// 				// getOrRegisterSchemaResult.Switch(
// 	// 				// 	schema => {
// 	// 				// 		context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, schema.SchemaVersionId);
// 	// 				// 		TypeRegistry.Register(schemaName, messageType);
// 	// 				// 	},
// 	// 				// 	error  => throw new Exception($"Failed to auto register schema: {schemaName}", error.Value)
// 	// 				// );
// 	// 			}
// 	// 		}
// 	// 		else {
// 	// 			var validateSchemaResult = await SchemaRegistry
// 	// 				.CheckSchemaCompatibility(schemaName, definition, context.DataFormat, cancellationToken)
// 	// 				.ConfigureAwait(false);
// 	//
// 	// 			validateSchemaResult.Switch(
// 	// 				versionId  => {
// 	// 					context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, versionId);
// 	// 					TypeRegistry.Register(schemaName, messageType);
// 	// 				},
// 	// 				errors   => throw new Exception($"Invalid schema: {schemaName} - {errors.Select(x => x.ToString()).ToArray()}"),
// 	// 				notfound => throw new Exception($"Schema not found: {schemaName}")
// 	// 			);
// 	// 		}
// 	// 	}
// 	// 	else
// 	// 		TypeRegistry.Register(schemaName, messageType);
// 	//
// 	// 	// enrich the metadata with schema name and data format
// 	// 	context.Metadata
// 	// 		.Set(SystemMetadataKeys.SchemaName, schemaName)
// 	// 		.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
// 	//
// 	// 	try {
// 	// 		return Serialize(value);
// 	// 	}
// 	// 	catch (Exception ex) {
// 	// 		throw new SerializationFailedException(DataFormat, schemaName,  ex);
// 	// 	}
// 	// }
// }
//
// public class SchemaManager(KurrentRegistryClient schemaRegistry, ISchemaExporter schemaExporter) {
// 	KurrentRegistryClient SchemaRegistry     { get; } = schemaRegistry;
// 	ISchemaExporter       SchemaExporter     { get; } = schemaExporter;
//
// 	// ConcurrentDictionary<Type, List<SchemaVersionId>> CompatibleVersions { get; } = new();
// 	// ConcurrentDictionary<SchemaName, SchemaVersion>   RegisteredSchemas  { get; } = new();
//
// 	ConcurrentDictionary<Type, List<SchemaVersionDescriptor>> CompatibleVersions2 { get; } = new();
//
// 	// /// <summary>
// 	// /// Retrieves the schema version for the specified schema name, or registers a new one if it does not exist.
// 	// /// </summary>
// 	// /// <param name="schemaName">The unique name of the schema.</param>
// 	// /// <param name="messageType">The type associated with the schema.</param>
// 	// /// <param name="dataFormat">The data format (e.g., Json, Protobuf, etc.) of the schema.</param>
// 	// /// <param name="stream">The stream for which the schema is associated, if any.</param>
// 	// /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
// 	// /// <returns>A descriptor of the schema version, including its ID and version number.</returns>
// 	// public async ValueTask<SchemaVersionDescriptor> GetOrRegisterSchema(
// 	// 	SchemaName schemaName, Type messageType, SchemaDataFormat dataFormat, string stream, CancellationToken cancellationToken
// 	// ) {
// 	// 	if (schemaName == SchemaName.None)
// 	// 		schemaName = SchemaNameStrategy.GenerateSchemaName(messageType, stream);
// 	//
// 	// 	if (RegisteredSchemas.TryGetValue(schemaName, out var schemaVersion))
// 	// 		return new SchemaVersionDescriptor(schemaVersion.VersionId, schemaVersion.VersionNumber);
// 	//
// 	// 	var getSchemaVersionResult = await SchemaRegistry
// 	// 		.GetSchemaVersion(schemaName, null, cancellationToken)
// 	// 		.ConfigureAwait(false);
// 	//
// 	// 	return await getSchemaVersionResult.Match<ValueTask<SchemaVersionDescriptor>>(
// 	// 		versionInfo => {
// 	// 			RegisteredSchemas.TryAdd(schemaName, versionInfo);
// 	// 			CompatibleVersions.TryAdd(messageType, [versionInfo.VersionId]);
// 	// 			TypeRegistry.TryRegister(schemaName, messageType);
// 	// 			var descriptor = new SchemaVersionDescriptor(versionInfo.VersionId, versionInfo.VersionNumber);
// 	// 			return new(descriptor);
// 	// 		},
// 	// 		async notFound => {
// 	// 			var definition = SchemaExporter.ExportSchemaDefinition(messageType);
// 	//
// 	// 			var createSchemaResult = await SchemaRegistry
// 	// 				.CreateSchema(schemaName, definition, dataFormat, cancellationToken)
// 	// 				.ConfigureAwait(false);
// 	//
// 	// 			return await createSchemaResult.Match(
// 	// 				versionInfo => {
// 	// 					var added = RegisteredSchemas.TryAdd(schemaName, new SchemaVersion {
// 	// 						VersionId        = versionInfo.VersionId,
// 	// 						VersionNumber    = versionInfo.VersionNumber, // always 1 so why return it?
// 	// 						SchemaDefinition = definition,
// 	// 						DataFormat       = dataFormat,
// 	// 						RegisteredAt     = DateTimeOffset.UtcNow // perhaps the date should be returned? or it does not really matter?
// 	// 					});
// 	//
// 	// 					// edge case: the schema was created by another client between checking and creating
// 	// 					if (!added)
// 	// 						return GetOrRegisterSchema(schemaName, messageType, dataFormat, stream, cancellationToken);
// 	//
// 	// 					CompatibleVersions.TryAdd(messageType, [versionInfo.VersionId]);
// 	// 					TypeRegistry.TryRegister(schemaName, messageType);
// 	// 					return new(new SchemaVersionDescriptor(versionInfo.VersionId, versionInfo.VersionNumber));
// 	// 				},
// 	// 				// edge case: the schema was created by another client between checking and creating
// 	// 				alreadyExists => GetOrRegisterSchema(schemaName, messageType, dataFormat, stream, cancellationToken)
// 	// 			);
// 	// 		}
// 	// 	);
// 	// }
//
// 	/// <summary>
// 	/// Attempts to register a schema for the specified schema name and message type in the schema registry.
// 	/// If the schema already exists, the corresponding version information is returned; otherwise, a new schema is created.
// 	/// </summary>
// 	/// <param name="schemaName">The name of the schema to register.</param>
// 	/// <param name="messageType">The type to serialize and register in the schema registry.</param>
// 	/// <param name="dataFormat">The data format (e.g., Json, Protobuf, etc.) of the schema.</param>
// 	/// <param name="stream">The stream associated with the schema, if applicable.</param>
// 	/// <param name="cancellationToken">A token to monitor for cancellation requests during the operation.</param>
// 	/// <returns>The version descriptor of the schema, including its unique ID and version number.</returns>
// 	public async ValueTask<SchemaVersionDescriptor> TryRegisterSchema(
// 		SchemaName schemaName, Type messageType, SchemaDataFormat dataFormat, string stream, CancellationToken cancellationToken
// 	) {
// 		if (schemaName == SchemaName.None)
// 			throw new ArgumentNullException(nameof(schemaName), "Schema name cannot be None");
//
// 		// if (TypeRegistry.TryGetMessageType(schemaName, out var registeredType)) {
// 		// 	if (registeredType != messageType)
// 		// 		throw new Exception($"Message schema '{schemaName}' is already registered with a different type: {registeredType}");
// 		// }
//
// 		if (CompatibleVersions2.TryGetValue(messageType, out var versions))
// 			return versions.Last();
//
// 		var getSchemaVersionResult = await SchemaRegistry
// 			.GetSchemaVersion(schemaName, null, cancellationToken)
// 			.ConfigureAwait(false);
//
//
// 		if (getSchemaVersionResult.Value is SchemaVersion version) {
// 			var versionInfo = new SchemaVersionDescriptor(version.VersionId, version.VersionNumber);
// 			CompatibleVersions2.TryAdd(messageType, [versionInfo]);
// 			return versionInfo;
// 		}
// 		else {
// 			var definition = SchemaExporter.ExportSchemaDefinition(messageType);
//
// 			var createSchemaResult = await SchemaRegistry
// 				.CreateSchema(schemaName, definition, dataFormat, cancellationToken)
// 				.ConfigureAwait(false);
//
// 			return await createSchemaResult.Match(
// 				versionInfo => {
// 					// edge case: the schema was created by another client between checking and creating
// 					var added = CompatibleVersions2.TryAdd(messageType, [versionInfo]);
// 					if (added) {
// 						return new(versionInfo);
// 					}
// 					else
// 						return TryRegisterSchema(schemaName, messageType, dataFormat, stream, cancellationToken);
// 				},
// 				// edge case: the schema was created by another client between checking and creating
// 				alreadyExists => TryRegisterSchema(schemaName, messageType, dataFormat, stream, cancellationToken)
// 			);
// 		}
//
// 		// return await getSchemaVersionResult.Match<ValueTask<SchemaVersionDescriptor>>(
// 		// 	version => {
// 		// 		CompatibleVersions2.TryAdd(messageType, [new SchemaVersionDescriptor(version.VersionId, version.VersionNumber)]);
// 		// 		TypeRegistry.TryRegister(schemaName, messageType);
// 		// 		var versionInfo = new SchemaVersionDescriptor(version.VersionId, version.VersionNumber);
// 		// 		return new(versionInfo);
// 		// 	},
// 		// 	async notFound => {
// 		// 		var definition = SchemaExporter.ExportSchemaDefinition(messageType);
// 		//
// 		// 		var createSchemaResult = await SchemaRegistry
// 		// 			.CreateSchema(schemaName, definition, dataFormat, cancellationToken)
// 		// 			.ConfigureAwait(false);
// 		//
// 		// 		return await createSchemaResult.Match(
// 		// 			versionInfo => {
// 		// 				var added = CompatibleVersions2.TryAdd(messageType, [versionInfo]);
// 		//
// 		// 				// edge case: the schema was created by another client between checking and creating
// 		// 				if (!added)
// 		// 					return TryRegisterSchema(schemaName, messageType, dataFormat, stream, cancellationToken);
// 		//
// 		// 				TypeRegistry.TryRegister(schemaName, messageType);
// 		// 				return new(versionInfo);
// 		// 			},
// 		// 			// edge case: the schema was created by another client between checking and creating
// 		// 			alreadyExists => TryRegisterSchema(schemaName, messageType, dataFormat, stream, cancellationToken)
// 		// 		);
// 		// 	}
// 		// );
// 	}
//
// 	/// <summary>
// 	/// Used for messages that were appended using the new client.
// 	/// </summary>
// 	/// <param name="schemaVersionId"></param>
// 	/// <param name="messageType"></param>
// 	/// <param name="dataFormat"></param>
// 	/// <param name="cancellationToken"></param>
// 	/// <returns></returns>
// 	public async ValueTask<SchemaVersionId> EnsureSchemaCompatibility(SchemaVersionId schemaVersionId, Type messageType, SchemaDataFormat dataFormat, CancellationToken cancellationToken) {
// 		// Check compatible versions cache and return the last version id if found
// 		if (TryGetLastSchemaVersion(schemaVersionId, out var foundSchemaVersion))
// 			return foundSchemaVersion.VersionId;
//
// 		var definition = SchemaExporter.ExportSchemaDefinition(messageType);
//
// 		var result = await SchemaRegistry
// 			.CheckSchemaCompatibility(schemaVersionId, definition, dataFormat, cancellationToken)
// 			.ConfigureAwait(false);
//
// 		return result.Match(
// 			lastSchemaVersionId => {
// 				// its impossible to update because we start by checking the cache
// 				CompatibleVersions2.AddOrUpdate(
// 					messageType,
// 					static (_, state) => [state.Version, state.LastVersion],
// 					static (_, versions, state) => {
// 						// just so that the last version can represent the latest one.
// 						// might end up not being that useful, but it is a good idea to have it
// 						// if the guids were sortable that would be even better and none of this
// 						// would be needed
// 						versions.Insert(versions.Count - 1, state.Version);
// 						return versions;
// 					}, (
// 						LastVersion: new SchemaVersionDescriptor(lastSchemaVersionId, 0),
// 						Version: new SchemaVersionDescriptor(schemaVersionId, 0)
// 					)
// 				);
//
// 				return lastSchemaVersionId;
// 			},
// 			errors => throw new Exception($"Invalid schema: {schemaVersionId} - {errors.Select(x => x.ToString()).ToArray()}"),
// 			notfound => throw new Exception($"Schema not found: {schemaVersionId}")
// 		);
//
// 		// if (result.IsSchemaVersionId)
// 		// 	CompatibleVersions.AddOrUpdate(
// 		// 		messageType,
// 		// 		static (_, state) => [state.VersionId, state.LastVersionId],
// 		// 		static (_, versions, state) => {
// 		// 			// just so that the last version can represent the latest one.
// 		// 			// might end up not being that useful, but it is a good idea to have it
// 		// 			// if the guids were sortable that would be even better and none of this
// 		// 			// would be needed
// 		// 			versions.Insert(versions.Count - 1, state.VersionId);
// 		// 			return versions;
// 		// 		},
// 		// 		(LastVersionId: result.AsSchemaVersionId, VersionId: schemaVersionId)
// 		// 	);
// 		//
// 		// return result;
// 	}
//
// 	/// <summary>
// 	/// Used for messages that were not appended using the new client.
// 	/// </summary>
// 	/// <param name="schemaName"></param>
// 	/// <param name="messageType"></param>
// 	/// <param name="dataFormat"></param>
// 	/// <param name="cancellationToken"></param>
// 	/// <returns></returns>
// 	public async ValueTask<SchemaVersionId> EnsureSchemaCompatibility(SchemaName schemaName, Type messageType, SchemaDataFormat dataFormat, CancellationToken cancellationToken) {
// 		if (CompatibleVersions2.TryGetValue(messageType, out var versions))
// 			return versions.Last().VersionId;
//
// 		var definition = SchemaExporter.ExportSchemaDefinition(messageType);
//
// 		var result = await SchemaRegistry
// 			.CheckSchemaCompatibility(schemaName, definition, dataFormat, cancellationToken)
// 			.ConfigureAwait(false);
//
// 		return result.Match(
// 			schemaVersionId => {
// 				// its impossible to update because we start by checking the cache
// 				CompatibleVersions2.TryAdd(messageType, [new SchemaVersionDescriptor(schemaVersionId, 0)]);
// 				return schemaVersionId;
// 			},
// 			errors => throw new Exception($"Invalid schema: {schemaName} - {errors.Select(x => x.ToString()).ToArray()}"),
// 			notfound => throw new Exception($"Schema not found: {schemaName}")
// 		);
//
// 		// if (result.IsSchemaVersionId)
// 		// 	CompatibleVersions.TryAdd(messageType, [result.AsSchemaVersionId]);
// 		//
// 		// return result;
// 	}
//
// 	/// <summary>
// 	/// Attempts to retrieve the last schema version ID that is compatible with the provided schema version ID.
// 	/// </summary>
// 	bool TryGetLastSchemaVersion(SchemaVersionId schemaVersionId, out SchemaVersionDescriptor lastSchemaVersion) {
// 		foreach (var entry in CompatibleVersions2) {
// 			if (entry.Value.Any(x => x.VersionId == schemaVersionId)) {
// #if NET48
// 				lastSchemaVersion = entry.Value.Last();
// #else
// 				lastSchemaVersion = entry.Value[^1];
// #endif
// 				return true;
// 			}
// 		}
//
// 		lastSchemaVersion = SchemaVersionDescriptor.None;
// 		return false;
// 	}
// }
