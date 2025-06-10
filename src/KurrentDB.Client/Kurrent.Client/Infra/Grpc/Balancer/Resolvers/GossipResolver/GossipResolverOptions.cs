using System.Net;

namespace Kurrent.Grpc.Balancer;

/// <summary>
/// Represents configuration options for the Gossip-based resolver that is used
/// to discover and select cluster nodes in a distributed system.
/// </summary>
public record GossipResolverOptions {
	public static readonly GossipResolverOptionsBuilder Build   = new();
	public static readonly GossipResolverOptions        Default = new();

	/// <summary>
	/// Gets the list of gossip seed endpoints used for initial discovery.
	/// </summary>
	public IReadOnlyList<DnsEndPoint> GossipSeeds { get; init; } = [];

	/// <summary>
	/// Gets the timeout for gossip requests.
	/// <para>
	/// Defaults to 5 seconds.
	/// </para>
	/// </summary>
	public TimeSpan GossipTimeout { get; init; } = TimeSpan.FromSeconds(5);

	/// <summary>
	/// Gets the interval at which the gossip resolver refreshes
	/// its discovery process to update the cluster nodes information.
	/// <para>
	/// Defaults to 30 minutes.
	/// </para>
	/// </summary>
	public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromMinutes(30);

	/// <summary>
	/// Gets the the maximum time between subsequent connection attempts.
	/// <para>
	/// The reconnect backoff starts at an initial backoff and then exponentially increases between attempts, up to the maximum reconnect backoff.
	/// Reconnect backoff adds a jitter to randomize the backoff. This is done to avoid spikes of connection attempts.
	/// </para>
	/// <para>
	/// A <c>Timeout.InfiniteTimeSpan</c> value removes the maximum reconnect backoff limit.
	/// </para>
	/// <para>
	/// Defaults to 120 seconds.
	/// </para>
	/// </summary>
	public TimeSpan MaxReconnectBackoff { get; init; } = TimeSpan.FromSeconds(120);

	/// <summary>
	/// Gets the time between the first and second connection attempts.
	/// <para>
	/// The reconnect backoff starts at an initial backoff and then exponentially increases between attempts, up to the maximum reconnect backoff.
	/// Reconnect backoff adds a jitter to randomize the backoff. This is done to avoid spikes of connection attempts.
	/// </para>
	/// <para>
	/// Defaults to 3 seconds.
	/// </para>
	/// </summary>
	public TimeSpan InitialReconnectBackoff { get; init; } = TimeSpan.FromSeconds(3);

	public GossipResolverOptions EnsureValid() {
		if (GossipSeeds.Count == 0)
			throw new ArgumentException("GossipSeeds cannot be empty.", nameof(GossipSeeds));

		if (GossipTimeout <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(GossipTimeout), "GossipTimeout must be greater than zero.");

		if (RefreshInterval != Timeout.InfiniteTimeSpan && RefreshInterval <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(RefreshInterval), "RefreshInterval must be greater than zero.");

		if (MaxReconnectBackoff != Timeout.InfiniteTimeSpan && MaxReconnectBackoff <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(MaxReconnectBackoff), "MaxReconnectBackoff must be infinite or greater than zero.");

		if (InitialReconnectBackoff <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(InitialReconnectBackoff), "InitialReconnectBackoff must be greater than zero.");

		return this;
	}
}

/// <summary>
/// Builder for creating instances of <see cref="GossipResolverOptions"/>.
/// </summary>
public record GossipResolverOptionsBuilder {
	/// <summary>
	/// Creates a new builder with default options.
	/// </summary>
	public GossipResolverOptionsBuilder() { }

	/// <summary>
	/// Creates a new builder with options initialized from the specified options.
	/// </summary>
	/// <param name="options">The options to initialize from.</param>
	public GossipResolverOptionsBuilder(GossipResolverOptions options) => Current = options;

	GossipResolverOptions Current { get; init; } = new();

