// using System.Collections.Concurrent;
// using Grpc.Core;
// using Grpc.Net.Client;
// using Grpc.Net.Client.Configuration;
// using Microsoft.Extensions.Logging;
//
// namespace KurrentDb.Client;
//
// public class KurrentDbConnectionFactory : IDisposable {
// 	readonly ConcurrentDictionary<Type, object> _clientCache = new();
// 	readonly string                             _connectionString;
// 	readonly Lazy<GrpcChannel>                  _lazyChannel;
// 	readonly ILogger                            _logger;
// 	readonly ILoggerFactory                     _loggerFactory;
// 	readonly KurrentDBConnectionOptions            _options;
//
// 	// /// <summary>
// 	// /// Creates a new instance of KurrentDbConnectionFactory
// 	// /// </summary>
// 	// /// <param name="connectionString">The KurrentDB connection string</param>
// 	// /// <param name="loggerFactory">Optional logger factory</param>
// 	// public KurrentDbConnectionFactory(
// 	// 	string connectionString,
// 	// 	ILoggerFactory loggerFactory
// 	// ) {
// 	// 	_connectionString = connectionString;
// 	// 	_loggerFactory    = loggerFactory;
// 	// 	_logger           = _loggerFactory.CreateLogger<KurrentDbConnectionFactory>();
// 	// 	_options          = ConnectionStringParser.Parse(connectionString);
// 	//
// 	// 	// Use lazy initialization for the channel
// 	// 	_lazyChannel = new Lazy<GrpcChannel>(InitializeChannel, LazyThreadSafetyMode.ExecutionAndPublication);
// 	// }
//
// 	/// <summary>
// 	/// Creates a new instance of KurrentDbConnectionFactory with pre-parsed options
// 	/// </summary>
// 	/// <param name="options">The connection options</param>
// 	/// <param name="loggerFactory">Optional logger factory</param>
// 	public KurrentDbConnectionFactory(
// 		KurrentDBConnectionOptions options,
// 		ILoggerFactory loggerFactory
// 	) {
// 		_options       = options;
// 		_loggerFactory = loggerFactory;
// 		_logger        = _loggerFactory.CreateLogger<KurrentDbConnectionFactory>();
//
// 		// Use lazy initialization for the channel
// 		_lazyChannel = new Lazy<GrpcChannel>(InitializeChannel, LazyThreadSafetyMode.ExecutionAndPublication);
// 	}
//
// 	/// <summary>
// 	/// Gets the initialized gRPC channel.
// 	/// </summary>
// 	public GrpcChannel Channel => _lazyChannel.Value;
//
// 	/// <summary>
// 	/// Disposes the channel and cached clients
// 	/// </summary>
// 	public void Dispose() {
// 		// Only dispose the channel if it was initialized
// 		if (_lazyChannel.IsValueCreated) _lazyChannel.Value.Dispose();
//
// 		// Clear the client cache
// 		_clientCache.Clear();
// 	}
//
// 	/// <summary>
// 	/// Initializes the gRPC channel with the KurrentDB resolver.
// 	/// This is called lazily when the channel is first needed.
// 	/// </summary>
// 	GrpcChannel InitializeChannel() {
// 		_logger.LogInformation("Initializing gRPC channel with KurrentDB resolver");
//
// 		// // Register the resolver factory
// 		// var resolverFactory = new KurrentDbResolverFactory(
// 		// 	_options,
// 		// 	_loggerFactory
// 		// );
// 		//
// 		// NameResolver.RegisterResolverFactory(resolverFactory);
//
// 		// Create a unique channel address using the resolver scheme
// 		var connectionId = _options.ConnectionName;
// 		if (string.IsNullOrEmpty(connectionId))
// 			// Use partial GUID if no connection name is provided
// 			connectionId = Guid.NewGuid().ToString("N").Substring(0, 8);
//
// 		var channelAddress = new Uri($"kurrentdb:///connection-{connectionId}");
//
// 		// Configure credentials
// 		var credentials = _options.UseTls ? ChannelCredentials.SecureSsl : ChannelCredentials.Insecure;
//
// 		// Add authentication if needed
// 		if (!string.IsNullOrEmpty(_options.Username) && !string.IsNullOrEmpty(_options.Password)) {
// 			var callCredentials = CallCredentials.FromInterceptor((context, metadata) => {
// 					var auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_options.Username}:{_options.Password}"));
// 					metadata.Add("Authorization", $"Basic {auth}");
// 					return Task.CompletedTask;
// 				}
// 			);
//
// 			credentials = ChannelCredentials.Create(credentials, callCredentials);
// 		}
//
// 		// Create HTTP handler
// 		var httpHandler = new SocketsHttpHandler {
// 			PooledConnectionIdleTimeout    = TimeSpan.FromMinutes(5),
// 			KeepAlivePingDelay             = TimeSpan.FromSeconds(_options.KeepAliveInterval),
// 			KeepAlivePingTimeout           = TimeSpan.FromSeconds(_options.KeepAliveTimeout),
// 			EnableMultipleHttp2Connections = true
// 		};
//
// 		// Configure TLS verification
// 		if (_options.UseTls && !_options.VerifyCert)
// 			httpHandler.SslOptions.RemoteCertificateValidationCallback =
// 				(sender, certificate, chain, errors) => true;
//
// 		// Configure channel options with load balancing
// 		var channelOptions = new GrpcChannelOptions {
// 			LoggerFactory = _loggerFactory,
// 			Credentials   = credentials,
// 			HttpHandler   = httpHandler,
// 			ServiceConfig = new ServiceConfig {
// 				// Use the round-robin load balancer
// 				LoadBalancingConfigs = { new LoadBalancingConfig("round_robin") },
//
// 				// Configure method defaults
// 				MethodConfigs = {
// 					new MethodConfig {
// 						Names = { MethodName.Default },
// 						RetryPolicy = new RetryPolicy {
// 							MaxAttempts          = _options.MaxDiscoverAttempts,
// 							InitialBackoff       = TimeSpan.FromMilliseconds(_options.DiscoveryInterval),
// 							MaxBackoff           = TimeSpan.FromSeconds(5),
// 							BackoffMultiplier    = 1.5,
// 							RetryableStatusCodes = { StatusCode.Unavailable }
// 						}
// 					}
// 				}
// 			}
// 		};
//
// 		return GrpcChannel.ForAddress(channelAddress, channelOptions);
// 	}
//
// 	/// <summary>
// 	/// Creates a client of the specified type, reusing an existing client instance if one exists.
// 	/// The channel is initialized lazily when the first client is created.
// 	/// </summary>
// 	public TClient CreateClient<TClient>() where TClient : ClientBase<TClient> {
// 		return (TClient)_clientCache.GetOrAdd(
// 			typeof(TClient),
// 			_ => {
// 				_logger.LogInformation("Creating new client of type {ClientType}", typeof(TClient).Name);
//
// 				// Use reflection to create an instance of the client with the channel
// 				var constructor = typeof(TClient).GetConstructor(new[] { typeof(GrpcChannel) });
// 				if (constructor != null) return constructor.Invoke(new object[] { Channel });
//
// 				throw new InvalidOperationException($"Could not find constructor for {typeof(TClient).Name} that accepts a GrpcChannel");
// 			}
// 		);
// 	}
// }
