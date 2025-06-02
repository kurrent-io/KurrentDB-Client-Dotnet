using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using Grpc.Core;
using Grpc.Net.Client.Balancer;
using Humanizer;
using Microsoft.Extensions.Logging;
using OneOf;

namespace Kurrent.Grpc.Balancer;

static class GossipErrors {
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public readonly struct GossipTimeout {
		public static readonly GossipTimeout Value = new();
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public readonly struct NoViableEndpoints {
		public static readonly NoViableEndpoints Value = new();
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public readonly struct GossipFailure(Exception exception) {
		public Exception Exception { get; } = exception;
	}
}

[GenerateOneOf]
partial class GossipDiscoveryResult : OneOfBase<BalancerAddress[], GossipErrors.GossipTimeout, GossipErrors.NoViableEndpoints, GossipErrors.GossipFailure>;

public sealed partial class GossipResolver : PollingResolver {
	public GossipResolver(
		GossipResolverOptions options,
		IGossipClientFactory clientFactory,
		ILoggerFactory loggerFactory
	) : this(
		options,
		clientFactory,
		new ExponentialBackoffPolicyFactory(
			options.InitialReconnectBackoff,
			options.MaxReconnectBackoff
		),
		loggerFactory
	) { }

	public GossipResolver(
		GossipResolverOptions options,
		IGossipClientFactory clientFactory,
		IBackoffPolicyFactory backoffPolicyFactory,
		ILoggerFactory loggerFactory
	) : base(loggerFactory, backoffPolicyFactory) {
		Options      = options;
		Logger       = loggerFactory.CreateLogger<GossipResolver>();
		CreateClient = clientFactory.Create;
		Clients      = [];

		// Cache the field info
		var resolveTaskField = typeof(PollingResolver).GetField("_resolveTask", BindingFlags.Instance | BindingFlags.NonPublic)!;
		ResolveTask = (Task)resolveTaskField.GetValue(this)!;
	}

	GossipResolverOptions Options { get; }
	ILogger               Logger  { get; }

	Func<DnsEndPoint, IGossipClient>                 CreateClient { get; }
	ConcurrentDictionary<DnsEndPoint, IGossipClient> Clients      { get; }

	Timer? RefreshTimer { get; set; }

	Action<ResolverResult>? ResolverResultHandler { get; set; }

	Task            ResolveTask { get; }
	ResolverResult? LastResult  { get; set; }

	public async Task<ResolverResult?> RefreshAsync(CancellationToken cancellationToken = default) {
		// Check if a refresh is already in progress
		// otherwise, start a new refresh
		if (ResolveTask.IsCompleted) {
			// Start a new refresh
			Refresh();
		}

		// Wait for the refresh to complete
		if (cancellationToken.CanBeCanceled)
			await ResolveTask.WaitAsync(cancellationToken);
		else
			await ResolveTask;

		// Return the last result, which should be set by the resolver
		// If the resolver hasn't set it, it means the refresh failed or was cancelled
		return LastResult;
	}

	public void OnResolverResult(Action<ResolverResult> handler) => ResolverResultHandler = handler;

	protected override void OnStarted() {
		base.OnStarted();

		// Initialize clients with gossip seeds
		ReseedClients();

		// Initialize the refresh timer
		if (Options.RefreshInterval != Timeout.InfiniteTimeSpan) {
			LogRefreshTimerStarted(Logger, Options.RefreshInterval);

			void TimerCallback(object? _) {
				try {
					LogScheduledRefreshStarting(Logger, DateTime.UtcNow);
					Refresh();
				}
				catch (Exception ex) {
					LogRefreshTimerError(Logger, ex);
				}
			}

			RefreshTimer = NonCapturingTimer.Create(TimerCallback, Options.RefreshInterval);
		}
	}

	protected override async Task ResolveAsync(CancellationToken cancellationToken) {
		LogStartingDiscovery(Logger, Clients.Count);

        var start = TimeProvider.System.GetTimestamp();

        try {
            // Attempt to discover endpoints using gossip protocol
            // Try each endpoint in random order to avoid always hitting the same endpoint first.
            foreach (var (endpoint, client) in Clients.OrderBy(_ => Random.Shared.Next())) {
                LogGossipAttempt(Logger, endpoint.ToString());

                var result = await DiscoverEndpoints(client, cancellationToken);

                switch (result.Value) {
                    case BalancerAddress[] addresses:
                        LogDiscoverySuccess(Logger, addresses.Length, addresses.Select(x => x.EndPoint.ToString()).ToArray());
                        UpdateClients(addresses);
                        PublishSuccess(addresses);
                        return;

                    case GossipErrors.GossipTimeout:
                        var elapsed = TimeProvider.System.GetElapsedTime(start).Humanize();
                        LogGossipRequestTimeOutFailure(Logger, endpoint.ToString(), elapsed);
                        break;

                    case GossipErrors.NoViableEndpoints:
                        LogGossipNoViableEndpointsFailure(Logger, endpoint.ToString());
                        break;

                    case GossipErrors.GossipFailure failure:
                        LogGossipFailure(Logger, endpoint.ToString(), failure.Exception);
                        break;
                }
            }

            // If we get here, we couldn't get gossip from any endpoint
            LogDiscoveryNoViableEndpointsFailure(Logger, Clients.Count, TimeProvider.System.GetElapsedTime(start).Humanize());

            // Reseed clients with the original gossip seeds
            ReseedClients();

            // We've exhausted all clients and we must report the failure so
            // that the PollingResolver can handle the failure and retry
            PublishFailure("No viable nodes found.");
        }
        catch (OperationCanceledException) {
            // Don't report cancellation as a failure
            LogDiscoveryCancelled(Logger);
        }
        catch (Exception ex) {
            // Report discovery failure
            LogDiscoveryFailure(Logger, TimeProvider.System.GetElapsedTime(start).Humanize(), ex);
            PublishFailure("Failed to discover viable nodes.");
        }

	}

	async Task<GossipDiscoveryResult> DiscoverEndpoints(IGossipClient client, CancellationToken cancellationToken) {
		using var cancellator = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		cancellator.CancelAfter(Options.GossipTimeout);
		var timeoutToken = cancellator.Token;

		try {
			var nodes = await client
				.GetClusterTopology(timeoutToken)
				.ConfigureAwait(false);

			return nodes.Length == 0
				? GossipErrors.NoViableEndpoints.Value
				: nodes;
		}
		catch (OperationCanceledException) when (timeoutToken.IsCancellationRequested) {
			return GossipErrors.GossipTimeout.Value;
		}
		catch (Exception ex) {
			return new GossipErrors.GossipFailure(ex);
		}

		// static BalancerAddress CreateBalancerAddress(ClusterNode node) {
		// 	var address = new BalancerAddress(node.Endpoint.Host, node.Endpoint.Port);
		// 	// pass attributes from the node to the balancer address
		// 	// useful for custom load balancing logic
		// 	foreach (var attribute in node.Attributes)
		// 		address.Attributes.WithValue(attribute.Key, attribute.Value);
		// 		// address.Attributes.Set(new BalancerAttributesKey<object?>(attribute.Key), attribute.Value);
		//
		// 	return address;
		// }
	}

	/// <summary>
	/// Updates the collection of known clients based on the given cluster nodes.
	/// Ensures the clients list reflects the current cluster topology by adding new clients,
	/// removing obsolete ones, and disposing of their resources when necessary.
	/// </summary>
	void UpdateClients(BalancerAddress[] addresses) {
		// Add or update clients for all nodes in the cluster
		foreach (var address in addresses)
			if (!Clients.ContainsKey(address.EndPoint))
				Clients.TryAdd(address.EndPoint, CreateClient(address.EndPoint));

		// Remove clients for endpoints no longer in the cluster
		var currentEndpoints = addresses.Select(m => m.EndPoint).ToHashSet();
		foreach (var endpoint in Clients.Keys.Where(ep => !currentEndpoints.Contains(ep)).ToList())
			if (Clients.TryRemove(endpoint, out var client))
				client.Dispose();

		LogClientsUpdated(Logger, Clients.Count);
	}

	/// <summary>
	/// Reseeds the clients with original gossip seeds endpoints
	/// </summary>
	void ReseedClients() {
		// Dispose existing clients before reseeding
		foreach (var client in Clients.Values)
			client.Dispose();

		Clients.Clear();

		// Recreate clients for each gossip seed endpoint
		foreach (var endpoint in Options.GossipSeeds)
			Clients.TryAdd(endpoint, CreateClient(endpoint));

		LogChannelsReseeded(Logger, Clients.Count);
	}

	/// <summary>
	/// Publishes the resolution results to the listener and invokes the result handler if it is set.
	/// This ensures the results are broadcasted for further processing or updates.
	/// </summary>
	void Publish(ResolverResult result) {
		// Call the base class Listener
		Listener(LastResult = result);

		// Notify our handler if set
		ResolverResultHandler?.Invoke(result);
	}

	void PublishFailure(string errorMessage) {
		var status = new Status(StatusCode.Unavailable, errorMessage);
		var result = ResolverResult.ForFailure(status);
		Publish(result);
	}

	void PublishSuccess(BalancerAddress[] addresses) {
		var result = ResolverResult.ForResult(addresses);
		Publish(result);
	}

	protected override void Dispose(bool disposing) {
		base.Dispose(disposing);

		if (disposing) {
			// Dispose the timer
			RefreshTimer?.Dispose();

			// Dispose all clients
			foreach (var client in Clients.Values)
				client.Dispose();

			Clients.Clear();
		}
	}

	#region Logging

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Starting cluster topology discovery with {NodeCount} available gossip nodes")]
    private static partial void LogStartingDiscovery(ILogger logger, int nodeCount);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Topology discovery successful. Found {NodeCount} cluster nodes: {Nodes}")]
    private static partial void LogDiscoverySuccess(ILogger logger, int nodeCount, string[] nodes);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Gossip request to node {Node} timed out after {ElapsedTime}")]
    private static partial void LogGossipRequestTimeOutFailure(ILogger logger, string node, string elapsedTime);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Gossip from node {Node} returned empty topology. No viable nodes found")]
    private static partial void LogGossipNoViableEndpointsFailure(ILogger logger, string node);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Failed to get gossip from node {Node}")]
    private static partial void LogGossipFailure(ILogger logger, string node, Exception exception);

    [LoggerMessage(EventId = 6, Level = LogLevel.Critical, Message = "Topology discovery failed. No viable nodes found after trying {AttemptCount} nodes. Time elapsed: {ElapsedTime}")]
    private static partial void LogDiscoveryNoViableEndpointsFailure(ILogger logger, int attemptCount, string elapsedTime);

    [LoggerMessage(EventId = 7, Level = LogLevel.Critical, Message = "Topology discovery failed after {ElapsedTime}")]
    private static partial void LogDiscoveryFailure(ILogger logger, string elapsedTime, Exception exception);

    [LoggerMessage(EventId = 8, Level = LogLevel.Trace, Message = "Reset to initial seed nodes. Now using {SeedCount} gossip seed nodes")]
    private static partial void LogChannelsReseeded(ILogger logger, int seedCount);

    [LoggerMessage(EventId = 9, Level = LogLevel.Trace, Message = "Updated node connections. Now tracking {NodeCount} cluster nodes")]
    private static partial void LogClientsUpdated(ILogger logger, int nodeCount);

    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Topology discovery operation cancelled")]
    private static partial void LogDiscoveryCancelled(ILogger logger);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Started topology refresh timer with interval {RefreshInterval}")]
    private static partial void LogRefreshTimerStarted(ILogger logger, TimeSpan refreshInterval);

    [LoggerMessage(EventId = 12, Level = LogLevel.Debug, Message = "Scheduled topology refresh starting at {Time}")]
    private static partial void LogScheduledRefreshStarting(ILogger logger, DateTime time);

    [LoggerMessage(EventId = 13, Level = LogLevel.Error, Message = "Error in topology refresh timer")]
    private static partial void LogRefreshTimerError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 15, Level = LogLevel.Trace, Message = "Requesting cluster topology from node {Node}")]
    private static partial void LogGossipAttempt(ILogger logger, string node);

