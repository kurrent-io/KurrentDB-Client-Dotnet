using System.Net;
using Grpc.Core;
using Grpc.Net.Client;
using EventStore.Client;
using EventStore.Client.Gossip;

namespace KurrentDB.LoadBalancer;

public record NodeInfo(Guid InstanceId, DnsEndPoint EndPoint, NodeState State, bool IsAlive);

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

public delegate ValueTask<NodeInfo[]> GetKurrentDBClusterTopology(CancellationToken cancellationToken);

public interface IGossipClient {
	public ValueTask<NodeInfo[]> GetClusterTopology(CancellationToken cancellationToken);
}

class KurrentDBGossipClient : IGossipClient {

	public KurrentDBGossipClient() {
		InnerClient = new Gossip.GossipClient(null!);
	}

	Gossip.GossipClient InnerClient { get; }

	static readonly Empty EmptyRequest = new Empty();

	public async ValueTask<NodeInfo[]> GetClusterTopology(CancellationToken cancellationToken) {
		var result = await InnerClient.ReadAsync(new Empty(), cancellationToken: cancellationToken);

		var nodes = result.Members.Select(MapNodeInfo).ToArray();

		return nodes;

		NodeInfo MapNodeInfo(MemberInfo x) => new(
			InstanceId: Uuid.FromDto(dto: x.InstanceId).ToGuid(),
			EndPoint: new DnsEndPoint(host: x.HttpEndPoint.Address, port: (int)x.HttpEndPoint.Port),
			State: (NodeState)x.State,
			IsAlive: x.IsAlive
		);

		// using var call = client.ReadAsync(
		// 	new Empty(),
		// 	KurrentDBCallOptions.CreateNonStreaming(_settings, ct));
		// var result = await call.ResponseAsync.ConfigureAwait(false);
		//
		// return new(result.Members.Select(x =>
		// 	new ClusterMessages.MemberInfo(
		// 		Uuid.FromDto(x.InstanceId),
		// 		(ClusterMessages.VNodeState)x.State,
		// 		x.IsAlive,
		// 		new DnsEndPoint(x.HttpEndPoint.Address, (int)x.HttpEndPoint.Port))).ToArray());

	}
}
