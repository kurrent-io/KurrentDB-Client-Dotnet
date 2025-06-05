using System.Collections.Immutable;
using System.Net;
using KurrentDB.Client;
using KurrentDB.Client.Next;
using UserCredentials = KurrentDB.Client.Next.UserCredentials;

namespace Kurrent.Client.Tests.Experimental;

public class KurrentDBConnectionParsingTests {
	[Test]
	public void Parse_SimpleDirectConnection_ShouldReturnCorrectSettings() {
		// Arrange
		var connectionString = "kurrentdb://localhost:2113";

		// Act
		var settings = KurrentClientConnectionSettings.Parse(connectionString);

		// Assert
		settings.Scheme.ShouldBe(ConnectionScheme.Direct);
		settings.Endpoints.ShouldHaveSingleItem();
		settings.Endpoints[0].ShouldBeOfType<DnsEndPoint>();
		var endpoint = (DnsEndPoint)settings.Endpoints[0];
		endpoint.Host.ShouldBe("localhost");
		endpoint.Port.ShouldBe(2113);
		settings.EffectiveTls.Enabled.ShouldBeTrue();
	}

	[Test]
	public void Parse_ClusterConnectionWithDiscovery_ShouldReturnCorrectSettings() {
		// Arrange
		var connectionString = "kurrentdb+discover://admin:changeit@cluster.local:2113";

		// Act
		var settings = KurrentClientConnectionSettings.Parse(connectionString);

		// Assert
		settings.Scheme.ShouldBe(ConnectionScheme.Discover);
		settings.UserCredentials.Username.ShouldBe("admin");
		settings.UserCredentials.Password.ShouldBe("changeit");
		settings.UserCredentials.IsEmpty.ShouldBeFalse();
		settings.Endpoints.ShouldHaveSingleItem();
		var endpoint = (DnsEndPoint)settings.Endpoints[0];
		endpoint.Host.ShouldBe("cluster.local");
		endpoint.Port.ShouldBe(2113);
	}

	[Test]
	public void Parse_MultipleHosts_ShouldReturnAllEndpoints() {
		// Arrange
		var connectionString = "kurrentdb+discover://node1:2113,node2:2114,192.168.1.100:2115";

		// Act
		var settings = KurrentClientConnectionSettings.Parse(connectionString);

		// Assert
		settings.Endpoints.Length.ShouldBe(3);

		var endpoint1 = (DnsEndPoint)settings.Endpoints[0];
		endpoint1.Host.ShouldBe("node1");
		endpoint1.Port.ShouldBe(2113);

		var endpoint2 = (DnsEndPoint)settings.Endpoints[1];
		endpoint2.Host.ShouldBe("node2");
		endpoint2.Port.ShouldBe(2114);

		var endpoint3 = (IPEndPoint)settings.Endpoints[2];
		endpoint3.Address.ToString().ShouldBe("192.168.1.100");
		endpoint3.Port.ShouldBe(2115);
	}

	[Test]
	public void Parse_InsecureConnection_ShouldDisableTls() {
		// Arrange
		var connectionString = "kurrentdb://localhost:2113?tls=false";

		// Act
		var settings = KurrentClientConnectionSettings.Parse(connectionString);

		// Assert
		settings.EffectiveTls.Enabled.ShouldBeFalse();
		settings.EffectiveTls.VerifyCertificate.ShouldBeFalse();
	}

	[Test]
	public void Parse_AllQueryParameters_ShouldParseCorrectly() {
		// Arrange
		var connectionString = "kurrentdb+discover://admin:password@cluster:2113" +
		                       "?tls=true&tlsVerifyCert=false&connectionName=TestConnection" +
		                       "&nodePreference=follower&maxDiscoverAttempts=5" +
		                       "&discoveryInterval=200&gossipTimeout=10" +
		                       "&defaultDeadline=30000&keepAliveInterval=15" +
		                       "&keepAliveTimeout=20&tlsCaFile=/path/to/ca.crt" +
		                       "&userCertFile=/path/to/cert.crt&userKeyFile=/path/to/key.key";

		// Act
		var settings = KurrentClientConnectionSettings.Parse(connectionString);

		// Assert
		settings.EffectiveTls.Enabled.ShouldBeTrue();
		settings.EffectiveTls.VerifyCertificate.ShouldBeFalse();
		settings.EffectiveTls.CaFile.ShouldBe("/path/to/ca.crt");
		settings.ConnectionName.ShouldBe("TestConnection");
		settings.NodePreference.ShouldBe(NodePreference.Follower);
		settings.EffectiveGossip.MaxDiscoverAttempts.ShouldBe(5);
		settings.Gossip.DiscoveryInterval.ShouldBe(TimeSpan.FromMilliseconds(200));
		settings.Gossip.Timeout.ShouldBe(TimeSpan.FromSeconds(10));
		settings.DefaultDeadline.ShouldBe(TimeSpan.FromMilliseconds(30000));
		settings.EffectiveKeepAliveInterval.ShouldBe(TimeSpan.FromSeconds(15));
		settings.EffectiveKeepAliveTimeout.ShouldBe(TimeSpan.FromSeconds(20));
		settings.CertificateCredentials.CertificateFile.ShouldBe("/path/to/cert.crt");
		settings.CertificateCredentials.KeyFile.ShouldBe("/path/to/key.key");
		settings.HasCertificateAuthentication.ShouldBeTrue();
	}