    #endregion
}

public class GossipResolverFactory : ResolverFactory {
	public GossipResolverFactory(string resolverSchemeName, GossipResolverOptions options, IGossipClientFactory clientFactory) {
		if (string.IsNullOrWhiteSpace(resolverSchemeName))
			throw new ArgumentException("Value cannot be null or whitespace.", nameof(resolverSchemeName));

		Name          = resolverSchemeName;
		Options       = options;
		ClientFactory = clientFactory;
	}

	GossipResolverOptions Options       { get; }
	IGossipClientFactory  ClientFactory { get; }

	public override string Name { get; }

	public override Resolver Create(ResolverOptions options) =>
		new GossipResolver(Options, ClientFactory, options.LoggerFactory);
}

// public sealed partial class BeforeNewLogsGossipResolver : PollingResolver {
// 	public GossipResolver(
// 		GossipResolverOptions options,
// 		IGossipClientFactory clientFactory,
// 		ILoggerFactory loggerFactory
// 	) : this(
// 		options,
// 		clientFactory,
// 		new ExponentialBackoffPolicyFactory(
// 			options.InitialReconnectBackoff,
// 			options.MaxReconnectBackoff
// 		),
// 		loggerFactory
// 	) { }
//
// 	public GossipResolver(
// 		GossipResolverOptions options,
// 		IGossipClientFactory clientFactory,
// 		IBackoffPolicyFactory backoffPolicyFactory,
// 		ILoggerFactory loggerFactory
// 	) : base(loggerFactory, backoffPolicyFactory) {
// 		Options      = options;
// 		Logger       = loggerFactory.CreateLogger<GossipResolver>();
// 		CreateClient = clientFactory.Create;
// 		Clients      = [];
// 	}
//
// 	GossipResolverOptions Options { get; }
// 	ILogger               Logger  { get; }
//
// 	Func<DnsEndPoint, IGossipClient>                 CreateClient { get; }
// 	ConcurrentDictionary<DnsEndPoint, IGossipClient> Clients      { get; }
//
// 	Timer? RefreshTimer { get; set; }
//
// 	Action<ResolverResult>? ResolverResultHandler { get; set; }
//
// 	TaskCompletionSource<ResolverResult> _refreshCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
//
// 	public Task<ResolverResult> ForceRefreshAsync(CancellationToken cancellationToken = default) {
// 		if (!_refreshCompletionSource.Task.IsCompleted) {
// 			// If a refresh is already in progress, return the existing task
// 			return _refreshCompletionSource.Task;
// 		}
//
// 		_refreshCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
//
// 		Refresh();
//
// 		cancellationToken.Register(() => _refreshCompletionSource?.TrySetCanceled(cancellationToken));
//
// 		return _refreshCompletionSource.Task;
// 	}
//
// 	public void OnResolverResult(Action<ResolverResult> handler) => ResolverResultHandler = handler;
//
//
// 	protected override void OnStarted() {
// 		base.OnStarted();
//
// 		// Initialize clients with gossip seeds
// 		ReseedClients();
//
// 		// Initialize the refresh timer
// 		if (Options.RefreshInterval != Timeout.InfiniteTimeSpan) {
// 			LogRefreshTimerStarted(Logger, Options.RefreshInterval);
//
// 			void TimerCallback(object? _) {
// 				try {
// 					LogScheduledRefreshStarting(Logger, DateTime.UtcNow);
// 					Refresh();
// 				}
// 				catch (Exception ex) {
// 					LogRefreshTimerError(Logger, ex);
// 				}
// 			}
//
// 			RefreshTimer = NonCapturingTimer.Create(TimerCallback, Options.RefreshInterval);
// 		}
// 	}
//
// 	protected override async Task ResolveAsync(CancellationToken cancellationToken) {
// 		LogStartingDiscovery(Logger);
//
// 		var start = TimeProvider.System.GetTimestamp();
//
// 		try {
// 			// Attempt to discover endpoints using gossip protocol
// 			// Try each endpoint in random order to avoid always hitting the same endpoint first.
// 			// Idea? Possibly start with resolved endpoints instead of seeds.
// 			foreach (var (endpoint, client) in Clients.OrderBy(_ => Random.Shared.Next())) {
// 				LogGossipAttempt(Logger, endpoint.ToString());
//
// 				var result = await DiscoverEndpoints(client, cancellationToken);
//
// 				switch (result.Value) {
// 					case BalancerAddress[] addresses:
// 						LogDiscoverySuccess(Logger, addresses.Length, addresses.Select(x => x.EndPoint.ToString()).ToArray());
// 						UpdateClients(addresses);
// 						PublishSuccess(addresses);
// 						return;
//
// 					case GossipErrors.GossipTimeout:
// 						LogGossipRequestTimeOutFailure(Logger, endpoint.ToString(), TimeProvider.System.GetElapsedTime(start).Humanize());
// 						break;
//
// 					case GossipErrors.NoViableEndpoints:
// 						LogGossipNoViableEndpointsFailure(Logger, endpoint.ToString());
// 						break;
//
// 					case GossipErrors.GossipFailure failure:
// 						LogGossipFailure(Logger, endpoint.ToString(), failure.Exception);
// 						break;
// 				}
// 			}
//
// 			// If we get here, we couldn't get gossip from any endpoint
// 			LogDiscoveryNoViableEndpointsFailure(Logger, TimeProvider.System.GetElapsedTime(start).Humanize());
//
// 			// Reseed clients with the original gossip seeds
// 			ReseedClients();
//
// 			// We've exhausted all clients and we must report the failure so
// 			// that the PollingResolver can handle the failure and retry
// 			PublishFailure("No viable KurrentDB endpoints found.");
// 		}
// 		catch (OperationCanceledException) {
// 			// Don't report cancellation as a failure
// 			LogDiscoveryCancelled(Logger);
// 		}
// 		catch (Exception ex) {
// 			// Report discovery failure
// 			LogDiscoveryFailure(Logger, TimeProvider.System.GetElapsedTime(start).Humanize(), ex);
// 			PublishFailure("Failed to discover KurrentDB endpoints.");
// 		}
// 	}
//
// 	/// <summary>
// 	/// Publishes the resolution results to the listener and invokes the result handler if it is set.
// 	/// This ensures the results are broadcasted for further processing or updates.
// 	/// </summary>
// 	void Publish(ResolverResult result) {
// 		// Call the base class Listener
// 		Listener(result);
//
// 		// Notify our handler if set
// 		ResolverResultHandler?.Invoke(result);
// 	}
//
// 	void PublishFailure(string errorMessage) {
// 		var status = new Status(StatusCode.Unavailable, errorMessage);
// 		var result = ResolverResult.ForFailure(status);
// 		Publish(result);
// 		_refreshCompletionSource?.TrySetResult(result);
// 	}
//
// 	void PublishSuccess(BalancerAddress[] addresses) {
// 		var result = ResolverResult.ForResult(addresses);
// 		Publish(result);
// 		_refreshCompletionSource?.TrySetResult(result);
// 	}
//
// 	async Task<GossipDiscoveryResult> DiscoverEndpoints(IGossipClient client, CancellationToken cancellationToken) {
// 		using var cancellator = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
// 		cancellator.CancelAfter(Options.GossipTimeout);
// 		var timeoutToken = cancellator.Token;
//
// 		try {
// 			var nodes = await client
// 				.GetClusterTopology(timeoutToken)
// 				.ConfigureAwait(false);
//
// 			return nodes.Length == 0
// 				? GossipErrors.NoViableEndpoints.Value
// 				: nodes
// 					.Select(CreateBalancerAddress)
// 					.ToArray();
// 		}
// 		catch (OperationCanceledException) when (timeoutToken.IsCancellationRequested) {
// 			return GossipErrors.GossipTimeout.Value;
// 		}
// 		catch (Exception ex) {
// 			return new GossipErrors.GossipFailure(ex);
// 		}
//
// 		static BalancerAddress CreateBalancerAddress(ClusterNode node) {
// 			var address = new BalancerAddress(node.Endpoint.Host, node.Endpoint.Port);
// 			// pass attributes from the node to the balancer address
// 			// useful for custom load balancing logic
// 			foreach (var attribute in node.Attributes)
// 				address.Attributes.WithValue(attribute.Key, attribute.Value);
// 				// address.Attributes.Set(new BalancerAttributesKey<object?>(attribute.Key), attribute.Value);
//
// 			return address;
// 		}
// 	}
//
// 	/// <summary>
// 	/// Updates the collection of known clients based on the given cluster nodes.
// 	/// Ensures the clients list reflects the current cluster topology by adding new clients,
// 	/// removing obsolete ones, and disposing of their resources when necessary.
// 	/// </summary>
// 	void UpdateClients(BalancerAddress[] addresses) {
// 		// Add or update clients for all nodes in the cluster
// 		foreach (var address in addresses)
// 			if (!Clients.ContainsKey(address.EndPoint))
// 				Clients.TryAdd(address.EndPoint, CreateClient(address.EndPoint));
//
// 		// Remove clients for endpoints no longer in the cluster
// 		var currentEndpoints = addresses.Select(m => m.EndPoint).ToHashSet();
// 		foreach (var endpoint in Clients.Keys.Where(ep => !currentEndpoints.Contains(ep)).ToList())
// 			if (Clients.TryRemove(endpoint, out var client))
// 				client.Dispose();
//
// 		LogClientsUpdated(Logger, Clients.Count);
// 	}
//
// 	/// <summary>
// 	/// Reseeds the clients with original gossip seeds endpoints
// 	/// </summary>
// 	void ReseedClients() {
// 		// Dispose existing clients before reseeding
// 		foreach (var client in Clients.Values)
// 			client.Dispose();
//
// 		Clients.Clear();
//
// 		// Recreate clients for each gossip seed endpoint
// 		foreach (var endpoint in Options.GossipSeeds)
// 			Clients.TryAdd(endpoint, CreateClient(endpoint));
//
// 		LogChannelsReseeded(Logger, Clients.Count);
// 	}
//
// 	protected override void Dispose(bool disposing) {
// 		base.Dispose(disposing);
//
// 		if (disposing) {
// 			// Dispose the timer
// 			RefreshTimer?.Dispose();
//
// 			// Dispose all clients
// 			foreach (var client in Clients.Values)
// 				client.Dispose();
//
// 			Clients.Clear();
// 		}
// 	}
//
// 	#region Logging
//
// 	[LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Starting endpoint discovery...")]
// 	private static partial void LogStartingDiscovery(ILogger logger);
//
// 	[LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Discovery successful with {EndpointCount} endpoints found: {Endpoints}")]
// 	private static partial void LogDiscoverySuccess(ILogger logger, int endpointCount, string[] endpoints);
//
// 	[LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Gossip request to {Endpoint} timed out after {Timeout}")]
// 	private static partial void LogGossipRequestTimeOutFailure(ILogger logger, string endpoint, TimeSpan timeout);
//
// 	[LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Gossip from {Endpoint} returned empty topology - No viable endpoints found. Will try next seed.")]
// 	private static partial void LogGossipNoViableEndpointsFailure(ILogger logger, string endpoint);
//
// 	[LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Failed to get gossip from {Endpoint}.")]
// 	private static partial void LogGossipFailure(ILogger logger, string endpoint, Exception exception);
//
// 	[LoggerMessage(EventId = 6, Level = LogLevel.Critical, Message = "Discovery failed with no viable endpoints found after exhausting all {ClientCount} endpoints in {ElapsedTime}.")]
// 	private static partial void LogDiscoveryNoViableEndpointsFailure(ILogger logger, int clientCount, string elapsedTime);
//
// 	[LoggerMessage(EventId = 7, Level = LogLevel.Critical, Message = "Discovery failed after {ElapsedTime} - {ErrorMessage}")]
// 	private static partial void LogDiscoveryFailure(ILogger logger, string elapsedTime, string errorMessage, Exception exception);
//
// 	[LoggerMessage(EventId = 8, Level = LogLevel.Trace, Message = "Reseeded clients with {ClientCount} gossip seeds")]
// 	private static partial void LogChannelsReseeded(ILogger logger, int clientCount);
//
// 	[LoggerMessage(EventId = 9, Level = LogLevel.Trace, Message = "Updated clients, now tracking {ClientCount} endpoints")]
// 	private static partial void LogClientsUpdated(ILogger logger, int clientCount);
//
// 	[LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Discovery operation cancelled")]
// 	private static partial void LogDiscoveryCancelled(ILogger logger);
//
// 	[LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Started refresh timer with interval {RefreshInterval}")]
// 	private static partial void LogRefreshTimerStarted(ILogger logger, TimeSpan refreshInterval);
//
// 	[LoggerMessage(EventId = 12, Level = LogLevel.Debug, Message = "Scheduled refresh starting at {Time}")]
// 	private static partial void LogScheduledRefreshStarting(ILogger logger, DateTime time);
//
// 	[LoggerMessage(EventId = 13, Level = LogLevel.Error, Message = "Error in refresh timer")]
// 	private static partial void LogRefreshTimerError(ILogger logger, Exception ex);
//
// 	[LoggerMessage(EventId = 14, Level = LogLevel.Debug, Message = "Beginning endpoint resolution with {ClientCount} available gossip clients")]
// 	private static partial void LogResolutionStarting(ILogger logger, int clientCount);
//
// 	[LoggerMessage(EventId = 15, Level = LogLevel.Trace, Message = "Attempting gossip request to endpoint {Endpoint}")]
// 	private static partial void LogGossipAttempt(ILogger logger, string endpoint);
//
// 	#endregion
// }


