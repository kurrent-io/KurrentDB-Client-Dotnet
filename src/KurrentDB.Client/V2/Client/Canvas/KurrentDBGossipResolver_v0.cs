// using System.Collections.Concurrent;
// using System.Net;
// using Grpc.Core;
// using Grpc.Net.Client;
// using Grpc.Net.Client.Balancer;
// using Microsoft.Extensions.Logging;
//
// namespace KurrentDB.Client.LoadBalancer;
//
// /// <summary>
// /// A polling resolver that uses KurrentDB gossip protocol for service discovery.
// /// Provides automatic node discovery and endpoint selection based on configured preferences.
// /// </summary>
// public sealed partial class KurrentDBGossipResolver : PollingResolver {
// 	readonly ConcurrentDictionary<DnsEndPoint, ChannelBase> _channels = new();
// 	readonly IGossipClient                                  _gossipClient;
// 	readonly ILogger<KurrentDBGossipResolver>               _logger;
// 	readonly NodeSelector                                   _nodeSelector;
// 	readonly KurrentDBClientSettings                        _settings;
// 	readonly ResolverOptions                                _resolverOptions;
//
// 	/// <summary>
// 	/// Initializes a new instance of the KurrentDbPollingResolver with client settings and gossip client.
// 	/// </summary>
// 	/// <param name="settings">The KurrentDB client settings</param>
// 	/// <param name="resolverOptions">The resolver options</param>
// 	/// <param name="backoffPolicyFactory">Factory for creating backoff policies</param>
// 	/// <param name="gossipClient">The gossip client for cluster discovery</param>
// 	public KurrentDBGossipResolver(KurrentDBClientSettings settings, ResolverOptions resolverOptions, IBackoffPolicyFactory backoffPolicyFactory, IGossipClient gossipClient)
// 		: base(resolverOptions.LoggerFactory, backoffPolicyFactory) {
// 		_settings        = settings;
// 		_resolverOptions = resolverOptions;
// 		_gossipClient    = gossipClient;
// 		_nodeSelector    = new(settings.ConnectivitySettings.NodePreference);
// 		_logger          = resolverOptions.LoggerFactory.CreateLogger<KurrentDBGossipResolver>();
//
// 		// Initialize the channel dictionary with gossip seeds
// 		foreach (var endpoint in _settings.ConnectivitySettings.GossipSeeds)
// 			_channels.TryAdd(endpoint, CreateGossipChannel(endpoint));
// 	}
//
// 	/// <summary>
// 	/// Performs the resolution of KurrentDB endpoints by querying the gossip protocol.
// 	/// </summary>
// 	protected override async Task ResolveAsync(CancellationToken cancellationToken) {
// 		try {
// 			// Attempt to discover endpoints using gossip protocol
// 			var endpoint = await DiscoverEndpointAsync(cancellationToken).ConfigureAwait(false);
//
// 			// Create a balancer address for the discovered endpoint
// 			var balancerAddress = new BalancerAddress(endpoint.Host, endpoint.Port);
//
// 			// Report success with the discovered endpoint
// 			LogDiscoverySuccess(_logger, endpoint.ToString());
// 			Listener(ResolverResult.ForResult([balancerAddress]));
// 		}
// 		catch (OperationCanceledException) {
// 			// Don't report cancellation as a failure
// 			LogDiscoveryCancelled(_logger);
// 		}
// 		catch (Exception ex) {
// 			// Report discovery failure
// 			LogDiscoveryFailure(_logger, _settings.ConnectivitySettings.MaxDiscoverAttempts, ex);
//
// 			var status = new Status(
// 				StatusCode.Unavailable,
// 				$"Failed to discover KurrentDB endpoint after {_settings.ConnectivitySettings.MaxDiscoverAttempts} attempts",
// 				ex
// 			);
//
// 			Listener(ResolverResult.ForFailure(status));
// 		}
// 	}
//
// 	/// <summary>
// 	/// Discovers a viable endpoint by querying gossip seeds and applying node selection logic.
// 	/// </summary>
// 	async Task<DnsEndPoint> DiscoverEndpointAsync(CancellationToken cancellationToken) {
// 		for (var attempt = 1; attempt <= _settings.ConnectivitySettings.MaxDiscoverAttempts; attempt++) {
// 			LogStartingDiscoveryAttempt(_logger, attempt, _settings.ConnectivitySettings.MaxDiscoverAttempts);
//
// 			// Try each endpoint in random order
// 			foreach (var (endpoint, channel) in GetRandomOrderedChannels())
// 				try {
// 					// Attempt to get cluster info from this endpoint
// 					var clusterInfo = await _gossipClient
// 						.GetAsync(channel, cancellationToken)
// 						.ConfigureAwait(false);
//
// 					// Select a node based on preference
// 					var selectedEndpoint = _nodeSelector.SelectNode(clusterInfo);
//
// 					// Update known endpoints with the latest cluster info
// 					UpdateChannels(clusterInfo);
//
// 					LogNodeSelection(
// 						_logger,
// 						_settings.ConnectivitySettings.NodePreference.ToString(),
// 						clusterInfo.Members.Length,
// 						selectedEndpoint.ToString()
// 					);
//
// 					return selectedEndpoint;
// 				}
// 				catch (Exception ex) {
// 					var remainingAttempts = _settings.ConnectivitySettings.MaxDiscoverAttempts - attempt;
// 					LogGossipFailure(_logger, endpoint.ToString(), remainingAttempts, ex);
// 				}
//
// 			// If we get here, we couldn't get gossip from any endpoint
// 			// Reseed with the original gossip seeds
// 			ReseedChannels();
//
// 			// Wait before the next attempt if this isn't the last attempt
// 			if (attempt < _settings.ConnectivitySettings.MaxDiscoverAttempts)
// 				await Task.Delay(_settings.ConnectivitySettings.DiscoveryInterval, cancellationToken).ConfigureAwait(false);
// 		}
//
// 		// If we get here, we've exhausted all retry attempts
// 		throw new DiscoveryException(_settings.ConnectivitySettings.MaxDiscoverAttempts);
//
// 		// Returns channels in random order to avoid always hitting the same endpoint first.
// 		IEnumerable<KeyValuePair<DnsEndPoint, ChannelBase>> GetRandomOrderedChannels() => _channels.OrderBy(_ => Random.Shared.Next());
// 	}
//
// 	/// <summary>
// 	/// Updates the channels collection with endpoints from cluster info.
// 	/// </summary>
// 	void UpdateChannels(ClusterMessages.ClusterInfo clusterInfo) {
// 		// Add or update channels for all members
// 		foreach (var member in clusterInfo.Members)
// 			if (!_channels.ContainsKey(member.EndPoint))
// 				_channels.TryAdd(member.EndPoint, CreateGossipChannel(member.EndPoint));
//
// 		// Remove channels for endpoints no longer in the cluster
// 		var currentEndpoints = clusterInfo.Members.Select(m => m.EndPoint).ToHashSet();
// 		foreach (var endpoint in _channels.Keys.Where(ep => !currentEndpoints.Contains(ep)).ToList())
// 			if (_channels.TryRemove(endpoint, out var channel))
// 				// Dispose channel if it's a GrpcChannel
// 				if (channel is GrpcChannel grpcChannel)
// 					grpcChannel.Dispose();
//
// 		LogChannelsUpdated(_logger, _channels.Count);
// 	}
//
// 	/// <summary>
// 	/// Reseeds the channels with original gossip seeds.
// 	/// </summary>
// 	void ReseedChannels() {
// 		// Clear existing channels
// 		foreach (var kvp in _channels)
// 			if (kvp.Value is GrpcChannel grpcChannel)
// 				grpcChannel.Dispose();
//
// 		_channels.Clear();
//
// 		// Add all gossip seeds
// 		foreach (var seed in _settings.ConnectivitySettings.GossipSeeds) {
// 			var endpoint = seed as DnsEndPoint ?? new DnsEndPoint(seed.GetHost(), seed.GetPort());
// 			_channels.TryAdd(endpoint, CreateGossipChannel(endpoint));
// 		}
//
// 		LogChannelsReseeded(_logger, _channels.Count);
// 	}
//
// 	ChannelBase CreateGossipChannel(DnsEndPoint endpoint) {
// 		var scheme  = _settings.ConnectivitySettings.Insecure ? Uri.UriSchemeHttp : Uri.UriSchemeHttps;
// 		var address = new Uri($"{scheme}://{endpoint}");
//
// 		// Create HTTP handler with appropriate settings
// 		var httpHandler = new SocketsHttpHandler {
// 			EnableMultipleHttp2Connections = false, // Single connection for gossip
// 			PooledConnectionIdleTimeout    = TimeSpan.FromMinutes(1),
// 			ConnectTimeout                 = TimeSpan.FromSeconds(5)
// 		};
//
// 		// Disable TLS verification if needed
// 		if (_settings.ConnectivitySettings is { Insecure: false, SslCredentials.VerifyServerCertificate: false })
// 			httpHandler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
//
// 		// Important: Disable load balancing for gossip channels
// 		// This is necessary because gossip relies on a single connection to a specific node
// 		// and does not use the gRPC load balancing features.
// 		// so maybe we can use this directly? without a resolver amd a server config?
// 		// must test this
// 		httpHandler.Properties["__GrpcLoadBalancingDisabled"] = true;
//
// 		var options = new GrpcChannelOptions {
// 			HttpHandler          = httpHandler,
// 			Credentials          = _resolverOptions.ChannelOptions.Credentials,
// 			CompressionProviders = _resolverOptions.ChannelOptions.CompressionProviders,
// 			LoggerFactory        = _resolverOptions.ChannelOptions.LoggerFactory,
// 		};
//
// 		return GrpcChannel.ForAddress(address, options);
// 	}
//
// 	/// <summary>
// 	/// Disposes all channels when resolver is disposed.
// 	/// </summary>
// 	protected override void Dispose(bool disposing) {
// 		if (disposing) {
// 			foreach (var kvp in _channels)
// 				if (kvp.Value is GrpcChannel grpcChannel)
// 					grpcChannel.Dispose();
//
// 			_channels.Clear();
// 		}
//
// 		base.Dispose(disposing);
// 	}
//
// 	#region Logging
//
// 	[LoggerMessage(Level = LogLevel.Debug, Message = "Starting node discovery attempt {AttemptNumber}/{MaxAttempts}")]
// 	private static partial void LogStartingDiscoveryAttempt(ILogger logger, int attemptNumber, int maxAttempts);
//
// 	[LoggerMessage(Level = LogLevel.Information, Message = "Successfully discovered endpoint: {SelectedEndpoint}")]
// 	private static partial void LogDiscoverySuccess(ILogger logger, string selectedEndpoint);
//
// 	[LoggerMessage(Level = LogLevel.Warning, Message = "Failed to get gossip from {Endpoint}. Attempts remaining: {RemainingAttempts}")]
// 	private static partial void LogGossipFailure(ILogger logger, string endpoint, int remainingAttempts, Exception exception);
//
// 	[LoggerMessage(Level = LogLevel.Critical, Message = "Failed to discover endpoint in {MaxAttempts} attempts")]
// 	private static partial void LogDiscoveryFailure(ILogger logger, int maxAttempts, Exception exception);
//
// 	[LoggerMessage(Level = LogLevel.Debug, Message = "Node selection (preference: {NodePreference}) found {MemberCount} members, selected: {SelectedEndpoint}")]
// 	private static partial void LogNodeSelection(ILogger logger, string nodePreference, int memberCount, string selectedEndpoint);
//
// 	[LoggerMessage(Level = LogLevel.Debug, Message = "Reseeded channels with {ChannelCount} gossip seeds")]
// 	private static partial void LogChannelsReseeded(ILogger logger, int channelCount);
//
// 	[LoggerMessage(Level = LogLevel.Debug, Message = "Updated channels, now tracking {ChannelCount} endpoints")]
// 	private static partial void LogChannelsUpdated(ILogger logger, int channelCount);
//
// 	[LoggerMessage(Level = LogLevel.Debug, Message = "Discovery operation cancelled")]
// 	private static partial void LogDiscoveryCancelled(ILogger logger);
//
// 	#endregion
// }
