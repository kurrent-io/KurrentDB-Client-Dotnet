using System.Net;
using Grpc.Net.Client;

namespace KurrentDB.Client;

interface IChannelSelector {
	// Let the channel selector pick an endpoint.
	Task<(GrpcChannel Channel, GrpcChannelOptions Options)> SelectChannelAsync(CancellationToken cancellationToken);

	// Get a channel for the specified endpoint
    (GrpcChannel Channel, GrpcChannelOptions Options) SelectEndpointChannel(DnsEndPoint endPoint);
}
