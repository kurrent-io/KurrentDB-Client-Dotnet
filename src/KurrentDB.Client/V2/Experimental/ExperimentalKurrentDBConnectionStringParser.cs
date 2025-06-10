using System.Net;
using System.Text.RegularExpressions;

namespace KurrentDB.Client.ConnectionString;

/// <summary>
/// Parses KurrentDB connection strings into structured components.
/// </summary>
/// <remarks>
/// <para>
/// Supports both single-node (<c>kurrentdb://</c>) and multi-node cluster (<c>kurrentdb+discover://</c>)
/// connection string formats.
/// </para>
/// <para>
/// Connection string examples:
/// <list type="bullet">
///   <item><description><c>kurrentdb://admin:changeit@localhost:2113</c> - Single node connection</description></item>
///   <item><description><c>kurrentdb+discover://admin:changeit@cluster.example.com:2113</c> - Cluster connection with DNS</description></item>
///   <item><description><c>kurrentdb://admin:changeit@localhost:2113?tls=false</c> - Non-secure connection</description></item>
///   <item><description><c>kurrentdb+discover://admin:changeit@node1.example.com:2113,node2.example.com:2113</c> - Cluster with multiple hosts</description></item>
/// </list>
/// </para>
/// </remarks>
static partial class ExperimentalKurrentDBConnectionStringParser {
    static readonly string[] SchemesDiscovery = ["esdb+discover", "eventstore+discover", "kurrentdb+discover"];
    static readonly string[] Schemes          = ["esdb", "eventstore", "kurrentdb", ..SchemesDiscovery];

    static readonly Regex HostAndPortRegex   = GetHostAndPortRegex();
    static readonly Regex MultipleHostsRegex = GetMultipleHostsRegex();

    /// <summary>
    /// Parses a KurrentDB connection string and extracts its components.
    /// </summary>
    /// <param name="connectionString">The connection string to parse.</param>
    /// <returns>A ConnectionStringComponents object containing the parsed information.</returns>
    /// <exception cref="ArgumentNullException">When connectionString is null or empty.</exception>
    /// <exception cref="FormatException">When the connection string has an invalid format.</exception>
    /// <example>
    /// <code>
    /// // Parse a single-node connection string
    /// var components = KurrentDBConnectionStringParser.Parse("kurrentdb://admin:changeit@localhost:2113");
    ///
    /// // Parse a cluster connection string with multiple hosts
    /// var clusterComponents = KurrentDBConnectionStringParser.Parse(
    ///     "kurrentdb+discover://admin:changeit@node1:2113,node2:2113,node3:2113?tls=true");
    /// </code>
    /// </example>
    public static KurrentDBConnectionStringComponents Parse(string connectionString) {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty");

        // Parse the connection string URI
        var uri = ParseUri(connectionString);

        // Extract all components before creating the record
        var scheme       = uri.Scheme;
        var useDiscovery = IsDiscoveryScheme(scheme);

        // Parse endpoints efficiently
        var endpoints = connectionString.Contains(',')
            ? ParseMultipleEndpoints(connectionString)
            : [new DnsEndPoint(uri.Host, uri.Port > 0 ? uri.Port : 2113)];

        // Extract credentials
        var credentials = ExtractCredentials(uri);

        // Parse query parameters
        var parameters = ParseQueryString(uri.Query);

        // Create the record with all properties initialized at once
        return new KurrentDBConnectionStringComponents {
            Scheme              = scheme,
            UseDiscovery        = useDiscovery,
            Endpoints           = endpoints,
            Credentials         = credentials,
            Parameters          = parameters,
            UseTls              = TryGetBoolParameter("tls", true),
            ConnectionName      = TryGetStringParameter("connectionName"),
            MaxDiscoverAttempts = TryGetIntParameter("maxDiscoverAttempts", 10),
            DiscoveryInterval   = TryGetIntParameter("discoveryInterval", 100),
            GossipTimeout       = TryGetIntParameter("gossipTimeout", 5),
            NodePreference      = TryGetStringParameter("nodePreference") ?? "leader",
            TlsVerifyCert       = TryGetBoolParameter("tlsVerifyCert", true),
            TlsCaFile           = TryGetStringParameter("tlsCaFile"),
            DefaultDeadline     = TryGetNullableIntParameter("defaultDeadline"),
            KeepAliveInterval   = TryGetIntParameter("keepAliveInterval", 10),
            KeepAliveTimeout    = TryGetIntParameter("keepAliveTimeout", 10),
            UserCertFile        = TryGetStringParameter("userCertFile"),
            UserKeyFile         = TryGetStringParameter("userKeyFile"),
            OperationTimeout    = TryGetNullableIntParameter("operationTimeout")
        };

        string? TryGetStringParameter(string paramName) => parameters.GetValueOrDefault(paramName);

        int TryGetIntParameter(string paramName, int defaultValue) =>
            parameters.TryGetValue(paramName, out var value) && int.TryParse(value, out var result)
                ? result
                : defaultValue;

        int? TryGetNullableIntParameter(string paramName) =>
            parameters.TryGetValue(paramName, out var value) && int.TryParse(value, out var result)
                ? result
                : null;

        bool TryGetBoolParameter(string paramName, bool defaultValue) =>
            parameters.TryGetValue(paramName, out var value) && bool.TryParse(value, out var result)
                ? result
                : defaultValue;
    }

