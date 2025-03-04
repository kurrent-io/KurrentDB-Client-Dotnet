using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace KurrentDB.Client {
	internal interface IGossipClient {
		public ValueTask<ClusterMessages.ClusterInfo> GetAsync(ChannelBase channel,
			CancellationToken cancellationToken);
	}
}
