using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using AutoFixture;

namespace KurrentDB.Client.Tests;

// public class RagingConnectionStringTests {
// 	[Fact]
// 	public void RagingConnectionStringTest() {
// 		var connString = "kurrentdb://admin:changeit@localhost:2113/?tls=true&maxDiscoverAttempts=5";
//
// 		var connectionString = KurrentDBConnectionString.Parse(connString);
//
// 		var settings = connectionString.ToClientSettings();
// 		var (channel, invoker) = settings.CreateGrpcChannel();
//
// 		// GrpcChannel grpcClient = GrpcChannel.ForAddress(
// 		// 	settings.ConnectivitySettings.ResolvedAddressOrDefault.ToString()
// 		// );
// 		//
// 		// var callInvoker = grpcClient.CreateCallInvoker();
//
//
// 		Assert.NotNull(settings);
// 	}
// }


[Trait("Category", "Target:Misc")]
public class ConnectionStringTests {
	public static IEnumerable<object?[]> ValidCases() {
		var fixture = new Fixture();
		fixture.Customize<TimeSpan>(composer => composer.FromFactory<int>(s => TimeSpan.FromSeconds(s % 60)));
		fixture.Customize<Uri>(
			composer => composer.FromFactory<DnsEndPoint>(
				e => new UriBuilder {
					Host = e.Host,
					Port = e.Port == 80 ? 81 : e.Port
				}.Uri
			)
		);

		fixture.Register<X509Certificate2>(() => null!);

		return Enumerable.Range(0, 3).SelectMany(GetTestCases);

		IEnumerable<object?[]> GetTestCases(int _) {
			var settings = new KurrentDBClientSettings {
				ConnectionName       = fixture.Create<string>(),
				ConnectivitySettings = fixture.Create<KurrentDBClientConnectivitySettings>(),
				OperationOptions     = fixture.Create<KurrentDBClientOperationOptions>()
			};

			settings.ConnectivitySettings.Address =
				new UriBuilder(KurrentDBClientConnectivitySettings.Default.ResolvedAddressOrDefault) {
					Scheme = settings.ConnectivitySettings.ResolvedAddressOrDefault.Scheme
				}.Uri;

			yield return [
				GetConnectionString(settings),
				settings
			];

			yield return [
				GetConnectionString(settings, MockingTone),
				settings
			];

			var ipGossipSettings = new KurrentDBClientSettings {
				ConnectionName       = fixture.Create<string>(),
				ConnectivitySettings = fixture.Create<KurrentDBClientConnectivitySettings>(),
				OperationOptions     = fixture.Create<KurrentDBClientOperationOptions>()
			};

			ipGossipSettings.ConnectivitySettings.Address =
				new UriBuilder(KurrentDBClientConnectivitySettings.Default.ResolvedAddressOrDefault) {
					Scheme = ipGossipSettings.ConnectivitySettings.ResolvedAddressOrDefault.Scheme
				}.Uri;

			ipGossipSettings.ConnectivitySettings.GossipSeeds = [];

			yield return [
				GetConnectionString(ipGossipSettings),
				ipGossipSettings
			];

			yield return [
				GetConnectionString(ipGossipSettings, MockingTone),
				ipGossipSettings
			];

			var singleNodeSettings = new KurrentDBClientSettings {
				ConnectionName       = fixture.Create<string>(),
				ConnectivitySettings = fixture.Create<KurrentDBClientConnectivitySettings>(),
				OperationOptions     = fixture.Create<KurrentDBClientOperationOptions>()
			};

			singleNodeSettings.ConnectivitySettings.GossipSeeds = [];
			singleNodeSettings.ConnectivitySettings.Address = new UriBuilder(fixture.Create<Uri>()) {
				Scheme = singleNodeSettings.ConnectivitySettings.ResolvedAddressOrDefault.Scheme
			}.Uri;

			yield return [
				GetConnectionString(singleNodeSettings),
				singleNodeSettings
			];

			yield return [
				GetConnectionString(singleNodeSettings, MockingTone),
				singleNodeSettings
			];
		}

		static string MockingTone(string key) => new(key.Select((c, i) => i % 2 == 0 ? char.ToUpper(c) : char.ToLower(c)).ToArray());
	}

