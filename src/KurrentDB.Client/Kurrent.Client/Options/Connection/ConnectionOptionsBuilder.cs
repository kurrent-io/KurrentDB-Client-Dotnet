using System.Net;

namespace Kurrent.Client;

[PublicAPI]
public class ConnectionOptionsBuilder : OptionsBuilder<ConnectionOptionsBuilder, KurrentClientOptions> {
    public ConnectionOptionsBuilder(KurrentClientOptions options) : base(options) { }
    public ConnectionOptionsBuilder() : base(new KurrentClientOptions()) { }

    /// <summary>
    /// Sets the connection name.
    /// </summary>
    /// <param name="connectionName">The name of the connection.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConnectionOptionsBuilder WithConnectionName(string connectionName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);
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
    public ConnectionOptionsBuilder WithDirectConnection(Uri uri) =>
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
    public ConnectionOptionsBuilder WithDirectConnection(string host, int port, bool useTls = true) {
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
    public ConnectionOptionsBuilder WithGossipEndpoint(Uri uri) =>
        WithGossipEndpoint(uri.Host, uri.Port, uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>
    /// Sets the connection scheme to discover and configures a gossip endpoint for cluster discovery.
    /// </summary>
    /// <param name="host">The hostname of the server.</param>
    /// <param name="port">The port of the server.</param>
    /// <param name="useTls">Whether to use TLS.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConnectionOptionsBuilder WithGossipEndpoint(string host, int port, bool useTls = true) =>
        WithGossipEndpoint(new DnsEndPoint(host, port));

    /// <summary>
    /// Sets the connection scheme to discover and configures a gossip endpoint for cluster discovery.
    /// </summary>
    /// <param name="endpoint">The DNS endpoint to use for gossip discovery.</param>
    /// <param name="useTls">Whether to use TLS.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConnectionOptionsBuilder WithGossipEndpoint(DnsEndPoint endpoint, bool useTls = true) {
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
    public ConnectionOptionsBuilder WithGossipEndpoint(params DnsEndPoint[] endpoints) {
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
    public ConnectionOptionsBuilder WithGossipOptions(KurrentClientGossipOptions gossipOptions) =>
        With(options => options with { Gossip = gossipOptions });

    /// <summary>
    /// Configures the existing gossip options.
    /// </summary>
    /// <param name="configure">
    /// A function to configure the existing <see cref="KurrentClientGossipOptions"/>.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConnectionOptionsBuilder WithGossipOptions(Func<KurrentClientGossipOptions, KurrentClientGossipOptions> configure) =>
        With(options => options with { Gossip = configure(options.Gossip) });

    /// <summary>
    /// Sets the authentication credentials for the client.
    /// </summary>
    /// <param name="credentials">
    /// The client credentials to use for authentication.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConnectionOptionsBuilder WithAuthentication(ClientCredentials credentials) =>
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
    public ConnectionOptionsBuilder WithBasicAuthentication(string username, string? password) =>
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
    public ConnectionOptionsBuilder WithCertificateAuthentication(string username, string? password) =>
        WithAuthentication(ClientCredentials.Basic(username, password));

    /// <summary>
    /// Sets the transport security configuration.
    /// </summary>
    /// <param name="transportSecurity">
    /// The transport security configuration to use.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConnectionOptionsBuilder WithTransportSecurity(TransportSecurity transportSecurity) =>
        With(options => options with { Security = options.Security with { Transport = transportSecurity } });

    /// <summary>
    /// Configures the client to use insecure transport (no TLS).
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public ConnectionOptionsBuilder WithInsecureTransport() =>
        With(options => options with { Security = options.Security with { Transport = TransportSecurity.Insecure } });

    /// <summary>
    /// Sets the security options.
    ///
    /// </summary>
    /// <param name="securityOptions">
    /// The security options to set. If provided, this will override the existing security options.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConnectionOptionsBuilder WithSecurity(KurrentClientSecurityOptions securityOptions) =>
        With(options => options with { Security =  securityOptions });

    /// <summary>
    /// Configures the existing security options.
    /// </summary>
    /// <param name="configure">
    /// A function to configure the existing <see cref="KurrentClientSecurityOptions"/>.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConnectionOptionsBuilder ConfigureSecurity(Func<KurrentClientSecurityOptions, KurrentClientSecurityOptions> configure) =>
        With(options => options with { Security = configure(options.Security) });
}