	[Test]
	public void Parse_NodePreferences_ShouldParseAllValues() {
		var testCases = new[] {
			("leader", NodePreference.Leader),
			("follower", NodePreference.Follower),
			("random", NodePreference.Random),
			("readOnlyReplica", NodePreference.ReadOnlyReplica),
			("readonly", NodePreference.ReadOnlyReplica) // Alias
		};

		foreach (var (preference, expected) in testCases) {
			// Arrange
			var connectionString = $"kurrentdb://localhost:2113?nodePreference={preference}";

			// Act
			var settings = KurrentClientConnectionSettings.Parse(connectionString);

			// Assert
			settings.NodePreference.ShouldBe(expected, $"Node preference '{preference}' should map to {expected}");
		}
	}

	[Test]
	public void Parse_EscapedCharacters_ShouldDecodeCorrectly() {
		// Arrange
		var connectionString = "kurrentdb://user%40domain.com:p%40ssw%24rd@localhost:2113?connectionName=My%20Connection";

		// Act
		var settings = KurrentClientConnectionSettings.Parse(connectionString);

		// Assert
		settings.UserCredentials.Username.ShouldBe("user@domain.com");
		settings.UserCredentials.Password.ShouldBe("p@ssw$rd");
		settings.ConnectionName.ShouldBe("My Connection");
	}

	[Test]
	public void TryParse_ValidConnectionString_ShouldReturnTrueAndSettings() {
		// Arrange
		var connectionString = "kurrentdb://localhost:2113";

		// Act
		var result = KurrentClientConnectionSettings.TryParse(connectionString, out var settings);

		// Assert
		result.ShouldBeTrue();
		settings.Scheme.ShouldBe(ConnectionScheme.Direct);
		settings.PrimaryEndpoint.ShouldNotBeNull();
	}

	[Test]
	public void TryParse_InvalidConnectionString_ShouldReturnFalse() {
		// Arrange
		var connectionString = "invalid://connection/string";

		// Act
		var result = KurrentClientConnectionSettings.TryParse(connectionString, out var settings);

		// Assert
		result.ShouldBeFalse();
		settings.ShouldBe(default(KurrentClientConnectionSettings));
	}
}

