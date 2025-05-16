using KurrentDB.Client.Model;
using KurrentDB.Client.SchemaRegistry.Serialization;
using OneOf;

namespace KurrentDB.Client.SchemaRegistry;


// // I was here
// class SchemaManager(SchemaSerializerOptions options, KurrentRegistryClient schemaRegistry, MessageTypeRegistry typeRegistry) {
// 	SchemaSerializerOptions Options        { get; } = options;
// 	KurrentRegistryClient   SchemaRegistry { get; } = schemaRegistry;
// 	MessageTypeRegistry     TypeRegistry   { get; } = typeRegistry;
//
// 	// 	public async ValueTask<(string SchemaName, Guid SchemaVersionId)> RegisterSchema(Type type, string schemaName, SchemaDataFormat dataFormat, CancellationToken cancellationToken = default) {
// // 		var registeredSchema = schemaInfo.SchemaNameMissing
// // 			? TypeRegistry.TryGetSubject(messageType, schemaInfo.SchemaDataFormat, out var foundSubject)
// // 				? await GetSchema(schemaInfo with { SchemaName = foundSubject })
// // 				: RegisteredSchema.None
// // 			: await GetSchema(schemaInfo);
// //
// // 		if (registeredSchema != RegisteredSchema.None)
// // 			return registeredSchema;
// //
// // 		if (!Options.AutoRegister)
// // 			throw new Exception($"The message schema for {messageType.FullName} is not registered and auto registration is disabled.");
// //
// // 		return await RegisterSchema(schemaInfo, "", messageType);
// // 	}
//
// 	public async ValueTask<SchemaVersionDescriptor> GetOrCreateSchema(
// 		Type messageType, SchemaName schemaName, string schemaDefinition,
// 		SchemaDataFormat dataFormat, CancellationToken cancellationToken = default
// 	) {
//
//
// 		if (!Options.AutoRegister)
// 			throw new Exception($"The message schema for {messageType.FullName} is not registered and auto registration is disabled.");
//
// 		if (TypeRegistry.TryRegister(messageType, schemaName, dataFormat)) {
// 			var result = await SchemaRegistry
// 				.CreateSchema(schemaName, schemaDefinition, dataFormat, cancellationToken)
// 				.ConfigureAwait(false);
//
// 			return result.Match(
// 				schemaVersion => new SchemaVersionDescriptor(schemaVersion.VersionId, schemaVersion.VersionNumber),
// 				alreadyExists => throw new Exception($"Schema {schemaName} already exists")
// 			);
// 		}
// 		else {
// 			var result = await SchemaRegistry.GetSchemaVersion(schemaName, versionNumber: null, cancellationToken);
//
// 			return result.Match(
// 				schemaVersion => new SchemaVersionDescriptor(schemaVersion.VersionId, schemaVersion.VersionNumber),
// 				notFound => throw new Exception($"Schema {schemaName} not found")
// 			);
// 		}
//
// 		// var result = await SchemaRegistry.GetSchemaVersion(schemaName, versionNumber: null, cancellationToken);
// 		//
// 		// if (result.Value is SchemaVersion schemaVersion) {
// 		// 	if (schemaVersion.DataFormat != dataFormat)
// 		// 		throw new Exception($"The schema {schemaName} is registered with a different data format: {schemaVersion.DataFormat}");
// 		//
// 		// 	return new SchemaVersionDescriptor(result.AsSchemaVersion.VersionId, result.AsSchemaVersion.VersionNumber);
// 		// }
// 		//
// 		// return result.Match<SchemaVersionDescriptor>(
// 		// 	 schemaVersion => {
// 		// 		if (schemaVersion.DataFormat != dataFormat)
// 		// 			throw new Exception($"The schema {schemaName} is registered with a different data format: {schemaVersion.DataFormat}");
// 		//
// 		// 		var version = new SchemaVersionDescriptor(schemaVersion.VersionId, schemaVersion.VersionNumber);
// 		//
// 		// 		return version;
// 		// 	 },
// 		// 	async notfound => {
// 		// 		if (!Options.AutoRegister)
// 		// 			throw new Exception($"The message schema for {messageType.FullName} is not registered and auto registration is disabled.");
// 		//
// 		// 		var createSchemaResult = await SchemaRegistry
// 		// 			.CreateSchema(schemaName, schemaDefinition, dataFormat, cancellationToken)
// 		// 			.ConfigureAwait(false);
// 		//
// 		// 		return createSchemaResult.Match<SchemaVersionDescriptor>(
// 		// 			schemaVersion => {
// 		// 				TypeRegistry.Register(messageType, schemaName, dataFormat);
// 		// 				return new SchemaVersionDescriptor(schemaVersion.VersionId, schemaVersion.VersionNumber);
// 		// 			},
// 		// 			alreadyExists => throw new Exception($"Schema {schemaName} already exists with version {alreadyExists.VersionNumber}")
// 		// 		);
// 		// 	}
// 		// );
// 	}
//
// 	public async ValueTask<SchemaVersionDescriptor> AutoRegisterSchema(
// 		Type messageType, SchemaName schemaName, string schemaDefinition,
// 		SchemaDataFormat dataFormat, CancellationToken cancellationToken = default
// 	) {
// 		var result = TypeRegistry.IsMessageTypeRegistered(messageType, dataFormat)
// 			? await SchemaRegistry.GetSchemaVersion(schemaName, versionNumber: null, cancellationToken)
// 			: ErrorDetails.SchemaNotFound.Value;
//
// 		if (result.IsSchemaVersion)
// 			return new SchemaVersionDescriptor(result.AsSchemaVersion.VersionId, result.AsSchemaVersion.VersionNumber);
//
// 		if (!Options.AutoRegister)
// 			throw new Exception($"The message schema for {messageType.FullName} is not registered and auto registration is disabled.");
//
// 		var createSchemaResult = await SchemaRegistry.CreateSchema(
// 			schemaName,
// 			schemaDefinition,
// 			dataFormat,
// 			CompatibilityMode.None,
// 			"",
// 			new Dictionary<string, string>(),
// 			cancellationToken
// 		);
//
// 		return createSchemaResult.Match(
// 			schemaVersion => {
// 				TypeRegistry.Register(messageType, schemaName, dataFormat);
// 				return new SchemaVersionDescriptor(schemaVersion.VersionId, schemaVersion.VersionNumber);
// 			},
// 			alreadyExists => throw new Exception($"Schema {schemaName} already exists with version {alreadyExists.VersionNumber}")
// 		);
// 	}
//
// 	// public static async ValueTask<(string SchemaName, Guid SchemaVersionId)> AutoRegisterSchema(
// 	// 	Type type, SchemaName schemaName, string schemaDefinition,
// 	// 	SchemaDataFormat dataFormat, CancellationToken cancellationToken = default
// 	// ) {
// 	// 	var registeredSchema = schemaInfo.SchemaNameMissing
// 	// 		? TypeRegistry.TryGetSubject(messageType, schemaInfo.SchemaDataFormat, out var foundSubject)
// 	// 			? await GetSchema(schemaInfo with { SchemaName = foundSubject })
// 	// 			: RegisteredSchema.None
// 	// 		: await GetSchema(schemaInfo);
// 	//
// 	// 	if (registeredSchema != RegisteredSchema.None)
// 	// 		return registeredSchema;
// 	//
// 	// 	if (!Options.AutoRegister)
// 	// 		throw new Exception($"The message schema for {messageType.FullName} is not registered and auto registration is disabled.");
// 	//
// 	// 	return await RegisterSchema(schemaInfo, "", messageType);
// 	//
// 	//
// 	// }
//
//
// }
//


















