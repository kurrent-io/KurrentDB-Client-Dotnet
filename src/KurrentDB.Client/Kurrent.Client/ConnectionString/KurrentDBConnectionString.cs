using System.Net;
using KurrentDB.Client;

namespace Kurrent.Client;

/// <summary>
/// Represents a parsed KurrentDB connection string with all its components
/// </summary>
public record KurrentDBConnectionString {
    const string SchemeSeparator = "://";

    const char UserInfoSeparator = '@';
    const char Colon             = ':';
    const char Slash             = '/';
    const char Comma             = ',';
    const char Ampersand         = '&';
    const char Equal             = '=';
    const char QuestionMark      = '?';

    const int DefaultPort = 2113;

    // Make constants and fields internal so they can be accessed by the extension method
    internal const string Tls                  = nameof(Tls);
    internal const string ConnectionName       = nameof(ConnectionName);
    internal const string MaxDiscoverAttempts  = nameof(MaxDiscoverAttempts);
    internal const string DiscoveryInterval    = nameof(DiscoveryInterval);
    internal const string GossipTimeout        = nameof(GossipTimeout);
    internal const string NodePreference       = nameof(NodePreference);
    internal const string TlsVerifyCert        = nameof(TlsVerifyCert);
    internal const string TlsCaFile            = nameof(TlsCaFile);
    internal const string DefaultDeadline      = nameof(DefaultDeadline);
    internal const string ThrowOnAppendFailure = nameof(ThrowOnAppendFailure);
    internal const string KeepAliveInterval    = nameof(KeepAliveInterval);
    internal const string KeepAliveTimeout     = nameof(KeepAliveTimeout);
    internal const string UserCertFile         = nameof(UserCertFile);
    internal const string UserKeyFile          = nameof(UserKeyFile);

    internal const           string   DefaultDirectScheme    = "kurrentdb";
    internal const           string   DefaultDiscoveryScheme = "kurrentdb+discover";
    internal static readonly string[] DiscoverySchemes       = [DefaultDiscoveryScheme, "esdb+discover", "eventstore+discover"];
    internal static readonly string[] AllSchemes             = [DefaultDirectScheme, "esdb", "eventstore", ..DiscoverySchemes];

    KurrentDBConnectionString(
        string connectionString, string scheme, (string User, string Password)? userInfo, DnsEndPoint[] hosts,
        Dictionary<string, string> options
    ) {
        ConnectionString = connectionString;
        Scheme           = scheme;
        UserInfo         = userInfo;
        Hosts            = hosts;
        Options          = options;
    }

    public string                          ConnectionString { get; }
    public string                          Scheme           { get; }
    public (string User, string Password)? UserInfo         { get; }
    public DnsEndPoint[]                   Hosts            { get; }
    public Dictionary<string, string>      Options          { get; }

    /// <summary>
    /// Parses a connection string into its components
    /// </summary>
    /// <param name="connectionString">The connection string to parse</param>
    /// <returns>A KurrentDBConnectionString containing the parsed components</returns>
    public static KurrentDBConnectionString Parse(string connectionString) {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var currentIndex = 0;

        var schemeIndex = connectionString.IndexOf(SchemeSeparator, currentIndex, StringComparison.Ordinal);
        if (schemeIndex == -1)
            throw new NoSchemeException();

        var scheme = ParseScheme(connectionString[..schemeIndex]);

        currentIndex = schemeIndex + SchemeSeparator.Length;

        var userInfoIndex = connectionString.IndexOf(UserInfoSeparator, currentIndex);

        (string User, string Password)? userInfo = null;
        if (userInfoIndex != -1) {
            userInfo     = ParseUserInfo(connectionString.Substring(currentIndex, userInfoIndex - currentIndex));
            currentIndex = userInfoIndex + 1;
        }

        var slashIndex        = connectionString.IndexOf(Slash, currentIndex);
        var questionMarkIndex = connectionString.IndexOf(QuestionMark, currentIndex);
        var endIndex          = connectionString.Length;

        //for simpler substring operations:
        if (slashIndex == -1)
            slashIndex = int.MaxValue;

        if (questionMarkIndex == -1)
            questionMarkIndex = int.MaxValue;

        var hostSeparatorIndex = Math.Min(Math.Min(slashIndex, questionMarkIndex), endIndex);
        var hosts              = ParseHosts(connectionString.Substring(currentIndex, hostSeparatorIndex - currentIndex));

        if (hosts.Length == 0)
            throw new ConnectionStringParseException("Connection string must specify at least one endpoint.");

        if (hosts.Length > 1 && scheme == DefaultDirectScheme)
            throw new ConnectionStringParseException(
                $"The '{DefaultDirectScheme}' connection scheme does not support multiple hosts. " +
                "Use a discovery scheme like 'kurrentdb+discover' or 'esdb+discover' for multiple hosts."
            );

        currentIndex = hostSeparatorIndex;

        var path = "";
        if (slashIndex != int.MaxValue)
            path = connectionString.Substring(
                currentIndex,
                Math.Min(questionMarkIndex, endIndex) - currentIndex
            );

        if (path != "" && path != "/")
            throw new ConnectionStringParseException(
                $"The specified path must be either an empty string or a forward slash (/) but the following path was found instead: '{path}'"
            );

        var options = new Dictionary<string, string>();

        if (questionMarkIndex != int.MaxValue) {
            currentIndex = questionMarkIndex + 1;
            options      = ParseParameters(connectionString[currentIndex..]);
        }

        return new KurrentDBConnectionString(
            connectionString, scheme, userInfo,
            hosts, options
        );

        static string ParseScheme(string input) =>
            AllSchemes.Contains(input)
                ? input.ToLowerInvariant()
                : throw new InvalidSchemeException(input, AllSchemes);

        static (string, string) ParseUserInfo(string input) {
            var tokens = input.Split(Colon);
            if (tokens.Length != 2)
                throw new InvalidUserCredentialsException(input);

            var username = Uri.UnescapeDataString(tokens[0]);
            var password = Uri.UnescapeDataString(tokens[1]);

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                throw new InvalidUserCredentialsException(input);

            return (username, password);
        }

        static DnsEndPoint[] ParseHosts(string input) {
            var hosts = input.Split(Comma).Select(hostToken => {
                    // Check for malformed host:port combinations (e.g., "host:" with trailing colon but no port)
                    if (hostToken.EndsWith(Colon) && !hostToken.EndsWith("::")) // Allow IPv6 addresses like "::1"
                        throw new InvalidHostException(hostToken);

                    // address can be in the form of "host:port" or just "host"
                    var token = hostToken.Split(Colon, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                    var endpoint = token.Length switch {
                        1 => new DnsEndPoint(token[0], DefaultPort),
                        2 => new DnsEndPoint(token[0], int.TryParse(token[1], out var port) ? port : throw new InvalidHostException(hostToken)),
                        _ => throw new InvalidHostException(hostToken)
                    };

                    return endpoint;
                }
            ).ToArray();

            return hosts.Length == 0 ? throw new InvalidHostException(input) : hosts;
        }

        static Dictionary<string, string> ParseParameters(string input) {
            return input.Split(Ampersand).Aggregate(
                new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase),
                (seed, option) => {
                    var (key, value) = ParseParameter(option);
                    return !seed.TryAdd(key, value)
                        ? throw new DuplicateKeyException(key)
                        : seed;
                }
            );

            static (string, string) ParseParameter(string input) {
                var keyValueToken = input.Split(Equal);
                return keyValueToken.Length != 2
                    ? throw new InvalidKeyValuePairException(input)
                    : (keyValueToken[0], Uri.UnescapeDataString(keyValueToken[1]));
            }
        }
    }
}