public class KurrentDBConnectionValidationTests {
	[Test]
	public void Validate_ValidSettings_ShouldReturnSuccess() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			[new DnsEndPoint("localhost", 2113)]
		);

		// Act
		var result = settings.Validate();

		// Assert
		result.IsValid.ShouldBeTrue();
		result.Errors.ShouldBeEmpty();
	}

	[Test]
	public void Validate_NoEndpoints_ShouldReturnError() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			ImmutableArray<EndPoint>.Empty
		);

		// Act
		var result = settings.Validate();

		// Assert
		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldContain("At least one endpoint must be specified");
	}

	[Test]
	public void Validate_InvalidPort_ShouldReturnError() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			[new DnsEndPoint("localhost", -1)]
		);

		// Act
		var result = settings.Validate();

		// Assert
		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldContain(error => error.Contains("Port must be between 1 and 65535"));
	}

	[Test]
	public void Validate_EmptyHostname_ShouldReturnError() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			[new DnsEndPoint("", 2113)]
		);

		// Act
		var result = settings.Validate();

		// Assert
		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldContain(error => error.Contains("Host cannot be empty"));
	}

	[Test]
	public void Validate_InvalidGossipSettings_ShouldReturnErrors() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Discover,
			[new DnsEndPoint("localhost", 2113)],
			Gossip: new GossipSettings(
				0,
				TimeSpan.FromSeconds(-1),
				TimeSpan.Zero
			)
		);

		// Act
		var result = settings.Validate();

		// Assert
		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldContain("MaxDiscoverAttempts must be greater than 0");
		result.Errors.ShouldContain(error => error.Contains("DiscoveryInterval must be greater than zero"));
		result.Errors.ShouldContain(error => error.Contains("GossipTimeout must be greater than zero"));
	}

	[Test]
	public void Validate_InvalidKeepAliveSettings_ShouldReturnErrors() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			[new DnsEndPoint("localhost", 2113)],
			KeepAliveInterval: TimeSpan.FromSeconds(-1),
			KeepAliveTimeout: TimeSpan.Zero
		);

		// Act
		var result = settings.Validate();

		// Assert
		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldContain("KeepAliveInterval must be greater than zero");
		result.Errors.ShouldContain("KeepAliveTimeout must be greater than zero");
	}

	[Test]
	public void Validate_InvalidCertificateCredentials_ShouldReturnErrors() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			[new DnsEndPoint("localhost", 2113)],
			CertificateCredentials: new CertificateCredentials("/path/to/cert.crt", null)
		);

		// Act
		var result = settings.Validate();

		// Assert
		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldContain("KeyFile must be specified when CertificateFile is provided");
	}

	[Test]
	public void Validate_TlsCaFileWithoutTls_ShouldReturnError() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			[new DnsEndPoint("localhost", 2113)],
			Tls: new TlsSettings(false, false, "/path/to/ca.crt")
		);

		// Act
		var result = settings.Validate();

		// Assert
		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldContain("TlsCaFile can only be used with TLS enabled");
	}

	[Test]
	public void Validate_InvalidDefaultDeadline_ShouldReturnError() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			[new DnsEndPoint("localhost", 2113)],
			DefaultDeadline: TimeSpan.FromSeconds(-1)
		);

		// Act
		var result = settings.Validate();

		// Assert
		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldContain("DefaultDeadline must be greater than zero when specified");
	}

	[Test]
	public void Validate_MultipleErrors_ShouldReturnAllErrors() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			ImmutableArray<EndPoint>.Empty,
			KeepAliveInterval: TimeSpan.Zero,
			DefaultDeadline: TimeSpan.FromSeconds(-1)
		);

		// Act
		var result = settings.Validate();

		// Assert
		result.IsValid.ShouldBeFalse();
		result.Errors.Length.ShouldBeGreaterThan(1);
		result.Errors.ShouldContain("At least one endpoint must be specified");
		result.Errors.ShouldContain("KeepAliveInterval must be greater than zero");
		result.Errors.ShouldContain("DefaultDeadline must be greater than zero when specified");
	}
}

