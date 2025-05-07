using System.Net;
using EventStore.Client;
using EventStore.Client.Gossip;
using Grpc.Core;

namespace KurrentDB.Client.LoadBalancer;

public record ClusterNode(Guid NodeId, DnsEndPoint EndPoint, NodeState State, bool IsAlive);

public enum NodeState {
	Initializing       = 0,
	DiscoverLeader     = 1,
	Unknown            = 2,
	PreReplica         = 3,
	CatchingUp         = 4,
	Clone              = 5,
	Follower           = 6,
	PreLeader          = 7,
	Leader             = 8,
	Manager            = 9,
	ShuttingDown       = 10,
	Shutdown           = 11,
	ReadOnlyLeaderless = 12,
	PreReadOnlyReplica = 13,
	ReadOnlyReplica    = 14,
	ResigningLeader    = 15
}

public delegate ValueTask<ClusterNode[]> GetKurrentDBClusterTopology(CancellationToken cancellationToken);

public interface IGossipClient {
	public ValueTask<ClusterNode[]> GetClusterTopology(CancellationToken cancellationToken);
}

class KurrentDBGossipClient : IGossipClient {

	public KurrentDBGossipClient() {
		InnerClient = new Gossip.GossipClient((CallInvoker)null!);
	}

	Gossip.GossipClient InnerClient { get; }

	static readonly Empty EmptyRequest = new Empty();

	public async ValueTask<ClusterNode[]> GetClusterTopology(CancellationToken cancellationToken) {
		var result = await InnerClient.ReadAsync(new Empty(), cancellationToken: cancellationToken);

		var nodes = result.Members.Select(MapNodeInfo).ToArray();

		return nodes;

		ClusterNode MapNodeInfo(MemberInfo source) => new(
			NodeId: Uuid.FromDto(source.InstanceId).ToGuid(),
			EndPoint: new DnsEndPoint(source.HttpEndPoint.Address, (int)source.HttpEndPoint.Port),
			State: (NodeState)source.State,
			IsAlive: source.IsAlive
		);
	}
}
