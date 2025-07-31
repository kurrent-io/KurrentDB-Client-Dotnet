using System.Net;
using KurrentDB.Client;

namespace Kurrent.Client.Tests.Legacy;

[Category("Legacy")]
public class NodeSelectorTests {
	[Test, Retry(3)]
	public void dead_nodes_are_not_considered() {
		var allowedNodeId = Uuid.NewUuid();
		var allowedNode   = new DnsEndPoint(allowedNodeId.ToString(), 2113);

		var notAllowedNodeId = Uuid.NewUuid();
		var notAllowedNode   = new DnsEndPoint(notAllowedNodeId.ToString(), 2114);

		var settings = new KurrentDBClientSettings {
			ConnectivitySettings = {
				GossipSeeds = [allowedNode, notAllowedNode],
				Insecure       = true
			}
		};

		var sut = new NodeSelector(settings.ConnectivitySettings.NodePreference);

		var selectedNode = sut.SelectNode(
			new(
				[
					new(allowedNodeId, ClusterMessages.VNodeState.Follower, true, allowedNode),
					new(notAllowedNodeId, ClusterMessages.VNodeState.Leader, false, notAllowedNode)
				]
			)
		);

		selectedNode.Host.ShouldBe(allowedNode.Host);
		selectedNode.Port.ShouldBe(allowedNode.Port);
	}

	[Test]
	[Arguments(NodePreference.Leader, "leader")]
	[Arguments(NodePreference.Follower, "follower2")]
	[Arguments(NodePreference.ReadOnlyReplica, "readOnlyReplica")]
	[Arguments(NodePreference.Random, "any")]
	public void can_prefer(NodePreference nodePreference, string expectedHost) {
		var sut = new NodeSelector(nodePreference);
		var selectedNode = sut.SelectNode(
			new(
				[
					new(Uuid.NewUuid(), ClusterMessages.VNodeState.Follower, false, new("follower1", 2113)),
					new(Uuid.NewUuid(), ClusterMessages.VNodeState.Leader, true, new("leader", 2113)),
					new(Uuid.NewUuid(), ClusterMessages.VNodeState.Follower, true, new("follower2", 2113)),
					new(Uuid.NewUuid(), ClusterMessages.VNodeState.ReadOnlyReplica, true, new("readOnlyReplica", 2113))
				]
			)
		);

		if (expectedHost == "any")
			return;

		selectedNode.Host.ShouldBe(expectedHost);
	}
}