	/// <summary>
	/// Adds a gossip seed endpoint to the builder if it's not already present.
	/// </summary>
	/// <param name="endpoint">The endpoint to add (DnsEndPoint, IPEndPoint, or any other EndPoint).</param>
	/// <returns>A new builder instance with the updated configuration.</returns>
	/// <exception cref="ArgumentNullException">Thrown when endpoint is null.</exception>
	/// <exception cref="ArgumentException">Thrown when endpoint is not a supported type.</exception>
	public GossipResolverOptionsBuilder WithGossipSeed(DnsEndPoint endpoint) {
		if (Current.GossipSeeds.Contains(endpoint))
			return this;

		return this with { Current = Current with { GossipSeeds = [.. Current.GossipSeeds, endpoint] } };
	}

	/// <summary>
	/// Adds a gossip seed endpoint to the builder if it's not already present.
	/// </summary>
	/// <param name="endpoint">The endpoint to add (DnsEndPoint, IPEndPoint, or any other EndPoint).</param>
	/// <returns>A new builder instance with the updated configuration.</returns>
	/// <exception cref="ArgumentNullException">Thrown when endpoint is null.</exception>
	/// <exception cref="ArgumentException">Thrown when endpoint is not a supported type.</exception>
	public GossipResolverOptionsBuilder WithGossipSeed(EndPoint endpoint) =>
		WithGossipSeed(ConvertToDnsEndPoint(endpoint));

	/// <summary>
	/// Adds a gossip seed endpoint to the builder.
	/// </summary>
	/// <param name="host">The DNS host name or IP address.</param>
	/// <param name="port">The port number.</param>
	/// <returns>A new builder instance with the updated configuration.</returns>
	/// <exception cref="ArgumentException">Thrown when host is null or empty.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when port is invalid.</exception>
	public GossipResolverOptionsBuilder WithGossipSeed(string host, int port) =>
		WithGossipSeed(new DnsEndPoint(host, port));

	/// <summary>
	/// Adds multiple gossip seed endpoints to the builder, skipping any duplicates.
	/// </summary>
	/// <param name="endpoints">The collection of endpoints (can be any type of EndPoint).</param>
	/// <returns>A new builder instance with the updated configuration.</returns>
	/// <exception cref="ArgumentNullException">Thrown when endpoints is null.</exception>
	public GossipResolverOptionsBuilder WithGossipSeeds(IList<EndPoint> endpoints) {
		if (endpoints.Count == 0)
			throw new ArgumentException("Endpoints array cannot be empty.", nameof(endpoints));

		// Convert and filter out duplicates
		var newSeeds = endpoints.Select(ConvertToDnsEndPoint)
			.Where(ep => !Current.GossipSeeds.Contains(ep))
			.ToArray();

		// If there are no new endpoints to add after
		// filtering duplicates, return current builder
		if (newSeeds.Length == 0)
			return this;

		return this with { Current = Current with { GossipSeeds = [.. Current.GossipSeeds, .. newSeeds] } };
	}

	/// <summary>
	/// Adds multiple gossip seed endpoints to the builder, skipping any duplicates.
	/// </summary>
	/// <param name="endpoints">The endpoints to add (can be any type of EndPoint).</param>
	/// <returns>A new builder instance with the updated configuration.</returns>
	/// <exception cref="ArgumentNullException">Thrown when endpoints is null.</exception>
	public GossipResolverOptionsBuilder WithGossipSeeds(params EndPoint[] endpoints) =>
		WithGossipSeeds(endpoints.ToList());

	/// <summary>
	/// Sets the timeout for gossip requests.
	/// </summary>
	/// <param name="timeout">The timeout value.</param>
	/// <returns>A new builder instance with the updated configuration.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is less than or equal to zero.</exception>
	public GossipResolverOptionsBuilder WithGossipTimeout(TimeSpan timeout) {
		if (timeout <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(timeout), "GossipTimeout must be greater than zero.");

		return this with { Current = Current with { GossipTimeout = timeout } };
	}

	/// <summary>
	/// Sets the interval at which the gossip resolver refreshes its discovery process.
	/// </summary>
	/// <param name="interval">The refresh interval.</param>
	/// <returns>A new builder instance with the updated configuration.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when interval is less than or equal to zero.</exception>
	public GossipResolverOptionsBuilder WithRefreshInterval(TimeSpan interval) {
		if (interval <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(interval), "RefreshInterval must be greater than zero.");

		return this with { Current = Current with { RefreshInterval = interval } };
	}

