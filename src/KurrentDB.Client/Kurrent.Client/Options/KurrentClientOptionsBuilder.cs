using System.Net;
using Grpc.Core.Interceptors;
using Kurrent.Client.Model;
using Kurrent.Client.Security;
using Microsoft.Extensions.Logging;

namespace Kurrent.Client;

#pragma warning disable CS8524
/// <summary>
/// Builder for creating <see cref="KurrentClientOptions"/> instances in a fluent manner.
/// </summary>
/// <remarks>
/// <para>
/// Provides a fluent API for configuring all aspects of KurrentDB client options,
/// including connection settings, security, resilience strategies, and schema configuration.
/// </para>
/// <para>
/// Start with <see cref="Create"/> for default options or <see cref="FromConnectionString"/> to
/// initialize from a connection string.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create with defaults and customize
/// var options = KurrentClientOptionsBuilder.Create()
///     .WithConnectionName("my-service")
///     .WithEndpoint("kurrentdb.example.com", 2113)
///     .WithSecurityOptions(KurrentClientSecurityOptions.MutualTls(
///         clientCertPath: "/path/to/client.crt",
///         clientKeyPath: "/path/to/client.key"
///     ))
///     .Build();
///
/// // Create from connection string
/// var options = KurrentClientOptionsBuilder
///     .FromConnectionString("kurrentdb://username:password@localhost:2113?tls=true")
///     .WithResilienceOptions(KurrentClientResilienceOptions.HighAvailability)
///     .Build();
/// </code>
/// </example>
[PublicAPI]
public sealed class KurrentClientOptionsBuilder {
    KurrentClientOptions Options { get; set; } = new();

    /// <summary>
    /// Creates a builder with default options.
    /// </summary>
    /// <returns>A new KurrentClientOptionsBuilder instance with default settings.</returns>
    public static KurrentClientOptionsBuilder Create() => new();

    /// <summary>
    /// Creates a builder initialized from a connection string.
    /// </summary>
    /// <param name="connectionString">The connection string to parse.</param>
    /// <returns>A new KurrentClientOptionsBuilder instance configured according to the connection string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when connectionString is null.</exception>
    /// <exception cref="ArgumentException">Thrown when connectionString is invalid or empty.</exception>
    public static KurrentClientOptionsBuilder FromConnectionString(string connectionString) {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty", nameof(connectionString));

        return new() { Options = KurrentClientOptions.Create(connectionString) };
    }

    /// <summary>
    /// Sets the connection name.
    /// </summary>
    /// <param name="connectionName">The name of the connection.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithConnectionName(string connectionName) {
        if (string.IsNullOrWhiteSpace(connectionName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionName));

