using System.Net;
using Grpc.Core;

namespace KurrentDB.Client;

class ChannelSelector(KurrentDBClientSettings settings, ChannelCache channelCache) : IChannelSelector {
	readonly IChannelSelector _inner = settings.ConnectivitySettings.IsSingleNode
		? new SingleNodeChannelSelector(settings, channelCache)
		: new GossipChannelSelector(settings, channelCache, new GrpcGossipClient(settings));

	public Task<ChannelBase> SelectChannelAsync(CancellationToken cancellationToken) =>
		_inner.SelectChannelAsync(cancellationToken);

	public ChannelBase SelectEndpointChannel(DnsEndPoint endPoint) =>
		_inner.SelectEndpointChannel(endPoint);
}
