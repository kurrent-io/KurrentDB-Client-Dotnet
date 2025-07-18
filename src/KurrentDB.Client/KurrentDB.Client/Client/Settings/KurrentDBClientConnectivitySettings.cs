using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace KurrentDB.Client;

/// <summary>
/// A class used to describe how to connect to an instance of KurrentDB.
/// </summary>
public class KurrentDBClientConnectivitySettings {
	/// <summary>
	/// The default port used for connecting to KurrentDB.
	/// </summary>
	public const int DefaultPort = 2113;

	// bool _insecure;

	/// <summary>
	/// The <see cref="Uri"/> of the KurrentDB. Use this when connecting to a single node.
	/// </summary>
	public Uri? Address {
		get => IsSingleNode ? field : null;
		set;
	}

	internal Uri ResolvedAddressOrDefault => Address ?? DefaultAddress;

	Uri DefaultAddress =>
		new UriBuilder {
			Scheme = Insecure ? Uri.UriSchemeHttp : Uri.UriSchemeHttps,
			Port   = DefaultPort
		}.Uri;

	/// <summary>
	/// The maximum number of times to attempt <see cref="EndPoint"/> discovery.
	/// </summary>
	public int MaxDiscoverAttempts { get; set; }

	// /// <summary>
	// /// An array of <see cref="EndPoint"/>s used to seed gossip.
	// /// </summary>
	// public EndPoint[] GossipSeeds =>
	// 	((object?)DnsGossipSeeds ?? IpGossipSeeds) switch {
	// 		DnsEndPoint[] dns => Array.ConvertAll<DnsEndPoint, EndPoint>(dns, x => x),
	// 		IPEndPoint[] ip   => Array.ConvertAll<IPEndPoint, EndPoint>(ip, x => x),
	// 		_                 => []
	// 	};

	public DnsEndPoint[] GossipSeeds { get; set; } = [];

	// public DnsEndPoint[] GossipSeeds =>
	// 	DnsGossipSeeds.Concat(IpGossipSeeds.Select(x => new DnsEndPoint(x.Address.ToString(), x.Port))).ToArray();

	// /// <summary>
	// /// An array of <see cref="DnsEndPoint"/>s to use for seeding gossip. This will be checked before <see cref="IpGossipSeeds"/>.
	// /// </summary>
	// public DnsEndPoint[] DnsGossipSeeds { get; set; } = [];
	//
	// /// <summary>
	// /// An array of <see cref="IPEndPoint"/>s to use for seeding gossip. This will be checked after <see cref="DnsGossipSeeds"/>.
	// /// </summary>
	// public IPEndPoint[] IpGossipSeeds { get; set; } = [];

	/// <summary>
	/// The <see cref="TimeSpan"/> after which an attempt to discover gossip will fail.
	/// </summary>
	public TimeSpan GossipTimeout { get; set; }

	// /// <summary>
	// /// Whether or not to use HTTPS when communicating via gossip.
	// /// </summary>
	// [Obsolete]
	// public bool GossipOverHttps { get; set; } = true;

	/// <summary>
	/// The polling interval used to discover the <see cref="EndPoint"/>.
	/// </summary>
	public TimeSpan DiscoveryInterval { get; set; }

	/// <summary>
	/// The <see cref="NodePreference"/> to use when connecting.
	/// </summary>
	public NodePreference NodePreference { get; set; }

	/// <summary>
	/// The optional amount of time to wait after which a keepalive ping is sent on the transport.
	/// </summary>
	public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(10);

	/// <summary>
	/// The optional amount of time to wait after which a sent keepalive ping is considered timed out.
	/// </summary>
	public TimeSpan KeepAliveTimeout { get; set; } = TimeSpan.FromSeconds(10);

	/// <summary>
	/// True if pointing to a single KurrentDB node.
	/// </summary>
	public bool IsSingleNode => GossipSeeds.Length == 0;

	/// <summary>
	/// True if communicating over an insecure channel; otherwise false.
	/// </summary>
	public bool Insecure {
		get => IsSingleNode ? string.Equals(Address?.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) : field;
		set;
	}

	/// <summary>
	/// True if certificates are validated; otherwise false.
	/// </summary>
	public bool TlsVerifyCert { get; set; } = true;

	/// <summary>
	/// Path to a certificate file for secure connection. Not required for enabling secure connection.
	/// Useful for a self-signed certificate not installed on the system trust store.
	/// </summary>
	public X509Certificate2? TlsCaFile { get; set; }

	/// <summary>
	/// Client certificate used for user authentication.
	/// </summary>
	public X509Certificate2? ClientCertificate { get; set; }

	// /// <summary>
	// /// Configuration for SSL/TLS credentials, including client and root certificates,
	// /// as well as server certificate verification settings.
	// /// </summary>
	// public SslCredentialsSettings SslCredentials { get; set; } = new();

	/// <summary>
	/// The default <see cref="KurrentDBClientConnectivitySettings"/>.
	/// </summary>
	public static KurrentDBClientConnectivitySettings Default => new() {
		MaxDiscoverAttempts = 10,
		GossipTimeout       = TimeSpan.FromSeconds(5),
		DiscoveryInterval   = TimeSpan.FromMilliseconds(100),
		NodePreference      = NodePreference.Leader,
		KeepAliveInterval   = TimeSpan.FromSeconds(10),
		KeepAliveTimeout    = TimeSpan.FromSeconds(10),
		TlsVerifyCert       = true
	};

	/// <summary>
	/// Creates a new instance with the same settings as this instance.
	/// </summary>
	/// <returns>A new <see cref="KurrentDBClientConnectivitySettings"/> with the same settings.</returns>
	public KurrentDBClientConnectivitySettings Clone() => new() {
		Address             = Address,
		MaxDiscoverAttempts = MaxDiscoverAttempts,
		GossipSeeds         = GossipSeeds.ToArray(),
		GossipTimeout       = GossipTimeout,
		DiscoveryInterval   = DiscoveryInterval,
		NodePreference      = NodePreference,
		KeepAliveInterval   = KeepAliveInterval,
		KeepAliveTimeout    = KeepAliveTimeout,
		TlsVerifyCert       = TlsVerifyCert,
		TlsCaFile           = TlsCaFile,
		ClientCertificate   = ClientCertificate,
		Insecure            = Insecure
	};
}