    static bool IsDiscoveryScheme(string scheme) =>
        SchemesDiscovery.Contains(scheme, StringComparer.OrdinalIgnoreCase);

    static (string? Username, string? Password) ExtractCredentials(Uri uri) {
        if (string.IsNullOrWhiteSpace(uri.UserInfo))
            return (null, null);

        var credentials = uri.UserInfo.Split(':');
        var username    = credentials.Length >= 1 ? WebUtility.UrlDecode(credentials[0]) : null;
        var password    = credentials.Length >= 2 ? WebUtility.UrlDecode(credentials[1]) : null;

        return (username, password);
    }

    static Uri ParseUri(string connectionString) {
        try {
            // For connection strings with multiple hosts (node1,node2,node3), we need special handling
            // as standard Uri parser doesn't support commas in the host part
            return connectionString.Contains(',')
                ? ParseMultiHostUri(connectionString)
                : new Uri(connectionString);
        }
        catch (UriFormatException ex) {
            throw new FormatException($"Invalid KurrentDB connection string format: {ex.Message}", ex);
        }


        static Uri ParseMultiHostUri(string connectionString) {
            // Extract scheme and query parts first
            var schemeEndIndex = connectionString.IndexOf("://", StringComparison.Ordinal);
            if (schemeEndIndex == -1)
                throw new FormatException("Invalid connection string format: scheme specifier not found");

            var scheme    = connectionString[..schemeEndIndex];
            var remaining = connectionString[(schemeEndIndex + 3)..];

            // Extract query part if exists
            var query      = string.Empty;
            var hostPart   = remaining;
            var queryIndex = remaining.IndexOf('?');
            if (queryIndex >= 0) {
                query    = remaining[queryIndex..];
                hostPart = remaining[..queryIndex];
            }

            // For multi-host, we'll use the first host to create a URI and store the rest separately
            var hostMatch = MultipleHostsRegex.Match(hostPart);
            if (!hostMatch.Success)
                throw new FormatException("Invalid host format in connection string");

            var userInfo = hostMatch.Groups["userinfo"].Value;
            var hosts    = hostMatch.Groups["hosts"].Value;
            var hostList = hosts.Split(',');

            if (hostList.Length == 0)
                throw new FormatException("No valid hosts found in connection string");

            // Use the first host to create a base URI
            var baseUri = $"{scheme}://{(string.IsNullOrEmpty(userInfo) ? "" : userInfo)}{hostList[0]}{query}";

            return new Uri(baseUri);
        }
    }

    static DnsEndPoint[] ParseMultipleEndpoints(string connectionString) {
        var hostStrings = ExtractMultiHostsFromConnectionString(connectionString);
        return hostStrings.Select(ParseEndPoint).ToArray();

        static DnsEndPoint ParseEndPoint(string hostAndPort) {
            var match = HostAndPortRegex.Match(hostAndPort);
            if (!match.Success)
                throw new FormatException($"Invalid host:port format: {hostAndPort}");

            var host = match.Groups["host"].Value;
            var port = 2113; // Default port

            if (match.Groups["port"].Success)
                port = int.Parse(match.Groups["port"].Value);

            return new DnsEndPoint(host, port);
        }

        static string[] ExtractMultiHostsFromConnectionString(string connectionString) {
            var schemeEndIndex = connectionString.IndexOf("://", StringComparison.Ordinal);
            if (schemeEndIndex == -1)
                throw new FormatException("Invalid connection string format: scheme specifier not found");

            var remaining = connectionString[(schemeEndIndex + 3)..];

            // Extract query part if exists
            var hostPart   = remaining;
            var queryIndex = remaining.IndexOf('?');
            if (queryIndex >= 0)
                hostPart = remaining[..queryIndex];

            // Remove userinfo if present
            var atIndex = hostPart.LastIndexOf('@');
            if (atIndex >= 0)
                hostPart = hostPart[(atIndex + 1)..];

            return hostPart.Split(',');
        }
    }

    static Dictionary<string, string> ParseQueryString(string query) {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(query) || query == "?") return parameters;

        // Remove the leading '?' if present
        var querySpan = query.AsSpan();
        if (querySpan[0] == '?')
            querySpan = querySpan[1..];

        // Avoid string allocations by using spans
        foreach (var pair in querySpan.ToString().Split('&')) {
            var index = pair.IndexOf('=');
            if (index >= 0) {
                var name  = WebUtility.UrlDecode(pair[..index]);
                var value = WebUtility.UrlDecode(pair[(index + 1)..]);
                parameters[name] = value;
            }
            else {
                // Handle parameters without values
                var name = WebUtility.UrlDecode(pair);
                parameters[name] = string.Empty;
            }
        }

