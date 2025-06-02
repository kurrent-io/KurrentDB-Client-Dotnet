// using Kurrent.Grpc;
// using Kurrent.Grpc.Resolvers;
//
// namespace KurrentDB.Client.LoadBalancing;
//
// public class KurrentDBClusterNodeSelector(NodePreference nodePreference) : IClusterNodeSelector {
// 	static readonly NodeState[] NotAllowedStates = [
// 		NodeState.Manager, NodeState.ShuttingDown,
// 		NodeState.Shutdown, NodeState.Unknown,
// 		NodeState.Initializing, NodeState.CatchingUp,
// 		NodeState.ResigningLeader, NodeState.PreLeader,
// 		NodeState.PreReplica, NodeState.PreReadOnlyReplica,
// 		NodeState.Clone, NodeState.DiscoverLeader
// 	];
//
// 	IComparer<NodeState> NodeStateComparer { get; } = nodePreference switch {
// 		NodePreference.Leader          => NodeStatePreferenceComparer.Leader,
// 		NodePreference.Follower        => NodeStatePreferenceComparer.Follower,
// 		NodePreference.ReadOnlyReplica => NodeStatePreferenceComparer.ReadOnlyReplica,
// 		_                              => NodeStatePreferenceComparer.None
// 	};
//
// 	public Kurrent.Grpc.Resolvers.ClusterNode[] Select(Kurrent.Grpc.Resolvers.ClusterNode[] nodes) {
// 		var endpoints = nodes
// 			.Where(IsConnectable)
// 			.OrderBy(node => node.Attributes.GetValueOrDefault("State", NodeState.Unknown), NodeStateComparer)
// 			.ToArray();
//
// 		//
//
// 		return endpoints;
//
// 		static bool IsConnectable(Kurrent.Grpc.Resolvers.ClusterNode node) =>
// 			node.Attributes.GetValueOrDefault("IsAlive", false) &&
// 			!NotAllowedStates.Contains(node.Attributes.GetValueOrDefault("State", NodeState.Unknown));
// 	}
//
// 	class NodeStatePreferenceComparer(Func<NodeState, int> getPriority) : IComparer<NodeState> {
// 		public int Compare(NodeState left, NodeState right) =>
// 			getPriority(left).CompareTo(getPriority(right));
//
// 		public static readonly IComparer<NodeState> Follower = new NodeStatePreferenceComparer(state =>
// 			state switch {
// 				NodeState.Follower           => 0,
// 				NodeState.Leader             => 1,
// 				NodeState.ReadOnlyReplica    => 2,
// 				NodeState.PreReadOnlyReplica => 3,
// 				NodeState.ReadOnlyLeaderless => 4,
// 				_                            => int.MaxValue
// 			}
// 		);
//
// 		public static readonly IComparer<NodeState> Leader = new NodeStatePreferenceComparer(state =>
// 			state switch {
// 				NodeState.Leader             => 0,
// 				NodeState.Follower           => 1,
// 				NodeState.ReadOnlyReplica    => 2,
// 				NodeState.PreReadOnlyReplica => 3,
// 				NodeState.ReadOnlyLeaderless => 4,
// 				_                            => int.MaxValue
// 			}
// 		);
//
// 		public static readonly IComparer<NodeState> None = new NodeStatePreferenceComparer(_ => 0);
//
// 		public static readonly IComparer<NodeState> ReadOnlyReplica = new NodeStatePreferenceComparer(state =>
// 			state switch {
// 				NodeState.ReadOnlyReplica    => 0,
// 				NodeState.PreReadOnlyReplica => 1,
// 				NodeState.ReadOnlyLeaderless => 2,
// 				NodeState.Leader             => 3,
// 				NodeState.Follower           => 4,
// 				_                            => int.MaxValue
// 			}
// 		);
// 	}
// }
//
// class ClusterNodeSelector(NodePreference nodePreference) {
// 	static readonly NodeState[] NotAllowedStates = [
// 		NodeState.Manager, NodeState.ShuttingDown,
// 		NodeState.Shutdown, NodeState.Unknown,
// 		NodeState.Initializing, NodeState.CatchingUp,
// 		NodeState.ResigningLeader, NodeState.PreLeader,
// 		NodeState.PreReplica, NodeState.PreReadOnlyReplica,
// 		NodeState.Clone, NodeState.DiscoverLeader
// 	];
//
// 	IComparer<NodeState> NodeStateComparer { get; } = nodePreference switch {
// 		NodePreference.Leader          => NodeStatePreferenceComparer.Leader,
// 		NodePreference.Follower        => NodeStatePreferenceComparer.Follower,
// 		NodePreference.ReadOnlyReplica => NodeStatePreferenceComparer.ReadOnlyReplica,
// 		_                              => NodeStatePreferenceComparer.None
// 	};
//
// 	/// <summary>
// 	/// Retrieves an array of nodes that are connectable,
// 	/// based on their state and the specified node preference.
// 	/// Nodes in non-connectable states are filtered out, and the remaining
// 	/// nodes are ordered based on their state comparer.
// 	/// </summary>
// 	/// <param name="nodes">An array of ClusterNode representing the state and endpoint of each node.</param>
// 	public ClusterNode[] GetConnectableNodes(ClusterNode[] nodes) {
// 		var endpoints = nodes
// 			.Where(IsConnectable)
// 			.OrderBy(x => x.State, NodeStateComparer)
// 			.ToArray();
//
// 		return endpoints;
//
// 		static bool IsConnectable(ClusterNode node) =>
// 			node.IsAlive && !NotAllowedStates.Contains(node.State);
// 	}
//
// 	class NodeStatePreferenceComparer(Func<NodeState, int> getPriority) : IComparer<NodeState> {
// 		public int Compare(NodeState left, NodeState right) =>
// 			getPriority(left).CompareTo(getPriority(right));
//
// 		public static readonly IComparer<NodeState> Follower = new NodeStatePreferenceComparer(state =>
// 			state switch {
// 				NodeState.Follower           => 0,
// 				NodeState.Leader             => 1,
// 				NodeState.ReadOnlyReplica    => 2,
// 				NodeState.PreReadOnlyReplica => 3,
// 				NodeState.ReadOnlyLeaderless => 4,
// 				_                            => int.MaxValue
// 			}
// 		);
//
// 		public static readonly IComparer<NodeState> Leader = new NodeStatePreferenceComparer(state =>
// 			state switch {
// 				NodeState.Leader             => 0,
// 				NodeState.Follower           => 1,
// 				NodeState.ReadOnlyReplica    => 2,
// 				NodeState.PreReadOnlyReplica => 3,
// 				NodeState.ReadOnlyLeaderless => 4,
// 				_                            => int.MaxValue
// 			}
// 		);
//
// 		public static readonly IComparer<NodeState> None = new NodeStatePreferenceComparer(_ => 0);
//
// 		public static readonly IComparer<NodeState> ReadOnlyReplica = new NodeStatePreferenceComparer(state =>
// 			state switch {
// 				NodeState.ReadOnlyReplica    => 0,
// 				NodeState.PreReadOnlyReplica => 1,
// 				NodeState.ReadOnlyLeaderless => 2,
// 				NodeState.Leader             => 3,
// 				NodeState.Follower           => 4,
// 				_                            => int.MaxValue
// 			}
// 		);
// 	}
// }
