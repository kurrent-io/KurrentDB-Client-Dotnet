using System.Collections.Concurrent;
using System.Net;
using EventStore.Client;
using EventStore.Client.Gossip;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using KurrentDB.Client;
using KurrentDB.Client.LoadBalancer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KurrentDb.Client;

class KurrentDbResolver : PollingResolver, IDisposable {
	readonly ConcurrentDictionary<string, Lazy<GrpcChannel>> _channelCache = new();
	readonly ILogger                                         _logger;
	readonly KurrentDBConnectionOptions                      _connectionOptions;
	readonly KurrentDB.Client.LoadBalancer.NodeSelector      _nodeSelector;

	/// <summary>
	/// Creates a new instance of KurrentDbResolver
	/// </summary>
	public KurrentDbResolver(
		KurrentDBConnectionOptions connectionOptions,
		ResolverOptions resolverOptions,
		IBackoffPolicyFactory backoffPolicyFactory
	) : base(resolverOptions.LoggerFactory, backoffPolicyFactory) {
		_connectionOptions = connectionOptions;
		_logger            = resolverOptions.LoggerFactory.CreateLogger<KurrentDbResolver>();
		_nodeSelector      = new(connectionOptions.NodePreference);

		var _originalAddress = resolverOptions.Address;

		var addressParsed = new Uri("temp://" + resolverOptions.Address.AbsolutePath.TrimStart('/'));
		var _dnsAddress = addressParsed.Host;
		var _port       = addressParsed.Port == -1 ? resolverOptions.DefaultPort : addressParsed.Port;


		var coreOptions = resolverOptions.ChannelOptions;

		var gossipChannelOptions = new GrpcChannelOptions {
			Credentials                             = coreOptions.Credentials,
			MaxSendMessageSize                      = coreOptions.MaxSendMessageSize,
			MaxReceiveMessageSize                   = coreOptions.MaxReceiveMessageSize,
			MaxRetryAttempts                        = coreOptions.MaxRetryAttempts,
			MaxRetryBufferSize                      = coreOptions.MaxRetryBufferSize,
			MaxRetryBufferPerCallSize               = coreOptions.MaxRetryBufferPerCallSize,
			CompressionProviders                    = coreOptions.CompressionProviders,
			LoggerFactory                           = coreOptions.LoggerFactory,
			HttpClient                              = coreOptions.HttpClient,
			HttpHandler                             = coreOptions.HttpHandler,
			DisposeHttpClient                       = coreOptions.DisposeHttpClient,
			ThrowOperationCanceledOnCancellation    = coreOptions.ThrowOperationCanceledOnCancellation,
			UnsafeUseInsecureChannelCallCredentials = coreOptions.UnsafeUseInsecureChannelCallCredentials,
			ServiceConfig                           = coreOptions.ServiceConfig,
			DisableResolverServiceConfig            = coreOptions.DisableResolverServiceConfig,
			MaxReconnectBackoff                     = coreOptions.MaxReconnectBackoff,
			InitialReconnectBackoff                 = coreOptions.InitialReconnectBackoff,
			ServiceProvider                         = coreOptions.ServiceProvider,
			HttpVersion                             = coreOptions.HttpVersion,
			HttpVersionPolicy                       = coreOptions.HttpVersionPolicy
		};

		if (gossipChannelOptions.HttpHandler is SocketsHttpHandler httpHandler)
			httpHandler.Properties["__GrpcLoadBalancingDisabled"] = true;
	}

	static readonly BalancerAttributesKey<string> ConnectionManagerHostOverrideKey = new BalancerAttributesKey<string>("HostOverride");

