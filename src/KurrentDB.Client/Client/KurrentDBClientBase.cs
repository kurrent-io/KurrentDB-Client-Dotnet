
using EventStore.Client.Operations;
using EventStore.Client.PersistentSubscriptions;
using EventStore.Client.Projections;
using EventStore.Client.Streams;
using EventStore.Client.Users;
using Grpc.Core;
using KurrentDB.Client.Model;
using KurrentDB.Client.SchemaRegistry.Serialization;
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
		Settings.LoggerFactory  ??= NullLoggerFactory.Instance;

		_legacyClusterClient = new LegacyClusterClient(Settings,
			channelInfo => {
				lock (_locker) {
					ChannelInfo                          = channelInfo;
					SchemaRegistryServiceClient          = new SchemaRegistryService.SchemaRegistryServiceClient(channelInfo.CallInvoker);
					OperationsServiceClient              = new Operations.OperationsClient(channelInfo.CallInvoker);
					ProjectionsServiceClient             = new Projections.ProjectionsClient(channelInfo.CallInvoker);
					PersistentSubscriptionsServiceClient = new PersistentSubscriptions.PersistentSubscriptionsClient(channelInfo.CallInvoker);
					UsersServiceClient                   = new Users.UsersClient(channelInfo.CallInvoker);
				}
			},
			exceptionMap);

		// trigger the initial connection here as it is our only choice
		// besides using some lazy async that is not even net48 compatible ffs.
		_legacyClusterClient.Connect().AsTask().GetAwaiter().GetResult();

		// SerializationManager = new KurrentSerializationManager([
		// 	new SystemJsonSchemaSerializer(schemaManager: SchemaManager),
		// 	new ProtobufSchemaSerializer(schemaManager: SchemaManager)
		// ]);
	}

	internal Streams.StreamsClient                                 StreamsServiceClient                 { get; private set; } = null!;
	internal SchemaRegistryService.SchemaRegistryServiceClient     SchemaRegistryServiceClient          { get; private set; } = null!;
	internal Operations.OperationsClient                           OperationsServiceClient              { get; private set; } = null!;
	internal Projections.ProjectionsClient                         ProjectionsServiceClient             { get; private set; } = null!;
	internal PersistentSubscriptions.PersistentSubscriptionsClient PersistentSubscriptionsServiceClient { get; private set; } = null!;
	internal Users.UsersClient                                     UsersServiceClient                   { get; private set; } = null!;

	protected ServerCapabilities ServerCapabilities => ChannelInfo.ServerCapabilities;

	protected KurrentDBClientSettings Settings { get; }

	protected internal ISchemaSerializerProvider SerializerProvider { get; } = null!;
	protected internal IMetadataDecoder          MetadataDecoder    { get; } = null!;

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



