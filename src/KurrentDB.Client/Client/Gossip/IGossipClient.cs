using Grpc.Core;

namespace KurrentDB.Client;

public interface IGossipClient {
	public ValueTask<ClusterMessages.ClusterInfo> GetAsync(ChannelBase channel, CancellationToken cancellationToken);
}
