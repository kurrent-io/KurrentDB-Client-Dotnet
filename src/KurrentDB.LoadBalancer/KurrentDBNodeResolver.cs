using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KurrentDB.LoadBalancer;

public class KurrentDBNodeResolver(KurrentDBNodeResolverOptions options, GetKurrentDBClusterTopology getClusterTopology, NodePreference nodePreference, ILoggerFactory loggerFactory, IBackoffPolicyFactory backoffPolicyFactory)
	: PollingResolver(loggerFactory, backoffPolicyFactory) {
	readonly KurrentDBNodeResolverOptions _options      = options;
	readonly NodeSelector                 _nodeSelector = new(nodePreference);

	protected override async Task ResolveAsync(CancellationToken cancellationToken) {

		var nodes = await getClusterTopology(cancellationToken);

		var connectableNodes = _nodeSelector.GetConnectableNodes(nodes);

		if (connectableNodes.Length == 0) {
			Listener(ResolverResult.ForFailure(new(StatusCode.FailedPrecondition, "No available nodes found")));
			return;
		}

		var addresses = connectableNodes
			.Select(node => new BalancerAddress(node.EndPoint.Host, node.EndPoint.Port))
			.ToList();

		Listener(ResolverResult.ForResult(addresses));
	}

	// async ValueTask<NodeInfo[]> GetNodes(CancellationToken cancellationToken) {
	// 	var attemptsLeft = _options.MaxDiscoverAttempts;
	// 	while (!cancellationToken.IsCancellationRequested || attemptsLeft > 0) {
	// 		try {
	// 			var nodes = await getClusterTopology(cancellationToken);
	//
	// 			var connectableNodes = nodes
	// 				.Where(node => node.IsAlive && node.State == NodeState.Leader)
	// 				.ToArray();
	//
	// 			return connectableNodes;
	// 		} catch (RpcException) {
	// 			attemptsLeft--;
	//
	// 			await Task
	// 				.WhenAny(Task.Delay(_options.DiscoveryInterval, cancellationToken))
	// 				.ConfigureAwait(false);
	// 		}
	//
	// 	}
	//
	// 	throw new RpcException(new Status(StatusCode.FailedPrecondition, "No available nodes found"));
	// }
}

/// <summary>
/// Represents configuration options for the KurrentDB node resolver, which discovers and resolves nodes in a KurrentDB cluster.
/// </summary>
/// <param name="MaxDiscoverAttempts">
/// The maximum number of attempts to discover the cluster before giving up.
/// </param>
/// <param name="DiscoveryInterval">
/// The interval between cluster discovery polling attempts.
/// </param>
/// <param name="GossipTimeout">
/// The timeout duration for gossip communication with the cluster nodes.
/// </param>
public record KurrentDBNodeResolverOptions(int MaxDiscoverAttempts, TimeSpan DiscoveryInterval, TimeSpan GossipTimeout);

public abstract class KurrentDBNodeResolverFactoryBase(NodePreference nodePreference) : ResolverFactory {
	public override Resolver Create(ResolverOptions options) {
		var backoffPolicyFactory = new ExponentialBackoffPolicyFactory(
			options.ChannelOptions.InitialReconnectBackoff,
			options.ChannelOptions.MaxReconnectBackoff
		);


		// maxDiscoverAttempts  Number	10	Number of attempts to discover the cluster.
		// discoveryInterval	Number	100	Cluster discovery polling interval in milliseconds.
		// gossipTimeout	    Number	5	Gossip timeout in seconds, when the gossip call times out, it will be retried.



		return new KurrentDBNodeResolver(null!, nodePreference, options.LoggerFactory, backoffPolicyFactory);
	}
}

public class KurrentDBNodeResolverFactory(NodePreference nodePreference) : KurrentDBNodeResolverFactoryBase(nodePreference) {
	public override string Name => "kurrentdb+discover";
}


// public class KurrentDBNodeResolverFactory(NodePreference nodePreference) : ResolverFactory {
// 	public override string Name => "kurrentdb+discover";
//
// 	public override Resolver Create(ResolverOptions options) {
// 		var backoffPolicyFactory = new ExponentialBackoffPolicyFactory(
// 			options.ChannelOptions.InitialReconnectBackoff,
// 			options.ChannelOptions.MaxReconnectBackoff
// 		);
// 		//
// 		// GetKurrentDBClusterTopology? gossipClient = token =>
//
// 		return new KurrentDBNodeResolver(null!, nodePreference, options.LoggerFactory, backoffPolicyFactory);
// 	}
// }

public static class GrpcChannelOptionsExtensions {
	public static GrpcChannelOptions ConfigureClientLoadBalancer(this GrpcChannelOptions options, NodePreference nodePreference) {
		options.ServiceProvider ??= new ServiceCollection()
			.AddSingleton<ResolverFactory>(new KurrentDBNodeResolverFactory(nodePreference))
			.BuildServiceProvider();

		options.ServiceConfig ??= new ServiceConfig();
		options.ServiceConfig.LoadBalancingConfigs.Add(new RoundRobinConfig());

		return options;
	}
}
