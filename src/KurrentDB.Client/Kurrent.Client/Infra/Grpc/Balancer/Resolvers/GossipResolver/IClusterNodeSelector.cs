using Grpc.Net.Client.Balancer;

namespace Kurrent.Client.Grpc.Balancer.Resolvers;

/// <summary>
/// Defines a mechanism for selecting nodes from a cluster based on custom criteria.
/// </summary>
public interface IClusterNodeSelector {
	/// <summary>
	/// Selects a cluster node from the provided list.
	/// </summary>
	/// <param name="nodes">The list of cluster nodes.</param>
	/// <returns>The selected cluster node.</returns>
	BalancerAddress[] Select(BalancerAddress[] nodes);
}
