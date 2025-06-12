using System.Net;
using Grpc.Core;
using Grpc.Net.Client.Balancer;
using Kurrent.Client.Testing.Fixtures;
using Kurrent.Client.Tests.Balancer.Resolvers;
using Kurrent.Grpc.Balancer;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Kurrent.Client.Tests;

public class GossipResolverIntegrationTests : TestFixture {
	[Test]
	public async Task full_gossip_resolution_workflow_succeeds() {
		// Arrange
		var options = GossipResolverOptions.Build
			.WithGossipSeed("seed1", 2113)
			.WithGossipSeed("seed2", 2113)
			.WithRefreshDisabled()
			.Build();

		// Setup test clients with the discoverable nodes
		var discoveredNodes = new BalancerAddress[] {
			new("discovered1", 2113),
			new("discovered2", 2113)
		};

		// Setup client factory with only the nodes we need
		var clientFactory = new TestGossipClientFactory();
		clientFactory.RegisterClient(new DnsEndPoint("seed1", 2113), new TestGossipClient(discoveredNodes));
		clientFactory.RegisterClient(new DnsEndPoint("seed2", 2113), new TestGossipClient(discoveredNodes));
		clientFactory.RegisterClient(new DnsEndPoint("discovered1", 2113), new TestGossipClient(discoveredNodes));
		clientFactory.RegisterClient(new DnsEndPoint("discovered2", 2113), new TestGossipClient(discoveredNodes));

		var resolverResults = new List<ResolverResult>();

		using var resolver = new GossipResolver(options, clientFactory, LoggerFactory);

		resolver.Start(result => resolverResults.Add(result));

		// Act - directly call RefreshAsync with a reasonable timeout
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
		var result = await resolver.RefreshAsync(cts.Token);

		// Assert
		result.ShouldNotBeNull();
		result.Status.StatusCode.ShouldBe(StatusCode.OK);
		result.Addresses.ShouldNotBeEmpty();

		// Result handler should have been called
		resolverResults.ShouldNotBeEmpty();
		resolverResults.Last().Status.StatusCode.ShouldBe(StatusCode.OK);

		Logger.LogInformation("Final resolver result: {StatusCode}", resolverResults.Last().Status.StatusCode);
	}

	[Test]
	public async Task handles_all_seeds_failing_gracefully() {
		// Arrange
		var options = GossipResolverOptions.Build
			.WithGossipSeed("dead-seed1", 2113)
			.WithGossipSeed("dead-seed2", 2113)
			.WithGossipTimeout(TimeSpan.FromMilliseconds(100))
			.Build();

		// Setup test clients that throw exceptions
		var deadClient1 = new TestGossipClient(new TimeoutException("Connection timeout"));
		var deadClient2 = new TestGossipClient(new InvalidOperationException("Connection refused"));

		// Setup client factory
		var clientFactory = new TestGossipClientFactory();
		clientFactory.RegisterClient(new DnsEndPoint("dead-seed1", 2113), deadClient1);
		clientFactory.RegisterClient(new DnsEndPoint("dead-seed2", 2113), deadClient2);

		var resolverResults = new List<ResolverResult>();

		using var resolver = new GossipResolver(options, clientFactory, LoggerFactory);
		resolver.OnResolverResult(result => resolverResults.Add(result));

		resolver.Start(result => {
			Log.Information("Resolver result: {StatusCode}", result.Status.StatusCode);
			resolverResults.Add(result);
		});

		// Act
		var result = await resolver.RefreshAsync();

		// Assert
		result.ShouldNotBeNull();
		result.Status.StatusCode.ShouldBe(StatusCode.Unavailable);

		// Result handler should show failure
		resolverResults.ShouldNotBeEmpty();
		resolverResults.Last().Status.StatusCode.ShouldBe(StatusCode.Unavailable);
	}

	[Test]
	public async Task timeout_handling_works_correctly() {
		// Arrange
		var options = GossipResolverOptions.Build
			.WithGossipSeed("slow-seed", 2113)
			.WithGossipTimeout(TimeSpan.FromMilliseconds(50)) // Very short timeout
			.Build();

		// Create a client that simulates a slow response
		var slowClient = new TestGossipClient(async ct => {
				await Task.Delay(3000, ct); // Longer than 50ms timeout
				return [new BalancerAddress("node", 2113)];
			}
		);

		// Setup client factory
		var clientFactory = new TestGossipClientFactory();
		clientFactory.RegisterClient(new DnsEndPoint("slow-seed", 2113), slowClient);

		using var resolver = new GossipResolver(options, clientFactory, LoggerFactory);
		resolver.Start(_ => { });

		// Act
		var result = await resolver.RefreshAsync();

		// Assert
		result.ShouldNotBeNull();
		result.Status.StatusCode.ShouldBe(StatusCode.Unavailable);
	}

	[Test]
	public async Task empty_topology_response_handled_correctly() {
		// Arrange
		var options = GossipResolverOptions.Build
			.WithGossipSeed("empty-seed", 2113)
			.WithRefreshDisabled()
			.WithMaxReconnectBackoff(TimeSpan.FromSeconds(1)) // Short reconnect backoff()
			.WithInitialReconnectBackoff(TimeSpan.FromSeconds(1)) // Short reconnect backoff()
			.Build();

		// Setup client that returns empty topology
		var emptyClient = new TestGossipClient(Array.Empty<BalancerAddress>());

		// Setup client factory
		var clientFactory = new TestGossipClientFactory();
		clientFactory.RegisterClient(new DnsEndPoint("empty-seed", 2113), emptyClient);


		var resolver = new GossipResolver(options, clientFactory, LoggerFactory);

		using var cts = new CancellationTokenSource(3000); // 3 seconds timeout for the test

		resolver.Start(_ => { });

		resolver.OnResolverResult(result => {
			Log.Information("Resolver result: {StatusCode}", result.Status.StatusCode);
			if (result.Status.StatusCode == StatusCode.Unavailable || cts.Token.IsCancellationRequested)
				cts.Cancel();
		});

		// Act
		var result = await resolver.RefreshAsync(cts.Token);

		resolver.Dispose();

		// Assert
		result.ShouldNotBeNull();
		result.Status.StatusCode.ShouldBe(StatusCode.Unavailable);
	}

	[Test]
	public async Task cancellation_is_respected() {
		// Arrange
		var options = GossipResolverOptions.Build
			.WithGossipSeed("cancellable-seed", 2113)
			.Build();

		// Setup client that never completes
		var nonCompletingClient = new TestGossipClient(async ct => {
				await Task.Delay(Timeout.InfiniteTimeSpan, ct);
				return [];
			}
		);

		// Setup client factory
		var clientFactory = new TestGossipClientFactory();
		clientFactory.RegisterClient(new DnsEndPoint("cancellable-seed", 2113), nonCompletingClient);

		using var resolver = new GossipResolver(options, clientFactory, LoggerFactory);
		using var cts      = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

		resolver.Start(result => {
			Log.Information("Resolver started with result: {StatusCode}", result.Status.StatusCode);
		});

		// Act & Assert
		await Should.ThrowAsync<OperationCanceledException>(async () =>
			await resolver.RefreshAsync(cts.Token)
		);
	}
}
