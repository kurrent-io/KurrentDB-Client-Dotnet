using System.Net;
using System.Net.Sockets;
using Kurrent.Client.Grpc.Balancer.Resolvers;
using Kurrent.Grpc.Balancer;

namespace Kurrent.Client.Tests;

public class GossipResolverOptionsTests {
	[Test]
	public void ensure_valid_throws_when_gossip_seeds_empty() {
		// Arrange
		var options = new GossipResolverOptions {
			GossipSeeds = []
		};

		// Act & Assert
		var exception = Should.Throw<ArgumentException>(() => options.EnsureValid());
		exception.ParamName.ShouldBe("GossipSeeds");
		exception.Message.ShouldContain("cannot be empty");
	}

	[Test]
	public void ensure_valid_throws_when_gossip_timeout_zero_or_negative() {
		// Arrange
		var options = new GossipResolverOptions {
			GossipSeeds   = [new DnsEndPoint("localhost", 2113)],
			GossipTimeout = TimeSpan.Zero
		};

		// Act & Assert
		var exception = Should.Throw<ArgumentOutOfRangeException>(() => options.EnsureValid());
		exception.ParamName.ShouldBe("GossipTimeout");
	}

	[Test]
	public void ensure_valid_throws_when_refresh_interval_zero_or_negative() {
		// Arrange
		var options = new GossipResolverOptions {
			GossipSeeds     = [new DnsEndPoint("localhost", 2113)],
			RefreshInterval = TimeSpan.FromSeconds(-1)
		};

		// Act & Assert
		var exception = Should.Throw<ArgumentOutOfRangeException>(() => options.EnsureValid());
		exception.ParamName.ShouldBe("RefreshInterval");
	}

	[Test]
	public void ensure_valid_throws_when_max_reconnect_backoff_zero_or_negative() {
		// Arrange
		var options = new GossipResolverOptions {
			GossipSeeds         = [new DnsEndPoint("localhost", 2113)],
			MaxReconnectBackoff = TimeSpan.FromSeconds(-1)
		};

		// Act & Assert
		var exception = Should.Throw<ArgumentOutOfRangeException>(() => options.EnsureValid());
		exception.ParamName.ShouldBe("MaxReconnectBackoff");
	}

	[Test]
	public void ensure_valid_allows_infinite_max_reconnect_backoff() {
		// Arrange
		var options = new GossipResolverOptions {
			GossipSeeds         = [new DnsEndPoint("localhost", 2113)],
			MaxReconnectBackoff = Timeout.InfiniteTimeSpan
		};

		// Act & Assert
		Should.NotThrow(() => options.EnsureValid());
	}

	[Test]
	public void ensure_valid_throws_when_initial_reconnect_backoff_zero_or_negative() {
		// Arrange
		var options = new GossipResolverOptions {
			GossipSeeds             = [new DnsEndPoint("localhost", 2113)],
			InitialReconnectBackoff = TimeSpan.Zero
		};

		// Act & Assert
		var exception = Should.Throw<ArgumentOutOfRangeException>(() => options.EnsureValid());
		exception.ParamName.ShouldBe("InitialReconnectBackoff");
	}
}

public class GossipResolverOptionsBuilderTests {
	[Test]
	public void build_returns_default_options_when_no_configuration() {
		// Act
		var options = GossipResolverOptions.Build.Build();

		// Assert
		options.GossipSeeds.ShouldBeEmpty();
		options.GossipTimeout.ShouldBe(TimeSpan.FromSeconds(5));
		options.RefreshInterval.ShouldBe(TimeSpan.FromMinutes(30));
		options.MaxReconnectBackoff.ShouldBe(TimeSpan.FromSeconds(120));
		options.InitialReconnectBackoff.ShouldBe(TimeSpan.FromSeconds(3));
	}

	[Test]
	public void with_gossip_seed_adds_dns_endpoint() {
		// Act
		var options = GossipResolverOptions.Build
			.WithGossipSeed("localhost", 2113)
			.Build();

		// Assert
		options.GossipSeeds.ShouldContain(new DnsEndPoint("localhost", 2113));
	}

	[Test]
	public void with_gossip_seed_prevents_duplicates() {
		// Act
		var builder = GossipResolverOptions.Build
			.WithGossipSeed("localhost", 2113)
			.WithGossipSeed("localhost", 2113); // Duplicate

		var options = builder.Build();

		// Assert
		options.GossipSeeds.Count.ShouldBe(1);
		options.GossipSeeds.ShouldContain(new DnsEndPoint("localhost", 2113));
	}

