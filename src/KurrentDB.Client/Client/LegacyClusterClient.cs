#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

using System.Net;
using Grpc.Core;
using Grpc.Core.Interceptors;
using KurrentDB.Client.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KurrentDB.Client;

interface ILegacyClusterClient {
	/// <summary>
	/// Establishes a connection to the cluster and retrieves the current channel information and server capabilities.
	/// If already connected, it returns the existing channel information.
	/// </summary>
	/// <param name="cancellationToken">A token to observe while waiting for the connection operation to complete.</param>
	ValueTask<ChannelInfo> Connect(CancellationToken cancellationToken);

	/// <summary>
	/// Forces a refresh of the cluster information by initiating rediscovery.
	/// If rediscovery is already in progress, it will proceed without being restarted.
	/// </summary>
	ValueTask<ChannelInfo> ForceReconnect(DnsEndPoint? leaderEndpoint = null);
}

/// <summary>
/// Just an attempt to make the components a bit more usable and readable until we can
/// get rid of the legacy code related with .NET48 and custom gRPC channels and discovery.
/// </summary>
class LegacyClusterClient : IAsyncDisposable, ILegacyClusterClient {
	readonly ChannelCache                                       _channelCache;
	readonly SharingProvider<ReconnectionRequired, ChannelInfo> _channelInfoProvider;
	readonly CancellationTokenSource                            _cancellator;

	public LegacyClusterClient(KurrentDBClientSettings settings, Action<ChannelInfo> onRefresh, Dictionary<string, Func<RpcException, Exception>>? exceptionMap = null) {
		_cancellator  = new CancellationTokenSource();
		_channelCache = new(settings);

		var clientName     = AppVersionInfo.Current.ProductName ?? "KurrentDB .NET Client";
		var clientVersion  = AppVersionInfo.Current.ProductVersion ?? AppVersionInfo.Current.FileVersion ?? "0.0.0";
		var requiresLeader = settings.ConnectivitySettings.NodePreference == NodePreference.Leader ? bool.TrueString : bool.FalseString;

		// Interceptor[] interceptors = [
		// 	new TypedExceptionInterceptor(exceptionMap ?? new Dictionary<string, Func<RpcException, Exception>>()),
		// 	// new HeadersInterceptor(new() {
		// 	// 	{ Constants.Headers.ClientName, clientName },
		// 	// 	{ Constants.Headers.ClientVersion, clientVersion },
		// 	// 	{ Constants.Headers.ConnectionName, settings.ConnectionName! },
		// 	// 	{ Constants.Headers.RequiresLeader, requiresLeader }
		// 	// }),
		// 	// new ClusterInfoRefreshInterceptor(this, settings.LoggerFactory?.CreateLogger<ClusterInfoRefreshInterceptor>() ?? NullLogger<ClusterInfoRefreshInterceptor>.Instance),
		// 	// ..settings.Interceptors ?? []
		// ];

		IChannelSelector channelSelector = settings.ConnectivitySettings.IsSingleNode
			? new SingleNodeChannelSelector(settings, _channelCache)
			: new GossipChannelSelector(settings, _channelCache, new GrpcGossipClient(settings));

		var token = _cancellator.Token;

		_channelInfoProvider = new SharingProvider<ReconnectionRequired, ChannelInfo>(
			async (reconnectionRequired, reconnect) => {
				var channel = reconnectionRequired switch {
					ReconnectionRequired.Rediscover => await channelSelector.SelectChannelAsync(token).ConfigureAwait(false),
					ReconnectionRequired.NewLeader (var endpoint) => channelSelector.SelectEndpointChannel(endpoint)
				};

				// var invoker = channel.CreateCallInvoker()
				// 	.Intercept(interceptors)
				// 	.Intercept(new ReportLeaderInterceptor(reconnect));
				//
				// Interceptor[] interceptors = [
				// 	new TypedExceptionInterceptor(exceptionMap ?? new Dictionary<string, Func<RpcException, Exception>>()),
				// 	new HeadersInterceptor(new() {
				// 		{ Constants.Headers.ClientName, clientName },
				// 		{ Constants.Headers.ClientVersion, clientVersion },
				// 		{ Constants.Headers.ConnectionName, settings.ConnectionName! },
				// 		{ Constants.Headers.RequiresLeader, requiresLeader }
				// 	}),
				// 	// new ClusterInfoRefreshInterceptor(this, settings.LoggerFactory?.CreateLogger<ClusterInfoRefreshInterceptor>() ?? NullLogger<ClusterInfoRefreshInterceptor>.Instance),
				// 	// ..settings.Interceptors ?? []
				// ];
				//
				// var invoker = channel.CreateCallInvoker();
				//
				// foreach (var interceptor in interceptors)
				// 	invoker = invoker.Intercept(interceptor);
				//
				// invoker = invoker.Intercept(new ReportLeaderInterceptor(reconnect));
				//
				// // if (settings.Interceptors is not null) {
				// // 	foreach (var interceptor in settings.Interceptors)
				// // 		invoker = invoker.Intercept(interceptor);
				// // }

				var invoker = channel.CreateCallInvoker()
					.Intercept(new TypedExceptionInterceptor(exceptionMap ?? new Dictionary<string, Func<RpcException, Exception>>()))
					.Intercept(
							new HeadersInterceptor(new() {
								{ Constants.Headers.ClientName, clientName },
								{ Constants.Headers.ClientVersion, clientVersion },
								{ Constants.Headers.ConnectionName, settings.ConnectionName! },
								{ Constants.Headers.RequiresLeader, requiresLeader }
							})
						)
					.Intercept(new ReportLeaderInterceptor(reconnect));

				// if (settings.Interceptors is not null) {
				// 	foreach (var interceptor in settings.Interceptors) {
				// 		invoker = invoker.Intercept(interceptor);
				// 	}
				// }

				var capabilities = await new GrpcServerCapabilitiesClient(settings)
					.GetAsync(invoker, token)
					.ConfigureAwait(false);

				return new(channel, capabilities, invoker);
			},
			settings.ConnectivitySettings.DiscoveryInterval,
			ReconnectionRequired.Rediscover.Instance,
			onRefresh,
			settings.LoggerFactory?.CreateLogger($"SharingProvider-{settings.ConnectionName}")
		);
	}

	public async ValueTask<ChannelInfo> Connect(CancellationToken cancellationToken = default) =>
		await _channelInfoProvider.CurrentAsync.WithCancellation(cancellationToken).ConfigureAwait(false);

	public async ValueTask<ChannelInfo> ForceReconnect(DnsEndPoint? leaderEndpoint = null) {
		_channelInfoProvider.Reset(leaderEndpoint is not null ? new ReconnectionRequired.NewLeader(leaderEndpoint) : null);

		// await _channelInfoProvider
		// 	.ResetAsync(leaderEndpoint is not null ? new ReconnectionRequired.NewLeader(leaderEndpoint) : null)
		// 	.ConfigureAwait(false);

		return await _channelInfoProvider.CurrentAsync
			.ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync() {
		_channelInfoProvider.Dispose();

		_cancellator.Cancel();
		_cancellator.Dispose();

		await _channelCache
			.DisposeAsync()
			.ConfigureAwait(false);
	}
}
