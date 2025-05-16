using System.Net.Http;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using KurrentDB.Client.Model;
using KurrentDB.Client.SchemaRegistry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KurrentDB.Client;

// /// <summary>
// /// Defines reconnection configuration for gRPC channel-level connections.
// /// </summary>
// public class KurrentDBChannelReconnectionSettings {
// 	/// <summary>
// 	/// Gets or sets the initial backoff delay for reconnection attempts.
// 	/// </summary>
// 	public TimeSpan InitialReconnectBackoff { get; set; } = TimeSpan.FromMilliseconds(500);
//
// 	/// <summary>
// 	/// Gets or sets the maximum backoff delay for reconnection attempts.
// 	/// </summary>
// 	public TimeSpan MaxReconnectBackoff { get; set; } = TimeSpan.FromSeconds(30);
//
// 	/// <summary>
// 	/// Gets or sets the minimum connection timeout.
// 	/// </summary>
// 	public TimeSpan MinConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);
//
// 	/// <summary>
// 	/// Gets or sets the maximum connection attempts. Set to null for unlimited attempts.
// 	/// </summary>
// 	public int? MaxAttempts { get; set; } = null; // Unlimited reconnection attempts for long-lived connections
//
// 	/// <summary>
// 	/// Default reconnection settings with the default values.
// 	/// </summary>
// 	public static KurrentDBChannelReconnectionSettings Default => new();
// }

/// <summary>
/// Defines retry configuration for gRPC operations.
/// </summary>
public record KurrentDBClientRetrySettings {
	/// <summary>
	/// Gets or sets whether retry is enabled.
	/// </summary>
	public bool IsEnabled { get; init; } = true;

	/// <summary>
	/// Gets or sets the maximum number of retry attempts.
	/// </summary>
	public int MaxAttempts { get; init; } = 3;

	/// <summary>
	/// Gets or sets the initial backoff delay.
	/// </summary>
	public TimeSpan InitialBackoff { get; init; } = TimeSpan.FromSeconds(250);

	/// <summary>
	/// Gets or sets the maximum backoff delay.
	/// </summary>
	public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromSeconds(10);

	/// <summary>
	/// Gets or sets the backoff multiplier. Each successive backoff increases by this multiplier.
	/// </summary>
	public double BackoffMultiplier { get; init; } = 2.0;

	/// <summary>
	/// Gets or sets the gRPC status codes that should trigger a retry.
	/// </summary>
	public StatusCode[] RetryableStatusCodes { get; init; } = [
		StatusCode.Unavailable,       // Server temporarily unavailable
		StatusCode.Unknown,           // Unknown error (often network issues)
		StatusCode.DeadlineExceeded,  // Server took too long to respond
		StatusCode.ResourceExhausted, // Server overloaded
	];

	/// <summary>
	/// Default retry settings with the default values.
	/// </summary>
	public static KurrentDBClientRetrySettings Default => new();

	/// <summary>
	/// Retry settings with retry disabled.
	/// </summary>
	public static KurrentDBClientRetrySettings NoRetry => new() { IsEnabled = false };

	internal MethodConfig GetRetryMethodConfig() {
		var retryPolicy = new RetryPolicy {
			MaxAttempts       = MaxAttempts,
			InitialBackoff    = InitialBackoff,
			MaxBackoff        = MaxBackoff,
			BackoffMultiplier = BackoffMultiplier
		};

		foreach (var statusCode in RetryableStatusCodes)
			retryPolicy.RetryableStatusCodes.Add(statusCode);

		return new() {
			Names       = { MethodName.Default },
			RetryPolicy = retryPolicy
		};
	}
}

/// <summary>
/// Defines schema settings for a KurrentDB client, including naming strategies and auto-registration.
/// </summary>
public record KurrentDBClientSchemaRegistrySettings {
	public ISchemaNameStrategy NameStrategy { get; init; } = new MessageSchemaNameStrategy();
	public bool                AutoRegister { get; init; } = true;
	public bool                Validate     { get; init; } = true;

	public static KurrentDBClientSchemaRegistrySettings Default => new();
}

