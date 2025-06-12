using System.ComponentModel;
using System.Net;
using Grpc.Core.Interceptors;
using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry;
using KurrentDB.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static System.TimeSpan;

#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.

namespace Kurrent.Client;

/// <summary>
/// Configuration options for the Kurrent client.
/// </summary>
/// <remarks>
/// <para>
/// Provides comprehensive configuration for the Kurrent client, including connection settings,
/// security options, resilience strategies, and extensibility points.
/// </para>
/// </remarks>
[PublicAPI]
public record KurrentClientOptions : KurrentClientOptionsBase {
    public static KurrentClientOptionsBuilder Create() =>
        KurrentClientOptionsBuilder.Create();

    public static KurrentClientOptionsBuilder FromConnectionString(string connectionString) =>
        KurrentClientOptionsBuilder.FromConnectionString(connectionString);

    const string DefaultConnectionString = "kurrentdb://admin:changeit@localhost:2113?tls=false&tlsVerifyCert=false";

    /// <summary>
    /// When options were created from a connection string, this property holds
    /// the connection string used to connect to the KurrentDB cluster.
    /// </summary>
    public string OriginalConnectionString { get; internal init; } = "";

    /// <summary>
    /// Indicates whether the original connection string was provided.
    /// </summary>
    public bool HasOriginalConnectionString => !string.IsNullOrWhiteSpace(OriginalConnectionString);

    /// <summary>
	/// The name of the connection.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A unique identifier for the connection, used in logging and diagnostics.
	/// </para>
	/// <para>
	/// Defaults to a randomly generated GUID-based name if not specified.
	/// </para>
	/// </remarks>
	public string ConnectionName { get; init; } = $"conn-{Guid.NewGuid()}";

    /// <summary>
    /// The URI scheme used for HTTP connections, based on the security settings.
    /// </summary>
    public string HttpUriScheme => Security.Transport.IsEnabled ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;

    /// <summary>
    /// The URI scheme used for the connection, based on the connection scheme.
    /// It can be either "kurrentdb" or "kurrentdb+discover".
    /// </summary>
    public string ConnectionUriScheme => ConnectionScheme.Description();

    /// <summary>
    /// Scheme used by the resolver for establishing connections to the cluster.
    /// It can be either "kurrentdb" for single-node connections or "kurrentdb+discover" for multi-node clusters with discovery.
    /// </summary>
    public KurrentConnectionScheme ConnectionScheme { get; init; } = KurrentConnectionScheme.Direct;


    /// <summary>
    /// Gets the address of the KurrentDB cluster based on the connection scheme and endpoints.
    /// </summary>
    public Uri Address {
        get {
            if (ConnectionScheme == KurrentConnectionScheme.Direct && Endpoints.Length == 1) {
                var directEndpoint = Endpoints[0];
                return new UriBuilder(HttpUriScheme, directEndpoint.Host, directEndpoint.Port).Uri;
            }

            // If we're using discovery, we don't have a direct address
            // we already validated that we must have endpoints
            return new Uri($"{KurrentConnectionScheme.Discover.Description()}://{Guids.CreateVersion7()}");
        }
    }

    /// <summary>
    /// List of <see cref="DnsEndPoint"/>s used to seed gossip.
    /// </summary>
    public DnsEndPoint[] Endpoints { get; init; } = [new DnsEndPoint("localhost", 2113)];

    /// <summary>
    /// Represents the configuration options for gossip-based discovery in the KurrentDB cluster.
    /// This property provides settings such as discovery intervals, timeouts, and preferences
    /// for selecting nodes during communication in the cluster.
    /// </summary>
    public KurrentClientGossipOptions Gossip { get; init; } = KurrentClientGossipOptions.Default;

    /// <summary>
    /// Security configuration for the client connection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls transport security (TLS) and authentication credentials.
    /// </para>
    /// <para>
    /// Provides comprehensive security options for the connection, including
    /// transport layer security and client authentication.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new KurrentClientOptions {
    ///     Security = KurrentClientSecurityOptions.MutualTls(
    ///         clientCertPath: "/path/to/client.crt",
    ///         clientKeyPath: "/path/to/client.key",
    ///         rootCaPath: "/path/to/ca.crt"
    ///     )
    /// };
    /// </code>
    /// </example>
    public KurrentClientSecurityOptions Security { get; init; } = KurrentClientSecurityOptions.Default;

