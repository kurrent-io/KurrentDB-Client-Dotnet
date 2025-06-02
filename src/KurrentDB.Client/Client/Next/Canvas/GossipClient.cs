// using System.Net;
// using EventStore.Client;
// using EventStore.Client.Gossip;
// using Grpc.Net.Client;
//
// namespace KurrentDB.Client.LoadBalancing;
//
// public interface IKurrentDBGossipClient : IDisposable {
// 	ValueTask<ClusterNode[]> GetClusterTopology(CancellationToken cancellationToken);
// }
//
// public interface IGossipClientFactory {
// 	public IKurrentDBGossipClient Create(DnsEndPoint endpoint, KurrentDBClientConnectivitySettings settings, GrpcChannelOptions channelOptions);
// }
//
// public record ClusterNode(Guid NodeId, DnsEndPoint EndPoint, NodeState State, bool IsAlive);
//
// public enum NodeState {
// 	Initializing       = 0,
// 	DiscoverLeader     = 1,
// 	Unknown            = 2,
// 	PreReplica         = 3,
// 	CatchingUp         = 4,
// 	Clone              = 5,
// 	Follower           = 6,
// 	PreLeader          = 7,
// 	Leader             = 8,
// 	Manager            = 9,
// 	ShuttingDown       = 10,
// 	Shutdown           = 11,
// 	ReadOnlyLeaderless = 12,
// 	PreReadOnlyReplica = 13,
// 	ReadOnlyReplica    = 14,
// 	ResigningLeader    = 15
// }
//
// class KurrentDBGossipClient(GrpcChannel channel) : IKurrentDBGossipClient {
// 	static readonly Empty EmptyRequest = new Empty();
//
// 	Gossip.GossipClient ServiceClient { get; } = new(channel);
// 	GrpcChannel         Channel       { get; } = channel;
//
// 	public async ValueTask<ClusterNode[]> GetClusterTopology(CancellationToken cancellationToken) {
// 		var result = await ServiceClient.ReadAsync(EmptyRequest, cancellationToken: cancellationToken);
// 		return result.Members.Select(Map).ToArray();
//
// 		ClusterNode Map(MemberInfo source) => new(
// 			NodeId: Uuid.FromDto(source.InstanceId).ToGuid(),
// 			EndPoint: new DnsEndPoint(source.HttpEndPoint.Address, (int)source.HttpEndPoint.Port),
// 			State: (NodeState)source.State,
// 			IsAlive: source.IsAlive
// 		);
// 	}
//
// 	public void Dispose() => Channel.Dispose();
// }
//
// public class KurrentDBGossipClientFactory : IGossipClientFactory {
// 	public IKurrentDBGossipClient Create(DnsEndPoint endpoint, KurrentDBClientConnectivitySettings settings, GrpcChannelOptions channelOptions) {
// 		var scheme  = settings.Insecure ? Uri.UriSchemeHttp : Uri.UriSchemeHttps;
// 		var address = new Uri($"{scheme}://{endpoint}");
//
// 		// Create HTTP handler with appropriate settings
// 		var httpHandler = new SocketsHttpHandler {
// 			EnableMultipleHttp2Connections = false, // Single connection for gossip
// 			PooledConnectionIdleTimeout    = TimeSpan.FromMinutes(1),
// 			ConnectTimeout                 = TimeSpan.FromSeconds(5)
// 		};
//
// 		// Disable TLS verification if needed
// 		if (settings is { Insecure: false, SslCredentials.VerifyServerCertificate: false })
// 			httpHandler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
//
// 		// Important: Disable load balancing for gossip channels
// 		// This is necessary because gossip relies on a single connection to a specific node
// 		// and does not use the gRPC load balancing features.
// 		// so maybe we can use this directly? without a resolver amd a server config?
// 		// must test this
// 		httpHandler.Properties["__GrpcLoadBalancingDisabled"] = true;
//
// 		var options = new GrpcChannelOptions {
// 			HttpHandler          = httpHandler,
// 			Credentials          = channelOptions.Credentials,
// 			CompressionProviders = channelOptions.CompressionProviders,
// 			LoggerFactory        = channelOptions.LoggerFactory,
// 		};
//
// 		var channel = GrpcChannel.ForAddress(address, options);
//
// 		return new KurrentDBGossipClient(channel);
// 	}
// }
