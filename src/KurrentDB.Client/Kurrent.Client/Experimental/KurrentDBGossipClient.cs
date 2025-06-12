using System.Net;
using EventStore.Client;
using EventStore.Client.Gossip;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using Kurrent.Grpc.Balancer;
using KurrentDB.Client;
using IGossipClient = Kurrent.Grpc.Balancer.IGossipClient;
using VNodeState = EventStore.Client.Gossip.MemberInfo.Types.VNodeState;

namespace Kurrent.Client.Experimental;

/// <summary>
/// Indicates the preferred KurrentDB node type to read from.
/// </summary>
enum NodeReadPreference {
    /// <summary>
    /// When attempting connection, prefers leader node.
    /// </summary>
    Leader = 0,

    /// <summary>
    /// When attempting connection, prefers follower node.
    /// </summary>
    Follower = 1,

    /// <summary>
    /// When attempting connection, has no node preference.
    /// </summary>
    Random = 2,

    /// <summary>
    /// When attempting connection, prefers read only replicas.
    /// </summary>
    ReadOnlyReplica = 3
}

class KurrentDBGossipClient(IClusterNodeSelector nodeSelector, GrpcChannel channel) : IGossipClient {
	static readonly Empty EmptyRequest = new Empty();

	Gossip.GossipClient ServiceClient { get; } = new(channel);
	GrpcChannel         Channel       { get; } = channel;

	public async ValueTask<BalancerAddress[]> GetClusterTopology(CancellationToken cancellationToken) {
		var result = await ServiceClient
			.ReadAsync(EmptyRequest, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var nodes = result.Members.Select(Map).ToArray();
		var selectedNodes = nodeSelector.Select(nodes);

		return selectedNodes;

		BalancerAddress Map(MemberInfo memberInfo) {
			var address = new BalancerAddress(memberInfo.HttpEndPoint.Address, (int)memberInfo.HttpEndPoint.Port);

			address.Attributes
				.WithValue("NodeId", Uuid.FromDto(memberInfo.InstanceId).ToGuid())
				.WithValue("State", memberInfo.State)
				.WithValue("IsAlive", memberInfo.IsAlive);

			return address;
		}
	}

	public void Dispose() => Channel.Dispose();
}

class KurrentDBGossipClientFactory(KurrentDBClusterNodeSelector nodeSelector, GrpcChannelOptions channelOptions) : IGossipClientFactory {
	public KurrentDBGossipClientFactory(NodeReadPreference nodePreference, GrpcChannelOptions channelOptions)
		: this(new KurrentDBClusterNodeSelector(nodePreference), channelOptions) { }

	public IGossipClient Create(DnsEndPoint endpoint) {
		var insecure = channelOptions.Credentials == ChannelCredentials.Insecure; // not sure if this will work 100%. must test.
		var scheme   = insecure ? Uri.UriSchemeHttp : Uri.UriSchemeHttps;
		var address  = new Uri($"{scheme}://{endpoint}");
		var channel  = GrpcChannel.ForAddress(address, channelOptions);
		return new KurrentDBGossipClient(nodeSelector, channel);
	}
}

class KurrentDBClusterNodeSelector(NodeReadPreference nodePreference) : IClusterNodeSelector {
	static readonly VNodeState[] NotAllowedStates = [
		VNodeState.Manager, VNodeState.ShuttingDown,
		VNodeState.Shutdown, VNodeState.Unknown,
		VNodeState.Initializing, VNodeState.CatchingUp,
		VNodeState.ResigningLeader, VNodeState.PreLeader,
		VNodeState.PreReplica, VNodeState.PreReadOnlyReplica,
		VNodeState.Clone, VNodeState.DiscoverLeader
	];

	IComparer<VNodeState> VNodeStateComparer { get; } = nodePreference switch {
        NodeReadPreference.Leader          => VNodeStatePreferenceComparer.Leader,
        NodeReadPreference.Follower        => VNodeStatePreferenceComparer.Follower,
        NodeReadPreference.ReadOnlyReplica => VNodeStatePreferenceComparer.ReadOnlyReplica,
		_                                  => VNodeStatePreferenceComparer.None
	};

	public BalancerAddress[] Select(BalancerAddress[] nodes) {
		// TODO SS: consider a custom load balancer with a custom picker to handle the node preference that would round robin replicas or followers per example.
		var endpoints = nodes
			.Where(IsConnectable)
			.OrderBy(GetNodeState, VNodeStateComparer)
			.ToArray();

		return endpoints;

		static VNodeState GetNodeState(BalancerAddress node) =>
			node.Attributes.GetValueOrDefault("State", VNodeState.Unknown);

		static bool IsConnectable(BalancerAddress node) =>
			node.Attributes.GetValueOrDefault("IsAlive", false) &&
			!NotAllowedStates.Contains(GetNodeState(node));
	}

	class VNodeStatePreferenceComparer(Func<VNodeState, int> getPriority) : IComparer<VNodeState> {
		public int Compare(VNodeState left, VNodeState right) =>
			getPriority(left).CompareTo(getPriority(right));

		public static readonly IComparer<VNodeState> Follower = new VNodeStatePreferenceComparer(state =>
			state switch {
				VNodeState.Follower           => 0,
				VNodeState.Leader             => 1,
				VNodeState.ReadOnlyReplica    => 2,
				VNodeState.PreReadOnlyReplica => 3,
				VNodeState.ReadOnlyLeaderless => 4,
				_                             => int.MaxValue
			}
		);

		public static readonly IComparer<VNodeState> Leader = new VNodeStatePreferenceComparer(state =>
			state switch {
				VNodeState.Leader             => 0,
				VNodeState.Follower           => 1,
				VNodeState.ReadOnlyReplica    => 2,
				VNodeState.PreReadOnlyReplica => 3,
				VNodeState.ReadOnlyLeaderless => 4,
				_                             => int.MaxValue
			}
		);

		public static readonly IComparer<VNodeState> None = new VNodeStatePreferenceComparer(_ => 0);

		public static readonly IComparer<VNodeState> ReadOnlyReplica = new VNodeStatePreferenceComparer(state =>
			state switch {
				VNodeState.ReadOnlyReplica    => 0,
				VNodeState.PreReadOnlyReplica => 1,
				VNodeState.ReadOnlyLeaderless => 2,
				VNodeState.Leader             => 3,
				VNodeState.Follower           => 4,
				_                             => int.MaxValue
			}
		);
	}
}
