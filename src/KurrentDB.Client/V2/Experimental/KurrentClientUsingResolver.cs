using EventStore.Client.Operations;
using EventStore.Client.PersistentSubscriptions;
using EventStore.Client.Projections;
using EventStore.Client.Streams;
using EventStore.Client.Users;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using Kurrent.Client.Infra;
using Kurrent.Grpc.Balancer;
using Kurrent.Grpc.Compression;
using KurrentDB.Protocol.Registry.V2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KurrentDB.Client;

/// <summary>
/// Represents gossip protocol settings for cluster discovery
/// </summary>
public record KurrentClientGossipOptions {
	public static readonly KurrentClientGossipOptions Default = new();

	public int      MaxDiscoverAttempts { get; init; } = 10;
	public TimeSpan DiscoveryInterval   { get; init; } = TimeSpan.FromMilliseconds(100);
	public TimeSpan Timeout             { get; init; } = TimeSpan.FromSeconds(5);

	/// <summary>
	/// The <see cref="NodePreference"/> to use when connecting.
	/// </summary>
	public NodePreference NodePreference { get; set; }
}

// /// <summary>
// /// The optional amount of time to wait after which a keepalive ping is sent on the transport.
// /// </summary>
// public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(10);
//
// /// <summary>
// /// The optional amount of time to wait after which a sent keepalive ping is considered timed out.
// /// </summary>
// public TimeSpan KeepAliveTimeout { get; set; } = TimeSpan.FromSeconds(10);



public record KurrentClientOptions {
	/// <summary>
	/// The name of the connection.
	/// </summary>
	public string ConnectionName { get; init; } = $"connection-{Guid.NewGuid()}";

	/// <summary>
	/// An optional list of <see cref="Interceptor"/>s to use.
	/// </summary>
	public IReadOnlyList<Interceptor> Interceptors { get; init; } = [];

	/// <summary>
	/// An optional <see cref="ILoggerFactory"/> to use.
	/// </summary>
	public ILoggerFactory LoggerFactory { get; init; } = NullLoggerFactory.Instance;

	/// <summary>
	/// The optional <see cref="UserCredentials"/> to use if none have been supplied to the operation.
	/// </summary>
	public UserCredentials? CallCredentials { get; set; }

	/// <summary>
	/// The optional <see cref="ChannelCredentials"/> to use when creating the <see cref="ChannelBase"/>.
	/// </summary>
	public ChannelCredentials? ChannelCredentials { get; init; }

	/// <summary>
	/// The <see cref="KurrentDBClientConnectivitySettings"/> to use.
	/// </summary>
	public KurrentDBClientConnectivitySettings ConnectivitySettings { get; init; } =
		KurrentDBClientConnectivitySettings.Default;

	/// <summary>
	/// The default deadline for calls. Will not be applied to reads or subscriptions.
	/// </summary>
	public TimeSpan? DefaultDeadline { get; set; } = TimeSpan.FromSeconds(10);

	public KurrentClientRetryOptions Retry { get; set; } = KurrentClientRetryOptions.NoRetry;

	/// <summary>
	/// Indicates whether the client requires channel-level credentials for secure communication.
	/// </summary>
	public bool RequiresSecureCommunication =>
		ChannelCredentials is not null
	 || CallCredentials is not null
	 || ConnectivitySettings.SslCredentials.Required;
}


public class KurrentClientUsingResolver : IAsyncDisposable {
	public KurrentClientUsingResolver(KurrentDBClientSettings settings) {
		Settings = settings;

		var (channel, invoker) = settings.CreateGrpcChannel(enableLoadBalancing: true);

		Channel = channel;
		Invoker = invoker;

		Streams                 = new(Invoker);
		SchemaRegistry          = new(Invoker);
		Operations              = new(Invoker);
		Projections             = new(Invoker);
		PersistentSubscriptions = new(Invoker);
		Users                   = new(Invoker);
	}

	GrpcChannel             Channel  { get; }
	CallInvoker             Invoker  { get; }
	KurrentDBClientSettings Settings { get; }

	internal Streams.StreamsClient                                 Streams                 { get; }
	internal SchemaRegistryService.SchemaRegistryServiceClient     SchemaRegistry          { get; }
	internal Operations.OperationsClient                           Operations              { get; }
	internal Projections.ProjectionsClient                         Projections             { get; }
	internal PersistentSubscriptions.PersistentSubscriptionsClient PersistentSubscriptions { get; }
	internal Users.UsersClient                                     Users                   { get; }

