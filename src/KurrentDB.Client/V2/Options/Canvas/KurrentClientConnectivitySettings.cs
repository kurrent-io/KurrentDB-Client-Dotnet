// using System.Net;
// using System.Security.Cryptography.X509Certificates;
//
// namespace Kurrent.Client;
//
// /// <summary>
// /// Represents TLS (Transport Layer Security) configuration settings.
// /// TLS is a cryptographic protocol that provides secure communication over networks,
// /// ensuring data privacy and integrity between client and server.
// /// </summary>
// /// <param name="Enabled">Whether to use TLS encryption</param>
// /// <param name="VerifyCertificate">Whether to verify TLS certificates</param>
// /// <param name="CaFile">Path to custom CA certificate file</param>
// [PublicAPI]
// public record struct KurrentClientTlsOptions(bool Enabled = true, bool VerifyCertificate = true, string? CaFile = null) {
// 	/// <summary>
// 	/// Default TLS settings (enabled with certificate verification)
// 	/// </summary>
// 	public static readonly KurrentClientTlsOptions Default = new(true, true);
//
// 	/// <summary>
// 	/// Insecure TLS settings (disabled) - only use for development
// 	/// </summary>
// 	public static readonly KurrentClientTlsOptions Insecure = new(false, false);
// }
//
//
// /// <summary>
// /// Represents X.509 certificate authentication credentials
// /// </summary>
// /// <param name="CertificateFile">Path to the client certificate file</param>
// /// <param name="KeyFile">Path to the private key file</param>
// [PublicAPI]
// public record CertificateCredentials(string? CertificateFile, string? KeyFile) {
// 	/// <summary>
// 	/// Indicates whether credentials are provided
// 	/// </summary>
// 	public bool IsEmpty => string.IsNullOrEmpty(CertificateFile) || string.IsNullOrEmpty(KeyFile);
// }
//
// /// <summary>
// /// Represents basic authentication credentials (username/password)
// /// </summary>
// /// <param name="Username">The username for authentication</param>
// /// <param name="Password">The password for authentication</param>
// [PublicAPI]
// public record struct UserCredentials(string? Username, string? Password) {
// 	/// <summary>
// 	/// Indicates whether credentials are provided
// 	/// </summary>
// 	public bool IsEmpty => string.IsNullOrEmpty(Username);
// }
//
//
//
// /// <summary>
// /// A class used to describe how to connect to an instance of KurrentDB.
// /// </summary>
// [PublicAPI]
// public class KurrentClientConnectivitySettings {
// 	// public static KurrentClientConnectivitySettingsBuilder Builder => new();
//
// 	/// <summary>
// 	/// The default port used for connecting to KurrentDB.
// 	/// </summary>
// 	public const int DefaultPort = 2113;
//
// 	/// <summary>
// 	/// An array of <see cref="DnsEndPoint"/>s used to seed gossip.
// 	/// </summary>
// 	public DnsEndPoint[] Endpoints { get; set; } = [];
//
// 	/// <summary>
// 	/// Configuration for SSL/TLS credentials, including client and root certificates,
// 	/// as well as server certificate verification settings.
// 	/// </summary>
// 	public KurrentClientSslCredentialsOptions SslCredentials { get; set; } = new();
//
//
//
// 	// bool _insecure;
//
// 	// /// <summary>
// 	// /// The <see cref="Uri"/> of the KurrentDB. Use this when connecting to a single node.
// 	// /// </summary>
// 	// public Uri? Address {
// 	// 	get => IsSingleNode ? field : null;
// 	// 	set;
// 	// }
//
// 	// internal Uri ResolvedAddressOrDefault => Address ?? DefaultAddress;
// 	//
// 	// Uri DefaultAddress =>
// 	// 	new UriBuilder {
// 	// 		Scheme = Insecure ? Uri.UriSchemeHttp : Uri.UriSchemeHttps,
// 	// 		Port   = DefaultPort
// 	// 	}.Uri;
//
//
// 	// /// <summary>
// 	// /// An array of <see cref="EndPoint"/>s used to seed gossip.
// 	// /// </summary>
// 	// public EndPoint[] GossipSeeds =>
// 	// 	((object?)DnsGossipSeeds ?? IpGossipSeeds) switch {
// 	// 		DnsEndPoint[] dns => Array.ConvertAll<DnsEndPoint, EndPoint>(dns, x => x),
// 	// 		IPEndPoint[] ip   => Array.ConvertAll<IPEndPoint, EndPoint>(ip, x => x),
// 	// 		_                 => []
// 	// 	};
//
//
// 	// public DnsEndPoint[] GossipSeeds =>
// 	// 	DnsGossipSeeds.Concat(IpGossipSeeds.Select(x => new DnsEndPoint(x.Address.ToString(), x.Port))).ToArray();
//
// 	// /// <summary>
// 	// /// An array of <see cref="DnsEndPoint"/>s to use for seeding gossip. This will be checked before <see cref="IpGossipSeeds"/>.
// 	// /// </summary>
// 	// public DnsEndPoint[] DnsGossipSeeds { get; set; } = [];
// 	//
// 	// /// <summary>
// 	// /// An array of <see cref="IPEndPoint"/>s to use for seeding gossip. This will be checked after <see cref="DnsGossipSeeds"/>.
// 	// /// </summary>
// 	// public IPEndPoint[] IpGossipSeeds { get; set; } = [];
//
//
// 	// /// <summary>
// 	// /// True if pointing to a single KurrentDB node.
// 	// /// </summary>
// 	// public bool IsSingleNode => GossipSeeds.Length == 0;
// 	//
// 	// /// <summary>
// 	// /// True if communicating over an insecure channel; otherwise false.
// 	// /// </summary>
// 	// public bool Insecure {
// 	// 	get => IsSingleNode ? string.Equals(Address?.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) : field;
// 	// 	set;
// 	// }
//
// 	/// <summary>
// 	/// True if certificates are validated; otherwise false.
// 	/// </summary>
// 	public bool TlsVerifyCert { get; set; } = true;
//
// 	/// <summary>
// 	/// Path to a certificate file for secure connection. Not required for enabling secure connection.
// 	/// Useful for a self-signed certificate not installed on the system trust store.
// 	/// </summary>
// 	public X509Certificate2? TlsCaFile { get; set; }
//
// 	/// <summary>
// 	/// Client certificate used for user authentication.
// 	/// </summary>
// 	public X509Certificate2? ClientCertificate { get; set; }
//
// 	// /// <summary>
// 	// /// The default <see cref="KurrentDBClientConnectivitySettings"/>.
// 	// /// </summary>
// 	// public static KurrentClientConnectivitySettings Default => new() {
// 	// 	MaxDiscoverAttempts = 10,
// 	// 	GossipTimeout       = TimeSpan.FromSeconds(5),
// 	// 	DiscoveryInterval   = TimeSpan.FromMilliseconds(100),
// 	// 	NodeReadPreference  = NodeReadPreference.Leader,
// 	// 	KeepAliveInterval   = TimeSpan.FromSeconds(10),
// 	// 	KeepAliveTimeout    = TimeSpan.FromSeconds(10),
// 	// 	TlsVerifyCert       = true,
// 	// 	SslCredentials      = new()
// 	// };
// }
