using Grpc.Core;

namespace KurrentDB.Client;

interface IGossipClient {
	public ValueTask<ClusterMessages.ClusterInfo> GetAsync(ChannelBase channel, CancellationToken cancellationToken);
}