	public ValueTask DisposeAsync() {
		Channel.Dispose();
		return ValueTask.CompletedTask;
	}
}

/// <summary>
/// Extension methods for KurrentDBClientSettings.
/// </summary>
public static class KurrentDBClientSettingsExtensions {
	public static (GrpcChannel Channel, CallInvoker Invoker) CreateGrpcChannel(this KurrentDBClientSettings settings, bool enableLoadBalancing) {
		// Enable HTTP/2 unencrypted support for insecure connections
		if (settings.ConnectivitySettings.Insecure)
			AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

		var handler = new SocketsHttpHandler {
			// Keep connections in the pool available for reuse
			PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5), // Timespan.FromMinutes(1),

			// Balance between network traffic and connection stability
			KeepAlivePingDelay   = settings.ConnectivitySettings.KeepAliveInterval, // TimeSpan.FromSeconds(20),
			KeepAlivePingTimeout = settings.ConnectivitySettings.KeepAliveTimeout,  // Timeout.InfiniteTimeSpan,

			// Critical for high-throughput scenarios
			EnableMultipleHttp2Connections = true,

			// // Allow incoming compressed messages to be automatically decompressed
			// AutomaticDecompression = DecompressionMethods.All,

			// Avoid delays when creating new connections
			ConnectTimeout = Timeout.InfiniteTimeSpan //TimeSpan.FromSeconds(10)
		};

		// Disable TLS verification if needed
		if (settings.ConnectivitySettings is { Insecure: false, SslCredentials.VerifyServerCertificate: false })
			handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;

		// Maximum message size for receiving messages
		// This is a workaround for the default gRPC limit of 4MB
		// and is set to 17MB to accommodate larger messages.
		// Note: This is a temporary solution and should be revisited in the future.
		const int maxReceiveMessageLength = 17 * 1024 * 1024; // 17MB

		// Configure service config with load balancing and/or retry as needed
		var serviceConfig = BuildServiceConfig(settings.RetrySettings);

		// Set up channel options with only necessary components
		var channelOptions = new GrpcChannelOptions {
			CompressionProviders  = GrpcCompression.DefaultOptimalProviders,
			Credentials           = CreateChannelCredentials(settings),
			LoggerFactory         = settings.LoggerFactory,
			ServiceConfig         = serviceConfig,
			HttpHandler           = handler,
			DisposeHttpClient     = true,
			MaxReceiveMessageSize = maxReceiveMessageLength,
		};

		// Create channel based on whether load balancing is enabled
		GrpcChannel channel;

		if (enableLoadBalancing && settings.ConnectivitySettings.GossipSeeds.Length > 0) {

			var resolverOptions = new GossipResolverOptions {
				GossipSeeds             = settings.ConnectivitySettings.GossipSeeds,
				GossipTimeout           = settings.ConnectivitySettings.GossipTimeout,
				InitialReconnectBackoff = settings.ConnectivitySettings.DiscoveryInterval,
				MaxReconnectBackoff     = settings.ConnectivitySettings.DiscoveryInterval * settings.ConnectivitySettings.MaxDiscoverAttempts,
				// RefreshInterval         = settings.ConnectivitySettings.RefreshInterval,
				// InitialReconnectBackoff = settings.ConnectivitySettings.InitialReconnectBackoff,
				// MaxReconnectBackoff     = settings.ConnectivitySettings.MaxReconnectBackoff,
			};

			var gossipClientFactory   = new KurrentDBGossipClientFactory(settings.ConnectivitySettings.NodePreference, channelOptions);
			var gossipResolverFactory = new GossipResolverFactory("kurrentdb+discover", resolverOptions, gossipClientFactory);

			channelOptions.ServiceProvider = new ResolverServiceProvider(gossipResolverFactory);

			channel = GrpcChannel.ForAddress($"{gossipResolverFactory.Name}://cluster", channelOptions);
		}
		else {
			// Create direct connection
			channel = GrpcChannel.ForAddress(settings.ConnectivitySettings.Address!, channelOptions);
		}

		var invoker = channel.Intercept(settings.Interceptors.ToArray());

		return (channel, invoker);

