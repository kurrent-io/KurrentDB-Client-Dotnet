// #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
//
// using System.Diagnostics;
// using KurrentDB.Client.Model;
//
// namespace KurrentDB.Client.SchemaRegistry.Serialization;
//
// public record struct SchemaSerializationContext(string Stream, Metadata Metadata, SchemaRegistryPolicy SchemaRegistryPolicy, CancellationToken CancellationToken);
//
// public interface ISchemaSerializer {
// 	SchemaDataFormat DataFormat { get; }
//
// 	ValueTask<ReadOnlyMemory<byte>> Serialize(object? value, SchemaSerializationContext context);
//
// 	ValueTask<object?> Deserialize(ReadOnlyMemory<byte> data, SchemaSerializationContext context);
// }
//
// public record SchemaSerializerOptions {
// 	/// <summary>
// 	/// Configuration options for schema registration, validation, and type mapping behaviors.
// 	/// </summary>
// 	public SchemaRegistrationOptions SchemaRegistration { get; init; } = new SchemaRegistrationOptions();
//
// 	/// <summary>
// 	/// Specifies whether only schemas explicitly mapped to a .NET type should be consumed during
// 	/// deserialization.
// 	/// When <c>true</c>, only schemas with mapped .NET types will be processed during deserialization.
// 	/// </summary>
// 	public bool ConsumeMappedOnly { get; set; } = true;
// }
//
// public abstract class SchemaSerializer(SchemaSerializerOptions options, SchemaManager schemaManager, MessageTypeMapper typeMapper) : ISchemaSerializer {
// 	SchemaSerializerOptions Options       { get; } = options;
// 	SchemaManager           SchemaManager { get; } = schemaManager;
// 	MessageTypeMapper       TypeMapper    { get; } = typeMapper;
//
// 	public abstract SchemaDataFormat DataFormat { get; }
//
// 	#region Serialize
//
// 	async ValueTask<ReadOnlyMemory<byte>> ISchemaSerializer.Serialize(object? value, SchemaSerializationContext context) {
// 		// -------------------------------------------------------------------------------------------------
// 		// these debug asserts ensure the required info is set during the development process
// 		// -------------------------------------------------------------------------------------------------
// 		Debug.Assert(!string.IsNullOrWhiteSpace(context.Stream) || context.Metadata.Get<string>(SystemMetadataKeys.Stream) is not null, "Stream name is missing in the metadata");
// 		Debug.Assert(context.Metadata.Get(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Unspecified) == DataFormat, "Schema data format does not match the serializer data format");
// 		// -------------------------------------------------------------------------------------------------
//
// 		if (value is null)
// 			return ReadOnlyMemory<byte>.Empty;
//
// 		var messageType = value.GetType();
//
// 		var policy = context.SchemaRegistryPolicy
// 			.Resolve(Options.SchemaRegistration, context.Stream)
// 			.EnsureDataFormatCompliance(DataFormat);
//
// 		try {
// 			var result = await RegisterOrValidateSchema(messageType, policy, context.CancellationToken);
//
// 			context.Metadata
// 				.Set(SystemMetadataKeys.SchemaName, result.SchemaName)
// 				.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat);
//
// 			if (result.SchemaVersionId != SchemaVersionId.None)
// 				context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, result.SchemaVersionId);
//
// 			return Serialize(value);
// 		}
// 		catch (Exception ex) {
// 			throw new SerializationFailedException(DataFormat, messageType,  ex);
// 		}
// 	}
//
// 	async ValueTask<(SchemaName SchemaName, SchemaVersionId SchemaVersionId)> RegisterOrValidateSchema(Type messageType, ResolvedSchemaRegistryPolicy policy, CancellationToken ct) {
// 		if (policy.AutoRegisterSchemas)
// 			return await HandleServerRegistration(messageType, policy, ct);
//
// 		if (policy.ValidateSchemas && DataFormat == SchemaDataFormat.Json)
// 			return await HandleServerValidation(messageType, policy, ct);
//
// 		return HandleLocalRegistration(messageType, policy);
// 	}
//
// 	(SchemaName schemaName, SchemaVersionId lastSchemaVersionId) HandleLocalRegistration(Type messageType, ResolvedSchemaRegistryPolicy policy) {
// 		if (policy.AutoMapMessages) {
// 			var mapped = TypeMapper.TryGetSchemaName(messageType, out var schemaName);
// 			if (!mapped) {
// 				schemaName = policy.GetSchemaName(messageType);
// 				TypeMapper.TryMap(schemaName, messageType);
// 			}
//
// 			return (schemaName, SchemaVersionId.None);
// 		}
// 		else if (TypeMapper.TryGetSchemaName(messageType, out var schemaName))
// 			return (schemaName, SchemaVersionId.None);
// 		else
// 			throw new AutoRegistrationDisabledException(DataFormat, messageType);
// 	}
//
// 	async ValueTask<(SchemaName schemaName, SchemaVersionId lastSchemaVersionId)> HandleServerValidation(Type messageType, ResolvedSchemaRegistryPolicy policy, CancellationToken ct) {
// 		var mapped = TypeMapper.TryGetSchemaName(messageType, out var schemaName);
//
// 		if (!mapped)
// 			schemaName = policy.GetSchemaName(messageType);
//
// 		var lastSchemaVersionId = await SchemaManager
// 			.EnsureSchemaCompatibility(schemaName, messageType, DataFormat, ct)
// 			.ConfigureAwait(false);
//
// 		if (!mapped)
// 			TypeMapper.TryMap(schemaName, messageType);
//
// 		return (schemaName, lastSchemaVersionId);
// 	}
//
// 	async ValueTask<(SchemaName SchemaName, SchemaVersionId SchemaVersionId)> HandleServerRegistration(Type messageType,  ResolvedSchemaRegistryPolicy policy, CancellationToken ct) {
// 		var mapped = TypeMapper.TryGetSchemaName(messageType, out var schemaName);
//
// 		if (!mapped)
// 			schemaName = policy.GetSchemaName(messageType);
//
// 		var versionInfo = await SchemaManager
// 			.TryRegisterSchema(schemaName, messageType, DataFormat, ct)
// 			.ConfigureAwait(false);
//
// 		if (!mapped)
// 			TypeMapper.TryMap(schemaName, messageType);
//
// 		return (schemaName, versionInfo.VersionId);
// 	}
//
// 	#endregion
//
// 	#region Deserialize
//
// 	async ValueTask<object?> ISchemaSerializer.Deserialize(ReadOnlyMemory<byte> data, SchemaSerializationContext context) {
// 		var schemaInfo = context.Metadata.GetSchemaInfo();
//
// 		// -------------------------------------------------------------------------------------------------
// 		// these debug asserts ensure the required info is set during the development process
// 		// -------------------------------------------------------------------------------------------------
// 		Debug.Assert(schemaInfo.HasSchemaName, "Schema name is missing in the metadata");
// 		Debug.Assert(schemaInfo.HasDataFormat, "Schema data format is missing in the metadata");
// 		Debug.Assert(schemaInfo.DataFormat == DataFormat, "Schema data format does not match the serializer data format");
// 		// -------------------------------------------------------------------------------------------------
//
// 		if (data.IsEmpty)
// 			return null;
//
// 		try {
// 			var policy = context.SchemaRegistryPolicy
// 				.Resolve(Options.SchemaRegistration, context.Stream)
// 				.EnsureDataFormatCompliance(DataFormat);
//
// 			var result = await ValidateAndEnsureSchemaCompatibility(schemaInfo, policy, context.CancellationToken);
//
// 			// set the schema version id in the metadata
// 			if(result.SchemaVersionId != SchemaVersionId.None)
// 				context.Metadata.Set(SystemMetadataKeys.SchemaVersionId, result.SchemaVersionId);
//
// 			return Deserialize(data, result.MessageType);
// 		}
// 		catch (Exception ex) {
// 			throw new DeserializationFailedException(DataFormat, schemaInfo.SchemaName,  ex);
// 		}
// 	}
//
// 	async ValueTask<(Type MessageType, SchemaVersionId SchemaVersionId)> ValidateAndEnsureSchemaCompatibility(RecordSchemaInfo schemaInfo, ResolvedSchemaRegistryPolicy policy, CancellationToken ct) {
// 		var messageType = TypeMapper.GetOrResolveMessageType(schemaInfo.SchemaName);
//
// 		if (!policy.ValidateSchemas)
// 			return (messageType, SchemaVersionId.None);
//
// 		if (schemaInfo.HasSchemaVersionId) {
// 			await SchemaManager
// 				.EnsureSchemaCompatibility(schemaInfo.SchemaVersionId, messageType, schemaInfo.DataFormat, ct)
// 				.ConfigureAwait(false);
//
// 			// it was already set so we don't need to do anything and dont return the version id
// 			return (messageType, SchemaVersionId.None);
// 		}
//
// 		// fallback behaviour for backwards compatibility
// 		var lastSchemaVersionId = await SchemaManager
// 			.EnsureSchemaCompatibility(schemaInfo.SchemaName, messageType, schemaInfo.DataFormat, ct)
// 			.ConfigureAwait(false);
//
// 		return (messageType, lastSchemaVersionId);
// 	}
//
// 	#endregion
//
// 	protected abstract ReadOnlyMemory<byte> Serialize(object? value);
//
// 	protected abstract object Deserialize(ReadOnlyMemory<byte> data, Type resolvedType);
// }