/// <summary>
/// A class that represents the settings to use for operations made from an implementation of <see cref="KurrentDBClientBase"/>.
/// </summary>
public class KurrentDBClientSettings {
	public static KurrentDBClientSettingsBuilder Builder => new();

	/// <summary>
	/// The name of the connection.
	/// </summary>
	public string? ConnectionName { get; set; }

	/// <summary>
	/// An optional list of <see cref="Interceptor"/>s to use.
	/// </summary>
	public IEnumerable<Interceptor>? Interceptors { get; set; }

	/// <summary>
	/// An optional <see cref="HttpMessageHandler"/> factory.
	/// </summary>
	public Func<HttpMessageHandler>? CreateHttpMessageHandler { get; set; }

	/// <summary>
	/// An optional <see cref="ILoggerFactory"/> to use.
	/// </summary>
	public ILoggerFactory? LoggerFactory { get; set; }

	/// <summary>
	/// The optional <see cref="ChannelCredentials"/> to use when creating the <see cref="ChannelBase"/>.
	/// </summary>
	public ChannelCredentials? ChannelCredentials { get; set; }

	/// <summary>
	/// The default <see cref="KurrentDBClientOperationOptions"/> to use.
	/// </summary>
	public KurrentDBClientOperationOptions OperationOptions { get; set; } =
		KurrentDBClientOperationOptions.Default;

	/// <summary>
	/// The <see cref="KurrentDBClientConnectivitySettings"/> to use.
	/// </summary>
	public KurrentDBClientConnectivitySettings ConnectivitySettings { get; set; } =
		KurrentDBClientConnectivitySettings.Default;

	/// <summary>
	/// The optional <see cref="UserCredentials"/> to use if none have been supplied to the operation.
	/// </summary>
	public UserCredentials? DefaultCredentials { get; set; }

	/// <summary>
	/// The default deadline for calls. Will not be applied to reads or subscriptions.
	/// </summary>
	public TimeSpan? DefaultDeadline { get; set; } = TimeSpan.FromSeconds(10);

	public KurrentDBClientSchemaRegistrySettings SchemaRegistry { get; set; } = null!;

	public IMessageTypeResolver MessageTypeResolver { get; set; } = null!;

	public IMetadataDecoder MetadataDecoder { get; set; } = null!;

	/// <summary>
	/// The retry settings to use for gRPC operations.
	/// </summary>
	public KurrentDBClientRetrySettings RetrySettings { get; set; } = KurrentDBClientRetrySettings.NoRetry;

	/// <summary>
	/// Creates client settings from a connection string
	/// </summary>
	/// <param name="connectionString">The connection string to parse</param>
	/// <returns>A configured KurrentDBClientSettings instance</returns>
	public static KurrentDBClientSettings Create(string connectionString) =>
		KurrentDBConnectionString.Parse(connectionString).ToClientSettings();