		static ServiceConfig BuildServiceConfig(KurrentDBClientRetrySettings retrySettings, string loadBalancerName = "pick_first") {
			return retrySettings.IsEnabled switch {
				true => new() {
					LoadBalancingConfigs = { BuildLoadBalancingConfig(loadBalancerName) },
					MethodConfigs        = { BuildRetryMethodConfig(retrySettings) }
				},
				false => new() {
					LoadBalancingConfigs = { BuildLoadBalancingConfig(loadBalancerName)  }
				}
			};
		}
	}

	public static (GrpcChannel Channel, CallInvoker Invoker) CreateGossipChannel(this KurrentDBClientSettings settings) {
		// Enable HTTP/2 unencrypted support for insecure connections
		if (settings.ConnectivitySettings.Insecure)
			AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

		var handler = new SocketsHttpHandler();

		// Disable TLS verification if needed
		if (settings.ConnectivitySettings is { Insecure: false, SslCredentials.VerifyServerCertificate: false })
			handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;

		// Configure service config with retry settings if enabled
		ServiceConfig? serviceConfig = settings.RetrySettings.IsEnabled
			? new() { MethodConfigs = { BuildRetryMethodConfig(settings.RetrySettings) } }
			: null;

		// Set up channel options with only necessary components
		var channelOptions = new GrpcChannelOptions {
			Credentials           = CreateChannelCredentials(settings),
			LoggerFactory         = settings.LoggerFactory,
			ServiceConfig         = serviceConfig,
			HttpHandler           = handler,
			DisposeHttpClient     = true,
		}.WithCompressionProviders();

		var channel = GrpcChannel.ForAddress(settings.ConnectivitySettings.Address!, channelOptions);
		var invoker = channel.Intercept(settings.Interceptors.ToArray());

		return (channel, invoker);
	}

	/// <summary>
	/// Simple service provider that provides resolver factories to the gRPC channel.
	/// </summary>
	class ResolverServiceProvider(ResolverFactory resolverFactory) : IServiceProvider {
		public object? GetService(Type serviceType) =>
			serviceType == typeof(IEnumerable<ResolverFactory>) ? new[] { resolverFactory } : null;
	}

	static MethodConfig BuildRetryMethodConfig(KurrentDBClientRetrySettings settings) {
		var retryPolicy = new RetryPolicy {
			MaxAttempts       = settings.MaxAttempts,
			InitialBackoff    = settings.InitialBackoff,
			MaxBackoff        = settings.MaxBackoff,
			BackoffMultiplier = settings.BackoffMultiplier
		};

		foreach (var statusCode in settings.RetryableStatusCodes)
			retryPolicy.RetryableStatusCodes.Add(statusCode);

		return new MethodConfig {
			Names       = { MethodName.Default },
			RetryPolicy = retryPolicy
		};
	}

	static LoadBalancingConfig BuildLoadBalancingConfig(string loadBalancerName = "pick_first") =>
		loadBalancerName switch {
			LoadBalancingConfig.RoundRobinPolicyName => new RoundRobinConfig(),
			LoadBalancingConfig.PickFirstPolicyName  => new PickFirstConfig(),
			_                                        => throw new ArgumentException($"Unknown load balancer name: {loadBalancerName}", nameof(loadBalancerName))
		};

	static ChannelCredentials CreateChannelCredentials(KurrentDBClientSettings settings) {
		var channelCredentials = settings.ChannelCredentials;

		// If no credentials are provided, create if necessary
		if (channelCredentials is null && settings.ConnectivitySettings.SslCredentials.Required) {
			channelCredentials = GrpcCredentialsHelper.CreateSslCredentials(
				settings.ConnectivitySettings.SslCredentials.ClientCertificatePath,
				settings.ConnectivitySettings.SslCredentials.ClientCertificateKeyPath,
				settings.ConnectivitySettings.SslCredentials.RootCertificatePath,
				settings.ConnectivitySettings.SslCredentials.VerifyServerCertificate
			);
		}

		var callCredentials = CreateCallCredentials(settings);
		channelCredentials = callCredentials is not null
			? ChannelCredentials.Create(channelCredentials ?? GetDefaultCredentials(), callCredentials)
			: GetDefaultCredentials();

		return channelCredentials;

		static CallCredentials? CreateCallCredentials(KurrentDBClientSettings settings) {
			if (settings.DefaultCredentials is null) return null;

			return CallCredentials.FromInterceptor(async (context, metadata) => {
				var token = await GetAuthToken(context).ConfigureAwait(false);
				metadata.Add(Constants.Headers.Authorization, token);
			});

			ValueTask<string> GetAuthToken(AuthInterceptorContext context) =>
				settings.OperationOptions.GetAuthenticationHeaderValue(settings.DefaultCredentials, context.CancellationToken);
		}

		ChannelCredentials GetDefaultCredentials() =>
			settings.ConnectivitySettings.Insecure
				? ChannelCredentials.Insecure
				: ChannelCredentials.SecureSsl;
	}
}
