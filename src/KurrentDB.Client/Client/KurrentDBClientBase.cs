using Grpc.Core;
using Grpc.Core.Interceptors;
using KurrentDB.Client.Interceptors;
using KurrentDB.Client.Model;
using KurrentDB.Client.Schema;
using KurrentDB.Client.Schema.Serialization.Json;
using KurrentDB.Client.Schema.Serialization.Protobuf;
using Enum = System.Enum;

namespace KurrentDB.Client;

/// <summary>
/// The base class used by clients used to communicate with the KurrentDB.
/// </summary>
public abstract class KurrentDBClientBase : IDisposable, IAsyncDisposable {
	// Note: for grpc.net we can dispose synchronously, but not for grpc.core

	readonly ChannelCache                                       _channelCache;
	readonly SharingProvider<ReconnectionRequired, ChannelInfo> _channelInfoProvider;
	readonly CancellationTokenSource                            _cts;
	readonly Dictionary<string, Func<RpcException, Exception>>  _exceptionMap;

	/// Constructs a new <see cref="KurrentDBClientBase"/>.
	protected KurrentDBClientBase(KurrentDBClientSettings? settings, Dictionary<string, Func<RpcException, Exception>> exceptionMap) {
		Settings      = settings ?? new KurrentDBClientSettings();
		_exceptionMap = exceptionMap;
		_cts          = new CancellationTokenSource();
		_channelCache = new(Settings);

		ConnectionName = Settings.ConnectionName ?? $"ES-{Guid.NewGuid()}";

		var channelSelector = new ChannelSelector(Settings, _channelCache);

		_channelInfoProvider = new SharingProvider<ReconnectionRequired, ChannelInfo>(
			(endPoint, onBroken) => GetChannelInfoExpensive(endPoint, onBroken, channelSelector, _cts.Token),
			Settings.ConnectivitySettings.DiscoveryInterval,
			ReconnectionRequired.Rediscover.Instance,
			Settings.LoggerFactory
		);

		ClientFactory = new LegacyClientFactory(ct => _channelInfoProvider.CurrentAsync.WithCancellation(ct));

		SchemaManager = new LegacyKurrentSchemaManager(Settings, ClientFactory);

		SerializationManager = new KurrentSerializationManager([
			new SystemJsonSchemaSerializer(schemaManager: SchemaManager),
			new ProtobufSchemaSerializer(schemaManager: SchemaManager)
		]);
	}

	/// The name of the connection.
	public string ConnectionName { get; }

	/// The <see cref="KurrentDBClientSettings"/>.
	protected KurrentDBClientSettings Settings { get; }

	/// A factory used to create gRPC clients utilizing the existing legacy load balancing and discovery mechanism.
	protected internal LegacyClientFactory ClientFactory { get; }

	/// The manager responsible for handling schema-related operations within KurrentDB.
	protected internal IKurrentSchemaManager SchemaManager { get; }

	/// The manager responsible for handling serialization operations for interacting with KurrentDB schemas.
	protected internal IKurrentSerializationManager SerializationManager { get; }

	/// <inheritdoc />
	public virtual async ValueTask DisposeAsync() {
		_channelInfoProvider.Dispose();
		_cts.Cancel();
		_cts.Dispose();
		await _channelCache.DisposeAsync().ConfigureAwait(false);
	}

	/// <inheritdoc />
	public virtual void Dispose() {
		_channelInfoProvider.Dispose();
		_cts.Cancel();
		_cts.Dispose();
		_channelCache.Dispose();
	}

	// Select a channel and query its capabilities. This is an expensive call that
	// we don't want to do often.
	async Task<ChannelInfo> GetChannelInfoExpensive(
		ReconnectionRequired reconnectionRequired,
		Action<ReconnectionRequired> onReconnectionRequired,
		IChannelSelector channelSelector,
		CancellationToken cancellationToken
	) {
		var channel = reconnectionRequired switch {
			ReconnectionRequired.Rediscover => await channelSelector.SelectChannelAsync(cancellationToken)
				.ConfigureAwait(false),
			ReconnectionRequired.NewLeader (var endPoint) => channelSelector.SelectChannel(endPoint),
			_                                             => throw new ArgumentException(null, nameof(reconnectionRequired))
		};

		var invoker = channel.CreateCallInvoker()
			.Intercept(new TypedExceptionInterceptor(_exceptionMap))
			.Intercept(new ConnectionNameInterceptor(ConnectionName))
			.Intercept(new ReportLeaderInterceptor(onReconnectionRequired));

		if (Settings.Interceptors is not null)
			foreach (var interceptor in Settings.Interceptors)
				invoker = invoker.Intercept(interceptor);

		var caps = await new GrpcServerCapabilitiesClient(Settings)
			.GetAsync(invoker, cancellationToken)
			.ConfigureAwait(false);

		return new(channel, caps, invoker);
	}

	/// Gets the current channel info.
	protected async ValueTask<ChannelInfo> GetChannelInfo(CancellationToken cancellationToken) =>
		await _channelInfoProvider.CurrentAsync.WithCancellation(cancellationToken).ConfigureAwait(false);

	/// <summary>
	/// Only exists so that we can manually trigger rediscovery in the tests
	/// in cases where the server doesn't yet let the client know that it needs to.
	/// note if rediscovery is already in progress it will continue, not restart.
	/// </summary>
	internal Task RediscoverAsync() {
		_channelInfoProvider.Reset();
		return Task.CompletedTask;
	}
}