	static async Task<BalancerAddress[]> ResolveHostAddress(string dnsAddress, int port, CancellationToken cancellationToken) {
		var addresses =
#if NET6_0_OR_GREATER
			await Dns.GetHostAddressesAsync(dnsAddress, cancellationToken).ConfigureAwait(false);
#else
            await Dns.GetHostAddressesAsync(_dnsAddress).ConfigureAwait(false);
#endif
		var hostOverride = $"{dnsAddress}:{port}";
		var endpoints = addresses.Select(a => {
			var address = new BalancerAddress(a.ToString(), port);
			address.Attributes.Set(ConnectionManagerHostOverrideKey, hostOverride);
			return address;
		}).ToArray();

		return endpoints;
	}

	/// <summary>
	/// Disposes resources
	/// </summary>
	public new void Dispose() {
		// Clean up the cached channels
		foreach (var lazyChannel in _channelCache.Values)
			if (lazyChannel.IsValueCreated)
				lazyChannel.Value.Dispose();

		_channelCache.Clear();

		base.Dispose();
	}

	protected override async Task ResolveAsync(CancellationToken cancellationToken) {
		try {
			var result = _connectionOptions.IsDiscovery
				? await DiscoverServerEndpoints(cancellationToken)
				: GetServerEndpoints();

			Listener(result);
		}
		catch (Exception ex) {
			_logger.LogError(ex, "Error resolving KurrentDB cluster endpoints");

			// Return a failure result with the exception details
			var status = new Status(
				StatusCode.Unavailable,
				$"Error resolving KurrentDB cluster endpoints: {ex.Message}",
				ex
			);

			Listener(ResolverResult.ForFailure(status));
		}

		// For direct connection, just use the servers from the connection string
		ResolverResult GetServerEndpoints() {
			var server = _connectionOptions.Servers.Single();
			var parts  = server.Split(':');
			var host   = parts[0];
			var port   = int.Parse(parts.Length > 1 ? parts[1] : "2113"); // Default to port 2113 if not specified

			return ResolverResult.ForResult([new BalancerAddress(host, port)]);
		}
	}

	async Task<ResolverResult> DiscoverServerEndpoints(CancellationToken cancellationToken) {
		// For discovery mode, use the gossip protocol to find all nodes
		Exception? lastException = null;

		// Try each seed server until we get a successful response
		foreach (var server in _connectionOptions.Servers) {
			_logger.LogDebug("Calling gossip service on {Server}", server);

			var channel = GetOrCreateChannel(server);

			try {
				var nodes = await new Gossip.GossipClient(channel)
					.GetClusterInfo(new TimeSpan(_connectionOptions.GossipTimeout), cancellationToken)
					.ConfigureAwait(false);

				_logger.LogInformation("Received gossip response with {Count} members", nodes.Length);

				var endpoints = _nodeSelector
					.GetConnectableNodes(nodes)
					.Select(node => {
						var endpoint = new BalancerAddress(node.EndPoint);
						endpoint.Attributes.Set(new("NodeId"), node.NodeId);
						endpoint.Attributes.Set(new("NodeState"), node.State);
						return endpoint;
					})
					.ToList();

				if (endpoints.Count == 0)
					_logger.LogWarning("No connectable nodes found in gossip response");
				else
					return ResolverResult.ForResult(endpoints);
			}
			catch (Exception ex) {
				_logger.LogWarning(ex, "Failed to get gossip from {Server}", server);
				lastException = ex;
			}
		}

		// if (endpoints.Count == 0) {
		// 	// Return a failure result when no endpoints are found
		// 	var status = new Status(StatusCode.Unavailable, "No endpoints found for KurrentDB cluster");
		// 	Listener(ResolverResult.ForFailure(status));
		// 	return;
		// }

		return ResolverResult.ForFailure(new Status(StatusCode.Unknown, "Failed to get cluster information from any seed server", lastException));

		GrpcChannel GetOrCreateChannel(string server) {
			return _channelCache.GetOrAdd(
				server,
				s => new Lazy<GrpcChannel>(() => {
						_logger.LogInformation("Creating new gRPC channel for seed server {Server}", s);

						// Create a direct channel to the server without load balancing
						var protocol = _connectionOptions.UseTls ? "https" : "http";
						var address  = new Uri($"{protocol}://{s}");

						// Create simplified channel options with no load balancing
						var bootstrapChannelOptions = CreateBootstrapChannelOptions(_connectionOptions);

						return GrpcChannel.ForAddress(address, bootstrapChannelOptions);
					}
				)
			).Value;

			GrpcChannelOptions CreateBootstrapChannelOptions(KurrentDBConnectionOptions options) {
				// Configure credentials based on TLS setting
				var credentials = options.UseTls ? ChannelCredentials.SecureSsl : ChannelCredentials.Insecure;

				// Add call credentials if we have username/password
				if (!string.IsNullOrEmpty(options.Username) && !string.IsNullOrEmpty(options.Password)) {
					var callCredentials = CallCredentials.FromInterceptor((context, metadata) => {
							var auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{options.Username}:{options.Password}"));
							metadata.Add("Authorization", $"Basic {auth}");
							return Task.CompletedTask;
						}
					);

					credentials = ChannelCredentials.Create(credentials, callCredentials);
				}

				// Create HTTP handler with appropriate settings
				var httpHandler = new SocketsHttpHandler {
					PooledConnectionIdleTimeout    = TimeSpan.FromMinutes(5),
					KeepAlivePingDelay             = TimeSpan.FromSeconds(options.KeepAliveInterval),
					KeepAlivePingTimeout           = TimeSpan.FromSeconds(options.KeepAliveTimeout),
					EnableMultipleHttp2Connections = false // Only need a single connection for discovery
				};

				// Configure TLS verification
				if (options.UseTls && !options.VerifyCert)
					httpHandler.SslOptions.RemoteCertificateValidationCallback =
						(sender, certificate, chain, errors) => true;

				// Important! Mark this property to disable load balancing
				httpHandler.Properties["__GrpcLoadBalancingDisabled"] = true;

				// Return configured channel options without any load balancing
				return new GrpcChannelOptions {
					Credentials   = credentials,
					HttpHandler   = httpHandler,
					LoggerFactory = NullLoggerFactory.Instance
				};
			}
		}
	}
}