public class KurrentDBConnectionSettingsTests {
	[Test]
	public void EffectiveTls_WhenNotSpecified_ShouldReturnDefault() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			[new DnsEndPoint("localhost", 2113)]
		);

		// Act & Assert
		settings.EffectiveTls.ShouldBe(TlsSettings.Default);
		settings.EffectiveTls.Enabled.ShouldBeTrue();
		settings.EffectiveTls.VerifyCertificate.ShouldBeTrue();
	}

	[Test]
	public void EffectiveGossip_WhenNotSpecified_ShouldReturnDefaults() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Discover,
			[new DnsEndPoint("localhost", 2113)]
		);

		// Act & Assert
		settings.EffectiveGossip.MaxDiscoverAttempts.ShouldBe(10);
		settings.EffectiveGossip.EffectiveDiscoveryInterval.ShouldBe(GossipSettings.DefaultDiscoveryInterval);
		settings.EffectiveGossip.EffectiveTimeout.ShouldBe(GossipSettings.DefaultTimeout);
	}

	[Test]
	public void EffectiveKeepAlive_WhenNotSpecified_ShouldReturnDefaults() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			[new DnsEndPoint("localhost", 2113)]
		);

		// Act & Assert
		settings.EffectiveKeepAliveInterval.ShouldBe(KurrentClientConnectionSettings.DefaultKeepAliveInterval);
		settings.EffectiveKeepAliveTimeout.ShouldBe(KurrentClientConnectionSettings.DefaultKeepAliveTimeout);
	}

	[Test]
	public void PrimaryEndpoint_WithEndpoints_ShouldReturnFirstEndpoint() {
		// Arrange
		var endpoint1 = new DnsEndPoint("node1", 2113);
		var endpoint2 = new DnsEndPoint("node2", 2113);
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Discover,
			[endpoint1, endpoint2]
		);

		// Act & Assert
		settings.PrimaryEndpoint.ShouldBe(endpoint1);
	}

	[Test]
	public void PrimaryEndpoint_WithoutEndpoints_ShouldReturnNull() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			ImmutableArray<EndPoint>.Empty
		);

		// Act & Assert
		settings.PrimaryEndpoint.ShouldBeNull();
	}

	[Test]
	public void IsClusterConnection_WithDiscoverScheme_ShouldReturnTrue() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Discover,
			[new DnsEndPoint("localhost", 2113)]
		);

		// Act & Assert
		settings.IsClusterConnection.ShouldBeTrue();
	}

	[Test]
	public void IsClusterConnection_WithDirectScheme_ShouldReturnFalse() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			[new DnsEndPoint("localhost", 2113)]
		);

		// Act & Assert
		settings.IsClusterConnection.ShouldBeFalse();
	}

	[Test]
	public void HasAuthentication_WithUserCredentials_ShouldReturnTrue() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			Scheme: ConnectionScheme.Direct,
			Endpoints: ImmutableArray.Create<EndPoint>(new DnsEndPoint("localhost", 2113)),
			UserCredentials: new UserCredentials("admin", "password")
		);

		// Act & Assert
		settings.HasAuthentication.ShouldBeTrue();
	}

	[Test]
	public void HasAuthentication_WithCertificateCredentials_ShouldReturnTrue() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			[new DnsEndPoint("localhost", 2113)],
			CertificateCredentials: new CertificateCredentials("/cert.crt", "/key.key")
		);

		// Act & Assert
		settings.HasAuthentication.ShouldBeTrue();
		settings.HasCertificateAuthentication.ShouldBeTrue();
	}

	[Test]
	public void HasAuthentication_WithoutCredentials_ShouldReturnFalse() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			[new DnsEndPoint("localhost", 2113)]
		);

		// Act & Assert
		settings.HasAuthentication.ShouldBeFalse();
		settings.HasCertificateAuthentication.ShouldBeFalse();
	}

	[Test]
	public void UserCredentials_IsEmpty_ShouldWorkCorrectly() {
		// Arrange & Act
		var emptyCredentials         = new UserCredentials();
		var nullCredentials          = new UserCredentials(null, null);
		var emptyUsernameCredentials = new UserCredentials("", "password");
		var validCredentials         = new UserCredentials("admin", "password");

		// Assert
		emptyCredentials.IsEmpty.ShouldBeTrue();
		nullCredentials.IsEmpty.ShouldBeTrue();
		emptyUsernameCredentials.IsEmpty.ShouldBeTrue();
		validCredentials.IsEmpty.ShouldBeFalse();
	}

	[Test]
	public void CertificateCredentials_IsEmpty_ShouldWorkCorrectly() {
		// Arrange & Act
		var emptyCredentials   = new CertificateCredentials();
		var partialCredentials = new CertificateCredentials("/cert.crt", null);
		var validCredentials   = new CertificateCredentials("/cert.crt", "/key.key");

		// Assert
		emptyCredentials.IsEmpty.ShouldBeTrue();
		partialCredentials.IsEmpty.ShouldBeTrue();
		validCredentials.IsEmpty.ShouldBeFalse();
	}
}

