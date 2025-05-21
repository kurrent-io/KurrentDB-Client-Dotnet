
using EventStore.Client.Operations;
using EventStore.Client.PersistentSubscriptions;
using EventStore.Client.Projections;
using EventStore.Client.Streams;
using EventStore.Client.Users;
using Grpc.Core;
using KurrentDB.Client.Model;
using KurrentDB.Client.SchemaRegistry;
using KurrentDB.Client.SchemaRegistry.Serialization;
using KurrentDB.Client.SchemaRegistry.Serialization.Bytes;
using KurrentDB.Client.SchemaRegistry.Serialization.Json;
using KurrentDB.Client.SchemaRegistry.Serialization.Protobuf;
using KurrentDB.Protocol.Registry.V2;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

namespace KurrentDB.Client;

/// <summary>
/// The base class used by clients used to communicate with the KurrentDB.
/// </summary>
public abstract class KurrentDBClientBase : IAsyncDisposable {
	readonly LegacyClusterClient _legacyClusterClient;
	readonly object              _locker = new();

	/// Constructs a new <see cref="KurrentDBClientBase"/>.
	protected KurrentDBClientBase(KurrentDBClientSettings? settings, Dictionary<string, Func<RpcException, Exception>>? exceptionMap = null) {
		Settings = settings ?? new();

		Settings.ConnectionName ??= $"conn-{Guid.NewGuid():D}";

		_legacyClusterClient = new(
			Settings,
			channelInfo => {
				lock (_locker) {
					ChannelInfo    = channelInfo;
					ServiceClients = new(channelInfo);
				}
			},
			exceptionMap ?? new()
		);

		// trigger the initial connection here as it is our only choice
		// besides using some lazy async that is not even net48 compatible ffs.
		_legacyClusterClient.Connect().AsTask().GetAwaiter().GetResult();

		var messageTypeRegistry = new MessageTypeMapper();
		var schemaExporter      = new SchemaExporter();

		var schemaManager = new SchemaManager(new KurrentRegistryClient(Settings), schemaExporter);

		SerializerProvider = new SchemaSerializerProvider([
			new BytesSerializer(),
			new JsonSchemaSerializer(
				new() {
					AutoRegister       = Settings.SchemaRegistry.AutoRegister,
					Validate           = Settings.SchemaRegistry.Validate,
					SchemaNameStrategy = Settings.SchemaRegistry.NameStrategy
				},
				schemaManager: schemaManager,
				typeMapper: messageTypeRegistry
			),
			new ProtobufSchemaSerializer(
				new() {
					AutoRegister       = Settings.SchemaRegistry.AutoRegister,
					Validate           = Settings.SchemaRegistry.Validate,
					SchemaNameStrategy = Settings.SchemaRegistry.NameStrategy
				},
				schemaManager: schemaManager,
				typeMapper: messageTypeRegistry
			)
		]);
	}

	internal class GrpcServiceClients(ChannelInfo channelInfo) {
		internal Streams.StreamsClient                                 Streams                 { get; private set; } = new(channelInfo.CallInvoker);
		internal SchemaRegistryService.SchemaRegistryServiceClient     SchemaRegistry          { get; private set; } = new(channelInfo.CallInvoker);
		internal Operations.OperationsClient                           Operations              { get; private set; } = new(channelInfo.CallInvoker);
		internal Projections.ProjectionsClient                         Projections             { get; private set; } = new(channelInfo.CallInvoker);
		internal PersistentSubscriptions.PersistentSubscriptionsClient PersistentSubscriptions { get; private set; } = new(channelInfo.CallInvoker);
		internal Users.UsersClient                                     Users                   { get; private set; } = new(channelInfo.CallInvoker);
	}

	internal KurrentDBClientSettings Settings       { get; }
	internal GrpcServiceClients      ServiceClients { get; private set; } = null!;

	protected internal ISchemaSerializerProvider SerializerProvider { get; }
	protected internal IMetadataDecoder          MetadataDecoder    { get; } = null!;

	internal ServerCapabilities ServerCapabilities => ChannelInfo.ServerCapabilities;

	#region . Legacy Stuff .

	// [Obsolete("You should not use this property, use the Service Clients properties available on this class instead.", false)]
	internal ChannelInfo ChannelInfo { get; private set; } = null!;

	// [Obsolete("Stop using this method and use the Service Clients properties available on this class instead.", false)]
	protected internal ValueTask<ChannelInfo> GetChannelInfo(CancellationToken cancellationToken = default) =>
		_legacyClusterClient.Connect(cancellationToken);

	protected internal async ValueTask<ChannelInfo> RediscoverAsync() =>
		await _legacyClusterClient.ForceReconnect().ConfigureAwait(false);

	#endregion

	protected virtual ValueTask DisposeAsyncCore() => new();

	public async ValueTask DisposeAsync() {
		await DisposeAsyncCore();

		await _legacyClusterClient
			.DisposeAsync()
			.ConfigureAwait(false);

		GC.SuppressFinalize(this);
	}
}