// using Google.Protobuf;
// using Grpc.Core;
// using KurrentDB.Client.Model;
// using KurrentDB.Protocol.Registry.V2;
// using OneOf;
// using static KurrentDB.Protocol.Registry.V2.SchemaRegistryService;
// using Metadata = KurrentDB.Client.Model.Metadata;
// using RegisteredSchema = KurrentDB.Client.Model.RegisteredSchema;
// using SchemaDataFormat = KurrentDB.Client.Model.SchemaDataFormat;
//
// namespace KurrentDB.Client.Schema;
//
// [GenerateOneOf]
// public partial class GetOrRegisterSchemaResult : OneOfBase<RegisteredSchema, OneOf.Types.Error<Exception>> {
// 	public bool IsError            => IsT1;
// 	public bool IsRegisteredSchema => IsT0;
// }
//
// [GenerateOneOf]
// public partial class ValidateSchemaResult : OneOfBase<OneOf.Types.Success<string>, OneOf.Types.Error<Exception>, OneOf.Types.NotFound>;
//
//
// [GenerateOneOf]
// public partial class RegisterSchemaResult : OneOfBase<SchemaVersion, Exception>;
//
//
// /// <summary>
// /// Service abstracting the schema registry.
// /// </summary>
// public interface IKurrentSchemaRegistry {
// 	ValueTask<GetOrRegisterSchemaResult> GetOrRegisterSchema(string schemaName, string schemaDefinition, SchemaDataFormat dataFormat, CancellationToken cancellationToken);
//
// 	// registers the first version and throws if it already exists
// 	ValueTask<RegisterSchemaResult> RegisterSchema(string schemaName, string schemaDefinition, SchemaDataFormat dataFormat, CancellationToken cancellationToken);
//
// 	ValueTask<ValidateSchemaResult> ValidateSchema(string schemaName, string schemaDefinition, CancellationToken cancellationToken);
// }
//
//
// public record SchemaVersion(Guid VersionId, int VersionNumber);
//
// class RegistryClient : IKurrentSchemaRegistry {
// 	public RegistryClient(KurrentDBClient proxy) => Proxy = proxy;
//
// 	KurrentDBClient Proxy { get; }
//
// 	public async ValueTask<GetOrRegisterSchemaResult> GetOrRegisterSchema(string schemaName, string schemaDefinition, SchemaDataFormat dataFormat, CancellationToken cancellationToken = default) {
// 		var client = await Proxy.ClientFactory
// 			.CreateAsync<SchemaRegistryServiceClient>(cancellationToken)
// 			.ConfigureAwait(false);
//
// 		var request = new  KurrentDB.Protocol.Registry.V2.Reg.GetOrRegisterSchemaRequest {
// 			SchemaName       = schemaName,
// 			SchemaDefinition = schemaDefinition,
// 			SchemaDataFormat = dataFormat
// 		};
//
// 		return await Proxy.GetOrRegisterSchemaAsync(
// 			new GetOrRegisterSchemaRequest {
// 				SchemaName       = schemaName,
// 				SchemaDefinition = schemaDefinition,
// 				SchemaDataFormat = dataFormat
// 			},
// 			cancellationToken: cancellationToken
// 		).ConfigureAwait(false);
// 	}
//
// 	public async ValueTask<RegisterSchemaResult> RegisterSchema(string schemaName, string schemaDefinition, SchemaDataFormat dataFormat, CancellationToken cancellationToken = default) {
// 		var client = await Proxy.ClientFactory
// 			.CreateAsync<SchemaRegistryServiceClient>(cancellationToken)
// 			.ConfigureAwait(false);
//
// 		var request = new CreateSchemaRequest {
// 			SchemaName = schemaName,
// 			Details = new SchemaDetails {
// 				DataFormat    = (Protocol.Registry.V2.SchemaDataFormat)dataFormat,
// 				Compatibility = CompatibilityMode.None, // how to do this, unspecified and the server will set the value?
// 				Description   = null
// 			},
// 			SchemaDefinition = ByteString.CopyFromUtf8(schemaDefinition)
// 		};
//
// 		try {
// 			var response    = await client.CreateSchemaAsync(request, Proxy.GetCallOptions(cancellationToken)).ConfigureAwait(false);
// 			return new SchemaVersion(Guid.Parse(response.SchemaVersionId), response.VersionNumber);
// 		}
// 		// catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists) {
// 		// 	return new RegisterSchemaResult(ex);
// 		// }
// 		catch (Exception ex) {
// 			return ex;
// 		}
// 	}
//
// 	public async ValueTask<ValidateSchemaResult> ValidateSchema(string schemaName, string schemaDefinition, CancellationToken cancellationToken = default) {
// 		var client = await Proxy.ClientFactory
// 			.CreateAsync<SchemaRegistryServiceClient>(cancellationToken)
// 			.ConfigureAwait(false);
//
// 		var request = new CheckSchemaCompatibilityRequest {
// 			SchemaName = schemaName,
// 			Definition = ByteString.CopyFromUtf8(schemaDefinition),
// 			DataFormat = Protocol.Registry.V2.SchemaDataFormat.Json
// 		};
//
// 		var result = await client.CheckSchemaCompatibilityAsync(request, Proxy.GetCallOptions(cancellationToken)).ConfigureAwait(false);
// 	}
// }
//
//
//
// public interface IKurrentSchemaManager {
// 	// manual registration - it tries to get the schema first and if it doesn't exist, it registers it
// 	ValueTask<(string SchemaName, Guid SchemaVersionId)> RegisterSchema(Type type, string schemaName, SchemaDataFormat dataFormat, CancellationToken cancellationToken = default);
//
// 	// zero code because it will always use the type full name to create the schema name
// 	ValueTask<(string SchemaName, Guid SchemaVersionId)> RegisterSchema(Type type, SchemaDataFormat dataFormat, CancellationToken cancellationToken = default);
// }
//
//
// public class SchemaManager : IKurrentSchemaManager {
// 	public SchemaManager(IKurrentSchemaRegistry schemaRegistry, MessageTypeRegistry typeRegistry) {
// 		SchemaRegistry    = schemaRegistry;
// 		TypeRegistry = typeRegistry;
// 	}
//
// 	IKurrentSchemaRegistry SchemaRegistry { get; }
// 	MessageTypeRegistry    TypeRegistry   { get; }
//
// 	public async ValueTask<(string SchemaName, Guid SchemaVersionId)> RegisterSchema(Type type, string schemaName, SchemaDataFormat dataFormat, CancellationToken cancellationToken = default) {
// 		var registeredSchema = schemaInfo.SchemaNameMissing
// 			? TypeRegistry.TryGetSubject(messageType, schemaInfo.SchemaDataFormat, out var foundSubject)
// 				? await GetSchema(schemaInfo with { SchemaName = foundSubject })
// 				: RegisteredSchema.None
// 			: await GetSchema(schemaInfo);
//
// 		if (registeredSchema != RegisteredSchema.None)
// 			return registeredSchema;
//
// 		if (!Options.AutoRegister)
// 			throw new Exception($"The message schema for {messageType.FullName} is not registered and auto registration is disabled.");
//
// 		return await RegisterSchema(schemaInfo, "", messageType);
// 	}
//
// 	public async ValueTask<(string SchemaName, Guid SchemaVersionId)> RegisterSchema(Type type, SchemaDataFormat dataFormat, CancellationToken cancellationToken = default) =>
// 		await RegisterSchema(type, NameStrategy.GenerateSchemaName(type), dataFormat, cancellationToken).ConfigureAwait(false);
// }
//
//
//
// //
// // class LegacyKurrentSchemaManager : IKurrentSchemaManager {
// // 	public LegacyKurrentSchemaManager(KurrentDBClientSettings legacySettings, LegacyClientFactory clientFactory) {
// // 		NameStrategy  = legacySettings.Schema.NameStrategy;
// // 		AutoRegister  = legacySettings.Schema.AutoRegister;
// // 		TypeResolver  = legacySettings.MessageTypeResolver;
// // 		ClientFactory = clientFactory;
// // 	}
// //
// // 	bool                 AutoRegister  { get; }
// // 	ISchemaNameStrategy  NameStrategy  { get; }
// // 	LegacyClientFactory  ClientFactory { get; }
// // 	IMessageTypeResolver TypeResolver  { get; }
// //
// // 	public async ValueTask<RegisteredSchema> GetOrRegisterSchema(SchemaInfo schemaInfo, Type messageType, CancellationToken cancellationToken = default) {
// // 		var client = await ClientFactory.CreateAsync<SchemaRegistryServiceClient>(cancellationToken).ConfigureAwait(false);
// //
// // 		// TODO SS: implement the logic to get or register the schema
// //
// // 		throw new NotImplementedException("GetOrRegisterSchema is not implemented yet.");
// // 	}
// //
// // 	public Type ResolveMessageType(string schemaName, string stream, Metadata metadata) =>
// // 		TypeResolver.ResolveType(schemaName, stream, metadata);
// //
// // 	public Type ResolveMessageType(EventRecord record) =>
// // 		TypeResolver.ResolveType(record.EventType, record.EventStreamId, new Metadata()); // record.Metadata
// //
// // 	public SchemaInfo CreateSchemaInfo(string stream, Type type, SchemaDataFormat dataFormat) =>
// // 		new(NameStrategy.GenerateSchemaName(type, stream), dataFormat);
// //
// // 	public SchemaInfo CreateSchemaInfo(Type type, SchemaDataFormat dataFormat) =>
// //         new(NameStrategy.GenerateSchemaName(type, ""), dataFormat);
// // }