public class KurrentDBConnectionStringGenerationTests {
	[Test]
	public void ToString_SimpleDirectConnection_ShouldGenerateCorrectString() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			[new DnsEndPoint("localhost", 2113)]
		);

		// Act
		var connectionString = settings.ToString();

		// Assert
		connectionString.ShouldBe("kurrentdb://localhost:2113");
	}

	[Test]
	public void ToString_ClusterConnectionWithAuth_ShouldGenerateCorrectString() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			Scheme: ConnectionScheme.Discover,
			Endpoints: ImmutableArray.Create<EndPoint>(new DnsEndPoint("cluster.local", 2113)),
			UserCredentials: new UserCredentials("admin", "changeit")
		);

		// Act
		var connectionString = settings.ToString();

		// Assert
		connectionString.ShouldBe("kurrentdb+discover://admin:changeit@cluster.local:2113");
	}

	[Test]
	public void ToString_MultipleEndpoints_ShouldGenerateCorrectString() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Discover,
			[new DnsEndPoint("node1", 2113), new DnsEndPoint("node2", 2114), new IPEndPoint(IPAddress.Parse("192.168.1.100"), 2115)]
		);

		// Act
		var connectionString = settings.ToString();

		// Assert
		connectionString.ShouldBe("kurrentdb+discover://node1:2113,node2:2114,192.168.1.100:2115");
	}

	[Test]
	public void ToString_InsecureConnection_ShouldIncludeTlsFalse() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			[new DnsEndPoint("localhost", 2113)],
			Tls: TlsSettings.Insecure
		);

		// Act
		var connectionString = settings.ToString();

		// Assert
		connectionString.ShouldBe("kurrentdb://localhost:2113?tls=false");
	}

	[Test]
	public void ToString_AllQueryParameters_ShouldGenerateCorrectString() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			Scheme: ConnectionScheme.Discover,
			Endpoints: ImmutableArray.Create<EndPoint>(new DnsEndPoint("cluster", 2113)),
			UserCredentials: new UserCredentials("admin", "password"),
			Tls: new TlsSettings(true, false, "/ca.crt"),
			Gossip: new GossipSettings(
				5,
				TimeSpan.FromMilliseconds(200),
				TimeSpan.FromSeconds(10)
			),
			ConnectionName: "TestConnection",
			NodePreference: NodePreference.Follower,
			DefaultDeadline: TimeSpan.FromMilliseconds(30000),
			KeepAliveInterval: TimeSpan.FromSeconds(15),
			KeepAliveTimeout: TimeSpan.FromSeconds(20),
			CertificateCredentials: new CertificateCredentials("/cert.crt", "/key.key")
		);

		// Act
		var connectionString = settings.ToString();

		// Assert
		connectionString.ShouldContain("kurrentdb+discover://admin:password@cluster:2113?");
		connectionString.ShouldContain("tlsVerifyCert=false");
		connectionString.ShouldContain("connectionName=TestConnection");
		connectionString.ShouldContain("nodePreference=follower");
		connectionString.ShouldContain("maxDiscoverAttempts=5");
		connectionString.ShouldContain("discoveryInterval=200");
		connectionString.ShouldContain("gossipTimeout=10");
		connectionString.ShouldContain("defaultDeadline=30000");
		connectionString.ShouldContain("keepAliveInterval=15");
		connectionString.ShouldContain("keepAliveTimeout=20");
		connectionString.ShouldContain("tlsCaFile=%2Fca.crt");
		connectionString.ShouldContain("userCertFile=%2Fcert.crt");
		connectionString.ShouldContain("userKeyFile=%2Fkey.key");
	}

	[Test]
	public void ToString_EscapedCharacters_ShouldEscapeCorrectly() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			Scheme: ConnectionScheme.Direct,
			Endpoints: ImmutableArray.Create<EndPoint>(new DnsEndPoint("localhost", 2113)),
			UserCredentials: new UserCredentials("user@domain.com", "p@ssw$rd"),
			ConnectionName: "My Connection"
		);

		// Act
		var connectionString = settings.ToString();

		// Assert
		connectionString.ShouldContain("user%40domain.com:p%40ssw%24rd@");
		connectionString.ShouldContain("connectionName=My%20Connection");
	}

	[Test]
	public void ToString_RoundTrip_ShouldParseBackToOriginalSettings() {
		// Arrange
		var originalSettings = new KurrentClientConnectionSettings(
			Scheme: ConnectionScheme.Discover,
			Endpoints: ImmutableArray.Create<EndPoint>(new DnsEndPoint("cluster.local", 2113)),
			UserCredentials: new UserCredentials("admin", "changeit"),
			Tls: new TlsSettings(false),
			ConnectionName: "TestConnection",
			NodePreference: NodePreference.Random
		);

		// Act
		var connectionString = originalSettings.ToString();
		var parsedSettings   = KurrentClientConnectionSettings.Parse(connectionString);

		// Assert
		parsedSettings.Scheme.ShouldBe(originalSettings.Scheme);
		parsedSettings.Endpoints.Length.ShouldBe(originalSettings.Endpoints.Length);
		parsedSettings.UserCredentials.Username.ShouldBe(originalSettings.UserCredentials.Username);
		parsedSettings.UserCredentials.Password.ShouldBe(originalSettings.UserCredentials.Password);
		parsedSettings.EffectiveTls.Enabled.ShouldBe(originalSettings.EffectiveTls.Enabled);
		parsedSettings.ConnectionName.ShouldBe(originalSettings.ConnectionName);
		parsedSettings.NodePreference.ShouldBe(originalSettings.NodePreference);
	}
}

public class KurrentDBConnectionExceptionTests {
	[Test]
	public void Parse_NullConnectionString_ShouldThrowArgumentException() {
		// Act & Assert
		Should.Throw<ArgumentException>(() => KurrentClientConnectionSettings.Parse(null!));
	}

	[Test]
	public void Parse_EmptyConnectionString_ShouldThrowArgumentException() {
		// Act & Assert
		Should.Throw<ArgumentException>(() => KurrentClientConnectionSettings.Parse(""));
	}

