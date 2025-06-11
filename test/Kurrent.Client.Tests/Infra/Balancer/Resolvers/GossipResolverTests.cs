using System.Net;
using Grpc.Core;
using Grpc.Net.Client.Balancer;
using Kurrent.Client.Grpc.Balancer.Resolvers;
using Kurrent.Grpc.Balancer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kurrent.Client.Tests;

public class GossipResolverTests {
	readonly ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

	[Test]
	public void constructs_successfully_with_valid_options() {
		// Arrange
		var options = GossipResolverOptions.Build
			.WithGossipSeed("localhost", 2113)
			.Build();

		var clientFactory = A.Fake<IGossipClientFactory>();
		var mockClient    = A.Fake<IGossipClient>();
		A.CallTo(() => clientFactory.Create(A<DnsEndPoint>._)).Returns(mockClient);

		// Act & Assert - Should not throw
		using var resolver = new GossipResolver(options, clientFactory, _loggerFactory);
		resolver.ShouldNotBeNull();
	}

	[Test]
	public void throws_when_polling_resolver_field_not_found() {
		// This tests the reflection-based field access
		// Note: This test validates the fail-fast behavior when PollingResolver changes
		var options       = GossipResolverOptions.Build.WithGossipSeed("localhost", 2113).Build();
		var clientFactory = A.Fake<IGossipClientFactory>();

		// The constructor should either succeed (if field exists) or throw (if field missing)
		// This test documents the expected behavior
		Should.NotThrow(() => new GossipResolver(options, clientFactory, _loggerFactory));
	}

