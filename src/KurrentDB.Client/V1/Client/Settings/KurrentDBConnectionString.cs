using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KurrentDB.Client;

/// <summary>
/// Represents a parsed KurrentDB connection string with all its components
/// </summary>
record KurrentDBConnectionString {
	const string SchemeSeparator = "://";

	const char UserInfoSeparator = '@';
	const char Colon             = ':';
	const char Slash             = '/';
	const char Comma             = ',';
	const char Ampersand         = '&';
	const char Equal             = '=';
	const char QuestionMark      = '?';

	const string Tls                  = nameof(Tls);
	const string ConnectionName       = nameof(ConnectionName);
	const string MaxDiscoverAttempts  = nameof(MaxDiscoverAttempts);
	const string DiscoveryInterval    = nameof(DiscoveryInterval);
	const string GossipTimeout        = nameof(GossipTimeout);
	const string NodePreference       = nameof(NodePreference);
	const string TlsVerifyCert        = nameof(TlsVerifyCert);
	const string TlsCaFile            = nameof(TlsCaFile);
	const string DefaultDeadline      = nameof(DefaultDeadline);
	const string ThrowOnAppendFailure = nameof(ThrowOnAppendFailure);
	const string KeepAliveInterval    = nameof(KeepAliveInterval);
	const string KeepAliveTimeout     = nameof(KeepAliveTimeout);
	const string UserCertFile         = nameof(UserCertFile);
	const string UserKeyFile          = nameof(UserKeyFile);

	static readonly string[] SchemesDiscovery = ["esdb+discover", "eventstore+discover", "kurrentdb+discover"];
	static readonly string[] Schemes          = ["esdb", "eventstore", "kurrentdb", ..SchemesDiscovery];

	const int  DefaultPort   = KurrentDBClientConnectivitySettings.DefaultPort;
	const bool DefaultUseTls = true;

	static readonly Dictionary<string, Type> SettingsType =
		new(StringComparer.InvariantCultureIgnoreCase) {
			{ ConnectionName, typeof(string) },
			{ MaxDiscoverAttempts, typeof(int) },
			{ DiscoveryInterval, typeof(int) },
			{ GossipTimeout, typeof(int) },
			{ NodePreference, typeof(string) },
			{ Tls, typeof(bool) },
			{ TlsVerifyCert, typeof(bool) },
			{ TlsCaFile, typeof(string) },
			{ DefaultDeadline, typeof(int) },
			{ ThrowOnAppendFailure, typeof(bool) },
			{ KeepAliveInterval, typeof(int) },
			{ KeepAliveTimeout, typeof(int) },
			{ UserCertFile, typeof(string) },
			{ UserKeyFile, typeof(string) }
		};

	KurrentDBConnectionString(string scheme, (string user, string pass)? userInfo, DnsEndPoint[] hosts, Dictionary<string, string> options) {
		Scheme   = scheme;
		UserInfo = userInfo;
		Hosts    = hosts;
		Options  = options;
	}

	public string                      Scheme   { get; }
	public (string user, string pass)? UserInfo { get; }
	public DnsEndPoint[]               Hosts    { get; }
	public Dictionary<string, string>  Options  { get; }

	public bool IsDiscoveryScheme => SchemesDiscovery.Contains(Scheme);