	[Test]
	public void Parse_WhitespaceConnectionString_ShouldThrowArgumentException() {
		// Act & Assert
		Should.Throw<ArgumentException>(() => KurrentClientConnectionSettings.Parse("   "));
	}

	[Test]
	public void Parse_InvalidScheme_ShouldThrowKurrentDBConnectionStringException() {
		// Arrange
		var connectionString = "invalid://localhost:2113";

		// Act & Assert
		var exception = Should.Throw<KurrentDBConnectionStringException>(() =>
			KurrentClientConnectionSettings.Parse(connectionString)
		);

		exception.ConnectionString.ShouldBe(connectionString);
		exception.Message.ShouldContain("Invalid connection string format");
	}

	[Test]
	public void Parse_InvalidFormat_ShouldThrowKurrentDBConnectionStringException() {
		// Arrange
		var connectionString = "not-a-valid-uri";

		// Act & Assert
		var exception = Should.Throw<KurrentDBConnectionStringException>(() =>
			KurrentClientConnectionSettings.Parse(connectionString)
		);

		exception.ConnectionString.ShouldBe(connectionString);
		exception.Message.ShouldContain("Invalid connection string format");
	}

	[Test]
	public void Parse_InvalidPort_ShouldThrowKurrentDBConnectionStringException() {
		// Arrange
		var connectionString = "kurrentdb://localhost:invalid";

		// Act & Assert
		var exception = Should.Throw<KurrentDBConnectionStringException>(() =>
			KurrentClientConnectionSettings.Parse(connectionString)
		);

		exception.ConnectionString.ShouldBe(connectionString);
		exception.Parameter.ShouldBe("hosts");
		exception.Message.ShouldContain("Invalid port specification");
	}

	[Test]
	public void Parse_PortOutOfRange_ShouldThrowKurrentDBConnectionStringException() {
		// Arrange
		var connectionString = "kurrentdb://localhost:99999";

		// Act & Assert
		var exception = Should.Throw<KurrentDBConnectionStringException>(() =>
			KurrentClientConnectionSettings.Parse(connectionString)
		);

		exception.ConnectionString.ShouldBe(connectionString);
		exception.Parameter.ShouldBe("hosts");
		exception.Message.ShouldContain("Port must be between 1 and 65535");
	}

	[Test]
	public void Parse_InvalidBooleanParameter_ShouldThrowKurrentDBConnectionStringException() {
		// Arrange
		var connectionString = "kurrentdb://localhost:2113?tls=maybe";

		// Act & Assert
		var exception = Should.Throw<KurrentDBConnectionStringException>(() =>
			KurrentClientConnectionSettings.Parse(connectionString)
		);

		exception.ConnectionString.ShouldBe(connectionString);
		exception.Parameter.ShouldBe("tls");
		exception.Message.ShouldContain("must be 'true' or 'false'");
	}

	[Test]
	public void Parse_InvalidIntegerParameter_ShouldThrowKurrentDBConnectionStringException() {
		// Arrange
		var connectionString = "kurrentdb://localhost:2113?maxDiscoverAttempts=invalid";

		// Act & Assert
		var exception = Should.Throw<KurrentDBConnectionStringException>(() =>
			KurrentClientConnectionSettings.Parse(connectionString)
		);

		exception.ConnectionString.ShouldBe(connectionString);
		exception.Parameter.ShouldBe("maxDiscoverAttempts");
		exception.Message.ShouldContain("must be an integer");
	}

	[Test]
	public void Parse_InvalidNodePreference_ShouldThrowKurrentDBConnectionStringException() {
		// Arrange
		var connectionString = "kurrentdb://localhost:2113?nodePreference=invalid";

		// Act & Assert
		var exception = Should.Throw<KurrentDBConnectionStringException>(() =>
			KurrentClientConnectionSettings.Parse(connectionString)
		);

		exception.ConnectionString.ShouldBe(connectionString);
		exception.Parameter.ShouldBe("nodePreference");
		exception.Message.ShouldContain("must be one of:");
	}

	[Test]
	public void Parse_ValidationFailure_ShouldThrowKurrentDBConnectionStringException() {
		// Arrange
		var connectionString = "kurrentdb://localhost:2113?maxDiscoverAttempts=0";

		// Act & Assert
		var exception = Should.Throw<KurrentDBConnectionStringException>(() =>
			KurrentClientConnectionSettings.Parse(connectionString)
		);

		exception.ConnectionString.ShouldBe(connectionString);
		exception.Message.ShouldContain("Connection string validation failed");
		exception.Message.ShouldContain("MaxDiscoverAttempts must be greater than 0");
	}

