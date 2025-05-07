using System.Globalization;
using System.Net;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KurrentDB.Client.LoadBalancer;

public class KurrentDBNodeResolver(
	KurrentDBNodeResolverOptions options,
	GetKurrentDBClusterTopology getClusterTopology,
	NodePreference nodePreference,
	ILoggerFactory loggerFactory,
	IBackoffPolicyFactory backoffPolicyFactory
)
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
public record KurrentDBNodeResolverOptions(
	Uri Address,
	int DefaultPort,
	GrpcChannelOptions ChannelOptions,
	NodePreference NodePreference,
	int MaxDiscoverAttempts,
	TimeSpan DiscoveryInterval,
	TimeSpan GossipTimeout
);

public class KurrentDBNodeResolverFactory(NodePreference nodePreference) : ResolverFactory {
	readonly        NodePreference _nodePreference = nodePreference;
	public override string         Name => "kurrentdb+discover";

	public override Resolver Create(ResolverOptions options) {
		var endpoints = GetEndPoints(options.Address, options.DefaultPort);

		var backoffPolicyFactory = new ExponentialBackoffPolicyFactory(
			options.ChannelOptions.InitialReconnectBackoff,
			options.ChannelOptions.MaxReconnectBackoff
		);

		// maxDiscoverAttempts  Number	10	Number of attempts to discover the cluster.
		// discoveryInterval	Number	100	Cluster discovery polling interval in milliseconds.
		// gossipTimeout	    Number	5	Gossip timeout in seconds, when the gossip call times out, it will be retried.

		// return new KurrentDBNodeResolver(null!, nodePreference, options.LoggerFactory, backoffPolicyFactory);

		return null!;
	}

	#region another attempt

	public static List<EndPoint> GetEndPoints(Uri uri, int defaultPort) {
		return ParseEndpointsFromHostString(uri.Host, uri.Port);
	}

	static List<EndPoint> ParseEndpointsFromHostString(string hostString, int uriPortHint) {
		var endpoints = new List<EndPoint>();

		if (string.IsNullOrWhiteSpace(hostString)) throw new FormatException("Connection string is missing host information.");

		// Scenario 1: Uri successfully parsed a single standard host and port.
		// uriPortHint > 0 is a strong indicator. hostString shouldn't contain ',' in this case.
		// We trust Uri's parsing here.
		if (uriPortHint > 0 && !hostString.Contains(',')) {
			if (TryParseEndPoint(hostString, uriPortHint, out var endpoint, out var error)) {
				endpoints.Add(endpoint);
				return endpoints;
			}

			throw new FormatException(
				$"Invalid host:port format '{hostString}:{uriPortHint}'. Error: {error?.Message ?? "Unknown error."}"
			);
		}

		// else: Fall through, maybe it's like "host:invalidPort" which Uri gave port=-1 for.
		// Scenario 2: uriPortHint <= 0 OR hostString contains commas.
		// This means it's either a comma-separated list or a single entry
		// that Uri couldn't parse as host:port (e.g., "hostnameonly", "host:nonnumeric").
		// We need to manually parse segments.
		var hostEntries = hostString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

		if (hostEntries.Length == 0) // Handle case where hostString was just "," or ", , " etc.
			throw new FormatException("Host part of the connection string contains only separators or is empty after splitting.");

		foreach (var entry in hostEntries)
			if (TryParseSegment(entry.Trim(), out var endpoint)) // Use helper for each segment
				endpoints.Add(endpoint);

		// If TryParseSegment returns false, we simply skip that invalid segment.
		// Final check: Did we find *any* valid endpoints?
		if (endpoints.Count == 0)
			throw new FormatException(
				$"No valid host:port endpoints could be parsed from '{hostString}'. Check format (e.g., 'host:port', '[::1]:port', 'host1:port1,host2:port2')."
			);

		return endpoints;
	}

	/// <summary>
	/// Attempts to parse a single host:port segment (like "host:1234" or "[::1]:2113").
	/// </summary>
	static bool TryParseSegment(string segment, out EndPoint endpoint) {
		endpoint = null!;
		if (string.IsNullOrWhiteSpace(segment)) return false;

		string hostPart;
		string portPart;

		var lastColonIndex = segment.LastIndexOf(':');

		// Handle IPv6 address format like [::1]:port or [ipv6addr]:port
		if (segment.StartsWith("[") && segment.Contains("]:") && lastColonIndex > segment.IndexOf(']')) {
			var closingBracketIndex = segment.LastIndexOf(']');
			// Check if colon is immediately after bracket and there's something after colon
			if (lastColonIndex == closingBracketIndex + 1 && lastColonIndex < segment.Length - 1) {
				hostPart = segment.Substring(1, closingBracketIndex - 1); // Extract content inside brackets
				// Check if hostPart is valid IPv6 representation (optional, DnsEndPoint will validate)
				if (!IPAddress.TryParse(hostPart, out _) || !hostPart.Contains(':')) // Basic check for IPv6
					return false;                                                    // Content inside brackets doesn't look like a valid IPv6

				portPart = segment.Substring(lastColonIndex + 1);
			}
			else {
				return false; // Malformed IPv6 or missing port (e.g., "[::1]" or "[::1]:")
			}
		}
		// Handle standard host:port or IPv4:port
		else if (lastColonIndex > 0 && lastColonIndex < segment.Length - 1) // Ensure colon isn't first or last char
		{
			hostPart = segment.Substring(0, lastColonIndex);
			portPart = segment.Substring(lastColonIndex + 1);
		}
		else {
			return false; // No valid colon separator found, or format is incorrect (e.g., "hostnameonly", ":port", "host:")
		}

		// Parse the port
		if (int.TryParse(portPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port) && port > 0 && port <= 65535)
			return TryParseEndPoint(hostPart, port, out endpoint, out _); // Use final helper

		return false;
	}

	static bool TryParseEndPoint(string host, int port, out EndPoint endpoint, out Exception? error) {
		try {
			endpoint = new DnsEndPoint(host, port);
			error    = null;
			return true;
		}
		catch (Exception ex) {
			endpoint = null!;
			error    = ex;
			return false;
		}
	}

	#endregion
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