class KurrentDbResolverFactory(KurrentDBConnectionOptions connectionOptions) : ResolverFactory {
	public override string Name => "kurrentdb";

	public override Resolver Create(ResolverOptions options) {

		// new StaticResolverFactory(uri => [
		// 		new BalancerAddress(options.Address.Host, options.Address.Port == -1 ? options.DefaultPort : options.Address.Port),
		// 		// or this?
		// 		//new BalancerAddress(uri.Host, uri.Port)
		// 	]
		// );
		//
		 // new DnsResolverFactory()


		// must be done here so we can use the pooling resolver default behaviour
		var backoffPolicyFactory = options.ChannelOptions.ServiceProvider?.GetService<IBackoffPolicyFactory>()
		                        ?? new ExponentialBackoffPolicyFactory(
			                           options.ChannelOptions.InitialReconnectBackoff,
			                           options.ChannelOptions.MaxReconnectBackoff
		                           );

		return new KurrentDbResolver(connectionOptions, options, backoffPolicyFactory);
	}
}

static class GossipClientExtensions {
	public static async ValueTask<ClusterNode[]> GetClusterInfo(
		this Gossip.GossipClient client,
		TimeSpan timeout,
		CancellationToken cancellationToken = default
	) {
		var callOptions = new CallOptions(
			deadline: DateTime.UtcNow.Add(timeout),
			cancellationToken: cancellationToken
		);

		var result = await client.ReadAsync(new Empty(), callOptions);
		var nodes  = result.Members.Select(MapNodeInfo).ToArray();
		return nodes;

		ClusterNode MapNodeInfo(MemberInfo source) => new(
			NodeId: Uuid.FromDto(source.InstanceId).ToGuid(),
			EndPoint: new DnsEndPoint(source.HttpEndPoint.Address, (int)source.HttpEndPoint.Port),
			State: (NodeState)source.State,
			IsAlive: source.IsAlive
		);
	}
}