	[Test]
	public void KurrentDBConnectionStringException_Properties_ShouldBeSetCorrectly() {
		// Arrange
		var connectionString = "test://connection";
		var parameter        = "testParam";
		var message          = "Test message";
		var innerException   = new InvalidOperationException("Inner");

		// Act
		var exception = new KurrentDBConnectionStringException(message, connectionString, parameter, innerException);

		// Assert
		exception.Message.ShouldBe(message);
		exception.ConnectionString.ShouldBe(connectionString);
		exception.Parameter.ShouldBe(parameter);
		exception.InnerException.ShouldBe(innerException);
	}

	[Test]
	public void ValidationResult_Success_ShouldCreateValidResult() {
		// Act
		var result = ValidationResult.Success();

		// Assert
		result.IsValid.ShouldBeTrue();
		result.Errors.ShouldBeEmpty();
	}

	[Test]
	public void ValidationResult_Failure_WithErrors_ShouldCreateInvalidResult() {
		// Arrange
		var errors = new[] { "Error 1", "Error 2" };

		// Act
		var result = ValidationResult.Failure(errors);

		// Assert
		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldBe(errors);
	}

	[Test]
	public void ValidationResult_Failure_WithSingleError_ShouldCreateInvalidResult() {
		// Arrange
		var error = "Single error";

		// Act
		var result = ValidationResult.Failure(error);

		// Assert
		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem();
		result.Errors[0].ShouldBe(error);
	}
}

public class KurrentDBConnectionEdgeCaseTests {
	[Test]
	public void Parse_IPv6Address_ShouldParseCorrectly() {
		// Arrange
		var connectionString = "kurrentdb://[::1]:2113";

		// Act
		var settings = KurrentClientConnectionSettings.Parse(connectionString);

		// Assert
		settings.Endpoints.ShouldHaveSingleItem();
		var endpoint = (IPEndPoint)settings.Endpoints[0];
		endpoint.Address.ShouldBe(IPAddress.IPv6Loopback);
		endpoint.Port.ShouldBe(2113);
	}

	[Test]
	public void Parse_PasswordWithSpecialCharacters_ShouldParseCorrectly() {
		// Arrange
		var connectionString = "kurrentdb://user:pa%24%24w%40rd%21@localhost:2113";

		// Act
		var settings = KurrentClientConnectionSettings.Parse(connectionString);

		// Assert
		settings.UserCredentials.Username.ShouldBe("user");
		settings.UserCredentials.Password.ShouldBe("pa$$w@rd!");
	}

	[Test]
	public void Parse_EmptyPassword_ShouldParseCorrectly() {
		// Arrange
		var connectionString = "kurrentdb://user:@localhost:2113";

		// Act
		var settings = KurrentClientConnectionSettings.Parse(connectionString);

		// Assert
		settings.UserCredentials.Username.ShouldBe("user");
		settings.UserCredentials.Password.ShouldBe("");
	}

	[Test]
	public void Parse_NoPasswordSeparator_ShouldParseUsernameOnly() {
		// Arrange
		var connectionString = "kurrentdb://user@localhost:2113";

		// Act
		var settings = KurrentClientConnectionSettings.Parse(connectionString);

		// Assert
		settings.UserCredentials.Username.ShouldBe("user");
		settings.UserCredentials.Password.ShouldBeNull();
	}

	[Test]
	public void Parse_DefaultPort_ShouldUse2113() {
		// Arrange
		var connectionString = "kurrentdb://localhost";

		// Act
		var settings = KurrentClientConnectionSettings.Parse(connectionString);

		// Assert
		settings.Endpoints.ShouldHaveSingleItem();
		var endpoint = (DnsEndPoint)settings.Endpoints[0];
		endpoint.Port.ShouldBe(2113);
	}

	[Test]
	public void Parse_MixedHostTypes_ShouldFail() {
		// Arrange
		var connectionString = "kurrentdb+discover://localhost:2113,192.168.1.100:2114,[::1]:2115";

		// Act & Assert
		Should.Throw<KurrentDBConnectionStringException>(() => KurrentClientConnectionSettings.Parse(connectionString));
	}

	[Test]
	public void Parse_QueryParameterCaseSensitivity_ShouldBeInsensitive() {
		// Arrange
		var connectionString = "kurrentdb://localhost:2113?TLS=FALSE&NodePreference=FOLLOWER";

		// Act
		var settings = KurrentClientConnectionSettings.Parse(connectionString);

		// Assert
		settings.EffectiveTls.Enabled.ShouldBeFalse();
		settings.NodePreference.ShouldBe(NodePreference.Follower);
	}

