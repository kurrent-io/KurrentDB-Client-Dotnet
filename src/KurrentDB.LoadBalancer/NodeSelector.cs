using System.Net;

namespace KurrentDB.LoadBalancer;

// Selects nodes to connect given the node preference.
// There can be only one leader node, but there can be multiple follower nodes.
// the idea is to create a grpc client load balancer with this.
class NodeSelector(NodePreference nodePreference) {
	static readonly NodeState[] NotAllowedStates = [
		NodeState.Manager, NodeState.ShuttingDown, NodeState.Shutdown, NodeState.Unknown, NodeState.Initializing, NodeState.CatchingUp, NodeState.ResigningLeader,
		NodeState.PreLeader, NodeState.PreReplica, NodeState.PreReadOnlyReplica, NodeState.Clone, NodeState.DiscoverLeader
	];

	IComparer<NodeState> NodeStateComparer { get; } = nodePreference switch {
		NodePreference.Leader          => NodePreferenceComparers.Leader,
		NodePreference.Follower        => NodePreferenceComparers.Follower,
		NodePreference.ReadOnlyReplica => NodePreferenceComparers.ReadOnlyReplica,
		_                              => NodePreferenceComparers.None
	};

	/// <summary>
	/// Retrieves an array of DnsEndPoint for nodes that are connectable,
	/// based on their state and the specified node preference.
	/// Nodes in non-connectable states are filtered out, and the remaining
	/// nodes are ordered based on their state comparer.
	/// </summary>
	/// <param name="nodes">An array of NodeInfo representing the state and endpoint of each node.</param>
	/// <returns>An array of DnsEndPoint representing the endpoints of the nodes that are connectable and meet the specified criteria.</returns>
	public NodeInfo[] GetConnectableNodes(NodeInfo[] nodes) {
		var endpoints = nodes
			.Where(IsConnectable)
			.OrderBy(x => x.State, NodeStateComparer)
			.ToArray();

		return endpoints;

		static bool IsConnectable(NodeInfo node) =>
			node.IsAlive && !NotAllowedStates.Contains(node.State);
	}
}
