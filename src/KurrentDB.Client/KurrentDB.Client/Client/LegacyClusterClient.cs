#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

using System.Net;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Kurrent.Client;
using Kurrent.Grpc.Interceptors;
using KurrentDB.Client.Interceptors;
using Microsoft.Extensions.Logging;

namespace KurrentDB.Client;

/// <summary>
/// Just an attempt to make the components a bit more usable and readable until we can
/// get rid of the legacy code related with .NET48 and custom gRPC channels and discovery.
/// </summary>
class LegacyClusterClient {
	readonly ChannelCache                                       _channelCache;
	readonly SharingProvider<ReconnectionRequired, ChannelInfo> _channelInfoProvider;
	readonly CancellationTokenSource                            _cancellator;

	bool _disposed;

	public LegacyClusterClient(KurrentDBClientSettings settings, Dictionary<string, Func<RpcException, Exception>> exceptionMap, bool dontLoadServerCapabilities = false) {
		_cancellator  = new CancellationTokenSource();
		_channelCache = new(settings);

		var clientName    = AppVersionInfo.Current.ProductName ?? "KurrentDB .NET Client";
		var clientVersion = AppVersionInfo.Current.ProductVersion ?? AppVersionInfo.Current.FileVersion ?? "0.0.0";

		// TODO SS: this should become node read preference.
		var requiresLeader = settings.ConnectivitySettings.NodePreference == NodePreference.Leader ? bool.TrueString : bool.FalseString;

		IChannelSelector channelSelector = settings.ConnectivitySettings.IsSingleNode
			? new SingleNodeChannelSelector(settings, _channelCache)
			: new GossipChannelSelector(settings, _channelCache, new GrpcGossipClient(settings));

		var token = _cancellator.Token;

		var logger = settings.LoggerFactory.CreateLogger<SharingProvider>();

		_channelInfoProvider = new SharingProvider<ReconnectionRequired, ChannelInfo>(
			factory: async (reconnectionRequired, _) => {
				var channel = reconnectionRequired switch {
					ReconnectionRequired.Rediscover => await channelSelector.SelectChannelAsync(token).ConfigureAwait(false),
					ReconnectionRequired.NewLeader (var endpoint) => channelSelector.SelectEndpointChannel(endpoint)
				};

				var invoker = channel.CreateCallInvoker().Intercept(ConfigureInterceptors());

				if(dontLoadServerCapabilities)
					return new(channel, new ServerCapabilities(), invoker);

				ServerCapabilities capabilities = new();

				try {
					 capabilities = await new GrpcServerCapabilitiesClient(settings)
						.GetAsync(invoker, token)
						.ConfigureAwait(false);
				}
				catch (Exception ex) {
					logger.LogWarning(ex, "Failed to get server capabilities. This may lead to unexpected behavior.");
				}

				return new(channel, capabilities, invoker);
			},
			factoryRetryDelay: settings.ConnectivitySettings.DiscoveryInterval,
			initialInput: ReconnectionRequired.Rediscover.Instance,
			onRefresh: null,
			logger: settings.LoggerFactory.CreateLogger($"SharingProvider-{settings.ConnectionName}")
		);

		ResolverScheme = settings.ConnectivitySettings.IsSingleNode ? "kurrentdb" : "kurrentdb+discover";

		return;

		Interceptor[] ConfigureInterceptors() {
			var requiresLeaderInterceptor = HeadersInterceptor.InjectHeaders(
				Constants.Headers.RequiresLeader, requiresLeader,
				operationsToExclude: "MultiStreamAppendSession"
			);

			var headersInterceptor = HeadersInterceptor.InjectHeaders(new() {
				[Constants.Headers.ClientName]     = clientName,
				[Constants.Headers.ClientVersion]  = clientVersion,
				[Constants.Headers.ConnectionName] = settings.ConnectionName!,
			});

			var topologyChangesInterceptor = new ClusterTopologyChangesInterceptor(
				this, settings.LoggerFactory.CreateLogger<ClusterTopologyChangesInterceptor>()
			);

			var authenticationInterceptor = new AuthenticationInterceptor(settings);

			return exceptionMap.Count == 0
				? [..settings.Interceptors, requiresLeaderInterceptor, headersInterceptor, topologyChangesInterceptor, authenticationInterceptor ]
				: [..settings.Interceptors, requiresLeaderInterceptor, headersInterceptor, new TypedExceptionInterceptor(exceptionMap), topologyChangesInterceptor, authenticationInterceptor];

			// return exceptionMap.Count == 0
			// 	? [..settings.Interceptors, requiresLeaderInterceptor, headersInterceptor, topologyChangesInterceptor ]
			// 	: [..settings.Interceptors, requiresLeaderInterceptor, headersInterceptor, new TypedExceptionInterceptor(exceptionMap), topologyChangesInterceptor ];
		}
	}

	public static LegacyClusterClient CreateWithoutExceptionMapping(KurrentDBClientSettings settings) =>
		new LegacyClusterClient(settings, [], true);

	public static LegacyClusterClient CreateWithExceptionMapping(KurrentDBClientSettings settings) =>
		new LegacyClusterClient(settings, KurrentDBClient.ExceptionMap, true);

	public string ResolverScheme { get; }

	public async ValueTask<ChannelInfo> Connect(CancellationToken cancellationToken = default) {
		ThrowIfDisposed();
		return await _channelInfoProvider.CurrentAsync.WithCancellation(cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask<ChannelInfo> ForceReconnect(DnsEndPoint? leaderEndpoint = null) {
		ThrowIfDisposed();
		ReconnectionRequired reconnectionRequired = leaderEndpoint is not null
			? new ReconnectionRequired.NewLeader(leaderEndpoint)
			: ReconnectionRequired.Rediscover.Instance;
		_channelInfoProvider.Reset(reconnectionRequired);
		return await _channelInfoProvider.CurrentAsync.ConfigureAwait(false);
	}

	internal void TriggerReconnect(DnsEndPoint? leaderEndpoint = null) {
		ThrowIfDisposed();
		_channelInfoProvider.Reset(leaderEndpoint is not null
			? new ReconnectionRequired.NewLeader(leaderEndpoint)
			: ReconnectionRequired.Rediscover.Instance);
	}

	public async ValueTask DisposeAsync() {
		if (_disposed)
			return;

		_disposed = true;
		_channelInfoProvider.Dispose();

		await _cancellator.CancelAsync();
		_cancellator.Dispose();

		await _channelCache
			.DisposeAsync()
			.ConfigureAwait(false);
	}

	void ThrowIfDisposed() {
		if (_disposed)
			throw new ObjectDisposedException(nameof(LegacyClusterClient), "The legacy cluster client has already been disposed and cannot be used anymore.");
	}
}