    /// <summary>
    /// Configuration for resilience strategies, including retries, hedging, and jitter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls how the client handles transient failures and recovery strategies.
    /// </para>
    /// </remarks>
    public KurrentClientResilienceOptions Resilience { get; init; } = KurrentClientResilienceOptions.NoResilience;

    /// <summary>
    /// Schema configuration options.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls how the client handles event schemas and versioning.
    /// </para>
    /// </remarks>
    public KurrentClientSchemaOptions Schema { get; init; } = KurrentClientSchemaOptions.FullValidation;

    /// <summary>
    /// Provides a mechanism for mapping message types to their corresponding schemas.
    /// </summary>
    /// <remarks>
    /// The mapper is used to associate specific message types with their schemas in the schema registry.
    /// This ensures that the correct schema is utilized when producing or consuming messages.
    /// </remarks>
    public MessageTypeMapper Mapper { get; init; } = new();

	/// <summary>
	/// An optional list of <see cref="Interceptor"/>s to use.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Interceptors allow for inspection and modification of gRPC calls.
	/// </para>
	/// <para>
	/// Common uses include logging, metrics collection, and authentication.
	/// </para>
	/// </remarks>
	public IReadOnlyList<Interceptor> Interceptors { get; init; } = [];

	/// <summary>
	/// Metadata decoder for processing event metadata.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Responsible for decoding metadata attached to events.
	/// </para>
	/// </remarks>
	public MetadataDecoderBase MetadataDecoder { get; init; } = new MetadataDecoder();

    /// <summary>
    /// An optional <see cref="ILoggerFactory"/> to use.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The logger factory used to create loggers for the client.
    /// </para>
    /// <para>
    /// Defaults to <see cref="NullLoggerFactory.Instance"/> if not specified.
    /// </para>
    /// </remarks>
    public ILoggerFactory LoggerFactory { get; init; } = NullLoggerFactory.Instance;

    public override string ToString() => this.GenerateConnectionString();

    public static KurrentClientOptions Create(string connectionString) =>
        KurrentDBConnectionString.Parse(connectionString).ToClientOptions();
}