        return With(options => options with { ConnectionName = connectionName });
    }

    /// <summary>
    /// Sets the connection scheme to direct and configures a single-node connection.
    /// <remarks>
    /// Overrides any existing endpoints and sets the connection scheme to <see cref="KurrentConnectionScheme.Direct"/>.
    /// </remarks>
    /// </summary>
    /// <param name="uri">The URI of the server.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithDirectConnection(Uri uri) =>
        WithDirectConnection(uri.Host, uri.Port, uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>
    /// Sets the connection scheme to direct and configures a single-node connection.
    /// <remarks>
    /// Overrides any existing endpoints and sets the connection scheme to <see cref="KurrentConnectionScheme.Direct"/>.
    /// </remarks>
    /// </summary>
    /// <param name="host">The hostname of the server.</param>
    /// <param name="port">The port of the server.</param>
    /// <param name="useTls">Whether to use TLS.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithDirectConnection(string host, int port, bool useTls = true) {
        return With(options => options with {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new DnsEndPoint(host, port)],
            Security         = options.Security with { Transport = useTls ? TransportSecurity.Standard : TransportSecurity.None }
        });
    }

    /// <summary>
    /// Sets the connection scheme to discover and configures a gossip endpoint for cluster discovery.
    /// </summary>
    /// <param name="uri">The URI of the server.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithGossipEndpoint(Uri uri) =>
        WithGossipEndpoint(uri.Host, uri.Port, uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>
    /// Sets the connection scheme to discover and configures a gossip endpoint for cluster discovery.
    /// </summary>
    /// <param name="host">The hostname of the server.</param>
    /// <param name="port">The port of the server.</param>
    /// <param name="useTls">Whether to use TLS.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithGossipEndpoint(string host, int port, bool useTls = true) =>
        WithGossipEndpoint(new DnsEndPoint(host, port));

    /// <summary>
    /// Sets the connection scheme to discover and configures a gossip endpoint for cluster discovery.
    /// </summary>
    /// <param name="endpoint">The DNS endpoint to use for gossip discovery.</param>
    /// <param name="useTls">Whether to use TLS.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithGossipEndpoint(DnsEndPoint endpoint, bool useTls = true) {
        return With(options => options with {
            ConnectionScheme = KurrentConnectionScheme.Discover,
            Endpoints        = options.Endpoints.Concat([endpoint]).ToArray(),
            Security         = options.Security with { Transport = useTls ? TransportSecurity.Standard : TransportSecurity.None }
        });
    }

    /// <summary>
    /// Sets the connection scheme to discover and configures multiple gossip endpoints for cluster discovery.
    /// </summary>
    /// <param name="endpoints">The DNS endpoints to use.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithGossipEndpoint(params DnsEndPoint[] endpoints) {
        return With(options => options with {
            ConnectionScheme = KurrentConnectionScheme.Discover,
            Endpoints        = options.Endpoints.Concat(endpoints).ToArray(),
            Security         = options.Security with { Transport = TransportSecurity.Standard }
        });
    }

    /// <summary>
    /// Sets the gossip options.
    /// </summary>
    /// <param name="gossipOptions">The gossip configuration options.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithGossipOptions(KurrentClientGossipOptions gossipOptions) =>
        With(options => options with { Gossip = gossipOptions });

    /// <summary>
    /// Configures the existing gossip options.
    /// </summary>
    /// <param name="configure">
    /// A function to configure the existing <see cref="KurrentClientGossipOptions"/>.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithGossipOptions(Func<KurrentClientGossipOptions, KurrentClientGossipOptions> configure) =>
        With(options => options with { Gossip = configure(options.Gossip) });

    /// <summary>
    /// Sets the authentication credentials for the client.
    /// </summary>
    /// <param name="credentials">
    /// The client credentials to use for authentication.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithAuthentication(ClientCredentials credentials) =>
        With(options => options with { Security = options.Security with { Authentication = credentials } });

    /// <summary>
    /// Sets the authentication credentials for the client using basic authentication.
    /// </summary>
    /// <param name="username">
    /// The username for basic authentication.
    /// </param>
    /// <param name="password">
    /// The password for basic authentication. Can be null if no password is required.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithBasicAuthentication(string username, string? password) =>
        WithAuthentication(ClientCredentials.Basic(username, password));

    /// <summary>
    /// Sets the authentication credentials for the client using basic authentication.
    /// </summary>
    /// <param name="username">
    /// The username for basic authentication.
    /// </param>
    /// <param name="password">
    /// The password for basic authentication. Can be null if no password is required.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithCertificateAuthentication(string username, string? password) =>
        WithAuthentication(ClientCredentials.Basic(username, password));

    /// <summary>
    /// Sets the transport security configuration.
    /// </summary>
    /// <param name="transportSecurity">
    /// The transport security configuration to use.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithTransportSecurity(TransportSecurity transportSecurity) =>
        With(options => options with { Security = options.Security with { Transport = transportSecurity } });

    // /// <summary>
    // /// Configures the client to use insecure transport (no TLS).
    // /// </summary>
    // /// <returns>The builder instance for method chaining.</returns>
    // public KurrentClientOptionsBuilder WithInsecureTransport() =>
    //     With(options => options with { Security = options.Security with { Transport = TransportSecurity.Insecure } });

    /// <summary>
    /// Sets the security options.
    ///
    /// </summary>
    /// <param name="securityOptions">
    /// The security options to set. If provided, this will override the existing security options.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithSecurity(KurrentClientSecurityOptions securityOptions) =>
        With(options => options with { Security =  securityOptions });

    /// <summary>
    /// Configures the existing security options.
    /// </summary>
    /// <param name="configure">
    /// A function to configure the existing <see cref="KurrentClientSecurityOptions"/>.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithSecurity(Func<KurrentClientSecurityOptions, KurrentClientSecurityOptions> configure) =>
        With(options => options with { Security = configure(options.Security) });

    /// <summary>
    /// Sets the resilience options.
    /// </summary>
    /// <param name="resilienceOptions">The resilience configuration options.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithResilience(KurrentClientResilienceOptions resilienceOptions) =>
        With(options => options with { Resilience = resilienceOptions });

    /// <summary>
    /// Configures the existing resilience options.
    /// </summary>
    /// <param name="configure">
    /// A function to configure the existing <see cref="KurrentClientResilienceOptions"/>.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder ConfigureResilience(Func<KurrentClientResilienceOptions, KurrentClientResilienceOptions> configure) =>
        With(options => options with { Resilience = configure(options.Resilience) });

    // /// <summary>
    // /// Sets the retry options for resilience.
    // /// </summary>
    // /// <param name="retryOptions">The retry configuration options.</param>
    // /// <returns>The builder instance for method chaining.</returns>
    // public KurrentClientOptionsBuilder WithRetryOptions(KurrentClientResilienceOptions.RetryOptions retryOptions) =>
    //     With(options => options with { Resilience = options.Resilience with { Retry = retryOptions } });
    //
    // /// <summary>
    // /// Configures the existing retry options.
    // /// </summary>
    // /// <param name="configure">
    // /// A function to configure the existing <see cref="KurrentClientResilienceOptions.RetryOptions"/>.
    // /// </param>
    // /// <returns>The builder instance for method chaining.</returns>
    // public KurrentClientOptionsBuilder WithRetryOptions(Func<KurrentClientResilienceOptions.RetryOptions, KurrentClientResilienceOptions.RetryOptions> configure) =>
    //     With(options => options with { Resilience = options.Resilience with { Retry = configure(options.Resilience.Retry) } });

    /// <summary>
    /// Sets the schema options.
    /// </summary>
    /// <param name="schemaOptions">The schema configuration options.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithSchema(KurrentClientSchemaOptions schemaOptions) =>
        With(options => options with { Schema = schemaOptions });

    /// <summary>
    /// Configures the existing schema options.
    /// </summary>
    /// <param name="configure">
    /// A function to configure the existing <see cref="KurrentClientSchemaOptions"/>.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder ConfigureSchema(Func<KurrentClientSchemaOptions, KurrentClientSchemaOptions> configure) =>
        With(options => options with { Schema = configure(options.Schema) });

    /// <summary>
    /// Sets the interceptors.
    /// </summary>
    /// <param name="interceptors">The gRPC interceptors to use.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithInterceptors(params Interceptor[] interceptors) =>
        With(options => options with { Interceptors = interceptors });

    /// <summary>
    /// Sets the metadata decoder.
    /// </summary>
    /// <param name="decoder">The metadata decoder to use.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithCustomMetadataDecoder(MetadataDecoderBase decoder) =>
        With(options => options with { MetadataDecoder = decoder });

    /// <summary>
    /// Sets the logger factory.
    /// </summary>
    /// <param name="loggerFactory">The logger factory to use.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithLoggerFactory(ILoggerFactory loggerFactory) =>
        With(options => options with { LoggerFactory = loggerFactory });

    /// <summary>
    /// Builds the final KurrentClientOptions instance after applying all configurations and validating them.
    /// </summary>
    /// <returns>A configured KurrentClientOptions instance.</returns>
    public KurrentClientOptions Build() {
        Options.EnsureConfigIsValid();
        return Options with { Endpoints = Options.Endpoints.ToArray() };
    }

    /// <summary>
    /// Creates a new <see cref="KurrentClient"/> instance using the configured options.
    /// </summary>
    /// <returns></returns>
    public KurrentClient CreateClient() =>
        KurrentClient.Create(Build());

    /// <summary>
    /// Helper method for applying transformations to options.
    /// </summary>
    KurrentClientOptionsBuilder With(Func<KurrentClientOptions, KurrentClientOptions> transform) {
        Options = transform(Options);
        return this;
    }
}