	public (GrpcChannel Channel, CallInvoker Invoker) CreateGrpcChannel(bool enableLoadBalancing = true) {
		// Configure service config with both load balancing and retry
		var retryMethodConfig = GetRetryMethodConfig();

		var serviceConfig = retryMethodConfig switch {
			not null when enableLoadBalancing => new ServiceConfig {
				LoadBalancingConfigs = { new RoundRobinConfig() },
				MethodConfigs        = { retryMethodConfig }
			},
			not null when !enableLoadBalancing => new ServiceConfig {
				MethodConfigs = { retryMethodConfig }
			},
			null when enableLoadBalancing => new ServiceConfig {
				LoadBalancingConfigs = { new RoundRobinConfig() },
			},
			_ => null
		};

		// Create the authenticated channel credentials
		var credentials = CreateChannelCredentials();

		// Set up channel options
		var channelOptions = new GrpcChannelOptions {
			Credentials   = credentials,
			HttpHandler   = CreateHttpMessageHandler?.Invoke(),
			LoggerFactory = LoggerFactory,
			ServiceConfig = serviceConfig
		};

		// Create a channel with the authenticated credentials
		var channel = GrpcChannel.ForAddress(ConnectivitySettings.Address!, channelOptions);

		return (channel, channel.Intercept(Interceptors?.ToArray() ?? []));

		ChannelCredentials CreateChannelCredentials() {
			if (DefaultCredentials is null)
				return ChannelCredentials ?? new SslCredentials();

			var getAuthValue = OperationOptions.GetAuthenticationHeaderValue;
			var callCredentials = CallCredentials.FromInterceptor(async (context, metadata) => {
				var authValue = await getAuthValue(DefaultCredentials, context.CancellationToken).ConfigureAwait(false);
				metadata.Add(Constants.Headers.Authorization, authValue);
			});

			// Combine with existing channel credentials or default to SSL
			return ChannelCredentials.Create(ChannelCredentials ?? new SslCredentials(), callCredentials);
		}

		MethodConfig? GetRetryMethodConfig() {
			if (!RetrySettings.IsEnabled)
				return null;

			var retryPolicy = new RetryPolicy {
				MaxAttempts       = RetrySettings.MaxAttempts,
				InitialBackoff    = RetrySettings.InitialBackoff,
				MaxBackoff        = RetrySettings.MaxBackoff,
				BackoffMultiplier = RetrySettings.BackoffMultiplier
			};

			foreach (var statusCode in RetrySettings.RetryableStatusCodes)
				retryPolicy.RetryableStatusCodes.Add(statusCode);

			// Apply retry policy to all methods
			return new MethodConfig {
				Names       = { MethodName.Default },
				RetryPolicy = retryPolicy
			};
		}

		// static ChannelCredentials CreateChannelCredentials3(
		// 	ChannelCredentials? channelCredentials, UserCredentials? userCredentials,
		// 	Func<UserCredentials, CancellationToken, ValueTask<string>> getUserAuthValue
		// ) {
		// 	channelCredentials ??= new SslCredentials();
		//
		// 	if (userCredentials is null)
		// 		return channelCredentials;
		//
		// 	var callCredentials = CallCredentials.FromInterceptor(async (context, metadata) => {
		// 		var authValue = await getUserAuthValue(userCredentials, context.CancellationToken).ConfigureAwait(false);
		// 		metadata.Add(Constants.Headers.Authorization, authValue);
		// 	});
		//
		// 	// Combine with existing channel credentials or default to SSL
		// 	return ChannelCredentials.Create(channelCredentials, callCredentials);
		// }

		// static ChannelCredentials CreateChannelCredentials(ChannelCredentials? channelCredentials, AsyncAuthInterceptor? authInterceptor = null) {
		// 	channelCredentials ??= new SslCredentials();
		// 	return authInterceptor is not null
		// 		? ChannelCredentials.Create(channelCredentials, CallCredentials.FromInterceptor(authInterceptor))
		// 		: channelCredentials;
		// }

	}

	static CallOptions CreateGrpcCallOptions(KurrentDBClientSettings settings, CancellationToken cancellationToken) {
		// var temp = new CallOptions()
		// 	.WithCancellationToken(cancellationToken)
		// 	.WithCredentials(settings.DefaultCredentials)

		var options =  new CallOptions(
			cancellationToken: cancellationToken,
			deadline: DeadlineAfter(settings.DefaultDeadline),
			headers: new() {
				{
					Constants.Headers.RequiresLeader,
					settings.ConnectivitySettings.NodePreference == NodePreference.Leader
						? bool.TrueString
						: bool.FalseString
				}
			},
			credentials: settings.DefaultCredentials is not null
				? CallCredentials.FromInterceptor(async (_, metadata) => {
						var authorizationHeader = await settings.OperationOptions
							.GetAuthenticationHeaderValue(settings.DefaultCredentials, CancellationToken.None)
							.ConfigureAwait(false);

						metadata.Add(Constants.Headers.Authorization, authorizationHeader);
					}
				)
				: null
		);

		return options;

		static DateTime? DeadlineAfter(TimeSpan? timeoutAfter) =>
			timeoutAfter.HasValue
				? timeoutAfter.Value == TimeSpan.MaxValue || timeoutAfter.Value == Timeout.InfiniteTimeSpan
					? DateTime.MaxValue
					: DateTime.UtcNow.Add(timeoutAfter.Value)
				: null;
	}
}