static class KurrentDBConnectionStringExtensions {
    /// <summary>
    /// Converts a V1 connection string configuration to a V2 client options instance.
    /// </summary>
    /// <param name="source">The V1 connection string configuration to convert</param>
    /// <returns>A new KurrentClientOptions instance configured based on the connection string</returns>
    /// <remarks>
    /// <para>
    /// This method enables seamless migration from V1 to V2 of the KurrentDB client by
    /// preserving connection settings and security configuration across versions.
    /// </para>
    /// <para>
    /// All resilience settings from the connection string are mapped to the equivalent
    /// resilience options in the V2 client.
    /// </para>
    /// </remarks>
    public static KurrentClientOptions ToClientOptions(this KurrentDBConnectionString source) {
        var connectionName = GetParameterValue<string>(source.Options, "ConnectionName", $"conn-{Guid.NewGuid():D}");

        var schemeType = KurrentDBConnectionString.DiscoverySchemes.Contains(source.Scheme, StringComparer.OrdinalIgnoreCase)
            ? KurrentConnectionScheme.Discover
            : KurrentConnectionScheme.Direct;

        return new() {
            ConnectionName           = connectionName,
            ConnectionScheme         = schemeType,
            Endpoints                = source.Hosts,
            Security                 = ConfigureSecurityOptions(),
            Resilience               = ConfigureResilienceOptions(),
            Gossip                   = ConfigureGossipOptions(),
            OriginalConnectionString = source.ConnectionString
        };

        KurrentClientSecurityOptions ConfigureSecurityOptions() {
            return new() {
                Transport      = ConfigureTransportSecurity(),
                Authentication = ConfigureAuthentication()
            };

            TransportSecurity ConfigureTransportSecurity() {
                var useTls        = GetParameterValue(source.Options, "Tls", true);
                var tlsVerifyCert = GetParameterValue(source.Options, "TlsVerifyCert", true);
                var tlsCaFile     = GetParameterValue(source.Options, "TlsCaFile", null);

                return !useTls
                    ? TransportSecurity.None
                    : tlsCaFile is not null
                        ? TransportSecurity.Certificate(tlsCaFile)
                        : tlsVerifyCert
                            ? TransportSecurity.Standard
                            : TransportSecurity.Insecure;
            }

            ClientCredentials ConfigureAuthentication() {
                var userCertFile = GetParameterValue(source.Options, "UserCertFile", null);
                var userKeyFile  = GetParameterValue(source.Options, "UserKeyFile", null);

                return !string.IsNullOrEmpty(userCertFile) && !string.IsNullOrEmpty(userKeyFile)
                    ? ClientCredentials.Certificate(userCertFile, userKeyFile)
                    : source.UserInfo is not null
                        ? ClientCredentials.Basic(source.UserInfo.Value.User, source.UserInfo.Value.Password)
                        : ClientCredentials.None;
            }
        }

        KurrentClientResilienceOptions ConfigureResilienceOptions() {
            var keepAliveIntervalMs = GetParameterValue(source.Options, "KeepAliveInterval", 60000);
            var keepAliveTimeoutMs  = GetParameterValue(source.Options, "KeepAliveTimeout", 30000);
            var defaultDeadlineMs   = GetParameterValue(source.Options, "DefaultDeadline", 30000);

            var keepAliveInterval = keepAliveIntervalMs == -1 ? Timeout.InfiniteTimeSpan : FromMilliseconds(keepAliveIntervalMs);
            var keepAliveTimeout  = keepAliveTimeoutMs  == -1 ? Timeout.InfiniteTimeSpan : FromMilliseconds(keepAliveTimeoutMs);
            var deadline          = defaultDeadlineMs   == -1 ? Timeout.InfiniteTimeSpan : FromMilliseconds(defaultDeadlineMs);

            return new() {
                KeepAliveInterval = keepAliveInterval,
                KeepAliveTimeout  = keepAliveTimeout,
                Deadline          = deadline
            };
        }

        KurrentClientGossipOptions ConfigureGossipOptions() {
            var maxDiscoverAttempts = GetParameterValue(source.Options, "MaxDiscoverAttempts", 10);
            var discoveryInterval   = GetParameterValue(source.Options, "DiscoveryInterval", 100);
            var gossipTimeout       = GetParameterValue(source.Options, "GossipTimeout", 5000);
            var nodePreference      = GetParameterValue(source.Options, "NodePreference", NodePreference.Random);

            return new() {
                MaxDiscoverAttempts = maxDiscoverAttempts,
                DiscoveryInterval   = FromMilliseconds(discoveryInterval),
                Timeout             = FromMilliseconds(gossipTimeout),
                ReadPreference      = nodePreference
            };
        }
    }

    static T GetParameterValue<T>(Dictionary<string, string> source, string key, T defaultValue) =>
        !source.TryGetValue(key, out var strValue) ? defaultValue : (T)Convert.ChangeType(strValue, typeof(T));

    static string? GetParameterValue(Dictionary<string, string> source, string key, string? defaultValue) =>
        !source.TryGetValue(key, out var value) ? defaultValue : value;

    static bool GetParameterValue(Dictionary<string, string> source, string key, bool defaultValue) =>
        !source.TryGetValue(key, out var strValue) ? defaultValue : bool.TryParse(strValue, out var value) ? value : defaultValue;
}

/// <summary>
/// Represents the connection scheme used by the Kurrent client.
/// </summary>
public enum KurrentConnectionScheme {
    /// <summary>Direct connection to a single node</summary>
    [Description("kurrentdb")]
    Direct,

    /// <summary>Cluster connection with discovery via gossip protocol</summary>
    [Description("kurrentdb+discover")]
    Discover
}
