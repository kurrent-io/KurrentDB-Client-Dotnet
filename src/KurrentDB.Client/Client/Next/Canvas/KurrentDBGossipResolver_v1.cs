// using System.Collections.Concurrent;
// using System.Net;
// using Grpc.Core;
// using Grpc.Net.Client;
// using Grpc.Net.Client.Balancer;
// using KurrentDB.Client.LoadBalancing;
// using Microsoft.Extensions.Logging;
//
// namespace KurrentDB.Client.LoadBalancer;
//
// /// <summary>
// /// A polling resolver that uses KurrentDB gossip protocol for service discovery.
// /// Provides automatic node discovery and endpoint selection based on configured preferences.
// /// </summary>
// public sealed partial class KurrentDBGossipResolver : PollingResolver {
// 	readonly ConcurrentDictionary<DnsEndPoint, IKurrentDBGossipClient> _channels = new();
// 	readonly IGossipClientFactory                                      _gossipClientFactory;
// 	readonly ILogger<KurrentDBGossipResolver>                          _logger;
// 	readonly ClusterNodeSelector                                       _nodeSelector;
// 	readonly KurrentDBClientConnectivitySettings                       _settings;
// 	readonly GrpcChannelOptions                                        _channelOptions;
//
// 	public KurrentDBGossipResolver(
// 		KurrentDBClientConnectivitySettings settings,
// 		GrpcChannelOptions channelOptions,
// 		ILoggerFactory loggerFactory,
// 		IBackoffPolicyFactory backoffPolicyFactory,
// 		IGossipClientFactory gossipClientFactory
// 	) : base(loggerFactory, backoffPolicyFactory) {
// 		_settings            = settings;
// 		_channelOptions      = channelOptions;
// 		_gossipClientFactory = gossipClientFactory;
// 		_nodeSelector        = new ClusterNodeSelector(settings.NodePreference);
// 		_logger              = loggerFactory.CreateLogger<KurrentDBGossipResolver>();
//
// 		// Initialize the channel dictionary with gossip seeds
// 		foreach (var endpoint in _settings.GossipSeeds)
// 			_channels.TryAdd(endpoint, gossipClientFactory.Create(endpoint, settings, channelOptions));
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
// 			LogDiscoveryFailure(_logger, _settings.MaxDiscoverAttempts, ex);
//
// 			var status = new Status(
// 				StatusCode.Unavailable,
// 				$"Failed to discover KurrentDB endpoint after {_settings.MaxDiscoverAttempts} attempts",
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
// 		for (var attempt = 1; attempt <= _settings.MaxDiscoverAttempts; attempt++) {
// 			LogStartingDiscoveryAttempt(_logger, attempt, _settings.MaxDiscoverAttempts);
//
// 			// Try each endpoint in random order
// 			foreach (var (endpoint, client) in GetRandomOrderedClients())
// 				try {
// 					// Attempt to get cluster info from this endpoint
// 					var nodes = await client
// 						.GetClusterTopology(cancellationToken)
// 						.ConfigureAwait(false);
//
// 					// Select a node based on preference
// 					ClusterNode[] selectedEndpoint = _nodeSelector.GetConnectableNodes(nodes);
//
// 					// Update known endpoints with the latest cluster info
// 					UpdateChannels(nodes);
//
// 					LogNodeSelection(
// 						_logger,
// 						_settings.NodePreference.ToString(),
// 						nodes.Members.Length,
// 						selectedEndpoint.ToString()
// 					);
//
// 					return selectedEndpoint;
// 				}
// 				catch (Exception ex) {
// 					var remainingAttempts = _settings.MaxDiscoverAttempts - attempt;
// 					LogGossipFailure(_logger, endpoint.ToString(), remainingAttempts, ex);
// 				}
//
// 			// If we get here, we couldn't get gossip from any endpoint
// 			// Reseed with the original gossip seeds
// 			ReseedChannels();
//
// 			// Wait before the next attempt if this isn't the last attempt
// 			if (attempt < _settings.MaxDiscoverAttempts)
// 				await Task.Delay(_settings.DiscoveryInterval, cancellationToken).ConfigureAwait(false);
// 		}
//
// 		// If we get here, we've exhausted all retry attempts
// 		throw new DiscoveryException(_settings.MaxDiscoverAttempts);
//
// 		// Returns channels in random order to avoid always hitting the same endpoint first.
// 		IEnumerable<KeyValuePair<DnsEndPoint, IKurrentDBGossipClient>> GetRandomOrderedClients() =>
// 			_channels.OrderBy(_ => Random.Shared.Next());
// 	}
//
// 	/// <summary>
// 	/// Updates the channels collection with endpoints from cluster info.
// 	/// </summary>
// 	void UpdateChannels(ClusterNode[] nodes) {
// 		// Add or update channels for all nodes in the cluster
// 		foreach (var node in nodes)
// 			if (!_channels.ContainsKey(node.EndPoint))
// 				_channels.TryAdd(node.EndPoint, _gossipClientFactory.Create(node.EndPoint, _settings, _channelOptions));
//
// 		// Remove channels for endpoints no longer in the cluster
// 		var currentEndpoints = nodes.Select(m => m.EndPoint).ToHashSet();
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
// 		foreach (var channel in _channels.Values.Cast<GrpcChannel>())
// 			channel.Dispose();
//
// 		_channels.Clear();
//
// 		// Add all gossip seeds
// 		foreach (var endpoint in _settings.GossipSeeds) {
// 			_channels.TryAdd(endpoint, CreateGossipChannel(endpoint));
// 		}
//
// 		LogChannelsReseeded(_logger, _channels.Count);
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
