using System.Net;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KurrentDB.Client;

class SingleNodeChannelSelector : IChannelSelector {
	readonly ILogger      _log;
	readonly ChannelCache _channelCache;
	readonly DnsEndPoint  _endPoint;

	public SingleNodeChannelSelector(KurrentDBClientSettings settings, ChannelCache channelCache) {
		_channelCache = channelCache;
		var uri = settings.ConnectivitySettings.ResolvedAddressOrDefault;
		_endPoint = new DnsEndPoint(host: uri.Host, port: uri.Port);
		_log      = settings.LoggerFactory?.CreateLogger<SingleNodeChannelSelector>() ?? NullLogger<SingleNodeChannelSelector>.Instance;
	}

	public Task<ChannelBase> SelectChannelAsync(CancellationToken cancellationToken) =>
		Task.FromResult(SelectEndpointChannel(_endPoint));

	public ChannelBase SelectEndpointChannel(DnsEndPoint endPoint) {
		_log.LogInformation("Selected {endPoint}.", endPoint);
		return _channelCache.CreateChannel(endPoint);
	}
}