	[Test]
	public void Parse_DuplicateQueryParameters_ShouldUseLastValue() {
		// Arrange
		var connectionString = "kurrentdb://localhost:2113?tls=true&tls=false";

		// Act
		var settings = KurrentClientConnectionSettings.Parse(connectionString);

		// Assert
		settings.EffectiveTls.Enabled.ShouldBeFalse();
	}

	[Test]
	public void GossipSettings_EffectiveValues_ShouldUseDefaultsWhenZero() {
		// Arrange
		var gossipSettings = new GossipSettings(
			5,
			TimeSpan.Zero,
			TimeSpan.Zero
		);

		// Act & Assert
		gossipSettings.EffectiveDiscoveryInterval.ShouldBe(GossipSettings.DefaultDiscoveryInterval);
		gossipSettings.EffectiveTimeout.ShouldBe(GossipSettings.DefaultTimeout);
		gossipSettings.MaxDiscoverAttempts.ShouldBe(5); // This should use the specified value
	}

	[Test]
	public void TlsSettings_Predefined_ShouldHaveCorrectValues() {
		// Act & Assert
		TlsSettings.Default.Enabled.ShouldBeTrue();
		TlsSettings.Default.VerifyCertificate.ShouldBeTrue();
		TlsSettings.Default.CaFile.ShouldBeNull();

		TlsSettings.Insecure.Enabled.ShouldBeFalse();
		TlsSettings.Insecure.VerifyCertificate.ShouldBeFalse();
		TlsSettings.Insecure.CaFile.ShouldBeNull();
	}

	// [Test]
	// public void Parse_EmptyHostInMultipleHosts_ShouldSkipEmptyHost() {
	// 	// Arrange
	// 	var connectionString = "kurrentdb+discover://node1:2113,,node2:2114";
	//
	// 	// Act
	// 	var settings = KurrentClientConnectionSettings.Parse(connectionString);
	//
	// 	// Assert
	// 	settings.Endpoints.Length.ShouldBe(2);
	// 	((DnsEndPoint)settings.Endpoints[0]).Host.ShouldBe("node1");
	// 	((DnsEndPoint)settings.Endpoints[1]).Host.ShouldBe("node2");
	// }

	[Test]
	public void Parse_WhitespaceInHosts_ShouldTrimWhitespace() {
		// Arrange
		var connectionString = "kurrentdb+discover:// node1 : 2113 ,  node2:2114  ";

		// Act
		var settings = KurrentClientConnectionSettings.Parse(connectionString);

		// Assert
		settings.Endpoints.Length.ShouldBe(2);
		((DnsEndPoint)settings.Endpoints[0]).Host.ShouldBe("node1");
		((DnsEndPoint)settings.Endpoints[1]).Host.ShouldBe("node2");
	}

	[Test]
	public void ToString_OnlyNonDefaultValues_ShouldOnlyIncludeChangedParameters() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			[new DnsEndPoint("localhost", 2113)],
			NodePreference: NodePreference.Leader,
			ConnectionName: null,
			Tls: TlsSettings.Default
		);

		// Act
		var connectionString = settings.ToString();

		// Assert
		connectionString.ShouldBe("kurrentdb://localhost:2113?maxDiscoverAttempts=0");
		connectionString.ShouldNotContain("nodePreference");
		connectionString.ShouldNotContain("connectionName");
	}

	[Test]
	public void ToString_OnlyNonDefaultValues_ShouldOnlyIncludeChangedParameters2() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			[new DnsEndPoint("localhost", 2113)],
			NodePreference: NodePreference.Leader,
			ConnectionName: null,
			Tls: TlsSettings.Insecure
		);

		// Act
		var connectionString = settings.ToString();

		// Assert
		connectionString.ShouldBe("kurrentdb://localhost:2113?tls=false&maxDiscoverAttempts=0");
		connectionString.ShouldNotContain("nodePreference");
		connectionString.ShouldNotContain("connectionName");
	}

	[Test]
	public void ToString_OnlyNonDefaultValues_ShouldOnlyIncludeChangedParameters3() {
		// Arrange
		var settings = new KurrentClientConnectionSettings(
			ConnectionScheme.Direct,
			[new DnsEndPoint("localhost", 2113)],
			NodePreference: NodePreference.Leader,
			ConnectionName: null,
			Tls: new TlsSettings(true, true)
		);

		// Act
		var connectionString = settings.ToString();

		// Assert
		connectionString.ShouldBe("kurrentdb://localhost:2113?maxDiscoverAttempts=0");
		connectionString.ShouldNotContain("nodePreference");
		connectionString.ShouldNotContain("connectionName");
	}
}
