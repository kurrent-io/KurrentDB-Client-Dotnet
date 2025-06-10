using System.Net;
using Grpc.Net.Client.Balancer;

namespace Kurrent.Grpc.Balancer;

public interface IGossipClient : IDisposable {
	ValueTask<BalancerAddress[]> GetClusterTopology(CancellationToken cancellationToken);
}

public interface IGossipClientFactory {
	public IGossipClient Create(DnsEndPoint endpoint);
}
