using Kurrent.Client.Testing.TUnit;
using static KurrentDB.Client.ClusterMessages.VNodeState;

namespace KurrentDB.Client.Tests;

[Category("Legacy")]
public class NodePreferenceComparerTests {
	public class LeaderTestCases : TestCaseGenerator<(ClusterMessages.VNodeState, ClusterMessages.VNodeState[])> {
		protected override IEnumerable<(ClusterMessages.VNodeState, ClusterMessages.VNodeState[])> Data() {
			yield return (Leader, [Leader, Follower, Follower, ReadOnlyReplica]);
			yield return (Follower, [Follower, Follower, ReadOnlyReplica]);
			yield return (ReadOnlyReplica, [ReadOnlyReplica, PreReadOnlyReplica, ReadOnlyLeaderless]);
			yield return (PreReadOnlyReplica, [PreReadOnlyReplica, ReadOnlyLeaderless]);
			yield return (ReadOnlyLeaderless, [ReadOnlyLeaderless, DiscoverLeader]);
		}
	}

	[Test, LeaderTestCases]
	public void LeaderTests((ClusterMessages.VNodeState Expected, ClusterMessages.VNodeState[] States) testCase) {
		var actual = testCase.States
			.OrderBy(state => state, NodePreferenceComparers.Leader)
			.ThenBy(_ => Guid.NewGuid())
			.First();

		actual.ShouldBe(testCase.Expected);
	}

	public class FollowerTestCases : TestCaseGenerator<(ClusterMessages.VNodeState, ClusterMessages.VNodeState[])> {
		protected override IEnumerable<(ClusterMessages.VNodeState, ClusterMessages.VNodeState[])> Data() {
			yield return (Follower, [Leader, Follower, Follower, ReadOnlyReplica]);
			yield return (Leader, [Leader, ReadOnlyReplica, ReadOnlyReplica]);
			yield return (ReadOnlyReplica, [ReadOnlyReplica, PreReadOnlyReplica, ReadOnlyLeaderless]);
			yield return (PreReadOnlyReplica, [PreReadOnlyReplica, ReadOnlyLeaderless]);
			yield return (ReadOnlyLeaderless, [ReadOnlyLeaderless, DiscoverLeader]);
		}
	}

	[Test, FollowerTestCases]
	public void FollowerTests((ClusterMessages.VNodeState Expected, ClusterMessages.VNodeState[] States) testCase) {
		var actual = testCase.States
			.OrderBy(state => state, NodePreferenceComparers.Follower)
			.ThenBy(_ => Guid.NewGuid())
			.First();

		actual.ShouldBe(testCase.Expected);
	}

	public class ReadOnlyReplicaTestCases : TestCaseGenerator<(ClusterMessages.VNodeState, ClusterMessages.VNodeState[])> {
		protected override IEnumerable<(ClusterMessages.VNodeState, ClusterMessages.VNodeState[])> Data() {
			yield return (ReadOnlyReplica, [Leader, Follower, Follower, ReadOnlyReplica]);
			yield return (PreReadOnlyReplica, [Leader, Follower, Follower, PreReadOnlyReplica]);
			yield return (ReadOnlyLeaderless, [Leader, Follower, Follower, ReadOnlyLeaderless]);
			yield return (Leader, [Leader, Follower, Follower]);
			yield return (Follower, [DiscoverLeader, Follower, Follower]);
		}
	}

	[Test, ReadOnlyReplicaTestCases]
	public void ReadOnlyReplicaTests((ClusterMessages.VNodeState Expected, ClusterMessages.VNodeState[] States) testCase) {
		var actual = testCase.States
			.OrderBy(state => state, NodePreferenceComparers.ReadOnlyReplica)
			.ThenBy(_ => Guid.NewGuid())
			.First();

		actual.ShouldBe(testCase.Expected);
	}
}
