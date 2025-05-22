using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace KurrentDB.Client;

/// <summary>
/// A fluent builder for creating instances of <see cref="KurrentDBClientConnectivitySettings"/>.
/// </summary>
[PublicAPI]
public class KurrentDBClientConnectivitySettingsBuilder {
	readonly KurrentDBClientConnectivitySettings _settings;

	/// <summary>
	/// Initializes a new instance of the <see cref="KurrentDBClientConnectivitySettingsBuilder"/> class.
	/// </summary>
	public KurrentDBClientConnectivitySettingsBuilder() =>
		_settings = KurrentDBClientConnectivitySettings.Default;

	/// <summary>
	/// Sets the address of the KurrentDB server for a single node connection.
	/// </summary>
	/// <param name="uri">The URI of the KurrentDB server.</param>
	/// <returns>The builder for method chaining.</returns>
	public KurrentDBClientConnectivitySettingsBuilder WithAddress(Uri uri) {
		_settings.Address = uri;
		return this;
	}

	/// <summary>
	/// Sets the address of the KurrentDB server for a single node connection.
	/// </summary>
	/// <param name="uriString">The URI string of the KurrentDB server.</param>
	/// <returns>The builder for method chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="uriString"/> is null.</exception>
	/// <exception cref="UriFormatException">Thrown when <paramref name="uriString"/> is not a valid URI.</exception>
	public KurrentDBClientConnectivitySettingsBuilder WithAddress(string uriString) {
		if (string.IsNullOrWhiteSpace(uriString))
			throw new ArgumentNullException(nameof(uriString));

		return WithAddress(new Uri(uriString));
	}

	/// <summary>
	/// Sets the maximum number of times to attempt endpoint discovery.
	/// </summary>
	/// <param name="maxDiscoverAttempts">The maximum number of discovery attempts.</param>
	/// <returns>The builder for method chaining.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxDiscoverAttempts"/> is less than 1.</exception>
	public KurrentDBClientConnectivitySettingsBuilder WithMaxDiscoverAttempts(int maxDiscoverAttempts) {
		if (maxDiscoverAttempts < 1)
			throw new ArgumentOutOfRangeException(nameof(maxDiscoverAttempts), "Max discover attempts must be at least 1.");

		_settings.MaxDiscoverAttempts = maxDiscoverAttempts;
		return this;
	}

	/// <summary>
	/// Sets DNS endpoints to use for seeding gossip.
	/// </summary>
	/// <param name="endpoints">An array of DNS endpoints.</param>
	/// <returns>The builder for method chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="endpoints"/> is null.</exception>
	public KurrentDBClientConnectivitySettingsBuilder WithDnsGossipSeeds(DnsEndPoint[] endpoints) {
		_settings.DnsGossipSeeds = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
		return this;
	}

	/// <summary>
	/// Sets a single DNS endpoint to use for seeding gossip.
	/// </summary>
	/// <param name="host">The host name.</param>
	/// <param name="port">The port number.</param>
	/// <returns>The builder for method chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> is null.</exception>
	public KurrentDBClientConnectivitySettingsBuilder WithDnsGossipSeed(string host, int port) {
		if (string.IsNullOrWhiteSpace(host))
			throw new ArgumentNullException(nameof(host));

		_settings.DnsGossipSeeds = new[] { new DnsEndPoint(host, port) };
		return this;
	}

	/// <summary>
	/// Sets IP endpoints to use for seeding gossip.
	/// </summary>
	/// <param name="endpoints">An array of IP endpoints.</param>
	/// <returns>The builder for method chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="endpoints"/> is null.</exception>
	public KurrentDBClientConnectivitySettingsBuilder WithIpGossipSeeds(IPEndPoint[] endpoints) {
		_settings.IpGossipSeeds = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
		return this;
	}