	[Theory]
	[MemberData(nameof(ValidCases))]
	public void valid_connection_string(string connectionString, KurrentDBClientSettings expected) {
		var result = KurrentDBClientSettings.Create(connectionString);

		Assert.Equal(expected, result, KurrentDBClientSettingsEqualityComparer.Instance);
	}

	[Theory]
	[MemberData(nameof(ValidCases))]
	public void valid_connection_string_with_empty_path(string connectionString, KurrentDBClientSettings expected) {
		var result = KurrentDBClientSettings.Create(connectionString.Replace("?", "/?"));

		Assert.Equal(expected, result, KurrentDBClientSettingsEqualityComparer.Instance);
	}

#if !GRPC_CORE
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void tls_verify_cert(bool tlsVerifyCert) {
		var       connectionString = $"kurrentdb://localhost:2113/?tlsVerifyCert={tlsVerifyCert}";
		var       result           = KurrentDBClientSettings.Create(connectionString);

		using var handler          = result.CreateHttpMessageHandler?.Invoke();

		var socketsHandler = Assert.IsType<SocketsHttpHandler>(handler);
		if (!tlsVerifyCert) {
			Assert.NotNull(socketsHandler.SslOptions.RemoteCertificateValidationCallback);
			Assert.True(socketsHandler.SslOptions.RemoteCertificateValidationCallback!.Invoke(null!, default, default, default));
		} else {
			Assert.Null(socketsHandler.SslOptions.RemoteCertificateValidationCallback);
		}
	}

#endif

