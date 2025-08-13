using System.Net;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace KurrentDB.Client;

class SingleNodeChannelSelector : IChannelSelector {
	readonly ILogger      _log;
	readonly ChannelCache _channelCache;
	readonly DnsEndPoint  _endPoint;

	public SingleNodeChannelSelector(KurrentDBClientSettings settings, ChannelCache channelCache) {
		_channelCache = channelCache;
		var uri = settings.ConnectivitySettings.Address!;
		_endPoint = new DnsEndPoint(uri.Host, uri.Port);
		_log      = settings.LoggerFactory.CreateLogger<SingleNodeChannelSelector>();
	}

	public Task<(GrpcChannel Channel, GrpcChannelOptions Options)> SelectChannelAsync(CancellationToken cancellationToken) =>
		Task.FromResult(SelectEndpointChannel(_endPoint));

	public (GrpcChannel Channel, GrpcChannelOptions Options) SelectEndpointChannel(DnsEndPoint endPoint) {
		_log.LogInformation("Selected {endPoint}.", endPoint);
		return _channelCache.CreateChannel(endPoint);
	}
}