	/// <summary>
	/// Disables the refresh interval by setting it to infinite.
	/// </summary>
	/// <returns>A new builder instance with refresh disabled.</returns>
	public GossipResolverOptionsBuilder WithRefreshDisabled() =>
		this with { Current = Current with { RefreshInterval = Timeout.InfiniteTimeSpan } };

	/// <summary>
	/// Sets the maximum time between subsequent connection attempts.
	/// </summary>
	/// <param name="backoff">The maximum reconnect backoff.</param>
	/// <returns>A new builder instance with the updated configuration.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when backoff is not infinite and is less than or equal to zero.</exception>
	public GossipResolverOptionsBuilder WithMaxReconnectBackoff(TimeSpan backoff) {
		if (backoff != Timeout.InfiniteTimeSpan && backoff <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(backoff), "MaxReconnectBackoff must be infinite or greater than zero.");

		return this with { Current = Current with { MaxReconnectBackoff = backoff } };
	}

	/// <summary>
	/// Sets the time between the first and second connection attempts.
	/// </summary>
	/// <param name="backoff">The initial reconnect backoff.</param>
	/// <returns>A new builder instance with the updated configuration.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when backoff is less than or equal to zero.</exception>
	public GossipResolverOptionsBuilder WithInitialReconnectBackoff(TimeSpan backoff) {
		if (backoff <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(backoff), "InitialReconnectBackoff must be greater than zero.");

		return this with { Current = Current with { InitialReconnectBackoff = backoff } };
	}

	/// <summary>
	/// Builds and validates a new instance of <see cref="GossipResolverOptions"/>.
	/// </summary>
	/// <returns>A configured and validated <see cref="GossipResolverOptions"/> instance.</returns>
	/// <exception cref="ArgumentException">Thrown when validation fails.</exception>
	public GossipResolverOptions Build() => Current.EnsureValid();

	/// <summary>
	/// Converts an EndPoint to a DnsEndPoint.
	/// </summary>
	static DnsEndPoint ConvertToDnsEndPoint(EndPoint endpoint) => endpoint switch {
		DnsEndPoint dns => dns,
		IPEndPoint ip   => new DnsEndPoint(ip.Address.ToString(), ip.Port),
		_               => throw new ArgumentException($"Unsupported endpoint type: {endpoint.GetType().Name}", nameof(endpoint))
	};
}


// Note: The following code is commented out as it refers to an older version of the GossipResolverOptions.
// /// <summary>
// /// Represents configuration options for the Gossip-based resolver that is used
// /// to discover and select cluster nodes in a distributed system.
// /// </summary>
// public record GossipResolverOptionsWithOldRetries {
// 	/// <summary>
// 	/// Gets the list of gossip seed endpoints used for initial discovery.
// 	/// These can be either DnsEndPoint or IPEndPoint instances.
// 	/// </summary>
// 	public IReadOnlyList<DnsEndPoint> GossipSeeds { get; init; } = [];
//
// 	/// <summary>
// 	/// Gets the maximum number of discovery attempts before giving up.
// 	/// </summary>
// 	public int MaxDiscoverAttempts { get; init; } = 3;
//
// 	/// <summary>
// 	/// Gets the interval to wait between discovery attempts.
// 	/// </summary>
// 	public TimeSpan DiscoveryInterval { get; init; } = TimeSpan.FromSeconds(3);
//
// 	/// <summary>
// 	/// Gets the timeout for gossip requests.
// 	/// </summary>
// 	public TimeSpan GossipTimeout { get; init; } = TimeSpan.FromSeconds(5);
//
// 	/// <summary>
// 	/// Gets the interval at which the gossip resolver refreshes
// 	/// its discovery process to update the cluster nodes information.
// 	/// </summary>
// 	public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromMinutes(30);
//
// 	/// <summary>
// 	/// Specifies the time allowed for completing the disposal of resources when shutting down.
// 	/// This defines a timeout period for cleanup processes to complete before termination.
// 	/// </summary>
// 	public TimeSpan DisposalTimeout { get; init; } = TimeSpan.FromSeconds(5);
// }
