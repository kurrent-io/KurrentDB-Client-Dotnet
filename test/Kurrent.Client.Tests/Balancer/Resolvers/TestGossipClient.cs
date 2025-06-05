using System.Net;
using Grpc.Net.Client.Balancer;
using Kurrent.Grpc.Balancer;

namespace Kurrent.Client.Tests.Balancer.Resolvers;

/// <summary>
/// A test implementation of IGossipClient for unit testing the GossipResolver.
/// </summary>
public class TestGossipClient : IGossipClient {
	readonly Func<CancellationToken, ValueTask<BalancerAddress[]>> _getTopologyFunc;

	bool _isDisposed;

	/// <summary>
	/// Creates a TestGossipClient that returns the specified addresses on GetClusterTopology.
	/// </summary>
	/// <param name="addresses">The addresses to return.</param>
	public TestGossipClient(params BalancerAddress[] addresses)
		: this(ct => ValueTask.FromResult(addresses)) { }

	/// <summary>
	/// Creates a TestGossipClient that throws the specified exception on GetClusterTopology.
	/// </summary>
	/// <param name="exception">The exception to throw.</param>
	public TestGossipClient(Exception exception)
		: this(ct => throw exception) { }

	/// <summary>
	/// Creates a TestGossipClient that returns topology based on the provided function.
	/// </summary>
	/// <param name="getTopologyFunc">Function that returns cluster topology.</param>
	public TestGossipClient(Func<CancellationToken, ValueTask<BalancerAddress[]>> getTopologyFunc) => _getTopologyFunc = getTopologyFunc;

	/// <summary>
	/// Gets the cluster topology with the behavior defined in the constructor.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Task containing BalancerAddress array.</returns>
	/// <exception cref="ObjectDisposedException">Thrown if client is disposed.</exception>
	public ValueTask<BalancerAddress[]> GetClusterTopology(CancellationToken cancellationToken) {
		return !_isDisposed
			? _getTopologyFunc(cancellationToken)
			: throw new ObjectDisposedException(nameof(TestGossipClient));
	}

	/// <summary>
	/// Disposes the client resources.
	/// </summary>
	public void Dispose() {
		if (!_isDisposed) _isDisposed = true;
	}
}

/// <summary>
/// Factory for creating TestGossipClient instances.
/// </summary>
public class TestGossipClientFactory : IGossipClientFactory {
	readonly Dictionary<DnsEndPoint, IGossipClient> _clientMappings = new(DnsEndPointEqualityComparer.Default);

	/// <summary>
	/// Creates an IGossipClient for the specified endpoint.
	/// </summary>
	/// <param name="endpoint">The endpoint.</param>
	/// <returns>The registered client for the endpoint or a default client.</returns>
	public IGossipClient Create(DnsEndPoint endpoint) {
		if (_clientMappings.TryGetValue(endpoint, out var client))
			return client;

		throw new InvalidOperationException($"No client mapping registered for endpoint {endpoint}");

		// Default client that fails with a meaningful exception if no mapping was registered
		// return new TestGossipClient(new InvalidOperationException($"No client mapping registered for endpoint {endpoint}"));
	}

	/// <summary>
	/// Registers a specific client to be returned for a given endpoint.
	/// </summary>
	/// <param name="endpoint">The endpoint.</param>
	/// <param name="client">The client to return.</param>
	public void RegisterClient(DnsEndPoint endpoint, IGossipClient client) =>
		_clientMappings[endpoint] = client;
}

//
// /// <summary>
// /// Factory for creating TestGossipClient instances.
// /// </summary>
// public class TestGossipClientFactory : IGossipClientFactory {
// 	readonly Dictionary<string, IGossipClient> _clientMappings = new();
//
// 	/// <summary>
// 	/// Creates an IGossipClient for the specified endpoint.
// 	/// </summary>
// 	/// <param name="endpoint">The endpoint.</param>
// 	/// <returns>The registered client for the endpoint or a default client.</returns>
// 	public IGossipClient Create(DnsEndPoint endpoint) {
// 		if (_clientMappings.TryGetValue($"{endpoint.Host}:{endpoint.Port}", out var client))
// 			return client;
//
// 		throw new InvalidOperationException($"No client mapping registered for endpoint {endpoint}");
//
// 		// Default client that fails with a meaningful exception if no mapping was registered
// 		// return new TestGossipClient(new InvalidOperationException($"No client mapping registered for endpoint {endpoint}"));
// 	}
//
// 	/// <summary>
// 	/// Registers a specific client to be returned for a given endpoint.
// 	/// </summary>
// 	/// <param name="endpoint">The endpoint.</param>
// 	/// <param name="client">The client to return.</param>
// 	public void RegisterClient(DnsEndPoint endpoint, IGossipClient client) =>
// 		_clientMappings[$"{endpoint.Host}:{endpoint.Port}"] = client;
// }