	[Test]
	public void with_gossip_seeds_adds_multiple_endpoints() {
		// Arrange
		var endpoints = new EndPoint[] {
			new DnsEndPoint("node1", 2113),
			new DnsEndPoint("node2", 2113),
			new IPEndPoint(IPAddress.Loopback, 2113)
		};

		// Act
		var options = GossipResolverOptions.Build
			.WithGossipSeeds(endpoints)
			.Build();

		// Assert
		options.GossipSeeds.Count.ShouldBe(3);
		options.GossipSeeds.ShouldContain(new DnsEndPoint("node1", 2113));
		options.GossipSeeds.ShouldContain(new DnsEndPoint("node2", 2113));
		options.GossipSeeds.ShouldContain(new DnsEndPoint("127.0.0.1", 2113));
	}

	[Test]
	public void with_gossip_seeds_throws_when_empty_array() {
		// Act & Assert
		var exception = Should.Throw<ArgumentException>(() =>
			GossipResolverOptions.Build.WithGossipSeeds([])
		);

		exception.ParamName.ShouldBe("endpoints");
	}

	[Test]
	public void with_gossip_seeds_converts_ip_endpoint_to_dns_endpoint() {
		// Arrange
		var ipEndpoint = new IPEndPoint(IPAddress.Parse("192.168.1.100"), 2113);

		// Act
		var options = GossipResolverOptions.Build
			.WithGossipSeed(ipEndpoint)
			.Build();

		// Assert
		options.GossipSeeds.ShouldContain(new DnsEndPoint("192.168.1.100", 2113));
	}

	[Test]
	public void with_gossip_seeds_throws_for_unsupported_endpoint_type() {
		// Arrange
		var unsupportedEndpoint = new FakeEndPoint();

		// Act & Assert
		Should.Throw<ArgumentException>(() =>
			GossipResolverOptions.Build.WithGossipSeed(unsupportedEndpoint)
		);
	}

	[Test]
	public void with_gossip_timeout_sets_timeout() {
		// Act
		var options = GossipResolverOptions.Build
			.WithGossipTimeout(TimeSpan.FromSeconds(10))
			.Build();

		// Assert
		options.GossipTimeout.ShouldBe(TimeSpan.FromSeconds(10));
	}

	[Test]
	public void with_gossip_timeout_throws_when_zero_or_negative() {
		// Act & Assert
		Should.Throw<ArgumentOutOfRangeException>(() =>
			GossipResolverOptions.Build.WithGossipTimeout(TimeSpan.Zero)
		);
	}

	[Test]
	public void with_refresh_interval_sets_interval() {
		// Act
		var options = GossipResolverOptions.Build
			.WithRefreshInterval(TimeSpan.FromMinutes(10))
			.Build();

		// Assert
		options.RefreshInterval.ShouldBe(TimeSpan.FromMinutes(10));
	}

	[Test]
	public void with_refresh_disabled_sets_infinite_interval() {
		// Act
		var options = GossipResolverOptions.Build
			.WithRefreshDisabled()
			.Build();

		// Assert
		options.RefreshInterval.ShouldBe(Timeout.InfiniteTimeSpan);
	}

	[Test]
	public void with_max_reconnect_backoff_sets_backoff() {
		// Act
		var options = GossipResolverOptions.Build
			.WithMaxReconnectBackoff(TimeSpan.FromMinutes(5))
			.Build();

		// Assert
		options.MaxReconnectBackoff.ShouldBe(TimeSpan.FromMinutes(5));
	}

	[Test]
	public void with_initial_reconnect_backoff_sets_backoff() {
		// Act
		var options = GossipResolverOptions.Build
			.WithInitialReconnectBackoff(TimeSpan.FromSeconds(1))
			.Build();

		// Assert
		options.InitialReconnectBackoff.ShouldBe(TimeSpan.FromSeconds(1));
	}

	[Test]
	public void fluent_interface_chains_correctly() {
		// Act
		var options = GossipResolverOptions.Build
			.WithGossipSeed("node1", 2113)
			.WithGossipSeed("node2", 2113)
			.WithGossipTimeout(TimeSpan.FromSeconds(10))
			.WithRefreshInterval(TimeSpan.FromMinutes(15))
			.WithMaxReconnectBackoff(TimeSpan.FromMinutes(2))
			.WithInitialReconnectBackoff(TimeSpan.FromSeconds(1))
			.Build();

		// Assert
		options.GossipSeeds.Count.ShouldBe(2);
		options.GossipTimeout.ShouldBe(TimeSpan.FromSeconds(10));
		options.RefreshInterval.ShouldBe(TimeSpan.FromMinutes(15));
		options.MaxReconnectBackoff.ShouldBe(TimeSpan.FromMinutes(2));
		options.InitialReconnectBackoff.ShouldBe(TimeSpan.FromSeconds(1));
	}

	class FakeEndPoint : EndPoint {
		public override AddressFamily AddressFamily => AddressFamily.Unknown;
	}
}
