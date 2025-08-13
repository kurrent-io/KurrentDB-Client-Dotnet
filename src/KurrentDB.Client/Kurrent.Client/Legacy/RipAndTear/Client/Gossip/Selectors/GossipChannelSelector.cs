using System.Net;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace KurrentDB.Client;

// Thread safe
class GossipChannelSelector : IChannelSelector {
	readonly KurrentDBClientSettings        _settings;
	readonly ChannelCache                   _channels;
	readonly IGossipClient                  _gossipClient;
	readonly ILogger<GossipChannelSelector> _log;
	readonly NodeSelector                   _nodeSelector;

	public GossipChannelSelector(KurrentDBClientSettings settings, ChannelCache channelCache, IGossipClient gossipClient) {
		_settings     = settings;
		_channels     = channelCache;
		_gossipClient = gossipClient;
		_log          = settings.LoggerFactory.CreateLogger<GossipChannelSelector>();
		_nodeSelector = new(_settings.ConnectivitySettings.NodePreference);
	}

	public (GrpcChannel Channel, GrpcChannelOptions Options) SelectEndpointChannel(DnsEndPoint endPoint) =>
		_channels.CreateChannel(endPoint);

	public async Task<(GrpcChannel Channel, GrpcChannelOptions Options)> SelectChannelAsync(CancellationToken cancellationToken) {
		var endPoint = await DiscoverAsync(cancellationToken).ConfigureAwait(false);
		_log.LogInformation("Successfully discovered candidate at {endPoint}.", endPoint);
		return _channels.CreateChannel(endPoint);
	}

	async Task<DnsEndPoint> DiscoverAsync(CancellationToken cancellationToken) {
		for (var attempt = 1; attempt <= _settings.ConnectivitySettings.MaxDiscoverAttempts; attempt++) {
			foreach (var kvp in _channels.GetRandomOrderSnapshot()) {
				var endPointToGetGossip = kvp.Key;
				var channelToGetGossip  = kvp.Value;

				try {
					var clusterInfo = await _gossipClient
						.GetAsync(channelToGetGossip, cancellationToken)
						.ConfigureAwait(false);

					var selectedEndpoint = _nodeSelector.SelectNode(clusterInfo);

					// Successfully selected an endpoint using this gossip!
					// We want _channels to contain exactly the nodes in ClusterInfo.
					// nodes no longer in the cluster can be forgotten.
					// new nodes are added so we can use them to get gossip.
					_channels.UpdateCache(clusterInfo.Members.Select(x => x.EndPoint).ToArray());

					return selectedEndpoint;

				} catch (Exception ex) {
					_log.Log(
						GetLogLevelForDiscoveryAttempt(attempt),
						ex,
						"Could not discover candidate from {endPoint}. Attempts remaining: {remainingAttempts}",
						endPointToGetGossip,
						_settings.ConnectivitySettings.MaxDiscoverAttempts - attempt);
				}
			}

			// couldn't select a node from any _channel. reseed the channels.
			_channels.UpdateCache(_settings.ConnectivitySettings.GossipSeeds);

			await Task
				.Delay(_settings.ConnectivitySettings.DiscoveryInterval, cancellationToken)
				.ConfigureAwait(false);
		}

		_log.LogError("Failed to discover candidate in {maxDiscoverAttempts} attempts.",
			_settings.ConnectivitySettings.MaxDiscoverAttempts);

		throw new DiscoveryException(_settings.ConnectivitySettings.MaxDiscoverAttempts);

		LogLevel GetLogLevelForDiscoveryAttempt(int attempt) => attempt switch {
			_ when attempt == _settings.ConnectivitySettings.MaxDiscoverAttempts => LogLevel.Error,
			1                                                                    => LogLevel.Warning,
			_                                                                    => LogLevel.Debug
		};
	}
}
