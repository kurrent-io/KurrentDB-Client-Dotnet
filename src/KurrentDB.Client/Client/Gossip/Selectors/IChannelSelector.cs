using System.Net;
using Grpc.Core;

namespace KurrentDB.Client;

interface IChannelSelector {
	// Let the channel selector pick an endpoint.
	Task<ChannelBase> SelectChannelAsync(CancellationToken cancellationToken);

	// Get a channel for the specified endpoint
	ChannelBase SelectEndpointChannel(DnsEndPoint endPoint);
}