/// <summary>
/// The base exception that is thrown when an KurrentDB connection string could not be parsed.
/// </summary>
public class ConnectionStringParseException : KurrentException {
    /// <summary>
    /// Constructs a new <see cref="ConnectionStringParseException"/>.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
    public ConnectionStringParseException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// The exception that is thrown when a key in the KurrentDB connection string is duplicated.
/// </summary>
public class DuplicateKeyException : ConnectionStringParseException {
    /// <summary>
    /// Constructs a new <see cref="DuplicateKeyException"/>.
    /// </summary>
    /// <param name="key"></param>
    public DuplicateKeyException(string key)
        : base($"Duplicate key: '{key}'") { }
}

/// <summary>
/// The exception that is thrown when a certificate is invalid or not found in the KurrentDB connection string.
/// </summary>
public class InvalidClientCertificateException : ConnectionStringParseException {
    /// <summary>
    /// Constructs a new <see cref="InvalidClientCertificateException"/>.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public InvalidClientCertificateException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

/// <summary>
/// The exception that is thrown when there is an invalid host in the KurrentDB connection string.
/// </summary>
public class InvalidHostException : ConnectionStringParseException {
    /// <summary>
    /// Constructs a new <see cref="InvalidHostException"/>.
    /// </summary>
    /// <param name="host"></param>
    public InvalidHostException(string host)
        : base($"Invalid host: '{host}'") { }
}

/// <summary>
/// The exception that is thrown when an invalid key value pair is found in an KurrentDB connection string.
/// </summary>
public class InvalidKeyValuePairException : ConnectionStringParseException {
    /// <summary>
    /// Constructs a new <see cref="InvalidKeyValuePairException"/>.
    /// </summary>
    /// <param name="keyValuePair"></param>
    public InvalidKeyValuePairException(string keyValuePair)
        : base($"Invalid key/value pair: '{keyValuePair}'") { }
}

/// <summary>
/// The exception that is thrown when an invalid scheme is defined in the KurrentDB connection string.
/// </summary>
public class InvalidSchemeException : ConnectionStringParseException {
    /// <summary>
    /// Constructs a new <see cref="InvalidSchemeException"/>.
    /// </summary>
    /// <param name="scheme"></param>
    /// <param name="supportedSchemes"></param>
    public InvalidSchemeException(string scheme, string[] supportedSchemes)
        : base($"Invalid scheme: '{scheme}'. Supported values are: {string.Join(",", supportedSchemes)}") { }
}

/// <summary>
/// The exception that is thrown when an invalid setting is found in an KurrentDB connection string.
/// </summary>
public class InvalidSettingException : ConnectionStringParseException {
    /// <summary>
    /// Constructs a new <see cref="InvalidSettingException"/>.
    /// </summary>
    /// <param name="message"></param>
    public InvalidSettingException(string message) : base(message) { }
}

/// <summary>
/// The exception that is thrown when an invalid <see cref="UserCredentials"/> is specified in the KurrentDB connection string.
/// </summary>
public class InvalidUserCredentialsException : ConnectionStringParseException {
    /// <summary>
    ///
    /// </summary>
    /// <param name="userInfo"></param>
    public InvalidUserCredentialsException(string userInfo)
        : base($"Invalid user credentials: '{userInfo}'. Username & password must be delimited by a colon") { }
}

/// <summary>
/// The exception that is thrown when no scheme was specified in the KurrentDB connection string.
/// </summary>
public class NoSchemeException : ConnectionStringParseException {
    /// <summary>
    /// Constructs a new <see cref="NoSchemeException"/>.
    /// </summary>
    public NoSchemeException()
        : base("Could not parse scheme from connection string") { }
}