// /// <summary>
// /// The base class used by clients used to communicate with the KurrentDB.
// /// </summary>
// public abstract class KurrentDBClientBase : IDisposable, IAsyncDisposable {
// 	// Note: for grpc.net we can dispose synchronously, but not for grpc.core
//
// 	readonly ChannelCache                                       _channelCache;
// 	readonly SharingProvider<ReconnectionRequired, ChannelInfo> _channelInfoProvider;
// 	readonly CancellationTokenSource                            _cts;
// 	readonly Dictionary<string, Func<RpcException, Exception>>  _exceptionMap;
//
// 	/// Constructs a new <see cref="KurrentDBClientBase"/>.
// 	protected KurrentDBClientBase(KurrentDBClientSettings? settings, Dictionary<string, Func<RpcException, Exception>> exceptionMap) {
// 		Settings      = settings ?? new KurrentDBClientSettings();
// 		_exceptionMap = exceptionMap;
// 		_cts          = new CancellationTokenSource();
// 		_channelCache = new(Settings);
//
// 		ConnectionName = Settings.ConnectionName ?? $"conn-{Guid.NewGuid():D}";
//
// 		var channelSelector = new ChannelSelector(Settings, _channelCache);
//
// 		_channelInfoProvider = new SharingProvider<ReconnectionRequired, ChannelInfo>(
// 			(endPoint, onBroken) => GetChannelInfoExpensive(endPoint, onBroken, channelSelector, _cts.Token),
// 			Settings.ConnectivitySettings.DiscoveryInterval,
// 			ReconnectionRequired.Rediscover.Instance,
// 			Settings.LoggerFactory
// 		);
//
// 		ClientFactory = new LegacyClientFactory(ct => _channelInfoProvider.CurrentAsync.WithCancellation(ct));
//
// 		GetCallOptions = ct => KurrentDBCallOptions.CreateNonStreaming(Settings, ct);
//
// 		// SchemaManager = new LegacyKurrentSchemaManager(Settings, ClientFactory);
// 		//
// 		// SerializationManager = new KurrentSerializationManager([
// 		// 	new SystemJsonSchemaSerializer(schemaManager: SchemaManager),
// 		// 	new ProtobufSchemaSerializer(schemaManager: SchemaManager)
// 		// ]);
// 	}
//
// 	/// The name of the connection.
// 	public string ConnectionName { get; }
//
// 	/// The name of the client.
// 	public string ClientName { get; } = AppVersionInfo.Current.ProductName ?? "KurrentDB .NET Client";
//
// 	/// The version of the client.
// 	public string ClientVersion { get; } = AppVersionInfo.Current.ProductVersion ?? AppVersionInfo.Current.FileVersion ?? "0.0.0";
//
// 	/// The <see cref="KurrentDBClientSettings"/>.
// 	protected internal KurrentDBClientSettings Settings { get; }
//
// 	/// A factory used to create gRPC clients utilizing the existing legacy load balancing and discovery mechanism.
// 	protected internal LegacyClientFactory ClientFactory { get; }
//
// 	protected internal Func<CancellationToken, CallOptions> GetCallOptions { get; }
//
// 	/// The manager responsible for handling schema-related operations within KurrentDB.
// 	protected internal IKurrentSchemaManager SchemaManager { get; }
//
// 	protected internal ISchemaSerializerProvider SerializerProvider { get; }
//
// 	protected internal IMetadataDecoder MetadataDecoder { get; }
//
// 	/// <inheritdoc />
// 	public virtual async ValueTask DisposeAsync() {
// 		_channelInfoProvider.Dispose();
// 		_cts.Cancel();
// 		_cts.Dispose();
// 		await _channelCache.DisposeAsync().ConfigureAwait(false);
// 	}
//
// 	/// <inheritdoc />
// 	public virtual void Dispose() {
// 		_channelInfoProvider.Dispose();
// 		_cts.Cancel();
// 		_cts.Dispose();
// 		_channelCache.Dispose();
// 	}
//
// 	// Select a channel and query its capabilities. This is an expensive call that
// 	// we don't want to do often.
// 	async Task<ChannelInfo> GetChannelInfoExpensive(
// 		ReconnectionRequired reconnectionRequired,
// 		Action<ReconnectionRequired> onReconnectionRequired,
// 		IChannelSelector channelSelector,
// 		CancellationToken cancellationToken
// 	) {
// 		var channel = await SelectChannelAsync();
//
// 		var invoker = channel.CreateCallInvoker()
// 			.Intercept(new TypedExceptionInterceptor(_exceptionMap))
// 			.Intercept(new ConnectionNameInterceptor(ConnectionName))
// 			.Intercept(new ReportLeaderInterceptor(onReconnectionRequired));
//
// 		if (Settings.Interceptors is not null)
// 			foreach (var interceptor in Settings.Interceptors)
// 				invoker = invoker.Intercept(interceptor);
//
// 		var capabilities = await new GrpcServerCapabilitiesClient(Settings)
// 			.GetAsync(invoker, cancellationToken)
// 			.ConfigureAwait(false);
//
// 		return new(channel, capabilities, invoker);
//
// 		async Task<ChannelBase> SelectChannelAsync() {
// 			return reconnectionRequired switch {
// 				ReconnectionRequired.Rediscover               => await channelSelector.SelectChannelAsync(cancellationToken).ConfigureAwait(false),
// 				ReconnectionRequired.NewLeader (var endPoint) => channelSelector.SelectChannel(endPoint),
// 				_                                             => throw new ArgumentException(null, nameof(reconnectionRequired))
// 			};
// 		}
// 	}
//
// 	/// Gets the current channel info.
// 	protected async ValueTask<ChannelInfo> GetChannelInfo(CancellationToken cancellationToken) =>
// 		await _channelInfoProvider.CurrentAsync.WithCancellation(cancellationToken).ConfigureAwait(false);
//
// 	/// <summary>
// 	/// Only exists so that we can manually trigger rediscovery in the tests
// 	/// in cases where the server doesn't yet let the client know that it needs to.
// 	/// note if rediscovery is already in progress it will continue, not restart.
// 	/// </summary>
// 	internal Task RediscoverAsync() {
// 		_channelInfoProvider.Reset();
// 		return Task.CompletedTask;
// 	}
// }