	/// <summary>
	/// Parses a connection string into its components
	/// </summary>
	/// <param name="connectionString">The connection string to parse</param>
	/// <returns>A KurrentDBConnectionString containing the parsed components</returns>
	public static KurrentDBConnectionString Parse(string connectionString) {
		var currentIndex = 0;

		var schemeIndex  = connectionString.IndexOf(SchemeSeparator, currentIndex, StringComparison.Ordinal);
		if (schemeIndex == -1)
			throw new NoSchemeException();

		var scheme = ParseScheme(connectionString[..schemeIndex]);

		currentIndex = schemeIndex + SchemeSeparator.Length;

		var userInfoIndex = connectionString.IndexOf(UserInfoSeparator, currentIndex);

		(string user, string pass)? userInfo = null;
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

		currentIndex = hostSeparatorIndex;

		var path = "";
		if (slashIndex != int.MaxValue)
			path = connectionString.Substring(
				currentIndex,
				Math.Min(questionMarkIndex, endIndex) - currentIndex
			);

		if (path != "" && path != "/")
			throw new ConnectionStringParseException(
				$"The specified path must be either an empty string or a forward slash (/) but the following path was found instead: '{path}'");

		var options = new Dictionary<string, string>();

		if (questionMarkIndex != int.MaxValue) {
			currentIndex = questionMarkIndex + 1;
			options      = ParseKeyValuePairs(connectionString[currentIndex..]);
		}

		return new KurrentDBConnectionString(scheme, userInfo, hosts, options);

		static string ParseScheme(string input) =>
			!Schemes.Contains(input) ? throw new InvalidSchemeException(input, Schemes) : input;

		static (string, string) ParseUserInfo(string input) {
			var tokens = input.Split(Colon);
			return tokens.Length != 2
				? throw new InvalidUserCredentialsException(input)
				: (tokens[0], tokens[1]);
		}

		static DnsEndPoint[] ParseHosts(string input) {
			// var this_would_be_nice = input.Split(Comma, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

			var hosts = input.Split(Comma).Select(hostToken => {
				// address can be in the form of "host:port" or just "host"
				var token = hostToken.Split(Colon, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

				var endpoint =  token.Length switch {
					1 => new DnsEndPoint(token[0], DefaultPort),
					2 => new DnsEndPoint(token[0], int.TryParse(token[1], out var port) ? port : throw new InvalidHostException(hostToken)),
					_ => throw new InvalidHostException(hostToken)
				};

				return endpoint;
			}).ToArray();

			return hosts.Length == 0 ? throw new InvalidHostException(input) : hosts;
		}

		static Dictionary<string, string> ParseKeyValuePairs(string input) {
			return input.Split(Ampersand)
				.Aggregate(
					new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase),
					(seed, option) => {
						var (key, value) = ParseKeyValuePair(option);
						return !seed.TryAdd(key, value)
							? throw new DuplicateKeyException(key)
							: seed;
					}
				);

			static (string, string) ParseKeyValuePair(string input) {
				var keyValueToken = input.Split(Equal);
				return keyValueToken.Length != 2
					? throw new InvalidKeyValuePairException(input)
					: (keyValueToken[0], keyValueToken[1]);
			}
		}
	}