// public sealed partial class GossipResolverRemoveSelector : PollingResolver {
// 	public GossipResolver(
// 		GossipResolverOptions options,
// 		IGossipClientFactory clientFactory,
// 		IClusterNodeSelector nodeSelector,
// 		ILoggerFactory loggerFactory
// 	) : this(
// 		options,
// 		clientFactory,
// 		nodeSelector,
// 		new ExponentialBackoffPolicyFactory(
// 			options.InitialReconnectBackoff,
// 			options.MaxReconnectBackoff
// 		),
// 		loggerFactory
// 	) { }
//
// 	public GossipResolver(
// 		GossipResolverOptions options,
// 		IGossipClientFactory clientFactory,
// 		IClusterNodeSelector nodeSelector,
// 		IBackoffPolicyFactory backoffPolicyFactory,
// 		ILoggerFactory loggerFactory
// 	) : base(loggerFactory, backoffPolicyFactory) {
// 		Options      = options;
// 		NodeSelector = nodeSelector;
// 		Logger       = loggerFactory.CreateLogger<GossipResolver>();
// 		CreateClient = clientFactory.Create;
// 		Clients      = [];
// 	}
//
// 	GossipResolverOptions  Options              { get; }
// 	IClusterNodeSelector   NodeSelector         { get; }
// 	ILogger                Logger               { get; }
//
// 	Func<DnsEndPoint, IGossipClient>                 CreateClient { get; }
// 	ConcurrentDictionary<DnsEndPoint, IGossipClient> Clients      { get; }
//
// 	Timer? RefreshTimer { get; set; }
//
// 	Action<ResolverResult>? ResolverResultHandler { get; set; }
//
// 	public void OnResolverResult(Action<ResolverResult> handler) => ResolverResultHandler = handler;
//
// 	/// <summary>
// 	/// Publishes the resolution results to the listener and invokes the result handler if it is set.
// 	/// This ensures the results are broadcasted for further processing or updates.
// 	/// </summary>
// 	void Publish(ResolverResult result) {
// 		// Call the base class Listener
// 		Listener(result);
//
// 		// Notify our handler if set
// 		ResolverResultHandler?.Invoke(result);
// 	}
//
// 	protected override void OnStarted() {
// 		base.OnStarted();
//
// 		// Initialize clients with gossip seeds
// 		ReseedClients();
//
// 		// Initialize the refresh timer
// 		if (Options.RefreshInterval != Timeout.InfiniteTimeSpan) {
// 			LogRefreshTimerStarted(Logger, Options.RefreshInterval);
//
// 			void TimerCallback(object? _) {
// 				try {
// 					LogScheduledRefreshStarting(Logger, DateTime.UtcNow);
// 					Refresh();
// 				}
// 				catch (Exception ex) {
// 					LogRefreshTimerError(Logger, ex);
// 				}
// 			}
//
// 			RefreshTimer = NonCapturingTimer.Create(TimerCallback, Options.RefreshInterval);
// 		}
// 	}
//
// 	protected override async Task ResolveAsync(CancellationToken cancellationToken) {
// 		try {
// 			// Attempt to discover endpoints using gossip protocol
// 			// Try each endpoint in random order to avoid always hitting the same endpoint first.
// 			// Idea? Possibly start with resolved endpoints instead of seeds.
// 			foreach (var (endpoint, client) in Clients.OrderBy(_ => Random.Shared.Next())) {
// 				// Attempt to get cluster topology from this endpoint
// 				var addresses = await DiscoverEndpoints(client, endpoint, cancellationToken);
//
// 				if (addresses.Length == 0) {
// 					LogGossipFailure(Logger, endpoint.ToString(), new Exception("No nodes returned from gossip."));
// 					continue; // try next endpoint
// 				}
//
// 				Publish(ResolverResult.ForResult(addresses));
//
// 			}
//
// 			// If we get here, we couldn't get gossip from any endpoint
// 			LogDiscoveryFailure(Logger, new Exception("No viable endpoints found."));
//
// 			// Reseed clients with the original gossip seeds
// 			ReseedClients();
//
// 			// We've exhausted all clients and we must report the failure so
// 			// that the PollingResolver can handle the failure and retry
// 			var status = new Status(
// 				StatusCode.Unavailable,
// 				"No viable endpoints found."
// 			);
//
// 			Publish(ResolverResult.ForFailure(status));
// 		}
// 		// catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
// 		catch (OperationCanceledException) {
// 			// Don't report cancellation as a failure
// 			LogDiscoveryCancelled(Logger);
// 		}
// 		catch (Exception ex) {
// 			// Report discovery failure
// 			LogDiscoveryFailure(Logger, ex);
//
// 			var status = new Status(
// 				StatusCode.Unavailable,
// 				"Failed to discover KurrentDB endpoints.",
// 				ex
// 			);
//
// 			Publish(ResolverResult.ForFailure(status));
// 		}
//
// 		return;
// 	}
//
// 	async Task<BalancerAddress[]> DiscoverEndpoints(IGossipClient client, DnsEndPoint endpoint, CancellationToken cancellationToken) {
// 		// ensure that each call timeouts after the configured gossip timeout
// 		using var cancellator = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
// 		cancellator.CancelAfter(Options.GossipTimeout);
// 		var timeoutToken = cancellator.Token;
//
// 		try {
// 			var nodes = await client
// 				.GetClusterTopology(timeoutToken)
// 				.ConfigureAwait(false);
//
// 			if (nodes.Length == 0) {
// 				return [];
// 			}
//
// 			LogDiscoverySuccess(Logger, nodes.Select(x => x.Endpoint.ToString()).ToArray());
//
// 			// Update known endpoints with the latest cluster info
// 			UpdateClients(nodes);
//
// 			// Select nodes based on selector criteria
// 			var addresses = NodeSelector
// 				.Select(nodes)
// 				.Select(CreateBalancerAddress)
// 				.ToArray();
//
// 			LogNodeSelection(Logger, nodes.Length, string.Join(", ", nodes.Select(n => n.Endpoint.ToString())));
//
// 			return addresses;
// 		}
// 		catch (OperationCanceledException) when (timeoutToken.IsCancellationRequested) {
// 			LogGossipFailure(Logger, endpoint.ToString(), new TimeoutException("Gossip request timed out."));
// 		}
// 		catch (Exception ex) {
// 			// try next endpoint if this one fails
// 			// don't report this as a failure yet
// 			LogGossipFailure(Logger, endpoint.ToString(), ex);
// 		}
//
// 		return [];
//
// 		static BalancerAddress CreateBalancerAddress(ClusterNode node) {
// 			var address = new BalancerAddress(node.Endpoint.Host, node.Endpoint.Port);
// 			// pass attributes from the node to the balancer address
// 			// useful for custom load balancing logic
// 			foreach (var attribute in node.Attributes)
// 				address.Attributes.Set(new BalancerAttributesKey<object?>(attribute.Key), attribute.Value);
//
// 			return address;
// 		}
// 	}
//
// 	/// <summary>
// 	/// Updates the collection of known clients based on the given cluster nodes.
// 	/// Ensures the clients list reflects the current cluster topology by adding new clients,
// 	/// removing obsolete ones, and disposing of their resources when necessary.
// 	/// </summary>
// 	void UpdateClients(ClusterNode[] nodes) {
// 		// Add or update clients for all nodes in the cluster
// 		foreach (var node in nodes)
// 			if (!Clients.ContainsKey(node.Endpoint))
// 				Clients.TryAdd(node.Endpoint, CreateClient(node.Endpoint));
//
// 		// Remove clients for endpoints no longer in the cluster
// 		var currentEndpoints = nodes.Select(m => m.Endpoint).ToHashSet();
// 		foreach (var endpoint in Clients.Keys.Where(ep => !currentEndpoints.Contains(ep)).ToList())
// 			if (Clients.TryRemove(endpoint, out var client))
// 				client.Dispose();
//
// 		LogClientsUpdated(Logger, Clients.Count);
// 	}
//
// 	/// <summary>
// 	/// Reseeds the clients with original gossip seeds endpoints
// 	/// </summary>
// 	void ReseedClients() {
// 		// Dispose existing clients before reseeding
// 		foreach (var client in Clients.Values)
// 			client.Dispose();
//
// 		Clients.Clear();
//
// 		// Recreate clients for each gossip seed endpoint
// 		foreach (var endpoint in Options.GossipSeeds)
// 			Clients.TryAdd(endpoint, CreateClient(endpoint));
//
// 		LogChannelsReseeded(Logger, Clients.Count);
// 	}
//
// 	protected override void Dispose(bool disposing) {
// 		base.Dispose(disposing);
//
// 		if (disposing) {
// 			// Dispose the timer
// 			RefreshTimer?.Dispose();
//
// 			// Dispose all clients
// 			foreach (var client in Clients.Values)
// 				client.Dispose();
//
// 			Clients.Clear();
// 		}
// 	}
//
// 	#region Logging
//
// 	[LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Starting node discovery attempt {AttemptNumber}/{MaxAttempts}")]
// 	private static partial void LogStartingDiscoveryAttempt(ILogger logger, int attemptNumber, int maxAttempts);
//
// 	// [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Successfully discovered endpoint: {SelectedEndpoint}")]
// 	// private static partial void LogDiscoverySuccess(ILogger logger, string selectedEndpoint);
//
// 	[LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Successfully discovered endpoints {Endpoints}")]
// 	private static partial void LogDiscoverySuccess(ILogger logger, string[] endpoints);
//
// 	[LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Failed to get gossip from {Endpoint}.")]
// 	private static partial void LogGossipFailure(ILogger logger, string endpoint, Exception exception);
//
// 	[LoggerMessage(EventId = 4, Level = LogLevel.Critical, Message = "Failed to discover endpoints")]
// 	private static partial void LogDiscoveryFailure(ILogger logger, Exception? exception = null);
//
// 	[LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Node selection found {MemberCount} members, selected: {SelectedEndpoints}")]
// 	private static partial void LogNodeSelection(ILogger logger, int memberCount, string selectedEndpoints);
//
// 	[LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Reseeded clients with {ChannelCount} gossip seeds")]
// 	private static partial void LogChannelsReseeded(ILogger logger, int channelCount);
//
// 	[LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "Updated clients, now tracking {ChannelCount} endpoints")]
// 	private static partial void LogClientsUpdated(ILogger logger, int channelCount);
//
// 	[LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Discovery operation cancelled")]
// 	private static partial void LogDiscoveryCancelled(ILogger logger);
//
// 	[LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Started refresh timer with interval {RefreshInterval}")]
// 	private static partial void LogRefreshTimerStarted(ILogger logger, TimeSpan refreshInterval);
//
// 	[LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Scheduled refresh starting at {Time}")]
// 	private static partial void LogScheduledRefreshStarting(ILogger logger, DateTime time);
//
// 	[LoggerMessage(EventId = 11, Level = LogLevel.Error, Message = "Error in refresh timer")]
// 	private static partial void LogRefreshTimerError(ILogger logger, Exception ex);
//
// 	#endregion
// }


