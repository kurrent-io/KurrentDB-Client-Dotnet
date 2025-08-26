using System.Net;
using Grpc.Net.Client;

namespace KurrentDB.Client;

class ChannelSelector(KurrentDBClientSettings settings, ChannelCache channelCache) : IChannelSelector {
	readonly IChannelSelector _inner = settings.ConnectivitySettings.IsSingleNode
		? new SingleNodeChannelSelector(settings, channelCache)
		: new GossipChannelSelector(settings, channelCache, new GrpcGossipClient(settings));

	public Task<(GrpcChannel Channel, GrpcChannelOptions Options)> SelectChannelAsync(CancellationToken cancellationToken) =>
		_inner.SelectChannelAsync(cancellationToken);

	public (GrpcChannel Channel, GrpcChannelOptions Options) SelectEndpointChannel(DnsEndPoint endPoint) =>
		_inner.SelectEndpointChannel(endPoint);
}
