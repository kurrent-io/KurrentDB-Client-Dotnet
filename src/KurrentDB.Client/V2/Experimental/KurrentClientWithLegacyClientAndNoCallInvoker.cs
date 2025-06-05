// using Grpc.Core;
// using Kurrent.Client.Features;
// using Kurrent.Client.Model;
// using Kurrent.Client.SchemaRegistry;
// using Kurrent.Client.SchemaRegistry.Serialization;
// using Kurrent.Client.SchemaRegistry.Serialization.Bytes;
// using Kurrent.Client.SchemaRegistry.Serialization.Json;
// using Kurrent.Client.SchemaRegistry.Serialization.Protobuf;
// using KurrentDB.Client;
// using static KurrentDB.Protocol.Streams.V2.StreamsService;
//
// #pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
//
// namespace Kurrent.Client;
//
// // public partial class KurrentClient : IAsyncDisposable {
// // 	public KurrentClient(KurrentDBClientSettings? settings) {
// // 		Settings = settings ?? new();
// //
// // 		Settings.ConnectionName ??= $"conn-{Guid.NewGuid():D}";
// //
// // 		LegacyClient = new KurrentDBClient(Settings);
// // 		Registry     = new KurrentRegistryClient(Connect(LegacyClient));
// // 		Streams      = new KurrentStreamsClient(this);
// // 		Admin        = new KurrentAdminClient(this);
// //
// // 		var typeMapper     = new MessageTypeMapper();
// // 		var schemaExporter = new SchemaExporter();
// // 		var schemaManager  = new SchemaManager(Registry, schemaExporter, typeMapper);
// //
// // 		SerializerProvider = new SchemaSerializerProvider([
// // 			new BytesPassthroughSerializer(), // How to enforce registry policies for this serializer?
// // 			new JsonSchemaSerializer(
// // 				options: new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
// // 				schemaManager: schemaManager
// // 			),
// // 			new ProtobufSchemaSerializer(
// // 				options: new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
// // 				schemaManager: schemaManager
// // 			)
// // 		]);
// //
// // 		MetadataDecoder = Settings.MetadataDecoder;
// //
// // 		DataConverter = new LegacyDataConverter(
// // 			SerializerProvider,
// // 			Settings.MetadataDecoder,
// // 			SchemaRegistryPolicy.NoRequirements
// // 		);
// //
// //
// // 		return;
// //
// // 		static Func<CancellationToken, ValueTask<(CallInvoker CallInvoker, ServerInfo ServerInfo)>> Connect(KurrentDBClient legacyClient) =>
// // 			async ct => {
// // 				var (_, capabilities, invoker) = await legacyClient.GetChannelInfo(ct).ConfigureAwait(false);
// // 				return (invoker, new ServerInfo { Version = capabilities.Version });
// // 			};
// // 	}
// //
// // 	internal ThinClientConnection<T> GetProxyConnection<T>() where T : ClientBase<T> =>
// // 		ThinClientConnection<T>.Create(async ct => {
// // 			var (_, capabilities, invoker) = await LegacyClient.GetChannelInfo(ct).ConfigureAwait(false);
// // 			return (invoker, new ServerInfo { Version = capabilities.Version });
// // 		});
// //
// // 	protected async ValueTask<(CallInvoker CallInvoker, ServerInfo ServerInfo)> GetConnection<T>(CancellationToken ct) where T : ClientBase<T> {
// // 		var (_, capabilities, invoker) = await LegacyClient.GetChannelInfo(ct).ConfigureAwait(false);
// // 		return (invoker, new ServerInfo { Version = capabilities.Version });
// // 	}
// //
// // 	protected internal KurrentDBClientSettings   Settings           { get; }
// // 	protected internal KurrentDBClient           LegacyClient       { get; }
// // 	protected internal ISchemaSerializerProvider SerializerProvider { get; }
// // 	protected internal IMetadataDecoder          MetadataDecoder    { get; }
// // 	protected internal LegacyDataConverter       DataConverter      { get; }
// //
// // 	public KurrentRegistryClient Registry { get; }
// // 	public KurrentStreamsClient  Streams  { get; }
// // 	public KurrentAdminClient    Admin    { get; }
// //
// // 	internal ValueTask<ChannelInfo> Rediscover() =>
// // 		LegacyClient.RediscoverAsync();
// //
// // 	public ValueTask DisposeAsync() =>
// // 		LegacyClient.DisposeAsync();
// // }
//
// // public class KurrentClientBeforeSegments : IAsyncDisposable {
// // 	protected KurrentClient(KurrentDBClientSettings? settings) {
// // 		Settings = settings ?? new();
// //
// // 		Settings.ConnectionName ??= $"conn-{Guid.NewGuid():D}";
// //
// // 		LegacyClient = new KurrentDBClient(Settings);
// // 		Registry     = new KurrentRegistryClient(Connect(LegacyClient));
// //
// // 		var typeMapper     = new MessageTypeMapper();
// // 		var schemaExporter = new SchemaExporter();
// // 		var schemaManager  = new SchemaManager(Registry, schemaExporter, typeMapper);
// //
// // 		SerializerProvider = new SchemaSerializerProvider([
// // 			new BytesPassthroughSerializer(),
// // 			new JsonSchemaSerializer(
// // 				options: new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
// // 				schemaManager: schemaManager
// // 			),
// // 			new ProtobufSchemaSerializer(
// // 				options: new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
// // 				schemaManager: schemaManager
// // 			)
// // 		]);
// //
// // 		LegacyDataConverter = new LegacyDataConverter(
// // 			SerializerProvider,
// // 			Settings.MetadataDecoder,
// // 			SchemaRegistryPolicy.NoRequirements
// // 		);
// //
// // 		StreamsConnection = GetProxyConnection<StreamsServiceClient>();
// //
// // 		return;
// //
// // 		static Func<CancellationToken, ValueTask<(CallInvoker CallInvoker, ServerInfo ServerInfo)>> Connect(KurrentDBClient legacyClient) =>
// // 			async ct => {
// // 				var (_, capabilities, invoker) = await legacyClient.GetChannelInfo(ct).ConfigureAwait(false);
// // 				return (invoker, new ServerInfo { Version = capabilities.Version });
// // 			};
// // 	}
// //
// // 	ThinClientConnection<StreamsServiceClient> StreamsConnection{ get; }
// //
// //
// // 	internal ThinClientConnection<T> GetProxyConnection<T>() where T : ClientBase<T> =>
// // 		ThinClientConnection<T>.Create(async ct => {
// // 			var (_, capabilities, invoker) = await LegacyClient.GetChannelInfo(ct).ConfigureAwait(false);
// // 			return (invoker, new ServerInfo { Version = capabilities.Version });
// // 		});
// //
// // 	internal async ValueTask<(CallInvoker CallInvoker, ServerInfo ServerInfo)> GetConnection<T>(CancellationToken ct) where T : ClientBase<T> {
// // 		var (_, capabilities, invoker) = await LegacyClient.GetChannelInfo(ct).ConfigureAwait(false);
// // 		return (invoker, new ServerInfo { Version = capabilities.Version });
// // 	}
// //
// // 	protected KurrentDBClientSettings   Settings            { get; }
// // 	protected KurrentRegistryClient     Registry            { get; }
// // 	protected ISchemaSerializerProvider SerializerProvider  { get; }
// // 	protected KurrentDBClient           LegacyClient        { get; }
// // 	protected LegacyDataConverter       LegacyDataConverter { get; }
// //
// // 	internal ValueTask<ChannelInfo> Rediscover() =>
// // 		LegacyClient.RediscoverAsync();
// //
// // 	public ValueTask DisposeAsync() =>
// // 		LegacyClient.DisposeAsync();
// // }
//
// public class KurrentClient : IAsyncDisposable {
// 	#region . Exception Map .
//
// 	static readonly Dictionary<string, Func<RpcException, Exception>> LegacyExceptionMap = new() {
// 		[Constants.Exceptions.InvalidTransaction] = ex => new InvalidTransactionException(ex.Message, ex),
// 		[Constants.Exceptions.StreamDeleted] = ex => new StreamDeletedException(
// 			ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.StreamName)?.Value ?? "<unknown>",
// 			ex
// 		),
// 		[Constants.Exceptions.WrongExpectedVersion] = ex => new WrongExpectedVersionException(
// 			ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.StreamName)?.Value!,
// 			ex.Trailers.GetStreamState(Constants.Exceptions.ExpectedVersion),
// 			ex.Trailers.GetStreamState(Constants.Exceptions.ActualVersion),
// 			ex,
// 			ex.Message
// 		),
// 		[Constants.Exceptions.MaximumAppendSizeExceeded] = ex => new MaximumAppendSizeExceededException(
// 			ex.Trailers.GetIntValueOrDefault(Constants.Exceptions.MaximumAppendSize),
// 			ex
// 		),
// 		[Constants.Exceptions.StreamNotFound] = ex => new StreamNotFoundException(
// 			ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.StreamName)?.Value!,
// 			ex
// 		),
// 		[Constants.Exceptions.MissingRequiredMetadataProperty] = ex => new RequiredMetadataPropertyMissingException(
// 			ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.MissingRequiredMetadataProperty)?.Value!,
// 			ex
// 		),
// 	};
//
// 	#endregion
//
// 	protected KurrentClient(KurrentDBClientSettings? settings) {
// 		Settings = settings ?? new();
//
// 		Settings.ConnectionName ??= $"conn-{Guid.NewGuid():D}";
//
// 		LegacyClient = new KurrentDBClient(Settings);
//
// 		LegacyCallInvoker = new KurrentDBLegacyCallInvoker(new LegacyClusterClient(Settings, LegacyExceptionMap));
//
//
//
// 		Streams = new KurrentStreamsClient(Settings, LegacyClient);
//
// 		Registry = new KurrentRegistryClient(LegacyClient.Connect());
//
// 	}
//
//
//
// 	KurrentDBClientSettings Settings            { get; }
// 	KurrentDBClient         LegacyClient        { get; }
// 	LegacyClusterClient     LegacyClusterClient { get; }
// 	KurrentDBLegacyCallInvoker LegacyCallInvoker { get; }
//
//
// 	public KurrentStreamsClient Streams { get; }
// 	public KurrentRegistryClient Registry { get; }
//
// 	internal ValueTask<ChannelInfo> Rediscover() =>
// 		LegacyClient.RediscoverAsync();
//
// 	public ValueTask DisposeAsync() =>
// 		LegacyClient.DisposeAsync();
// }
//
// public class KurrentStreamsClient {
// 	internal KurrentStreamsClient(KurrentDBClientSettings settings, KurrentDBClient legacyClient) {
// 		Settings = settings;
//
// 		Settings.ConnectionName ??= $"conn-{Guid.NewGuid():D}";
//
// 		Registry = new KurrentRegistryClient(settings, legacyClient);
//
// 		var typeMapper     = new MessageTypeMapper();
// 		var schemaExporter = new SchemaExporter();
// 		var schemaManager  = new SchemaManager(Registry, schemaExporter, typeMapper);
//
// 		SerializerProvider = new SchemaSerializerProvider([
// 			new BytesPassthroughSerializer(),
// 			new JsonSchemaSerializer(
// 				options: new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
// 				schemaManager: schemaManager
// 			),
// 			new ProtobufSchemaSerializer(
// 				options: new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
// 				schemaManager: schemaManager
// 			)
// 		]);
//
// 		LegacyDataConverter = new LegacyDataConverter(
// 			SerializerProvider,
// 			Settings.MetadataDecoder,
// 			SchemaRegistryPolicy.NoRequirements
// 		);
//
// 		Proxy = legacyClient.GetProxyConnection<StreamsServiceClient>();
// 	}
//
// 	KurrentDBClientSettings            Settings            { get; }
// 	KurrentRegistryClient              Registry            { get; }
// 	ISchemaSerializerProvider          SerializerProvider  { get; }
// 	LegacyDataConverter                LegacyDataConverter { get; }
// 	ServiceProxy<StreamsServiceClient> Proxy               { get; }
//
// 	StreamsServiceClient ServiceClient => Proxy.ServiceClient;
// }
//
// static class LegacyConnectionExtensions {
// 	internal static ServiceProxy<T> GetProxyConnection<T>(this KurrentDBClient legacyClient) where T : ClientBase<T> =>
// 		ServiceProxy<T>.Create(async ct => {
// 			var (_, capabilities, invoker) = await legacyClient.GetChannelInfo(ct).ConfigureAwait(false);
// 			return (invoker, new ServerInfo { Version = capabilities.Version });
// 		});
//
// 	internal static async ValueTask<(CallInvoker CallInvoker, ServerInfo ServerInfo)> GetConnection<T>(this KurrentDBClient legacyClient, CancellationToken ct) where T : ClientBase<T> {
// 		var (_, capabilities, invoker) = await legacyClient.GetChannelInfo(ct).ConfigureAwait(false);
// 		return (invoker, new ServerInfo { Version = capabilities.Version });
// 	}
//
// 	internal static Func<CancellationToken, ValueTask<(CallInvoker CallInvoker, ServerInfo ServerInfo)>> Connect(this KurrentDBClient legacyClient) =>
// 		async ct => {
// 			var (_, capabilities, invoker) = await legacyClient.GetChannelInfo(ct).ConfigureAwait(false);
// 			return (invoker, new ServerInfo { Version = capabilities.Version });
// 		};
// }