	public static IEnumerable<object?[]> InvalidTlsCertificates() {
		yield return [Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "path", "not", "found")];
		yield return [Assembly.GetExecutingAssembly().Location];
	}

	[Theory]
	[MemberData(nameof(InvalidTlsCertificates))]
	public void connection_string_with_invalid_tls_certificate_should_throw(string clientCertificatePath) {
		Assert.Throws<InvalidClientCertificateException>(
			() => KurrentDBClientSettings.Create($"kurrentdb://admin:changeit@localhost:2113/?tls=true&tlsVerifyCert=true&tlsCAFile={clientCertificatePath}")
		);
	}

	public static IEnumerable<object?[]> InvalidClientCertificates() {
		var invalidPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "path", "not", "found");
		var validPath   = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certs", "ca", "ca.crt");
		yield return [invalidPath, invalidPath];
		yield return [validPath, invalidPath];
	}

	[Theory]
	[MemberData(nameof(InvalidClientCertificates))]
	public void connection_string_with_invalid_client_certificate_should_throw(string userCertFile, string userKeyFile) {
		Assert.Throws<InvalidClientCertificateException>(
			() => KurrentDBClientSettings.Create(
				$"kurrentdb://admin:changeit@localhost:2113/?tls=true&tlsVerifyCert=true&userCertFile={userCertFile}&userKeyFile={userKeyFile}"
			)
		);
	}

	[RetryFact]
	public void infinite_grpc_timeouts() {
		var result = KurrentDBClientSettings.Create("kurrentdb://localhost:2113?keepAliveInterval=-1&keepAliveTimeout=-1");

		Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, result.ConnectivitySettings.KeepAliveInterval);
		Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, result.ConnectivitySettings.KeepAliveTimeout);

		using var handler = result.CreateHttpMessageHandler?.Invoke();

		var socketsHandler = Assert.IsType<SocketsHttpHandler>(handler);
		Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, socketsHandler.KeepAlivePingTimeout);
		Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, socketsHandler.KeepAlivePingDelay);
	}

	[RetryFact]
	public void connection_string_with_no_schema() => Assert.Throws<NoSchemeException>(() => KurrentDBClientSettings.Create(":so/mething/random"));

	[Theory]
	[InlineData("esdbwrong://")]
	[InlineData("wrong://")]
	[InlineData("badkurrentdb://")]
	public void connection_string_with_invalid_scheme_should_throw(string connectionString) =>
		Assert.Throws<InvalidSchemeException>(() => KurrentDBClientSettings.Create(connectionString));

	[Theory]
	[InlineData("kurrentdb://userpass@127.0.0.1/")]
	[InlineData("kurrentdb://user:pa:ss@127.0.0.1/")]
	[InlineData("kurrentdb://us:er:pa:ss@127.0.0.1/")]
	public void connection_string_with_invalid_userinfo_should_throw(string connectionString) =>
		Assert.Throws<InvalidUserCredentialsException>(() => KurrentDBClientSettings.Create(connectionString));

	[Theory]
	[InlineData("kurrentdb://user:pass@127.0.0.1:abc")]
	[InlineData("kurrentdb://user:pass@127.0.0.1:abc/")]
	[InlineData("kurrentdb://user:pass@127.0.0.1:1234,127.0.0.2:abc,127.0.0.3:4321")]
	[InlineData("kurrentdb://user:pass@127.0.0.1:1234,127.0.0.2:abc,127.0.0.3:4321/")]
	[InlineData("kurrentdb://user:pass@127.0.0.1:abc:def")]
	[InlineData("kurrentdb://user:pass@127.0.0.1:abc:def/")]
	[InlineData("kurrentdb://user:pass@localhost:1234,127.0.0.2:abc:def,127.0.0.3:4321")]
	[InlineData("kurrentdb://user:pass@localhost:1234,127.0.0.2:abc:def,127.0.0.3:4321/")]
	[InlineData("kurrentdb://user:pass@localhost:1234,,127.0.0.3:4321")]
	[InlineData("kurrentdb://user:pass@localhost:1234,,127.0.0.3:4321/")]
	public void connection_string_with_invalid_host_should_throw(string connectionString) =>
		Assert.Throws<InvalidHostException>(() => KurrentDBClientSettings.Create(connectionString));

	[Theory]
	[InlineData("kurrentdb://user:pass@127.0.0.1/test")]
	[InlineData("kurrentdb://user:pass@127.0.0.1/maxDiscoverAttempts=10")]
	[InlineData("kurrentdb://user:pass@127.0.0.1/hello?maxDiscoverAttempts=10")]
	public void connection_string_with_non_empty_path_should_throw(string connectionString) =>
		Assert.Throws<ConnectionStringParseException>(() => KurrentDBClientSettings.Create(connectionString));

	[Theory]
	[InlineData("kurrentdb://user:pass@127.0.0.1")]
	[InlineData("kurrentdb://user:pass@127.0.0.1/")]
	[InlineData("kurrentdb+discover://user:pass@127.0.0.1")]
	[InlineData("kurrentdb+discover://user:pass@127.0.0.1/")]
	public void connection_string_with_no_key_value_pairs_specified_should_not_throw(string connectionString) =>
		KurrentDBClientSettings.Create(connectionString);

	[Theory]
	[InlineData("kurrentdb://user:pass@127.0.0.1/?maxDiscoverAttempts=12=34")]
	[InlineData("kurrentdb://user:pass@127.0.0.1/?maxDiscoverAttempts1234")]
	public void connection_string_with_invalid_key_value_pair_should_throw(string connectionString) =>
		Assert.Throws<InvalidKeyValuePairException>(() => KurrentDBClientSettings.Create(connectionString));

	[Theory]
	[InlineData("kurrentdb://user:pass@127.0.0.1/?maxDiscoverAttempts=1234&MaxDiscoverAttempts=10")]
	[InlineData("kurrentdb://user:pass@127.0.0.1/?gossipTimeout=10&gossipTimeout=30")]
	public void connection_string_with_duplicate_key_should_throw(string connectionString) =>
		Assert.Throws<DuplicateKeyException>(() => KurrentDBClientSettings.Create(connectionString));

	[Theory]
	[InlineData("kurrentdb://user:pass@127.0.0.1/?unknown=1234")]
	[InlineData("kurrentdb://user:pass@127.0.0.1/?maxDiscoverAttempts=1234&hello=test")]
	[InlineData("kurrentdb://user:pass@127.0.0.1/?maxDiscoverAttempts=abcd")]
	[InlineData("kurrentdb://user:pass@127.0.0.1/?discoveryInterval=abcd")]
	[InlineData("kurrentdb://user:pass@127.0.0.1/?gossipTimeout=defg")]
	[InlineData("kurrentdb://user:pass@127.0.0.1/?tlsVerifyCert=truee")]
	[InlineData("kurrentdb://user:pass@127.0.0.1/?nodePreference=blabla")]
	[InlineData("kurrentdb://user:pass@127.0.0.1/?keepAliveInterval=-2")]
	[InlineData("kurrentdb://user:pass@127.0.0.1/?keepAliveTimeout=-2")]
	public void connection_string_with_invalid_settings_should_throw(string connectionString) =>
		Assert.Throws<InvalidSettingException>(() => KurrentDBClientSettings.Create(connectionString));

	[RetryFact]
	public void with_default_settings() {
		var settings = KurrentDBClientSettings.Create("kurrentdb://hostname:4321/");

		Assert.Null(settings.ConnectionName);
		Assert.Equal(
			KurrentDBClientConnectivitySettings.Default.ResolvedAddressOrDefault.Scheme,
			settings.ConnectivitySettings.ResolvedAddressOrDefault.Scheme
		);

		Assert.Equal(
			KurrentDBClientConnectivitySettings.Default.DiscoveryInterval.TotalMilliseconds,
			settings.ConnectivitySettings.DiscoveryInterval.TotalMilliseconds
		);

		Assert.Empty(KurrentDBClientConnectivitySettings.Default.GossipSeeds);
		Assert.Equal(
			KurrentDBClientConnectivitySettings.Default.GossipTimeout.TotalMilliseconds,
			settings.ConnectivitySettings.GossipTimeout.TotalMilliseconds
		);

		Assert.Equal(
			KurrentDBClientConnectivitySettings.Default.MaxDiscoverAttempts,
			settings.ConnectivitySettings.MaxDiscoverAttempts
		);

		Assert.Equal(
			KurrentDBClientConnectivitySettings.Default.NodePreference,
			settings.ConnectivitySettings.NodePreference
		);

		Assert.Equal(
			KurrentDBClientConnectivitySettings.Default.Insecure,
			settings.ConnectivitySettings.Insecure
		);

		Assert.Equal(TimeSpan.FromSeconds(10), settings.DefaultDeadline);
		Assert.Equal(
			KurrentDBClientOperationOptions.Default.ThrowOnAppendFailure,
			settings.OperationOptions.ThrowOnAppendFailure
		);

		Assert.Equal(
			KurrentDBClientConnectivitySettings.Default.KeepAliveInterval,
			settings.ConnectivitySettings.KeepAliveInterval
		);

		Assert.Equal(
			KurrentDBClientConnectivitySettings.Default.KeepAliveTimeout,
			settings.ConnectivitySettings.KeepAliveTimeout
		);
	}

	[Theory]
	[InlineData("kurrentdb://localhost", true)]
	[InlineData("kurrentdb://localhost/?tls=false", false)]
	[InlineData("kurrentdb://localhost/?tls=true", true)]
	[InlineData("kurrentdb://localhost1,localhost2,localhost3", true)]
	[InlineData("kurrentdb://localhost1,localhost2,localhost3/?tls=false", false)]
	[InlineData("kurrentdb://localhost1,localhost2,localhost3/?tls=true", true)]
	public void use_tls(string connectionString, bool expectedUseTls) {
		var result         = KurrentDBClientSettings.Create(connectionString);
		var expectedScheme = expectedUseTls ? "https" : "http";
		Assert.NotEqual(expectedUseTls, result.ConnectivitySettings.Insecure);
		Assert.Equal(expectedScheme, result.ConnectivitySettings.ResolvedAddressOrDefault.Scheme);
	}

	[Theory]
	[InlineData("kurrentdb://localhost", null, true)]
	[InlineData("kurrentdb://localhost", true, false)]
	[InlineData("kurrentdb://localhost", false, true)]
	[InlineData("kurrentdb://localhost/?tls=true", null, true)]
	[InlineData("kurrentdb://localhost/?tls=true", true, false)]
	[InlineData("kurrentdb://localhost/?tls=true", false, true)]
	[InlineData("kurrentdb://localhost/?tls=false", null, false)]
	[InlineData("kurrentdb://localhost/?tls=false", true, false)]
	[InlineData("kurrentdb://localhost/?tls=false", false, true)]
	[InlineData("kurrentdb://localhost1,localhost2,localhost3", null, true)]
	[InlineData("kurrentdb://localhost1,localhost2,localhost3", true, true)]
	[InlineData("kurrentdb://localhost1,localhost2,localhost3", false, true)]
	[InlineData("kurrentdb://localhost1,localhost2,localhost3/?tls=true", null, true)]
	[InlineData("kurrentdb://localhost1,localhost2,localhost3/?tls=true", true, true)]
	[InlineData("kurrentdb://localhost1,localhost2,localhost3/?tls=true", false, true)]
	[InlineData("kurrentdb://localhost1,localhost2,localhost3/?tls=false", null, false)]
	[InlineData("kurrentdb://localhost1,localhost2,localhost3/?tls=false", true, false)]
	[InlineData("kurrentdb://localhost1,localhost2,localhost3/?tls=false", false, false)]
	public void allow_tls_override_for_single_node(string connectionString, bool? insecureOverride, bool expectedUseTls) {
		var result   = KurrentDBClientSettings.Create(connectionString);
		var settings = result.ConnectivitySettings;

		if (insecureOverride.HasValue)
			settings.Address = new UriBuilder {
				Scheme = insecureOverride.Value ? "hTTp" : "HttpS"
			}.Uri;

		var expectedScheme = expectedUseTls ? "https" : "http";
		Assert.Equal(expectedUseTls, !settings.Insecure);
		Assert.Equal(expectedScheme, result.ConnectivitySettings.ResolvedAddressOrDefault.Scheme);
	}

	[Theory]
	[InlineData("kurrentdb://localhost:1234", "localhost", 1234)]
	[InlineData("kurrentdb://localhost:1234,localhost:4567", null, null)]
	[InlineData("kurrentdb+discover://localhost:1234", null, null)]
	[InlineData("kurrentdb+discover://localhost:1234,localhost:4567", null, null)]
	public void connection_string_with_custom_ports(string connectionString, string? expectedHost, int? expectedPort) {
		var result               = KurrentDBClientSettings.Create(connectionString);
		var connectivitySettings = result.ConnectivitySettings;

		Assert.Equal(expectedHost, connectivitySettings.Address?.Host);
		Assert.Equal(expectedPort, connectivitySettings.Address?.Port);
	}

	static string GetConnectionString(
		KurrentDBClientSettings settings,
		Func<string, string>? getKey = default
	) =>
		$"{GetScheme(settings)}{GetAuthority(settings)}?{GetKeyValuePairs(settings, getKey)}";

	static string GetScheme(KurrentDBClientSettings settings) =>
		settings.ConnectivitySettings.IsSingleNode
			? "kurrentdb://"
			: "kurrentdb+discover://";

	static string GetAuthority(KurrentDBClientSettings settings) =>
		settings.ConnectivitySettings.IsSingleNode
			? $"{settings.ConnectivitySettings.ResolvedAddressOrDefault.Host}:{settings.ConnectivitySettings.ResolvedAddressOrDefault.Port}"
			: string.Join(",", settings.ConnectivitySettings.GossipSeeds.Select(x => $"{x.Host}:{x.Port}"));

	static string GetKeyValuePairs(
		KurrentDBClientSettings settings,
		Func<string, string>? getKey = default
	) {
		var pairs = new Dictionary<string, string?> {
			["tls"]                 = (!settings.ConnectivitySettings.Insecure).ToString(),
			["connectionName"]      = settings.ConnectionName,
			["maxDiscoverAttempts"] = settings.ConnectivitySettings.MaxDiscoverAttempts.ToString(),
			["discoveryInterval"]   = settings.ConnectivitySettings.DiscoveryInterval.TotalMilliseconds.ToString(),
			["gossipTimeout"]       = settings.ConnectivitySettings.GossipTimeout.TotalMilliseconds.ToString(),
			["nodePreference"]      = settings.ConnectivitySettings.NodePreference.ToString(),
			["keepAliveInterval"]   = settings.ConnectivitySettings.KeepAliveInterval.TotalMilliseconds.ToString(),
			["keepAliveTimeout"]    = settings.ConnectivitySettings.KeepAliveTimeout.TotalMilliseconds.ToString()
		};

		if (settings.DefaultDeadline.HasValue)
			pairs.Add(
				"defaultDeadline",
				settings.DefaultDeadline.Value.TotalMilliseconds.ToString()
			);

		if (settings.CreateHttpMessageHandler != null) {
			using var handler = settings.CreateHttpMessageHandler.Invoke();

			if (handler is SocketsHttpHandler socketsHttpHandler &&
			    socketsHttpHandler.SslOptions.RemoteCertificateValidationCallback != null)
				pairs.Add("tlsVerifyCert", "false");
		}

		return string.Join("&", pairs.Select(pair => $"{getKey?.Invoke(pair.Key) ?? pair.Key}={pair.Value}"));
	}

	class KurrentDBClientSettingsEqualityComparer : IEqualityComparer<KurrentDBClientSettings> {
		public static readonly KurrentDBClientSettingsEqualityComparer Instance = new();

		public bool Equals(KurrentDBClientSettings? x, KurrentDBClientSettings? y) {
			if (ReferenceEquals(x, y))
				return true;

			if (ReferenceEquals(x, null))
				return false;

			if (ReferenceEquals(y, null))
				return false;

			if (x.GetType() != y.GetType())
				return false;

			return x.ConnectionName == y.ConnectionName &&
			       KurrentDBClientConnectivitySettingsEqualityComparer.Instance.Equals(
				       x.ConnectivitySettings,
				       y.ConnectivitySettings
			       ) &&
			       KurrentDBClientOperationOptionsEqualityComparer.Instance.Equals(
				       x.OperationOptions,
				       y.OperationOptions
			       ) &&
			       Equals(x.DefaultCredentials?.ToString(), y.DefaultCredentials?.ToString());
		}

		public int GetHashCode(KurrentDBClientSettings obj) =>
			HashCode.Hash
				.Combine(obj.ConnectionName)
				.Combine(KurrentDBClientConnectivitySettingsEqualityComparer.Instance.GetHashCode(obj.ConnectivitySettings))
				.Combine(KurrentDBClientOperationOptionsEqualityComparer.Instance.GetHashCode(obj.OperationOptions));
	}

	class KurrentDBClientConnectivitySettingsEqualityComparer
		: IEqualityComparer<KurrentDBClientConnectivitySettings> {
		public static readonly KurrentDBClientConnectivitySettingsEqualityComparer Instance = new();

		public bool Equals(KurrentDBClientConnectivitySettings? x, KurrentDBClientConnectivitySettings? y) {
			if (ReferenceEquals(x, y))
				return true;

			if (ReferenceEquals(x, null))
				return false;

			if (ReferenceEquals(y, null))
				return false;

			if (x.GetType() != y.GetType())
				return false;

			return (!x.IsSingleNode || x.ResolvedAddressOrDefault.Equals(y.Address)) &&
			       x.MaxDiscoverAttempts == y.MaxDiscoverAttempts &&
			       x.GossipSeeds.SequenceEqual(y.GossipSeeds) &&
			       x.GossipTimeout.Equals(y.GossipTimeout) &&
			       x.DiscoveryInterval.Equals(y.DiscoveryInterval) &&
			       x.NodePreference == y.NodePreference &&
			       x.KeepAliveInterval.Equals(y.KeepAliveInterval) &&
			       x.KeepAliveTimeout.Equals(y.KeepAliveTimeout) &&
			       x.Insecure == y.Insecure;
		}

		public int GetHashCode(KurrentDBClientConnectivitySettings obj) =>
			obj.GossipSeeds.Aggregate(
				HashCode.Hash
					.Combine(obj.ResolvedAddressOrDefault.GetHashCode())
					.Combine(obj.MaxDiscoverAttempts)
					.Combine(obj.GossipTimeout)
					.Combine(obj.DiscoveryInterval)
					.Combine(obj.NodePreference)
					.Combine(obj.KeepAliveInterval)
					.Combine(obj.KeepAliveTimeout)
					.Combine(obj.Insecure),
				(hashcode, endpoint) => hashcode.Combine(endpoint.GetHashCode())
			);
	}

	class KurrentDBClientOperationOptionsEqualityComparer
		: IEqualityComparer<KurrentDBClientOperationOptions> {
		public static readonly KurrentDBClientOperationOptionsEqualityComparer Instance = new();

		public bool Equals(KurrentDBClientOperationOptions? x, KurrentDBClientOperationOptions? y) {
			if (ReferenceEquals(x, y))
				return true;

			if (ReferenceEquals(x, null))
				return false;

			if (ReferenceEquals(y, null))
				return false;

			return x.GetType() == y.GetType();
		}

		public int GetHashCode(KurrentDBClientOperationOptions obj) =>
			System.HashCode.Combine(obj.ThrowOnAppendFailure);
	}
}
