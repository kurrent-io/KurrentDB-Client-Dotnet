namespace KurrentDB.LoadBalancer;

static class NodePreferenceComparers {
	public static readonly IComparer<NodeState> Follower = new Comparer(state =>
		state switch {
			NodeState.Follower           => 0,
			NodeState.Leader             => 1,
			NodeState.ReadOnlyReplica    => 2,
			NodeState.PreReadOnlyReplica => 3,
			NodeState.ReadOnlyLeaderless => 4,
			_                            => int.MaxValue
		}
	);

	public static readonly IComparer<NodeState> Leader = new Comparer(state =>
		state switch {
			NodeState.Leader             => 0,
			NodeState.Follower           => 1,
			NodeState.ReadOnlyReplica    => 2,
			NodeState.PreReadOnlyReplica => 3,
			NodeState.ReadOnlyLeaderless => 4,
			_                            => int.MaxValue
		}
	);

	public static readonly IComparer<NodeState> None = new Comparer(_ => 0);

	public static readonly IComparer<NodeState> ReadOnlyReplica = new Comparer(state =>
		state switch {
			NodeState.ReadOnlyReplica    => 0,
			NodeState.PreReadOnlyReplica => 1,
			NodeState.ReadOnlyLeaderless => 2,
			NodeState.Leader             => 3,
			NodeState.Follower           => 4,
			_                            => int.MaxValue
		}
	);

	class Comparer(Func<NodeState, int> getPriority) : IComparer<NodeState> {
		public int Compare(NodeState left, NodeState right) =>
			getPriority(left).CompareTo(getPriority(right));
	}
}