	/// <summary>
	/// Creates a KurrentDBClientSettings from the parsed connection string
	/// </summary>
	/// <returns>A configured KurrentDBClientSettings instance</returns>
	public KurrentDBClientSettings ToClientSettings() {
		var settings = new KurrentDBClientSettings {
			ConnectivitySettings = KurrentDBClientConnectivitySettings.Default,
			OperationOptions     = KurrentDBClientOperationOptions.Default
		};

		if (UserInfo.HasValue)
			settings.DefaultCredentials = new UserCredentials(UserInfo.Value.user, UserInfo.Value.pass);

		var typedOptions = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
		foreach (var kv in Options) {
			if (!SettingsType.TryGetValue(kv.Key, out var type))
				throw new InvalidSettingException($"Unknown option: {kv.Key}");

			if (type == typeof(int)) {
				if (!int.TryParse(kv.Value, out var intValue))
					throw new InvalidSettingException($"{kv.Key} must be an integer value");

				typedOptions.Add(kv.Key, intValue);
			}
			else if (type == typeof(bool)) {
				if (!bool.TryParse(kv.Value, out var boolValue))
					throw new InvalidSettingException($"{kv.Key} must be either true or false");

				typedOptions.Add(kv.Key, boolValue);
			}
			else if (type == typeof(string)) {
				typedOptions.Add(kv.Key, kv.Value);
			}
		}

		if (typedOptions.TryGetValue(ConnectionName, out var connectionName))
			settings.ConnectionName = (string)connectionName;

		if (typedOptions.TryGetValue(MaxDiscoverAttempts, out var maxDiscoverAttempts))
			settings.ConnectivitySettings.MaxDiscoverAttempts = (int)maxDiscoverAttempts;

		if (typedOptions.TryGetValue(DiscoveryInterval, out var discoveryInterval))
			settings.ConnectivitySettings.DiscoveryInterval = TimeSpan.FromMilliseconds((int)discoveryInterval);

		if (typedOptions.TryGetValue(GossipTimeout, out var gossipTimeout))
			settings.ConnectivitySettings.GossipTimeout = TimeSpan.FromMilliseconds((int)gossipTimeout);

		if (typedOptions.TryGetValue(NodePreference, out var nodePreference))
			settings.ConnectivitySettings.NodePreference = ((string)nodePreference).ToLowerInvariant() switch {
				"leader"          => Client.NodePreference.Leader,
				"follower"        => Client.NodePreference.Follower,
				"random"          => Client.NodePreference.Random,
				"readonlyreplica" => Client.NodePreference.ReadOnlyReplica,
				_                 => throw new InvalidSettingException($"Invalid NodePreference: {nodePreference}")
			};

		var useTls                                             = DefaultUseTls;
		if (typedOptions.TryGetValue(Tls, out var tls)) useTls = (bool)tls;

		if (typedOptions.TryGetValue(DefaultDeadline, out var operationTimeout))
			settings.DefaultDeadline = TimeSpan.FromMilliseconds((int)operationTimeout);

		if (typedOptions.TryGetValue(ThrowOnAppendFailure, out var throwOnAppendFailure))
			settings.OperationOptions.ThrowOnAppendFailure = (bool)throwOnAppendFailure;

		if (typedOptions.TryGetValue(KeepAliveInterval, out var keepAliveIntervalMs))
			settings.ConnectivitySettings.KeepAliveInterval = keepAliveIntervalMs switch {
				-1                 => Timeout.InfiniteTimeSpan,
				int value and >= 0 => TimeSpan.FromMilliseconds(value),
				_                  => throw new InvalidSettingException($"Invalid KeepAliveInterval: {keepAliveIntervalMs}")
			};

		if (typedOptions.TryGetValue(KeepAliveTimeout, out var keepAliveTimeoutMs))
			settings.ConnectivitySettings.KeepAliveTimeout = keepAliveTimeoutMs switch {
				-1                 => Timeout.InfiniteTimeSpan,
				int value and >= 0 => TimeSpan.FromMilliseconds(value),
				_                  => throw new InvalidSettingException($"Invalid KeepAliveTimeout: {keepAliveTimeoutMs}")
			};

		settings.ConnectivitySettings.Insecure = !useTls;

		if (Hosts.Length == 1 && !SchemesDiscovery.Contains(Scheme))
			settings.ConnectivitySettings.Address = Hosts[0].ToUri(useTls);
		else {
			settings.ConnectivitySettings.Address     = null; //new Uri("kurrentdb+discover://cluster");
			settings.ConnectivitySettings.GossipSeeds = Hosts;

			// why? if discovery then all hosts are used for discovery
			// if (Hosts.Any(x => x is DnsEndPoint))
			// 	settings.ConnectivitySettings.DnsGossipSeeds =
			// 		Array.ConvertAll(Hosts, x => new DnsEndPoint(x.GetHost(), x.GetPort()));
			// else
			// 	settings.ConnectivitySettings.IpGossipSeeds = Array.ConvertAll(Hosts, x => (IPEndPoint)x);
		}

		if (typedOptions.TryGetValue(TlsVerifyCert, out var tlsVerifyCert))
			settings.ConnectivitySettings.TlsVerifyCert = (bool)tlsVerifyCert;

		if (typedOptions.TryGetValue(TlsCaFile, out var tlsCaFile)) {
			var tlsCaFilePath = Path.GetFullPath((string)tlsCaFile);
			if (!string.IsNullOrEmpty(tlsCaFilePath) && !File.Exists(tlsCaFilePath))
				throw new InvalidClientCertificateException("Failed to load certificate. File was not found.");

			try {
#if NET9_0_OR_GREATER
				settings.ConnectivitySettings.TlsCaFile = X509CertificateLoader.LoadCertificateFromFile(tlsCaFilePath);
#else
				settings.ConnectivitySettings.TlsCaFile = new X509Certificate2(tlsCaFilePath);
#endif
			}
			catch (CryptographicException) {
				throw new InvalidClientCertificateException("Failed to load certificate. Invalid file format.");
			}
		}

		settings.ConnectivitySettings.SslCredentials = new() {
			ClientCertificatePath    = GetOptionValueAsString(UserCertFile),
			ClientCertificateKeyPath = GetOptionValueAsString(UserKeyFile),
			RootCertificatePath      = GetOptionValueAsString(TlsCaFile),
			VerifyServerCertificate  = settings.ConnectivitySettings.TlsVerifyCert
		};

		ConfigureClientCertificate(settings, typedOptions);

		settings.CreateHttpMessageHandler = CreateDefaultHandler;

		return settings;

		string GetOptionValueAsString(string key) => typedOptions.TryGetValue(key, out var value) ? (string)value : "";

		HttpMessageHandler CreateDefaultHandler() {
			var handler = new SocketsHttpHandler {
				KeepAlivePingDelay             = settings.ConnectivitySettings.KeepAliveInterval,
				KeepAlivePingTimeout           = settings.ConnectivitySettings.KeepAliveTimeout,
				EnableMultipleHttp2Connections = true
			};

			if (settings.ConnectivitySettings.Insecure)
				return handler;

			if (settings.ConnectivitySettings.ClientCertificate is not null)
				handler.SslOptions.ClientCertificates = new X509CertificateCollection {
					settings.ConnectivitySettings.ClientCertificate
				};

			handler.SslOptions.RemoteCertificateValidationCallback = settings.ConnectivitySettings.TlsVerifyCert switch {
				false => delegate { return true; },
				true when settings.ConnectivitySettings.TlsCaFile is not null => (_, certificate, chain, _) => {
					if (certificate is not X509Certificate2 peerCertificate || chain is null) return false;

					chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
					chain.ChainPolicy.CustomTrustStore.Add(settings.ConnectivitySettings.TlsCaFile);
					return chain.Build(peerCertificate);
				},
				_ => null
			};

			return handler;
		}

		static void ConfigureClientCertificate(KurrentDBClientSettings settings, IReadOnlyDictionary<string, object> options) {
			var certPemFilePath = GetOptionValueAsString(UserCertFile);
			var keyPemFilePath  = GetOptionValueAsString(UserKeyFile);

			if (string.IsNullOrEmpty(certPemFilePath) && string.IsNullOrEmpty(keyPemFilePath))
				return;

			if (string.IsNullOrEmpty(certPemFilePath) || string.IsNullOrEmpty(keyPemFilePath))
				throw new InvalidClientCertificateException("Invalid client certificate settings. Both UserCertFile and UserKeyFile must be set.");

			if (!File.Exists(certPemFilePath))
				throw new InvalidClientCertificateException($"Invalid client certificate settings. The specified UserCertFile does not exist: {certPemFilePath}");

			if (!File.Exists(keyPemFilePath))
				throw new InvalidClientCertificateException($"Invalid client certificate settings. The specified UserKeyFile does not exist: {keyPemFilePath}");

			try {
				settings.ConnectivitySettings.ClientCertificate = X509Certificates.CreateFromPemFile(certPemFilePath, keyPemFilePath);
			}
			catch (Exception ex) {
				throw new InvalidClientCertificateException($"Failed to create client certificate: {ex.Message}", ex);
			}

			return;

			string GetOptionValueAsString(string key) => options.TryGetValue(key, out var value) ? (string)value : "";
		}
	}
}