//
// public sealed partial class GossipResolverWithOldRetries : PollingResolver {
//
// 	public GossipResolver(
// 		GossipResolverOptions options,
// 		IGossipClientFactory clientFactory,
// 		IClusterNodeSelector nodeSelector,
// 		ILoggerFactory loggerFactory,
// 		IBackoffPolicyFactory? backoffPolicyFactory
// 	) : base(loggerFactory) {
// 		Options              = options;
// 		NodeSelector         = nodeSelector;
// 		BackoffPolicyFactory = backoffPolicyFactory;
// 		Logger               = loggerFactory.CreateLogger<GossipResolver>();
// 		CreateClient         = clientFactory.Create;
// 		Clients              = [];
// 	}
//
// 	GossipResolverOptions  Options              { get; }
// 	IClusterNodeSelector   NodeSelector         { get; }
// 	IBackoffPolicyFactory? BackoffPolicyFactory { get; }
// 	ILogger                Logger               { get; }
//
// 	Func<DnsEndPoint, IGossipClient>                 CreateClient { get; }
// 	ConcurrentDictionary<DnsEndPoint, IGossipClient> Clients      { get; }
//
// 	Timer? RefreshTimer { get; set; }
//
// 	protected override void OnStarted() {
// 		base.OnStarted();
//
// 		// Initialize clients with gossip seeds
// 		ReseedClients();
//
// 		// Initialize the refresh timer
// 		if (Options.RefreshInterval != Timeout.InfiniteTimeSpan) {
// 			LogRefreshTimerStarted(Logger, Options.RefreshInterval);
//
// 			void TimerCallback(object? _) {
// 				try {
// 					LogScheduledRefreshStarting(Logger, DateTime.UtcNow);
// 					Refresh();
// 				}
// 				catch (Exception ex) {
// 					LogRefreshTimerError(Logger, ex);
// 				}
// 			}
//
// 			RefreshTimer = NonCapturingTimer.Create(TimerCallback, Options.RefreshInterval);
// 		}
// 	}
//
// 	protected override async Task ResolveAsync(CancellationToken cancellationToken) {
// 		try {
// 			// Attempt to discover endpoints using gossip protocol
// 			var nodes = await DiscoverEndpointsAsync(cancellationToken).ConfigureAwait(false);
//
// 			// Select nodes based on selector criteria
// 			var addresses = NodeSelector
// 				.Select(nodes)
// 				.Select(node => {
// 					LogDiscoverySuccess(Logger, node.Endpoint.ToString());
//
// 					var address = new BalancerAddress(node.Endpoint.Host, node.Endpoint.Port);
//
// 					// pass attributes from the node to the balancer address
// 					// useful for custom load balancing logic
// 					foreach (var attribute in node.Attributes)
// 						address.Attributes.Set(new BalancerAttributesKey<object?>(attribute.Key), attribute.Value);
//
// 					return address;
// 				})
// 				.ToArray();
//
// 			Listener(ResolverResult.ForResult(addresses));
// 		}
// 		catch (OperationCanceledException) {
// 			// Don't report cancellation as a failure
// 			LogDiscoveryCancelled(Logger);
// 		}
// 		catch (Exception ex) {
// 			// Report discovery failure
// 			LogDiscoveryFailure(Logger, Options.MaxDiscoverAttempts, ex);
//
// 			var status = new Status(
// 				StatusCode.Unavailable,
// 				$"Failed to discover KurrentDB endpoint after {Options.MaxDiscoverAttempts} attempts",
// 				ex
// 			);
//
// 			Listener(ResolverResult.ForFailure(status));
// 		}
// 	}
//
// 	/// <summary>
// 	/// Discovers endpoints by querying gossip seeds.
// 	/// </summary>
// 	async Task<ClusterNode[]> DiscoverEndpointsAsync(CancellationToken cancellationToken) {
// 		// Try each endpoint in random order to avoid always hitting the same endpoint first.
// 		// Possibly start with resolved endpoints instead of seeds.
// 		foreach (var (endpoint, client) in Clients.OrderBy(_ => Random.Shared.Next()))
// 			try {
// 				// Attempt to get cluster topology from this endpoint
// 				using var cancellator = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
// 				cancellator.CancelAfter(Options.GossipTimeout);
// 				var timeoutToken = cancellator.Token;
//
// 				var nodes = await client
// 					.GetClusterTopology(timeoutToken)
// 					.ConfigureAwait(false);
//
// 				// Update known endpoints with the latest cluster info
// 				UpdateClients(nodes);
//
// 				LogNodeSelection(Logger, nodes.Length, string.Join(", ", nodes.Select(n => n.Endpoint.ToString())));
//
// 				return nodes;
// 			}
// 			catch (Exception ex) {
// 				LogGossipFailure(Logger, endpoint.ToString(), -1, ex);
// 			}
//
// 		// If we get here, we couldn't get gossip from any endpoint
// 		// Reseed clients with the original gossip seeds
// 		ReseedClients();
//
// 		// If we get here, we've exhausted all clients and we must throw
// 		// so that the PollingResolver can handle the failure and retry
// 		throw new Exception("No viable endpoints found.");
// 	}
//
// 	/// <summary>
// 	/// Discovers endpoints by querying gossip seeds.
// 	/// </summary>
// 	async Task<ClusterNode[]> DiscoverEndpointsAsyncWithoutBackOffPolicy(CancellationToken cancellationToken) {
// 		for (var attempt = 1; attempt <= Options.MaxDiscoverAttempts; attempt++) {
// 			LogStartingDiscoveryAttempt(Logger, attempt, Options.MaxDiscoverAttempts);
//
// 			// Try each endpoint in random order to avoid always hitting the same endpoint first.
// 			// Possibly start with resolved endpoints instead of seeds.
// 			foreach (var (endpoint, client) in Clients.OrderBy(_ => Random.Shared.Next()))
// 				try {
// 					// Attempt to get cluster topology from this endpoint
// 					using var cancellator = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
// 					cancellator.CancelAfter(Options.GossipTimeout);
// 					var timeoutToken = cancellator.Token;
//
// 					var nodes = await client
// 						.GetClusterTopology(timeoutToken)
// 						.ConfigureAwait(false);
//
// 					// Update known endpoints with the latest cluster info
// 					UpdateClients(nodes);
//
// 					LogNodeSelection(Logger, nodes.Length, string.Join(", ", nodes.Select(n => n.Endpoint.ToString())));
//
// 					return nodes;
// 				}
// 				catch (Exception ex) {
// 					var remainingAttempts = Options.MaxDiscoverAttempts - attempt;
// 					LogGossipFailure(Logger, endpoint.ToString(), remainingAttempts, ex);
// 				}
//
// 			// If we get here, we couldn't get gossip from any endpoint
// 			// Reseed clients with the original gossip seeds
// 			ReseedClients();
//
// 			// Wait before the next attempt if this isn't the last attempt
// 			if (attempt < Options.MaxDiscoverAttempts)
// 				await Task.Delay(Options.DiscoveryInterval, cancellationToken).ConfigureAwait(false);
// 		}
//
// 		// If we get here, we've exhausted all retry attempts
// 		throw new Exception($"Failed to discover an endpoint after {Options.MaxDiscoverAttempts} attempts. No viable endpoints found.");
// 	}
//
// 	/// <summary>
// 	/// Updates the collection of known clients based on the given cluster nodes.
// 	/// Ensures the clients list reflects the current cluster topology by adding new clients,
// 	/// removing obsolete ones, and disposing of their resources when necessary.
// 	/// </summary>
// 	/// <param name="nodes">An array of <see cref="ClusterNode"/> representing the current cluster topology. Clients will be updated to match these nodes.</param>
// 	void UpdateClients(ClusterNode[] nodes) {
// 		// Add or update clients for all nodes in the cluster
// 		foreach (var node in nodes)
// 			if (!Clients.ContainsKey(node.Endpoint))
// 				Clients.TryAdd(node.Endpoint, CreateClient(node.Endpoint));
//
// 		// Remove clients for endpoints no longer in the cluster
// 		var currentEndpoints = nodes.Select(m => m.Endpoint).ToHashSet();
// 		foreach (var endpoint in Clients.Keys.Where(ep => !currentEndpoints.Contains(ep)).ToList())
// 			if (Clients.TryRemove(endpoint, out var client))
// 				client.Dispose();
//
// 		LogClientsUpdated(Logger, Clients.Count);
// 	}
//
// 	/// <summary>
// 	/// Reseeds the clients with original gossip seeds endpoints
// 	/// </summary>
// 	void ReseedClients() {
// 		// Dispose existing clients before reseeding
// 		foreach (var client in Clients.Values)
// 			client.Dispose();
//
// 		Clients.Clear();
//
// 		// Recreate clients for each gossip seed endpoint
// 		foreach (var endpoint in Options.GossipSeeds)
// 			Clients.TryAdd(endpoint, CreateClient(endpoint));
//
// 		LogChannelsReseeded(Logger, Clients.Count);
// 	}
//
// 	protected override void Dispose(bool disposing) {
// 		base.Dispose(disposing);
//
// 		if (disposing) {
// 			// Dispose the timer
// 			RefreshTimer?.Dispose();
//
// 			// Dispose all clients
// 			foreach (var client in Clients.Values)
// 				client.Dispose();
//
// 			Clients.Clear();
// 		}
// 	}
//
// 	#region Logging
//
// 	[LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Starting node discovery attempt {AttemptNumber}/{MaxAttempts}")]
// 	private static partial void LogStartingDiscoveryAttempt(ILogger logger, int attemptNumber, int maxAttempts);
//
// 	[LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Successfully discovered endpoint: {SelectedEndpoint}")]
// 	private static partial void LogDiscoverySuccess(ILogger logger, string selectedEndpoint);
//
// 	[LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Failed to get gossip from {Endpoint}. Attempts remaining: {RemainingAttempts}")]
// 	private static partial void LogGossipFailure(ILogger logger, string endpoint, int remainingAttempts, Exception exception);
//
// 	[LoggerMessage(EventId = 4, Level = LogLevel.Critical, Message = "Failed to discover endpoint in {MaxAttempts} attempts")]
// 	private static partial void LogDiscoveryFailure(ILogger logger, int maxAttempts, Exception exception);
//
// 	[LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Node selection found {MemberCount} members, selected: {SelectedEndpoints}")]
// 	private static partial void LogNodeSelection(ILogger logger, int memberCount, string selectedEndpoints);
//
// 	[LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Reseeded clients with {ChannelCount} gossip seeds")]
// 	private static partial void LogChannelsReseeded(ILogger logger, int channelCount);
//
// 	[LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "Updated clients, now tracking {ChannelCount} endpoints")]
// 	private static partial void LogClientsUpdated(ILogger logger, int channelCount);
//
// 	[LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Discovery operation cancelled")]
// 	private static partial void LogDiscoveryCancelled(ILogger logger);
//
// 	[LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Started refresh timer with interval {RefreshInterval}")]
// 	private static partial void LogRefreshTimerStarted(ILogger logger, TimeSpan refreshInterval);
//
// 	[LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Scheduled refresh starting at {Time}")]
// 	private static partial void LogScheduledRefreshStarting(ILogger logger, DateTime time);
//
// 	[LoggerMessage(EventId = 11, Level = LogLevel.Error, Message = "Error in refresh timer")]
// 	private static partial void LogRefreshTimerError(ILogger logger, Exception ex);
//
// 	#endregion
// }
