// using System.Text;
// using KurrentDB.Client;
//
// namespace KurrentDb.Client;
//
// public class KurrentDBConnectionOptions {
// 	/// <summary>
// 	/// List of server addresses
// 	/// </summary>
// 	public string[] Servers { get; set; } = [];
//
// 	/// <summary>
// 	/// Username for authentication
// 	/// </summary>
// 	public string Username { get; set; } = string.Empty;
//
// 	/// <summary>
// 	/// Password for authentication
// 	/// </summary>
// 	public string Password { get; set; } = string.Empty;
//
// 	/// <summary>
// 	/// Whether to use TLS for secure connections
// 	/// </summary>
// 	public bool UseTls { get; set; } = true;
//
// 	/// <summary>
// 	/// Whether to use discovery mode (kurrentdb+discover:// scheme)
// 	/// </summary>
// 	public bool IsDiscovery { get; set; }
//
// 	/// <summary>
// 	/// Optional connection name
// 	/// </summary>
// 	public string ConnectionName { get; set; } = string.Empty;
//
// 	/// <summary>
// 	/// Maximum number of attempts to discover the cluster
// 	/// </summary>
// 	public int MaxDiscoverAttempts { get; set; } = 10;
//
// 	/// <summary>
// 	/// Interval between discovery polls in milliseconds
// 	/// </summary>
// 	public int DiscoveryInterval { get; set; } = 100;
//
// 	/// <summary>
// 	/// Timeout for gossip calls in seconds
// 	/// </summary>
// 	public int GossipTimeout { get; set; } = 5;
//
// 	/// <summary>
// 	/// Preferred node role to connect to
// 	/// </summary>
// 	public NodePreference NodePreference { get; set; } = NodePreference.Leader;
//
// 	/// <summary>
// 	/// Whether to verify TLS certificates
// 	/// </summary>
// 	public bool VerifyCert { get; set; } = true;
//
// 	/// <summary>
// 	/// Path to CA file for TLS
// 	/// </summary>
// 	public string TlsCaFile { get; set; } = string.Empty;
//
// 	/// <summary>
// 	/// Default timeout for operations in milliseconds
// 	/// </summary>
// 	public int? DefaultDeadline { get; set; } = null;
//
// 	/// <summary>
// 	/// Interval between keep-alive pings in seconds
// 	/// </summary>
// 	public int KeepAliveInterval { get; set; } = 10;
//
// 	/// <summary>
// 	/// Timeout for keep-alive pings in seconds
// 	/// </summary>
// 	public int KeepAliveTimeout { get; set; } = 10;
//
// 	/// <summary>
// 	/// Path to user certificate file for X.509 authentication
// 	/// </summary>
// 	public string UserCertFile { get; set; } = string.Empty;
//
// 	/// <summary>
// 	/// Path to user key file for X.509 authentication
// 	/// </summary>
// 	public string UserKeyFile { get; set; } = string.Empty;
//
// 	public string OriginalConnectionString { get; private set; } = string.Empty;
//
// 	/// <summary>
// 	/// Generates a KurrentDB connection string from the current option values.
// 	/// </summary>
// 	/// <returns>A connection string representing the options.</returns>
// 	/// <exception cref="InvalidOperationException">Thrown if the Servers list is null or empty.</exception>
// 	public string ToConnectionString() {
// 		if (Servers == null || Servers.Length == 0)
// 			throw new InvalidOperationException("Cannot generate a connection string without at least one server specified.");
//
// 		var sb = new StringBuilder();
//
// 		// 1. Scheme
// 		sb.Append(IsDiscovery ? "kurrentdb+discover://" : "kurrentdb://");
//
// 		// 2. Credentials (URL-encoded)
// 		if (!string.IsNullOrEmpty(Username)) {
// 			sb.Append(Uri.EscapeDataString(Username));
// 			if (!string.IsNullOrEmpty(Password)) {
// 				sb.Append(':');
// 				sb.Append(Uri.EscapeDataString(Password));
// 			}
//
// 			sb.Append('@');
// 		}
//
// 		// 3. Servers
// 		sb.Append(string.Join(",", Servers));
//
// 		// 4. Query Parameters (only include if different from default)
// 		var queryParams = new List<string>();
// 		var defaults    = new KurrentDBConnectionOptions(); // Get default values
//
// 		if (UseTls != defaults.UseTls)
// 			queryParams.Add($"tls={UseTls.ToString().ToLower()}"); // Match parser's bool format
//
// 		if (!string.IsNullOrEmpty(ConnectionName) && ConnectionName != defaults.ConnectionName)
// 			queryParams.Add($"connectionname={Uri.EscapeDataString(ConnectionName)}");
//
// 		// Discovery related options (consider if they should only be added if IsDiscovery is true)
// 		// The current Parse doesn't restrict this, so we mirror it.
// 		if (MaxDiscoverAttempts != defaults.MaxDiscoverAttempts)
// 			queryParams.Add($"maxdiscoverattempts={MaxDiscoverAttempts}");
//
// 		if (DiscoveryInterval != defaults.DiscoveryInterval)
// 			queryParams.Add($"discoveryinterval={DiscoveryInterval}");
//
// 		if (GossipTimeout != defaults.GossipTimeout)
// 			queryParams.Add($"gossiptimeout={GossipTimeout}");
//
// 		if (NodePreference != defaults.NodePreference)
// 			queryParams.Add($"nodepreference={Uri.EscapeDataString(NodePreference.ToString())}"); // Case might matter depending on enum parsing
//
// 		if (VerifyCert != defaults.VerifyCert)
// 			queryParams.Add($"tlsverifycert={VerifyCert.ToString().ToLower()}");
//
// 		if (!string.IsNullOrEmpty(TlsCaFile) && TlsCaFile != defaults.TlsCaFile)
// 			queryParams.Add($"tlscafile={Uri.EscapeDataString(TlsCaFile)}");
//
// 		if (DefaultDeadline.HasValue && DefaultDeadline != defaults.DefaultDeadline) // Check HasValue for nullable int
// 			queryParams.Add($"defaultdeadline={DefaultDeadline.Value}");
//
// 		if (KeepAliveInterval != defaults.KeepAliveInterval)
// 			queryParams.Add($"keepaliveinterval={KeepAliveInterval}");
//
// 		if (KeepAliveTimeout != defaults.KeepAliveTimeout)
// 			queryParams.Add($"keepalivetimeout={KeepAliveTimeout}");
//
// 		if (!string.IsNullOrEmpty(UserCertFile) && UserCertFile != defaults.UserCertFile)
// 			queryParams.Add($"usercertfile={Uri.EscapeDataString(UserCertFile)}");
//
// 		if (!string.IsNullOrEmpty(UserKeyFile) && UserKeyFile != defaults.UserKeyFile)
// 			queryParams.Add($"userkeyfile={Uri.EscapeDataString(UserKeyFile)}");
//
// 		if (queryParams.Count > 0) {
// 			sb.Append('?');
// 			sb.Append(string.Join("&", queryParams));
// 		}
//
// 		return sb.ToString();
// 	}
//
// 	/// <summary>
// 	/// Parses a KurrentDB connection string into options
// 	/// </summary>
// 	/// <param name="connectionString">The connection string to parse</param>
// 	/// <returns>A ConnectionStringOptions object with the parsed settings</returns>
// 	public static KurrentDBConnectionOptions Parse(string connectionString) {
// 		if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
//
// 		var options = new KurrentDBConnectionOptions {
// 			OriginalConnectionString = connectionString
// 		};
//
// 		// Check for schema
// 		options.IsDiscovery = connectionString.StartsWith("kurrentdb+discover://");
// 		var schema = options.IsDiscovery ? "kurrentdb+discover://" : "kurrentdb://";
//
// 		// Remove schema
// 		if (!connectionString.StartsWith(schema))
// 			throw new ArgumentException($"Invalid connection string. Expected it to start with '{schema}'", nameof(connectionString));
//
// 		var remaining = connectionString[schema.Length..];
//
// 		// Split credentials and server part
// 		var atIndex     = remaining.IndexOf('@');
// 		var credentials = atIndex > 0 ? remaining[..atIndex] : string.Empty;
// 		var serverPart  = atIndex > 0 ? remaining[(atIndex + 1)..] : remaining;
//
// 		// Parse credentials
// 		if (!string.IsNullOrEmpty(credentials)) {
// 			var credParts = credentials.Split(':');
//
// 			if (credParts.Length > 0)
// 				options.Username = credParts[0];
//
// 			if (credParts.Length > 1)
// 				options.Password = credParts[1];
// 		}
//
// 		// Parse server and query parameters
// 		var queryIndex = serverPart.IndexOf('?');
// 		var servers    = queryIndex > 0 ? serverPart[..queryIndex] : serverPart;
// 		var query      = queryIndex > 0 ? serverPart[(queryIndex + 1)..] : string.Empty;
//
// 		if (string.IsNullOrEmpty(servers)) throw new ArgumentException("No servers specified in connection string", nameof(connectionString));
//
// 		options.Servers = servers.Split(',');
//
// 		// Parse query parameters
// 		if (!string.IsNullOrEmpty(query)) {
// 			var queryParams = query.Split('&');
// 			foreach (var param in queryParams) {
// 				var kvp = param.Split('=');
// 				if (kvp.Length != 2) continue;
//
// 				var key   = kvp[0].ToLower();
// 				var value = kvp[1];
//
// 				switch (key) {
// 					case "tls":
// 						options.UseTls = bool.Parse(value);
// 						break;
//
// 					case "connectionname":
// 						options.ConnectionName = value;
// 						break;
//
// 					case "maxdiscoverattempts":
// 						options.MaxDiscoverAttempts = int.Parse(value);
// 						break;
//
// 					case "discoveryinterval":
// 						options.DiscoveryInterval = int.Parse(value);
// 						break;
//
// 					case "gossiptimeout":
// 						options.GossipTimeout = int.Parse(value);
// 						break;
//
// 					case "nodepreference":
// 						options.NodePreference = Enum.Parse<NodePreference>(value, true);
// 						break;
//
// 					case "tlsverifycert":
// 						options.VerifyCert = bool.Parse(value);
// 						break;
//
// 					case "tlscafile":
// 						options.TlsCaFile = value;
// 						break;
//
// 					case "defaultdeadline":
// 						options.DefaultDeadline = int.Parse(value);
// 						break;
//
// 					case "keepaliveinterval":
// 						options.KeepAliveInterval = int.Parse(value);
// 						break;
//
// 					case "keepalivetimeout":
// 						options.KeepAliveTimeout = int.Parse(value);
// 						break;
//
// 					case "usercertfile":
// 						options.UserCertFile = value;
// 						break;
//
// 					case "userkeyfile":
// 						options.UserKeyFile = value;
// 						break;
// 				}
// 			}
// 		}
//
// 		return options;
// 	}
// }