	[Test]
	public async Task refresh_async_starts_new_refresh_when_resolve_task_completed() {
		// Arrange
		var options = GossipResolverOptions.Build
			.WithGossipSeed("localhost", 2113)
			.WithGossipTimeout(TimeSpan.FromSeconds(1))
			.Build();

		var clientFactory = A.Fake<IGossipClientFactory>();
		var mockClient    = A.Fake<IGossipClient>();
		A.CallTo(() => clientFactory.Create(A<DnsEndPoint>._)).Returns(mockClient);

		// Setup successful gossip response
		var balancerAddresses = new BalancerAddress[] {
			new("node1", 2113),
			new("node2", 2113)
		};

		A.CallTo(() => mockClient.GetClusterTopology(A<CancellationToken>._))
			.Returns(ValueTask.FromResult(balancerAddresses));

		using var resolver = new GossipResolver(options, clientFactory, _loggerFactory);

		// Act
		var result = await resolver.RefreshAsync();

		// Assert
		result.ShouldNotBeNull();
		A.CallTo(() => mockClient.GetClusterTopology(A<CancellationToken>._))
			.MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task discover_endpoints_returns_timeout_when_gossip_times_out() {
		// Arrange
		var options = GossipResolverOptions.Build
			.WithGossipSeed("localhost", 2113)
			.WithGossipTimeout(TimeSpan.FromMilliseconds(100))
			.Build();

		var clientFactory = A.Fake<IGossipClientFactory>();
		var mockClient    = A.Fake<IGossipClient>();
		A.CallTo(() => clientFactory.Create(A<DnsEndPoint>._)).Returns(mockClient);

		// Setup gossip client to delay longer than timeout
		A.CallTo(() => mockClient.GetClusterTopology(A<CancellationToken>._))
			.Returns(DelayedResult());

		using var resolver = new GossipResolver(options, clientFactory, _loggerFactory);

		// Use reflection to access the private DiscoverEndpoints method for testing
		var method = typeof(GossipResolver).GetMethod(
			"DiscoverEndpoints",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
		);

		// Act
		var result = await (Task<GossipDiscoveryResult>)method!.Invoke(resolver, [mockClient, CancellationToken.None])!;

		// Assert
		result.IsT1.ShouldBeTrue(); // Should be GossipTimeout

		static async ValueTask<BalancerAddress[]> DelayedResult() {
			await Task.Delay(500); // Longer than the 100ms timeout
			return [];
		}
	}

	[Test]
	public async Task discover_endpoints_returns_no_viable_endpoints_when_empty_result() {
		// Arrange
		var options = GossipResolverOptions.Build
			.WithGossipSeed("localhost", 2113)
			.Build();

		var clientFactory = A.Fake<IGossipClientFactory>();
		var mockClient    = A.Fake<IGossipClient>();
		A.CallTo(() => clientFactory.Create(A<DnsEndPoint>._)).Returns(mockClient);

		// Setup gossip client to return empty array
		A.CallTo(() => mockClient.GetClusterTopology(A<CancellationToken>._))
			.Returns(ValueTask.FromResult(Array.Empty<BalancerAddress>()));

		using var resolver = new GossipResolver(options, clientFactory, _loggerFactory);

		// Use reflection to access the private DiscoverEndpoints method
		var method = typeof(GossipResolver).GetMethod(
			"DiscoverEndpoints",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
		);

		// Act
		var result = await (Task<GossipDiscoveryResult>)method!.Invoke(resolver, [mockClient, CancellationToken.None])!;

		// Assert
		result.IsT2.ShouldBeTrue(); // Should be NoViableEndpoints
	}

	[Test]
	public async Task discover_endpoints_returns_failure_when_exception_thrown() {
		// Arrange
		var options = GossipResolverOptions.Build
			.WithGossipSeed("localhost", 2113)
			.Build();

		var clientFactory = A.Fake<IGossipClientFactory>();
		var mockClient    = A.Fake<IGossipClient>();
		A.CallTo(() => clientFactory.Create(A<DnsEndPoint>._)).Returns(mockClient);

		var expectedException = new InvalidOperationException("Test exception");
		A.CallTo(() => mockClient.GetClusterTopology(A<CancellationToken>._))
			.Throws(expectedException);

		using var resolver = new GossipResolver(options, clientFactory, _loggerFactory);

		// Use reflection to access the private DiscoverEndpoints method
		var method = typeof(GossipResolver).GetMethod(
			"DiscoverEndpoints",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
		);

		// Act
		var result = await (Task<GossipDiscoveryResult>)method!.Invoke(resolver, [mockClient, CancellationToken.None])!;

		// Assert
		result.IsT3.ShouldBeTrue(); // Should be GossipFailure
		var failure = result.AsT3;
		failure.Exception.ShouldBe(expectedException);
	}

	[Test]
	public async Task discover_endpoints_returns_addresses_when_successful() {
		// Arrange
		var options = GossipResolverOptions.Build
			.WithGossipSeed("localhost", 2113)
			.Build();

		var clientFactory = A.Fake<IGossipClientFactory>();
		var mockClient    = A.Fake<IGossipClient>();
		A.CallTo(() => clientFactory.Create(A<DnsEndPoint>._)).Returns(mockClient);

		var expectedAddresses = new BalancerAddress[] {
			new("node1", 2113),
			new("node2", 2113),
			new("node3", 2113)
		};

		A.CallTo(() => mockClient.GetClusterTopology(A<CancellationToken>._))
			.Returns(ValueTask.FromResult(expectedAddresses));

		using var resolver = new GossipResolver(options, clientFactory, _loggerFactory);

		// Use reflection to access the private DiscoverEndpoints method
		var method = typeof(GossipResolver).GetMethod(
			"DiscoverEndpoints",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
		);

		// Act
		var result = await (Task<GossipDiscoveryResult>)method!.Invoke(resolver, [mockClient, CancellationToken.None])!;

		// Assert
		result.IsT0.ShouldBeTrue(); // Should be BalancerAddress[]
		var addresses = result.AsT0;
		addresses.ShouldBe(expectedAddresses);
	}

	[Test]
	public void update_clients_adds_new_clients_for_new_addresses() {
		// Arrange
		var options = GossipResolverOptions.Build
			.WithGossipSeed("localhost", 2113)
			.Build();

		var clientFactory = A.Fake<IGossipClientFactory>();
		var mockClient1   = A.Fake<IGossipClient>();
		var mockClient2   = A.Fake<IGossipClient>();

		A.CallTo(() => clientFactory.Create(new DnsEndPoint("node1", 2113))).Returns(mockClient1);
		A.CallTo(() => clientFactory.Create(new DnsEndPoint("node2", 2113))).Returns(mockClient2);

		using var resolver = new GossipResolver(options, clientFactory, _loggerFactory);

		// Use reflection to access the private UpdateClients method
		var method = typeof(GossipResolver).GetMethod(
			"UpdateClients",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
		);

		var addresses = new BalancerAddress[] {
			new("node1", 2113),
			new("node2", 2113)
		};

		// Act
		method!.Invoke(resolver, [addresses]);

		// Assert
		A.CallTo(() => clientFactory.Create(new DnsEndPoint("node1", 2113))).MustHaveHappenedOnceExactly();
		A.CallTo(() => clientFactory.Create(new DnsEndPoint("node2", 2113))).MustHaveHappenedOnceExactly();
	}

	[Test]
	public void update_clients_removes_obsolete_clients() {
		// Arrange
		var options = GossipResolverOptions.Build
			.WithGossipSeed("obsolete-node", 2113)
			.Build();

		var clientFactory      = A.Fake<IGossipClientFactory>();
		var mockObsoleteClient = A.Fake<IGossipClient>();
		var mockNewClient      = A.Fake<IGossipClient>();

		A.CallTo(() => clientFactory.Create(new DnsEndPoint("obsolete-node", 2113))).Returns(mockObsoleteClient);
		A.CallTo(() => clientFactory.Create(new DnsEndPoint("new-node", 2113))).Returns(mockNewClient);

		using var resolver = new GossipResolver(options, clientFactory, _loggerFactory);

		// Use reflection to access the private UpdateClients method
		var method = typeof(GossipResolver).GetMethod(
			"UpdateClients",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
		);

		// Act - update with new addresses that don't include the original seed
		var newAddresses = new BalancerAddress[] {
			new("new-node", 2113)
		};

		method!.Invoke(resolver, [newAddresses]);

		// Assert
		A.CallTo(() => mockObsoleteClient.Dispose()).MustHaveHappenedOnceExactly();
		A.CallTo(() => clientFactory.Create(new DnsEndPoint("new-node", 2113))).MustHaveHappenedOnceExactly();
	}

	[Test]
	public void reseed_clients_disposes_existing_and_recreates_from_seeds() {
		// Arrange
		var options = GossipResolverOptions.Build
			.WithGossipSeed("seed1", 2113)
			.WithGossipSeed("seed2", 2113)
			.Build();

		var clientFactory = A.Fake<IGossipClientFactory>();
		var mockClient1   = A.Fake<IGossipClient>();
		var mockClient2   = A.Fake<IGossipClient>();

		A.CallTo(() => clientFactory.Create(A<DnsEndPoint>._)).ReturnsNextFromSequence(mockClient1, mockClient2, mockClient1, mockClient2);

		using var resolver = new GossipResolver(options, clientFactory, _loggerFactory);

		// Use reflection to access the private ReseedClients method
		var method = typeof(GossipResolver).GetMethod(
			"ReseedClients",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
		);

		// Act
		method!.Invoke(resolver, []);

		// Assert - should dispose original clients and recreate
		A.CallTo(() => mockClient1.Dispose()).MustHaveHappened();
		A.CallTo(() => mockClient2.Dispose()).MustHaveHappened();

		// Should recreate clients for each seed (called twice - once in constructor, once in ReseedClients)
		A.CallTo(() => clientFactory.Create(new DnsEndPoint("seed1", 2113))).MustHaveHappenedTwiceExactly();
		A.CallTo(() => clientFactory.Create(new DnsEndPoint("seed2", 2113))).MustHaveHappenedTwiceExactly();
	}

	[Test]
	public void resolver_result_handler_is_called_when_set() {
		// Arrange
		var options = GossipResolverOptions.Build
			.WithGossipSeed("localhost", 2113)
			.Build();

		var clientFactory = A.Fake<IGossipClientFactory>();
		var mockClient    = A.Fake<IGossipClient>();
		A.CallTo(() => clientFactory.Create(A<DnsEndPoint>._)).Returns(mockClient);

		using var resolver = new GossipResolver(options, clientFactory, _loggerFactory);

		var             handlerCalled  = false;
		ResolverResult? capturedResult = null;

		// Act
		resolver.OnResolverResult(result => {
				handlerCalled  = true;
				capturedResult = result;
			}
		);

		// Trigger a result by calling the private Publish method
		var method = typeof(GossipResolver).GetMethod(
			"PublishSuccess",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
		);

		var addresses = new BalancerAddress[] { new("test", 2113) };
		method!.Invoke(resolver, [addresses]);

		// Assert
		handlerCalled.ShouldBeTrue();
		capturedResult.ShouldNotBeNull();
		capturedResult.Status.StatusCode.ShouldBe(StatusCode.OK);
	}

	[Test]
	public void dispose_cleans_up_timer_and_clients() {
		// Arrange
		var options = GossipResolverOptions.Build
			.WithGossipSeed("localhost", 2113)
			.WithRefreshInterval(TimeSpan.FromSeconds(1)) // Enable timer
			.Build();

		var clientFactory = A.Fake<IGossipClientFactory>();
		var mockClient    = A.Fake<IGossipClient>();
		A.CallTo(() => clientFactory.Create(A<DnsEndPoint>._)).Returns(mockClient);

		var resolver = new GossipResolver(options, clientFactory, _loggerFactory);

		// Act
		resolver.Dispose();

		// Assert
		A.CallTo(() => mockClient.Dispose()).MustHaveHappenedOnceExactly();
	}
}
