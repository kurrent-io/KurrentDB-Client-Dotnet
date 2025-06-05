// using System.Collections.Immutable;
// using System.Globalization;
// using System.Net;
// using System.Text.RegularExpressions;
// using System.Web;
//
// namespace KurrentDB.Client.Next;
//
// /// <summary>
// /// Comprehensive configuration settings for KurrentDB connections
// /// </summary>
// /// <param name="Scheme">The connection scheme (direct or discover)</param>
// /// <param name="Endpoints">List of cluster endpoints</param>
// /// <param name="UserCredentials">Basic authentication credentials</param>
// /// <param name="CertificateCredentials">X.509 certificate authentication credentials</param>
// /// <param name="Tls">TLS (Transport Layer Security) configuration</param>
// /// <param name="Gossip">Gossip protocol settings for cluster discovery</param>
// /// <param name="ConnectionName">Optional connection identifier</param>
// /// <param name="NodePreference">Preferred node type for routing requests</param>
// /// <param name="DefaultDeadline">Default timeout for client operations</param>
// /// <param name="KeepAliveInterval">Interval between keep-alive ping calls</param>
// /// <param name="KeepAliveTimeout">Timeout for keep-alive ping calls</param>
// public readonly record struct KurrentClientConnectionSettings(
// 	ConnectionScheme Scheme,
// 	ImmutableArray<EndPoint> Endpoints,
// 	UserCredentials UserCredentials = default,
// 	CertificateCredentials CertificateCredentials = default,
// 	TlsSettings Tls = default,
// 	GossipSettings Gossip = default,
// 	string? ConnectionName = null,
// 	NodePreference NodePreference = NodePreference.Leader,
// 	TimeSpan? DefaultDeadline = null,
// 	TimeSpan KeepAliveInterval = default,
// 	TimeSpan KeepAliveTimeout = default
// ) {
// 	/// <summary>
// 	/// Default keep-alive interval (10 seconds)
// 	/// </summary>
// 	public static readonly TimeSpan DefaultKeepAliveInterval = TimeSpan.FromSeconds(10);
//
// 	/// <summary>
// 	/// Default keep-alive timeout (10 seconds)
// 	/// </summary>
// 	public static readonly TimeSpan DefaultKeepAliveTimeout = TimeSpan.FromSeconds(10);
//
// 	// /// <summary>
// 	// /// Gets the effective TLS settings, using defaults if not specified
// 	// /// </summary>
// 	// public TlsSettings EffectiveTls =>
// 	// 	Tls.Equals(default(TlsSettings)) ? TlsSettings.Default : Tls;
//
// 	public TlsSettings EffectiveTls => Tls;
//
// 	/// <summary>
// 	/// Gets the effective gossip settings, using defaults if not specified
// 	/// </summary>
// 	public GossipSettings EffectiveGossip =>
// 		Gossip.Equals(default(GossipSettings)) ? new() : Gossip;
//
// 	/// <summary>
// 	/// Gets the keep-alive interval, using default if not specified
// 	/// </summary>
// 	public TimeSpan EffectiveKeepAliveInterval =>
// 		KeepAliveInterval == TimeSpan.Zero ? DefaultKeepAliveInterval : KeepAliveInterval;
//
// 	/// <summary>
// 	/// Gets the keep-alive timeout, using default if not specified
// 	/// </summary>
// 	public TimeSpan EffectiveKeepAliveTimeout =>
// 		KeepAliveTimeout == TimeSpan.Zero ? DefaultKeepAliveTimeout : KeepAliveTimeout;
//
// 	/// <summary>
// 	/// Gets the primary endpoint (first in the list)
// 	/// </summary>
// 	public EndPoint? PrimaryEndpoint =>
// 		Endpoints.IsEmpty ? null : Endpoints[0];
//
// 	/// <summary>
// 	/// Indicates whether this is a cluster connection
// 	/// </summary>
// 	public bool IsClusterConnection => Scheme == ConnectionScheme.Discover;
//
// 	/// <summary>
// 	/// Indicates whether authentication is configured
// 	/// </summary>
// 	public bool HasAuthentication => !UserCredentials.IsEmpty || !CertificateCredentials.IsEmpty;
//
// 	/// <summary>
// 	/// Indicates whether X.509 certificate authentication is configured
// 	/// </summary>
// 	public bool HasCertificateAuthentication => !CertificateCredentials.IsEmpty;
//
// 	/// <summary>
// 	/// Returns the connection string representation
// 	/// </summary>
// 	/// <returns>Connection string representation</returns>
// 	public override string ToString() => ToConnectionString(this);
//
// 	/// <summary>
// 	/// Validates the connection settings for correctness and completeness
// 	/// </summary>
// 	public ValidationResult Validate() =>
// 		KurrentDBConnectionString.Validate(this);
//
// 	public static KurrentClientConnectionSettings Parse(string connectionString) =>
// 		KurrentDBConnectionString.Parse(connectionString);
//
// 	public static bool TryParse(string connectionString, out KurrentClientConnectionSettings settings) =>
// 		KurrentDBConnectionString.TryParse(connectionString, out settings);
//
// 	static string ToConnectionString(KurrentClientConnectionSettings settings) {
// 		var scheme = settings.Scheme switch {
// 			ConnectionScheme.Direct   => "kurrentdb",
// 			ConnectionScheme.Discover => "kurrentdb+discover",
// 			_                         => throw new ArgumentException($"Unknown scheme: {settings.Scheme}")
// 		};
//
// 		var authority = string.Empty;
// 		if (settings is { HasAuthentication: true, HasCertificateAuthentication: false }) {
// 			var username = Uri.EscapeDataString(settings.UserCredentials.Username ?? string.Empty);
// 			var password = string.IsNullOrEmpty(settings.UserCredentials.Password)
// 				? string.Empty
// 				: $":{Uri.EscapeDataString(settings.UserCredentials.Password)}";
//
// 			authority = $"{username}{password}@";
// 		}
//
// 		var hosts = string.Join(",", settings.Endpoints.Select(FormatEndPoint));
//
// 		var queryParams = new List<string>();
//
// 		if (!settings.EffectiveTls.Enabled)
// 			queryParams.Add("tls=false");
//
// 		if (settings.EffectiveTls is { VerifyCertificate: false, Enabled: true })
// 			queryParams.Add("tlsVerifyCert=false");
//
// 		if (!string.IsNullOrEmpty(settings.ConnectionName))
// 			queryParams.Add($"connectionName={Uri.EscapeDataString(settings.ConnectionName)}");
//
// 		if (settings.NodePreference != NodePreference.Leader) {
// 			var preference = settings.NodePreference switch {
// 				NodePreference.Follower        => "follower",
// 				NodePreference.Random          => "random",
// 				NodePreference.ReadOnlyReplica => "readOnlyReplica",
// 				_                              => "leader"
// 			};
//
// 			queryParams.Add($"nodePreference={preference}");
// 		}
//
// 		if (settings.EffectiveGossip.MaxDiscoverAttempts != 10)
// 			queryParams.Add($"maxDiscoverAttempts={settings.EffectiveGossip.MaxDiscoverAttempts}");
//
// 		if (settings.Gossip.DiscoveryInterval != TimeSpan.Zero)
// 			queryParams.Add($"discoveryInterval={settings.Gossip.DiscoveryInterval.TotalMilliseconds:F0}");
//
// 		if (settings.Gossip.Timeout != TimeSpan.Zero)
// 			queryParams.Add($"gossipTimeout={settings.Gossip.Timeout.TotalSeconds:F0}");
//
// 		if (settings.DefaultDeadline.HasValue)
// 			queryParams.Add($"defaultDeadline={settings.DefaultDeadline.Value.TotalMilliseconds:F0}");
//
// 		if (settings.KeepAliveInterval != TimeSpan.Zero)
// 			queryParams.Add($"keepAliveInterval={settings.KeepAliveInterval.TotalSeconds:F0}");
//
// 		if (settings.KeepAliveTimeout != TimeSpan.Zero)
// 			queryParams.Add($"keepAliveTimeout={settings.KeepAliveTimeout.TotalSeconds:F0}");
//
// 		if (!string.IsNullOrEmpty(settings.EffectiveTls.CaFile))
// 			queryParams.Add($"tlsCaFile={Uri.EscapeDataString(settings.EffectiveTls.CaFile)}");
//
// 		if (!string.IsNullOrEmpty(settings.CertificateCredentials.CertificateFile))
// 			queryParams.Add($"userCertFile={Uri.EscapeDataString(settings.CertificateCredentials.CertificateFile)}");
//
// 		if (!string.IsNullOrEmpty(settings.CertificateCredentials.KeyFile))
// 			queryParams.Add($"userKeyFile={Uri.EscapeDataString(settings.CertificateCredentials.KeyFile)}");
//
// 		var query = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : string.Empty;
//
// 		return $"{scheme}://{authority}{hosts}{query}";
//
// 		static string FormatEndPoint(EndPoint endpoint) =>
// 			endpoint switch {
// 				DnsEndPoint dns => $"{dns.Host}:{dns.Port}",
// 				IPEndPoint ip   => $"{ip.Address}:{ip.Port}",
// 				_               => endpoint.ToString() ?? string.Empty
// 			};
// 	}
// }
//
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
//
// /// <summary>
// /// Represents gossip protocol settings for cluster discovery
// /// </summary>
// /// <param name="MaxDiscoverAttempts">Maximum number of discovery attempts</param>
// /// <param name="DiscoveryInterval">Interval between discovery attempts</param>
// /// <param name="Timeout">Timeout for gossip protocol requests</param>
// public record struct GossipSettings(int MaxDiscoverAttempts = 10, TimeSpan DiscoveryInterval = default, TimeSpan Timeout = default) {
// 	/// <summary>
// 	/// Default discovery interval (100ms)
// 	/// </summary>
// 	public static readonly TimeSpan DefaultDiscoveryInterval = TimeSpan.FromMilliseconds(100);
//
// 	/// <summary>
// 	/// Default gossip timeout (5 seconds)
// 	/// </summary>
// 	public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
//
// 	/// <summary>
// 	/// Gets the discovery interval, using default if not specified
// 	/// </summary>
// 	public TimeSpan EffectiveDiscoveryInterval =>
// 		DiscoveryInterval == TimeSpan.Zero ? DefaultDiscoveryInterval : DiscoveryInterval;
//
// 	/// <summary>
// 	/// Gets the gossip timeout, using default if not specified
// 	/// </summary>
// 	public TimeSpan EffectiveTimeout =>
// 		Timeout == TimeSpan.Zero ? DefaultTimeout : Timeout;
// }
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
//
// /// <summary>
// /// Exception thrown when connection string parsing fails
// /// </summary>
// public sealed class KurrentDBConnectionStringException : Exception {
// 	public KurrentDBConnectionStringException(string message) : base(message) { }
//
// 	public KurrentDBConnectionStringException(string message, string? connectionString, string? parameter = null)
// 		: base(message) {
// 		ConnectionString = connectionString;
// 		Parameter        = parameter;
// 	}
//
// 	public KurrentDBConnectionStringException(string message, Exception innerException)
// 		: base(message, innerException) { }
//
// 	public KurrentDBConnectionStringException(string message, string? connectionString, string? parameter, Exception innerException)
// 		: base(message, innerException) {
// 		ConnectionString = connectionString;
// 		Parameter        = parameter;
// 	}
//
// 	public string? ConnectionString { get; }
// 	public string? Parameter        { get; }
// }
//
// /// <summary>
// /// Validation result for connection string parsing
// /// </summary>
// /// <param name="IsValid">Whether the validation passed</param>
// /// <param name="Errors">Collection of validation errors</param>
// public readonly record struct ValidationResult(bool IsValid, ImmutableArray<string> Errors) {
// 	public static ValidationResult Success() => new(true, ImmutableArray<string>.Empty);
//
// 	public static ValidationResult Failure(params string[] errors) =>
// 		new(false, [..errors]);
//
// 	public static ValidationResult Failure(IEnumerable<string> errors) =>
// 		new(false, [..errors]);
// }
//
// /// <summary>
// /// Production-ready KurrentDB connection string parser and settings factory
// /// </summary>
// static partial class KurrentDBConnectionString {
// 	static readonly Regex ConnectionStringRegex = GetConnectionStringRegex();
//
// 	static readonly IDictionary<string, NodePreference> NodePreferenceMap =
// 		new Dictionary<string, NodePreference>(StringComparer.OrdinalIgnoreCase) {
// 			["leader"]          = NodePreference.Leader,
// 			["follower"]        = NodePreference.Follower,
// 			["random"]          = NodePreference.Random,
// 			["readonlyreplica"] = NodePreference.ReadOnlyReplica,
// 			["readonly"]        = NodePreference.ReadOnlyReplica
// 		};
//
// 	/// <summary>
// 	/// Parses a KurrentDB connection string into connection settings
// 	/// </summary>
// 	/// <param name="connectionString">The connection string to parse</param>
// 	/// <returns>Parsed connection settings</returns>
// 	/// <exception cref="ArgumentException">Thrown when connection string is null or empty</exception>
// 	/// <exception cref="KurrentDBConnectionStringException">Thrown when parsing fails</exception>
// 	public static KurrentClientConnectionSettings Parse(string connectionString) {
// 		ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
//
// 		try {
// 			// First validate the format with regex
// 			var match = ConnectionStringRegex.Match(connectionString.Trim());
// 			if (!match.Success)
// 				throw new KurrentDBConnectionStringException(
// 					"Invalid connection string format. Expected format: kurrentdb[+discover]://[username:password@]host[:port][,host2[:port2]][?param=value]",
// 					connectionString
// 				);
//
// 			// Then use Uri for robust parsing
// 			Uri uri;
// 			try {
//
// 				var temp = connectionString
// 					.Replace(" ", "")
// 					.Replace(",", "%2C");
//
// 				uri = new Uri(temp, UriKind.Absolute);
// 			}
// 			catch (Exception ex) {
// 				throw new KurrentDBConnectionStringException(ex.Message, ex);
// 			}
//
// 			var scheme          = ParseScheme(uri.Scheme, connectionString);
// 			var endpoints       = ParseEndpoints(uri, connectionString);
// 			var userCredentials = ParseUserCredentials(uri);
// 			var parameters      = ParseQueryParameters(uri.Query, connectionString);
//
// 			var certificateCredentials = new CertificateCredentials(
// 				parameters.GetValueOrDefault<string?>("userCertFile", null),
// 				parameters.GetValueOrDefault<string?>("userKeyFile", null)
// 			);
//
// 			var tlsSettings = new TlsSettings(
// 				parameters.GetValueOrDefault("tls", true),
// 				parameters.GetValueOrDefault("tlsVerifyCert", true),
// 				parameters.GetValueOrDefault<string?>("tlsCaFile", null)
// 			);
//
// 			var gossipSettings = new GossipSettings(
// 				parameters.GetValueOrDefault("maxDiscoverAttempts", 10),
// 				parameters.GetValueOrDefault("discoveryInterval", TimeSpan.Zero),
// 				parameters.GetValueOrDefault("gossipTimeout", TimeSpan.Zero)
// 			);
//
// 			var settings = new KurrentClientConnectionSettings(
// 				scheme,
// 				endpoints,
// 				userCredentials,
// 				certificateCredentials,
// 				tlsSettings,
// 				gossipSettings,
// 				parameters.GetValueOrDefault<string?>("connectionName", null),
// 				parameters.GetValueOrDefault("nodePreference", NodePreference.Leader),
// 				parameters.GetValueOrDefault<TimeSpan?>("defaultDeadline", null),
// 				parameters.GetValueOrDefault("keepAliveInterval", TimeSpan.Zero),
// 				parameters.GetValueOrDefault("keepAliveTimeout", TimeSpan.Zero)
// 			);
//
// 			var validationResult = Validate(settings);
// 			if (!validationResult.IsValid)
// 				throw new KurrentDBConnectionStringException(
// 					$"Connection string validation failed: {string.Join(", ", validationResult.Errors)}",
// 					connectionString
// 				);
//
// 			return settings;
// 		}
// 		catch (KurrentDBConnectionStringException) {
// 			throw;
// 		}
// 		catch (Exception ex) {
// 			throw new KurrentDBConnectionStringException("Failed to parse connection string", connectionString, null, ex);
// 		}
// 	}
//
// 	/// <summary>
// 	/// Attempts to parse a connection string, returning success status and settings
// 	/// </summary>
// 	/// <param name="connectionString">The connection string to parse</param>
// 	/// <param name="settings">The parsed settings if successful</param>
// 	/// <returns>True if parsing succeeded, false otherwise</returns>
// 	public static bool TryParse(string connectionString, out KurrentClientConnectionSettings settings) {
// 		try {
// 			settings = Parse(connectionString);
// 			return true;
// 		}
// 		catch {
// 			settings = default;
// 			return false;
// 		}
// 	}
//
// 	internal static ValidationResult Validate(KurrentClientConnectionSettings settings) {
// 		var errors = new List<string>();
//
// 		if (settings.Endpoints.IsEmpty)
// 			errors.Add("At least one endpoint must be specified");
//
// 		foreach (var (endpoint, index) in settings.Endpoints.Select((e, i) => (e, i)))
// 			if (endpoint is DnsEndPoint dnsEndpoint) {
// 				if (string.IsNullOrWhiteSpace(dnsEndpoint.Host))
// 					errors.Add($"Endpoint {index}: Host cannot be empty");
//
// 				if (dnsEndpoint.Port is <= 0 or > 65535)
// 					errors.Add($"Endpoint {index}: Port must be between 1 and 65535");
// 			}
// 			else if (endpoint is IPEndPoint ipEndpoint) {
// 				if (ipEndpoint.Port is <= 0 or > 65535)
// 					errors.Add($"Endpoint {index}: Port must be between 1 and 65535");
// 			}
//
// 		if (settings.EffectiveGossip.MaxDiscoverAttempts <= 0)
// 			errors.Add("MaxDiscoverAttempts must be greater than 0");
//
// 		if (settings.EffectiveGossip.EffectiveDiscoveryInterval <= TimeSpan.Zero)
// 			errors.Add("DiscoveryInterval must be greater than zero");
//
// 		if (settings.EffectiveGossip.EffectiveTimeout <= TimeSpan.Zero)
// 			errors.Add("GossipTimeout must be greater than zero");
//
// 		if (settings.EffectiveKeepAliveInterval <= TimeSpan.Zero)
// 			errors.Add("KeepAliveInterval must be greater than zero");
//
// 		if (settings.EffectiveKeepAliveTimeout <= TimeSpan.Zero)
// 			errors.Add("KeepAliveTimeout must be greater than zero");
//
// 		if (settings.DefaultDeadline <= TimeSpan.Zero)
// 			errors.Add("DefaultDeadline must be greater than zero when specified");
//
// 		if (!string.IsNullOrEmpty(settings.CertificateCredentials.CertificateFile) && string.IsNullOrEmpty(settings.CertificateCredentials.KeyFile))
// 			errors.Add("KeyFile must be specified when CertificateFile is provided");
//
// 		if (!string.IsNullOrEmpty(settings.CertificateCredentials.KeyFile) && string.IsNullOrEmpty(settings.CertificateCredentials.CertificateFile))
// 			errors.Add("CertificateFile must be specified when KeyFile is provided");
//
// 		if (!string.IsNullOrEmpty(settings.EffectiveTls.CaFile) && !settings.EffectiveTls.Enabled)
// 			errors.Add("TlsCaFile can only be used with TLS enabled");
//
// 		if (settings.EffectiveTls is { VerifyCertificate: false, Enabled: false })
// 			errors.Add("TlsVerifyCert can only be configured when TLS is enabled");
//
// 		return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
// 	}
//
// 	static ConnectionScheme ParseScheme(string scheme, string connectionString) =>
// 		scheme.ToLowerInvariant() switch {
// 			"kurrentdb"          => ConnectionScheme.Direct,
// 			"kurrentdb+discover" => ConnectionScheme.Discover,
// 			_ => throw new KurrentDBConnectionStringException(
// 				$"Unsupported scheme '{scheme}'. Supported schemes: kurrentdb, kurrentdb+discover",
// 				connectionString, "scheme"
// 			)
// 		};
//
// 	static ImmutableArray<EndPoint> ParseEndpoints(Uri uri, string connectionString) {
// 		var hostAndPort = uri.Host;
// 		var port        = uri.Port != -1 ? uri.Port : 2113;
//
// 		// Single host from Uri
// 		if (!hostAndPort.Contains(','))
// 			return [CreateEndPoint(uri.Host, port)];
//
// 		// Handle multiple hosts in the host part (comma-separated)
// 		var endpoints = new List<EndPoint>();
// 		var hostParts = hostAndPort.Split(',', StringSplitOptions.RemoveEmptyEntries);
//
// 		foreach (var hostPart in hostParts) {
// 			var trimmedHost = hostPart.Trim();
// 			if (string.IsNullOrEmpty(trimmedHost))
// 				continue;
//
// 			var colonIndex = trimmedHost.LastIndexOf(':');
// 			if (colonIndex == -1)
// 				endpoints.Add(CreateEndPoint(trimmedHost, 2113)); // No port specified, use default
// 			else {
// 				var host       = trimmedHost[..colonIndex];
// 				var portString = trimmedHost[(colonIndex + 1)..];
//
// 				if (string.IsNullOrWhiteSpace(host))
// 					throw new KurrentDBConnectionStringException($"Invalid host specification: '{hostPart}'", connectionString, "hosts");
//
// 				if (!int.TryParse(portString, out var hostPort) || hostPort <= 0 || hostPort > 65535)
// 					throw new KurrentDBConnectionStringException(
// 						$"Invalid port specification: '{portString}'. Port must be between 1 and 65535",
// 						connectionString, "hosts"
// 					);
//
// 				endpoints.Add(CreateEndPoint(host, hostPort));
// 			}
// 		}
//
// 		if (endpoints.Count == 0)
// 			throw new KurrentDBConnectionStringException("At least one valid endpoint must be specified", connectionString, "hosts");
//
// 		return [..endpoints];
//
// 	}
//
// 	static EndPoint CreateEndPoint(string host, int port) =>
// 		IPAddress.TryParse(host, out var ipAddress)
// 			? new IPEndPoint(ipAddress, port)
// 			: new DnsEndPoint(host, port);
//
// 	static UserCredentials ParseUserCredentials(Uri uri) {
// 		var userInfo = uri.UserInfo;
// 		if (string.IsNullOrEmpty(userInfo))
// 			return default;
//
// 		var parts    = userInfo.Split(':', 2);
// 		var username = Uri.UnescapeDataString(parts[0]);
// 		var password = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : null;
//
// 		return new(username, password);
// 	}
//
// 	static ImmutableDictionary<string, object?> ParseQueryParameters(string query, string connectionString) {
// 		var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
//
// 		if (string.IsNullOrWhiteSpace(query))
// 			return parameters.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
//
// 		var collection = HttpUtility.ParseQueryString(query);
//
// 		foreach (var key in collection.AllKeys) {
// 			if (string.IsNullOrEmpty(key))
// 				continue;
//
// 			var value = collection[key];
// 			if (value is null)
// 				continue;
//
// 			if (value.Contains(','))
// 				value = value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim()).Last();
//
// 			try {
// 				parameters[key] = ParseParameterValue(key, value, connectionString);
// 			}
// 			catch (Exception ex) {
// 				throw new KurrentDBConnectionStringException(
// 					$"Failed to parse parameter '{key}' with value '{value}'",
// 					connectionString, key, ex
// 				);
// 			}
// 		}
//
// 		return parameters.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
// 	}
//
// 	static object? ParseParameterValue(string key, string value, string connectionString) {
// 		return key.ToLowerInvariant() switch {
// 			"tls" or "tlsverifycert"                                           => ParseBooleanParameter(key, value, connectionString),
// 			"maxdiscoverattempts"                                              => ParseIntParameter(key, value, connectionString, 1, int.MaxValue),
// 			"discoveryinterval"                                                => ParseTimeSpanParameter(key, value, connectionString, TimeSpan.FromMilliseconds),
// 			"gossiptimeout"                                                    => ParseTimeSpanParameter(key, value, connectionString, TimeSpan.FromSeconds),
// 			"defaultdeadline"                                                  => ParseNullableTimeSpanParameter(key, value, connectionString, TimeSpan.FromMilliseconds),
// 			"keepaliveinterval" or "keepalivetimeout"                          => ParseTimeSpanParameter(key, value, connectionString, TimeSpan.FromSeconds),
// 			"nodepreference"                                                   => ParseNodePreference(key, value, connectionString),
// 			"connectionname" or "tlscafile" or "usercertfile" or "userkeyfile" => string.IsNullOrEmpty(value) ? null : Uri.UnescapeDataString(value),
// 			_                                                                  => value
// 		};
// 	}
//
// 	static bool ParseBooleanParameter(string key, string value, string connectionString) {
// 		if (bool.TryParse(value, out var result))
// 			return result;
//
// 		throw new KurrentDBConnectionStringException(
// 			$"Parameter '{key}' must be 'true' or 'false', got '{value}'",
// 			connectionString, key
// 		);
// 	}
//
// 	static int ParseIntParameter(string key, string value, string connectionString, int min, int max) {
// 		if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) && result >= min && result <= max)
// 			return result;
//
// 		throw new KurrentDBConnectionStringException(
// 			$"Parameter '{key}' must be an integer between {min} and {max}, got '{value}'",
// 			connectionString, key
// 		);
// 	}
//
// 	static TimeSpan ParseTimeSpanParameter(string key, string value, string connectionString, Func<double, TimeSpan> factory) {
// 		if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) &&
// 		    result > 0)
// 			return factory(result);
//
// 		throw new KurrentDBConnectionStringException(
// 			$"Parameter '{key}' must be a positive number, got '{value}'",
// 			connectionString,
// 			key
// 		);
// 	}
//
// 	static TimeSpan? ParseNullableTimeSpanParameter(string key, string value, string connectionString, Func<double, TimeSpan> factory) =>
// 		string.IsNullOrEmpty(value) ? null : ParseTimeSpanParameter(key, value, connectionString, factory);
//
// 	static NodePreference ParseNodePreference(string key, string value, string connectionString) {
// 		if (NodePreferenceMap.TryGetValue(value, out var preference))
// 			return preference;
//
// 		throw new KurrentDBConnectionStringException(
// 			$"Parameter '{key}' must be one of: {string.Join(", ", NodePreferenceMap.Keys)}, got '{value}'",
// 			connectionString, key
// 		);
// 	}
//
// 	[GeneratedRegex(@"^(?<scheme>kurrentdb(\+discover)?):\/\/(?:(?<username>[^:@]+)(?::(?<password>[^@]+))?@)?(?<hosts>[^?]+)(?:\?(?<query>.*))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
// 	private static partial Regex GetConnectionStringRegex();
// }
//
// static class ParameterExtensions {
// 	public static T GetValueOrDefault<T>(this ImmutableDictionary<string, object?> parameters, string key, T defaultValue) =>
// 		parameters.TryGetValue(key, out var value) && value is T typedValue ? typedValue : defaultValue;
// }
