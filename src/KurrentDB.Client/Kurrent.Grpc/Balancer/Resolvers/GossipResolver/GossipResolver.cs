using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using Grpc.Core;
using Grpc.Net.Client.Balancer;
using Humanizer;
using Kurrent.Client;
using Kurrent.Client.Grpc;
using Kurrent.Client.Grpc.Balancer.Resolvers;
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

	[StructLayout(LayoutKind.Sequential)]
	public readonly struct GossipFailure(Exception exception) {
		public Exception Exception { get; } = exception;
	}
}

[GenerateOneOf]
partial class GossipDiscoveryResult : OneOfBase<BalancerAddress[], GossipErrors.GossipTimeout, GossipErrors.NoViableEndpoints, GossipErrors.GossipFailure>;

public partial class GossipResolver : PollingResolver {
	static readonly FieldInfo ResolveTaskField =
		typeof(PollingResolver).GetField("_resolveTask", BindingFlags.Instance | BindingFlags.NonPublic)!;

	static readonly FieldInfo ResolveSuccessfulField =
		typeof(PollingResolver).GetField("_resolveSuccessful", BindingFlags.Instance | BindingFlags.NonPublic)!;

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
		Clients      = new ConcurrentDictionary<DnsEndPoint, IGossipClient>(DnsEndPointEqualityComparer.Default);
	}

	GossipResolverOptions Options { get; }
	ILogger               Logger  { get; }

	Func<DnsEndPoint, IGossipClient>                 CreateClient { get; }
	ConcurrentDictionary<DnsEndPoint, IGossipClient> Clients      { get; }

	Timer? RefreshTimer { get; set; }

	Action<ResolverResult>? ResolverResultHandler { get; set; }

	Task ResolveTask       => (Task)ResolveTaskField.GetValue(this)!;
	bool ResolveSuccessful => (bool)ResolveSuccessfulField.GetValue(this)!;

	ResolverResult? LastResult  { get; set; }

	public async Task<ResolverResult?> RefreshAsync(CancellationToken cancellationToken = default) {
		// Check if a refresh is already in progress
		// otherwise, start a new refresh
		if (ResolveTask.IsCompleted)
			Refresh();

		// Wait for the refresh to complete
		await Task.WhenAny(ResolveTask.WaitAsync(cancellationToken));
		// if (cancellationToken.CanBeCanceled)
		// 	await Task.WhenAny(ResolveTask.WaitAsync(cancellationToken));
		// else
		// 	await ResolveTask;

		// If the resolve was not successful, wait for the next attempt
		while (!ResolveSuccessful && !cancellationToken.IsCancellationRequested)
			await Task.WhenAny(Task.Delay(100, cancellationToken));

		// Return the last result, which should be set by the resolver
		// If the resolver hasn't set it, it means the refresh failed or was cancelled
		return LastResult;
	}

	public void OnResolverResult(Action<ResolverResult> handler) => ResolverResultHandler = handler;

	protected override void OnStarted() {
		// Initialize clients with gossip seeds
		foreach (var endpoint in Options.GossipSeeds)
			Clients.TryAdd(endpoint, CreateClient(endpoint));

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

                var result = await DiscoverEndpoints(client, cancellationToken).ConfigureAwait(false);

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
		catch (ObjectDisposedException) {
			throw;
		}
		catch (Exception ex) {
			return new GossipErrors.GossipFailure(ex);
		}
	}

	/// <summary>
	/// Updates the collection of known clients based on the given cluster nodes.
	/// Ensures the clients list reflects the current cluster topology by adding new clients,
	/// removing obsolete ones, and disposing of their resources when necessary.
	/// </summary>
	void UpdateClients(BalancerAddress[] addresses) {
		// Track current valid endpoints
		var currentEndpoints = addresses.Select(a => a.EndPoint).ToHashSet();

		// Add or update clients for all nodes in the cluster
		foreach (var endpoint in currentEndpoints)
			Clients.GetOrAdd(endpoint, CreateClient);

		// Find endpoints to remove (endpoints in Clients but not in currentEndpoints)
		var endpointsToRemove = Clients.Keys.Except(currentEndpoints).ToArray();

		// Remove only the obsolete endpoints, no longer in the cluster
		foreach (var endpoint in endpointsToRemove)
			if (Clients.TryRemove(endpoint, out var client))
				client.Dispose();

		LogClientsUpdated(Logger, Clients.Count);
	}

	/// <summary>
	/// Reseeds the clients with original gossip seeds endpoints
	/// </summary>
	void ReseedClients() {
		// Take a snapshot of clients to dispose, then clear the dictionary
		var clientsToDispose = Clients.Values.ToArray();
		Clients.Clear();

		// Dispose clients after clearing the dictionary to prevent new requests
		// from being sent to clients that are about to be disposed
		foreach (var client in clientsToDispose)
			client.Dispose();

		// Recreate clients for each gossip seed endpoint
		foreach (var endpoint in Options.GossipSeeds)
			Clients.GetOrAdd(endpoint, CreateClient(endpoint));

		LogChannelsReseeded(Logger, Clients.Count);
	}

	/// <summary>
	/// Publishes the resolution results to the listener and invokes the result handler if it is set.
	/// This ensures the results are broadcasted for further processing or updates.
	/// </summary>
	void Publish(ResolverResult result) {
		Listener(LastResult = result);
		ResolverResultHandler?.Invoke(result);
	}

	void PublishFailure(string errorMessage) =>
		Publish(ResolverResult.ForFailure(new Status(StatusCode.Unavailable, errorMessage)));

	void PublishSuccess(BalancerAddress[] addresses) =>
		Publish(ResolverResult.ForResult(addresses));

	protected override void Dispose(bool disposing) {
		if (!disposing) return;

		base.Dispose(disposing);

		// Dispose the timer
		RefreshTimer?.Dispose();

		// Dispose all clients
		var clientsToDispose = Clients.Values.ToArray();
		Clients.Clear();

		foreach (var client in clientsToDispose)
			client.Dispose();
	}

	#region . Logging .

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Starting cluster topology discovery with {NodeCount} available gossip nodes")]
    public static partial void LogStartingDiscovery(ILogger logger, int nodeCount);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Topology discovery successful. Found {NodeCount} cluster nodes: {Nodes}")]
    public static partial void LogDiscoverySuccess(ILogger logger, int nodeCount, string[] nodes);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Gossip request to node {Node} timed out after {ElapsedTime}")]
    public static partial void LogGossipRequestTimeOutFailure(ILogger logger, string node, string elapsedTime);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Gossip from node {Node} returned empty topology. No viable nodes found")]
    public static partial void LogGossipNoViableEndpointsFailure(ILogger logger, string node);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Failed to get gossip from node {Node}")]
    public static partial void LogGossipFailure(ILogger logger, string node, Exception exception);

    [LoggerMessage(EventId = 6, Level = LogLevel.Critical, Message = "Topology discovery failed. No viable nodes found after trying {AttemptCount} nodes. Time elapsed: {ElapsedTime}")]
    public static partial void LogDiscoveryNoViableEndpointsFailure(ILogger logger, int attemptCount, string elapsedTime);

    [LoggerMessage(EventId = 7, Level = LogLevel.Critical, Message = "Topology discovery failed after {ElapsedTime}")]
    public static partial void LogDiscoveryFailure(ILogger logger, string elapsedTime, Exception exception);

    [LoggerMessage(EventId = 8, Level = LogLevel.Trace, Message = "Reset to initial seed nodes. Now using {SeedCount} gossip seed nodes")]
    public static partial void LogChannelsReseeded(ILogger logger, int seedCount);

    [LoggerMessage(EventId = 9, Level = LogLevel.Trace, Message = "Updated node connections. Now tracking {NodeCount} cluster nodes")]
    public static partial void LogClientsUpdated(ILogger logger, int nodeCount);

    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Topology discovery operation cancelled")]
    public static partial void LogDiscoveryCancelled(ILogger logger);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Started topology refresh timer with interval {RefreshInterval}")]
    public static partial void LogRefreshTimerStarted(ILogger logger, TimeSpan refreshInterval);

    [LoggerMessage(EventId = 12, Level = LogLevel.Debug, Message = "Scheduled topology refresh starting at {Time}")]
    public static partial void LogScheduledRefreshStarting(ILogger logger, DateTime time);

    [LoggerMessage(EventId = 13, Level = LogLevel.Error, Message = "Error in topology refresh timer")]
    public static partial void LogRefreshTimerError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 15, Level = LogLevel.Trace, Message = "Requesting cluster topology from node {Node}")]
    public static partial void LogGossipAttempt(ILogger logger, string node);

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
