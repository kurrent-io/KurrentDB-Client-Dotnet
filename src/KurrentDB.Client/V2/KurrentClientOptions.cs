using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KurrentDB.Client;

/// <summary>
/// Defines retry configuration for gRPC operations.
/// </summary>
public record KurrentClientRetryOptions {
	/// <summary>
	/// Gets or sets whether retry is enabled.
	/// </summary>
	public bool Enabled { get; init; } = true;

	/// <summary>
	/// Gets or sets the maximum number of retry attempts.
	/// </summary>
	public int MaxAttempts { get; init; } = 3;

	/// <summary>
	/// Gets or sets the initial backoff delay for reconnection attempts.
	/// </summary>
	public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromMilliseconds(250);

	/// <summary>
	/// Gets or sets the maximum backoff delay for reconnection attempts.
	/// </summary>
	public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromSeconds(10);

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

	public static KurrentClientRetryOptions Default => new();

	public static KurrentClientRetryOptions NoRetry => new() { Enabled = false };

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

//
// /// <summary>
// /// Represents TLS (Transport Layer Security) configuration settings.
// /// TLS is a cryptographic protocol that provides secure communication over networks,
// /// ensuring data privacy and integrity between client and server.
// /// </summary>
// /// <param name="Enabled">Whether to use TLS encryption</param>
// /// <param name="VerifyCertificate">Whether to verify TLS certificates</param>
// /// <param name="CaFile">Path to custom CA certificate file</param>
// public record struct TlsSettings(bool Enabled = true, bool VerifyCertificate = true, string? CaFile = null) {
// 	/// <summary>
// 	/// Default TLS settings (enabled with certificate verification)
// 	/// </summary>
// 	public static readonly TlsSettings Default = new(true, true);
//
// 	/// <summary>
// 	/// Insecure TLS settings (disabled) - only use for development
// 	/// </summary>
// 	public static readonly TlsSettings Insecure = new(false, false);
// }
//
// // /// <summary>
// // /// Enumeration of supported node preferences for connection routing
// // /// </summary>
// // public enum NodePreference {
// // 	Leader,
// // 	Follower,
// // 	Random,
// // 	ReadOnlyReplica
// // }
//
// /// <summary>
// /// Enumeration of supported connection schemes
// /// </summary>
// public enum ConnectionScheme {
// 	/// <summary>Direct connection to a single node</summary>
// 	Direct,
//
// 	/// <summary>Cluster connection with discovery via gossip protocol</summary>
// 	Discover
// }


// /// <summary>
// /// Represents basic authentication credentials (username/password)
// /// </summary>
// /// <param name="Username">The username for authentication</param>
// /// <param name="Password">The password for authentication</param>
// public record struct UserCredentials(string? Username, string? Password) {
// 	/// <summary>
// 	/// Indicates whether credentials are provided
// 	/// </summary>
// 	public bool IsEmpty => string.IsNullOrEmpty(Username);
// }
//
// /// <summary>
// /// Represents X.509 certificate authentication credentials
// /// </summary>
// /// <param name="CertificateFile">Path to the client certificate file</param>
// /// <param name="KeyFile">Path to the private key file</param>
// public record struct CertificateCredentials(string? CertificateFile, string? KeyFile) {
// 	/// <summary>
// 	/// Indicates whether credentials are provided
// 	/// </summary>
// 	public bool IsEmpty => string.IsNullOrEmpty(CertificateFile) || string.IsNullOrEmpty(KeyFile);
// }