        return parameters;
    }

	[GeneratedRegex(@"^(?<host>[^:]+)(:(?<port>\d+))?$", RegexOptions.Compiled)]
	private static partial Regex GetHostAndPortRegex();
	[GeneratedRegex(@"^(?<userinfo>.+@)?(?<hosts>.+)$", RegexOptions.Compiled)]
	private static partial Regex GetMultipleHostsRegex();
}

/// <summary>
/// Contains the components of a parsed KurrentDB connection string.
/// </summary>
/// <remarks>
/// <para>
/// Provides structured access to all parts of a KurrentDB connection string, including
/// scheme, endpoints, credentials, and configuration parameters.
/// </para>
/// <para>
/// Connection parameters are stored in the Parameters dictionary, but are also
/// accessible through strongly-typed properties that handle parsing and default values.
/// </para>
/// </remarks>
record KurrentDBConnectionStringComponents {
    /// <summary>
    /// The connection string scheme.
    /// </summary>
    public string Scheme { get; init; } = string.Empty;

    /// <summary>
    /// Indicates whether discovery protocol should be used.
    /// </summary>
    public bool UseDiscovery { get; init; }

    /// <summary>
    /// The DNS endpoints for KurrentDB servers.
    /// </summary>
    public DnsEndPoint[] Endpoints { get; init; } = [];

    /// <summary>
    /// The username and password for authentication, if specified.
    /// </summary>
    public (string? Username, string? Password) Credentials { get; init; } = (null, null);

    /// <summary>
    /// The query parameters from the connection string.
    /// </summary>
    public Dictionary<string, string> Parameters { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Indicates whether TLS should be used.
    /// </summary>
    /// <remarks>
    /// Defaults to true if not specified in the connection string.
    /// </remarks>
    public bool UseTls { get; init; } = true;

    /// <summary>
    /// The connection name if specified.
    /// </summary>
    public string? ConnectionName { get; init; }

    /// <summary>
    /// The maximum number of discovery attempts.
    /// </summary>
    /// <remarks>
    /// Defaults to 10 if not specified in the connection string.
    /// </remarks>
    public int MaxDiscoverAttempts { get; init; } = 10;

    /// <summary>
    /// The discovery interval in milliseconds.
    /// </summary>
    /// <remarks>
    /// Defaults to 100ms if not specified in the connection string.
    /// </remarks>
    public int DiscoveryInterval { get; init; } = 100;

    /// <summary>
    /// The gossip timeout in seconds.
    /// </summary>
    /// <remarks>
    /// Defaults to 5 seconds if not specified in the connection string.
    /// </remarks>
    public int GossipTimeout { get; init; } = 5;

    /// <summary>
    /// The preferred node role.
    /// </summary>
    /// <remarks>
    /// Defaults to "leader" if not specified in the connection string.
    /// </remarks>
    public string NodePreference { get; init; } = "leader";

    /// <summary>
    /// Indicates whether to verify TLS certificates.
    /// </summary>
    /// <remarks>
    /// Defaults to true if not specified in the connection string.
    /// </remarks>
    public bool TlsVerifyCert { get; init; } = true;

    /// <summary>
    /// The path to the CA file.
    /// </summary>
    public string? TlsCaFile { get; init; }

    /// <summary>
    /// The default operation deadline in milliseconds, if specified.
    /// </summary>
    public int? DefaultDeadline { get; init; }

    /// <summary>
    /// The keep-alive interval in seconds.
    /// </summary>
    /// <remarks>
    /// Defaults to 10 seconds if not specified in the connection string.
    /// </remarks>
    public int KeepAliveInterval { get; init; } = 10;

    /// <summary>
    /// The keep-alive timeout in seconds.
    /// </summary>
    /// <remarks>
    /// Defaults to 10 seconds if not specified in the connection string.
    /// </remarks>
    public int KeepAliveTimeout { get; init; } = 10;

    /// <summary>
    /// The path to the user certificate file, if specified.
    /// </summary>
    public string? UserCertFile { get; init; }

    /// <summary>
    /// The path to the user key file, if specified.
    /// </summary>
    public string? UserKeyFile { get; init; }

    /// <summary>
    /// The operation timeout in milliseconds, if specified.
    /// </summary>
    public int? OperationTimeout { get; init; }

    /// <summary>
    /// Tries to get a string parameter value.
    /// </summary>
    string? TryGetStringParameter(string paramName) =>
        Parameters.GetValueOrDefault(paramName);

    /// <summary>
    /// Tries to get an integer parameter value with a default.
    /// </summary>
    int TryGetIntParameter(string paramName, int defaultValue) =>
        Parameters.TryGetValue(paramName, out var value) && int.TryParse(value, out var result)
            ? result
            : defaultValue;

    /// <summary>
    /// Tries to get a nullable integer parameter value.
    /// </summary>
    int? TryGetNullableIntParameter(string paramName) =>
        Parameters.TryGetValue(paramName, out var value) && int.TryParse(value, out var result)
            ? result
            : null;

    /// <summary>
    /// Tries to get a boolean parameter value with a default.
    /// </summary>
    bool TryGetBoolParameter(string paramName, bool defaultValue) =>
        Parameters.TryGetValue(paramName, out var value) && bool.TryParse(value, out var result)
            ? result
            : defaultValue;
}
