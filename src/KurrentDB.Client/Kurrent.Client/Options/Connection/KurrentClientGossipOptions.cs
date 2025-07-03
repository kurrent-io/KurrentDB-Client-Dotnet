using KurrentDB.Client;

namespace Kurrent.Client;

/// <summary>
/// Represents configuration settings for the gossip protocol used in cluster discovery and management.
/// </summary>
/// <remarks>
/// <para>
/// The gossip protocol enables KurrentDB clients to automatically discover available nodes in a cluster,
/// manage node failover, and implement intelligent read/write routing.
/// </para>
/// <para>
/// These settings control the behavior of the discovery process, including timing, retry attempts,
/// and node selection preferences.
/// </para>
/// </remarks>
[PublicAPI]
public record KurrentClientGossipOptions : OptionsBase<KurrentClientGossipOptions, KurrentClientGossipOptionsValidator> {
    /// <summary>
    /// The maximum number of times to attempt node discovery.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If all discovery attempts fail, the client will shut down.
    /// </para>
    /// <para>
    /// Each attempt will use the specified <see cref="DiscoveryInterval"/> between retries.
    /// </para>
    /// </remarks>
    public int MaxDiscoverAttempts { get; init; } = 10;

    /// <summary>
    /// The polling interval used between node discovery attempts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This controls how long the client waits between consecutive discovery attempts.
    /// </para>
    /// <para>
    /// Lower values make the discovery process more aggressive but may increase network traffic.
    /// Higher values reduce network overhead but increase total discovery time.
    /// </para>
    /// </remarks>
    public TimeSpan DiscoveryInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// The timeout after which an individual discovery attempt will fail.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the maximum time allowed for a single gossip discovery request to complete.
    /// If a request exceeds this timeout, it will be canceled and the next attempt will proceed.
    /// </para>
    /// <para>
    /// Should be set higher than typical network latency to the cluster to avoid premature timeouts.
    /// </para>
    /// </remarks>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Controls which nodes in the cluster are selected for read operations.
    /// </summary>
    /// <remarks>
    /// This allows for tailoring read distribution based on use case requirements.
    /// For example, use <see cref="NodePreference.Leader"/> for guaranteed consistency, or
    /// <see cref="NodePreference.Follower"/> for scaling reads across the cluster.
    /// The available options are:
    /// <list type="table">
    ///   <item><description><see cref="NodePreference.Leader"/> - Always read from the cluster leader</description></item>
    ///   <item><description><see cref="NodePreference.Follower"/> - Prefer reading from follower nodes</description></item>
    ///   <item><description><see cref="NodePreference.Random"/> - Select any available node randomly</description></item>
    ///   <item><description><see cref="NodePreference.ReadOnlyReplica"/> - Prefer read-only replica nodes</description></item>
    /// </list>
    /// </remarks>
    public NodePreference ReadPreference { get; init; } = NodePreference.Random;

    /// <summary>
    /// The default gossip configuration settings.
    /// This provides a sensible starting point for most applications using KurrentDB.
    /// </summary>
    /// <remarks>
    /// Default configuration includes:
    /// <list type="bullet">
    ///   <item><description>MaxDiscoverAttempts = 10</description></item>
    ///   <item><description>DiscoveryInterval = 100ms</description></item>
    ///   <item><description>Timeout = 5s</description></item>
    ///   <item><description>ReadPreference = Random</description></item>
    /// </list>
    /// </remarks>
    public static readonly KurrentClientGossipOptions Default = new() {
        MaxDiscoverAttempts = 10,
        DiscoveryInterval   = TimeSpan.FromMilliseconds(100),
        Timeout             = TimeSpan.FromSeconds(5),
        ReadPreference      = NodePreference.Random
    };
}