	/// <summary>
	/// Sets the gossip timeout.
	/// </summary>
	/// <param name="timeout">The timeout for gossip operations.</param>
	/// <returns>The builder for method chaining.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> is negative.</exception>
	public KurrentDBClientConnectivitySettingsBuilder WithGossipTimeout(TimeSpan timeout) {
		if (timeout < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout cannot be negative.");

		_settings.GossipTimeout = timeout;
		return this;
	}

	/// <summary>
	/// Sets the interval for discovery polling.
	/// </summary>
	/// <param name="interval">The interval between discovery attempts.</param>
	/// <returns>The builder for method chaining.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="interval"/> is negative.</exception>
	public KurrentDBClientConnectivitySettingsBuilder WithDiscoveryInterval(TimeSpan interval) {
		if (interval < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(interval), "Interval cannot be negative.");

		_settings.DiscoveryInterval = interval;
		return this;
	}

	/// <summary>
	/// Sets the node preference used when connecting.
	/// </summary>
	/// <param name="preference">The node preference.</param>
	/// <returns>The builder for method chaining.</returns>
	public KurrentDBClientConnectivitySettingsBuilder WithNodePreference(NodePreference preference) {
		_settings.NodePreference = preference;
		return this;
	}

	/// <summary>
	/// Sets the keepalive interval.
	/// </summary>
	/// <param name="interval">The interval for sending keepalive pings.</param>
	/// <returns>The builder for method chaining.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="interval"/> is negative.</exception>
	public KurrentDBClientConnectivitySettingsBuilder WithKeepAliveInterval(TimeSpan interval) {
		if (interval < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(interval), "Interval cannot be negative.");

		_settings.KeepAliveInterval = interval;
		return this;
	}

	/// <summary>
	/// Sets the keepalive timeout.
	/// </summary>
	/// <param name="timeout">The time after which a keepalive ping is considered timed out.</param>
	/// <returns>The builder for method chaining.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> is negative.</exception>
	public KurrentDBClientConnectivitySettingsBuilder WithKeepAliveTimeout(TimeSpan timeout) {
		if (timeout < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout cannot be negative.");

		_settings.KeepAliveTimeout = timeout;
		return this;
	}

	/// <summary>
	/// Sets whether to use an insecure channel.
	/// </summary>
	/// <param name="insecure">True to use an insecure channel, false otherwise.</param>
	/// <returns>The builder for method chaining.</returns>
	public KurrentDBClientConnectivitySettingsBuilder WithInsecure(bool insecure) {
		_settings.Insecure = insecure;
		return this;
	}

	/// <summary>
	/// Sets whether to verify TLS certificates.
	/// </summary>
	/// <param name="verify">True to verify certificates, false otherwise.</param>
	/// <returns>The builder for method chaining.</returns>
	public KurrentDBClientConnectivitySettingsBuilder WithTlsVerifyCert(bool verify) {
		_settings.TlsVerifyCert = verify;
		return this;
	}

	/// <summary>
	/// Sets the CA file for TLS connections.
	/// </summary>
	/// <param name="certificate">The certificate to use.</param>
	/// <returns>The builder for method chaining.</returns>
	public KurrentDBClientConnectivitySettingsBuilder WithTlsCaFile(X509Certificate2 certificate) {
		_settings.TlsCaFile = certificate;
		return this;
	}

	/// <summary>
	/// Sets the client certificate for authentication.
	/// </summary>
	/// <param name="certificate">The client certificate to use.</param>
	/// <returns>The builder for method chaining.</returns>
	public KurrentDBClientConnectivitySettingsBuilder WithClientCertificate(X509Certificate2 certificate) {
		_settings.ClientCertificate = certificate;
		return this;
	}

	/// <summary>
	/// Builds and returns the configured <see cref="KurrentDBClientConnectivitySettings"/> instance.
	/// </summary>
	/// <returns>A configured <see cref="KurrentDBClientConnectivitySettings"/> instance.</returns>
	public KurrentDBClientConnectivitySettings Build() => _settings;
}
