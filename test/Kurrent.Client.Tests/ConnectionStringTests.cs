namespace Kurrent.Client.Tests.Legacy;

[Category("Legacy")]
public class NewConnectionStringTests {
    #region Basic Parsing Tests

    [Test]
    [Arguments("kurrentdb")]
    [Arguments("esdb")]
    [Arguments("eventstore")]
    [Arguments("kurrentdb+discover")]
    [Arguments("esdb+discover")]
    [Arguments("eventstore+discover")]
    public void parses_valid_schemes(string scheme) {
        var connectionString = $"{scheme}://localhost:2113";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Scheme.ShouldBe(scheme.ToLowerInvariant());
    }

    [Test]
    public void parses_single_host_with_default_port() {
        var connectionString = "kurrentdb://localhost";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Hosts.Length.ShouldBe(1);
        result.Hosts[0].Host.ShouldBe("localhost");
        result.Hosts[0].Port.ShouldBe(2113);
    }

    [Test]
    public void parses_single_host_with_custom_port() {
        var connectionString = "kurrentdb://localhost:1234";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Hosts.Length.ShouldBe(1);
        result.Hosts[0].Host.ShouldBe("localhost");
        result.Hosts[0].Port.ShouldBe(1234);
    }

    [Test]
    public void parses_multiple_hosts_for_discovery() {
        var connectionString = "kurrentdb+discover://node1:2113,node2:2114,node3:2115";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Hosts.Length.ShouldBe(3);
        result.Hosts[0].Host.ShouldBe("node1");
        result.Hosts[0].Port.ShouldBe(2113);
        result.Hosts[1].Host.ShouldBe("node2");
        result.Hosts[1].Port.ShouldBe(2114);
        result.Hosts[2].Host.ShouldBe("node3");
        result.Hosts[2].Port.ShouldBe(2115);
    }

    [Test]
    public void parses_user_credentials() {
        var connectionString = "kurrentdb://admin:changeit@localhost:2113";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.UserInfo.ShouldNotBeNull();
        result.UserInfo.Value.User.ShouldBe("admin");
        result.UserInfo.Value.Password.ShouldBe("changeit");
    }

    [Test]
    public void parses_url_encoded_credentials() {
        var connectionString = "kurrentdb://admin%40domain:pass%40word@localhost:2113";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.UserInfo.ShouldNotBeNull();
        result.UserInfo.Value.User.ShouldBe("admin@domain");
        result.UserInfo.Value.Password.ShouldBe("pass@word");
    }

    [Test]
    public void parses_options() {
        var connectionString = "kurrentdb://localhost:2113?tls=false&maxDiscoverAttempts=5";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Options.Count.ShouldBe(2);
        result.Options["tls"].ShouldBe("false");
        result.Options["maxDiscoverAttempts"].ShouldBe("5");
    }

    [Test]
    public void parses_case_insensitive_options() {
        var connectionString = "kurrentdb://localhost:2113?TLS=false&MaxDiscoverAttempts=5";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Options.Count.ShouldBe(2);
        result.Options["TLS"].ShouldBe("false");
        result.Options["MaxDiscoverAttempts"].ShouldBe("5");
    }

    [Test]
    public void parses_url_encoded_option_values() {
        var connectionString = "kurrentdb://localhost:2113?connectionName=My%20Connection";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Options["connectionName"].ShouldBe("My Connection");
    }

    #endregion

    #region Error Condition Tests

    [Test]
    public void throws_on_null_connection_string() {
        Should.Throw<ArgumentNullException>(() => KurrentDBConnectionString.Parse(null!));
    }

    [Test]
    public void throws_on_empty_connection_string() {
        Should.Throw<ArgumentException>(() => KurrentDBConnectionString.Parse(""));
    }

    [Test]
    public void throws_on_whitespace_connection_string() {
        Should.Throw<ArgumentException>(() => KurrentDBConnectionString.Parse("   "));
    }

    [Test]
    public void throws_on_missing_scheme() {
        Should.Throw<NoSchemeException>(() => KurrentDBConnectionString.Parse("localhost:2113"));
    }

    [Test]
    [Arguments("invalid://localhost")]
    [Arguments("kurrentdbwrong://localhost")]
    [Arguments("http://localhost")]
    public void throws_on_invalid_scheme(string connectionString) {
        Should.Throw<InvalidSchemeException>(() => KurrentDBConnectionString.Parse(connectionString));
    }

    [Test]
    [Arguments("kurrentdb://user@localhost")]
    [Arguments("kurrentdb://user:pass:extra@localhost")]
    [Arguments("kurrentdb://:password@localhost")]
    public void throws_on_invalid_user_credentials(string connectionString) {
        Should.Throw<InvalidUserCredentialsException>(() => KurrentDBConnectionString.Parse(connectionString));
    }

    [Test]
    [Arguments("kurrentdb://localhost:abc")]
    [Arguments("kurrentdb://localhost:")]
    [Arguments("kurrentdb://localhost:123:456")]
    [Arguments("kurrentdb://")]
    public void throws_on_invalid_host(string connectionString) {
        Should.Throw<InvalidHostException>(() => KurrentDBConnectionString.Parse(connectionString));
    }

    [Test]
    [Arguments("kurrentdb://localhost/path")]
    [Arguments("kurrentdb://localhost/test?tls=false")]
    public void throws_on_non_empty_path(string connectionString) {
        Should.Throw<ConnectionStringParseException>(() => KurrentDBConnectionString.Parse(connectionString));
    }

    [Test]
    [Arguments("kurrentdb://localhost?key")]
    [Arguments("kurrentdb://localhost?key=value=extra")]
    public void throws_on_invalid_key_value_pair(string connectionString) {
        Should.Throw<InvalidKeyValuePairException>(() => KurrentDBConnectionString.Parse(connectionString));
    }

    [Test]
    public void throws_on_duplicate_keys() {
        var connectionString = "kurrentdb://localhost?tls=true&TLS=false";

        Should.Throw<DuplicateKeyException>(() => KurrentDBConnectionString.Parse(connectionString));
    }

    #endregion

    #region Discovery vs Direct Connection Tests

    [Test]
    public void allows_single_host_for_direct_scheme() {
        var connectionString = "kurrentdb://localhost:2113";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Hosts.Length.ShouldBe(1);
    }

    [Test]
    public void throws_on_multiple_hosts_for_direct_scheme() {
        var connectionString = "kurrentdb://node1:2113,node2:2113";

        Should.Throw<ConnectionStringParseException>(() => KurrentDBConnectionString.Parse(connectionString));
    }

    [Test]
    public void allows_multiple_hosts_for_discovery_scheme() {
        var connectionString = "kurrentdb+discover://node1:2113,node2:2113,node3:2113";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Hosts.Length.ShouldBe(3);
    }

    #endregion

    #region Comprehensive Parameter Parsing Tests

    [Test]
    [Arguments("tls", "true")]
    [Arguments("tls", "false")]
    [Arguments("tls", "True")]
    [Arguments("tls", "False")]
    public void parses_tls_parameter(string key, string value) {
        var connectionString = $"kurrentdb://localhost?{key}={value}";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Options[key].ShouldBe(value);
    }

    [Test]
    [Arguments("connectionName", "MyConnection")]
    [Arguments("maxDiscoverAttempts", "10")]
    [Arguments("discoveryInterval", "100")]
    [Arguments("gossipTimeout", "5000")]
    [Arguments("nodePreference", "leader")]
    [Arguments("tlsVerifyCert", "false")]
    [Arguments("tlsCaFile", "/path/to/ca.crt")]
    [Arguments("defaultDeadline", "30000")]
    [Arguments("throwOnAppendFailure", "true")]
    [Arguments("keepAliveInterval", "10000")]
    [Arguments("keepAliveTimeout", "20000")]
    [Arguments("userCertFile", "/path/to/user.crt")]
    [Arguments("userKeyFile", "/path/to/user.key")]
    public void parses_known_parameters(string key, string value) {
        var connectionString = $"kurrentdb://localhost?{key}={value}";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Options[key].ShouldBe(value);
    }

    [Test]
    public void parses_multiple_parameters() {
        var connectionString = "kurrentdb://localhost?tls=false&connectionName=Test&maxDiscoverAttempts=5";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Options.Count.ShouldBe(3);
        result.Options["tls"].ShouldBe("false");
        result.Options["connectionName"].ShouldBe("Test");
        result.Options["maxDiscoverAttempts"].ShouldBe("5");
    }

    [Test]
    [Arguments("nodePreference", "leader")]
    [Arguments("nodePreference", "follower")]
    [Arguments("nodePreference", "random")]
    [Arguments("nodePreference", "readOnlyReplica")]
    public void parses_node_preference_values(string key, string value) {
        var connectionString = $"kurrentdb://localhost?{key}={value}";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Options[key].ShouldBe(value);
    }

    #endregion

    #region Edge Case Tests

    [Test]
    public void handles_empty_path() {
        var connectionString = "kurrentdb://localhost/";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Hosts.Length.ShouldBe(1);
        result.Hosts[0].Host.ShouldBe("localhost");
    }

    [Test]
    public void handles_no_options() {
        var connectionString = "kurrentdb://localhost";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Options.Count.ShouldBe(0);
    }

    [Test]
    public void throws_on_empty_query_parameter() {
        var connectionString = "kurrentdb://localhost?";

        Should.Throw<InvalidKeyValuePairException>(() => KurrentDBConnectionString.Parse(connectionString));
    }

    [Test]
    public void handles_host_with_underscores() {
        var connectionString = "kurrentdb://my_host:2113";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Hosts[0].Host.ShouldBe("my_host");
    }

    [Test]
    public void handles_host_with_hyphens() {
        var connectionString = "kurrentdb://my-host:2113";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Hosts[0].Host.ShouldBe("my-host");
    }

    [Test]
    public void handles_numeric_host() {
        var connectionString = "kurrentdb://192.168.1.100:2113";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Hosts[0].Host.ShouldBe("192.168.1.100");
    }

    [Test]
    public void preserves_original_connection_string() {
        var original = "kurrentdb://admin:changeit@localhost:2113?tls=false";

        var result = KurrentDBConnectionString.Parse(original);

        result.ConnectionString.ShouldBe(original);
    }

    [Test]
    public void throws_on_mixed_case_schemes() {
        var connectionString = "KurrentDB://localhost";

        Should.Throw<InvalidSchemeException>(() => KurrentDBConnectionString.Parse(connectionString));
    }

    #endregion

    #region Real-world Example Tests

    [Test]
    public void parses_local_insecure_connection() {
        var connectionString = "kurrentdb://localhost:2113?tls=false&tlsVerifyCert=false";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Scheme.ShouldBe("kurrentdb");
        result.Hosts.Length.ShouldBe(1);
        result.Hosts[0].Host.ShouldBe("localhost");
        result.Hosts[0].Port.ShouldBe(2113);
        result.Options["tls"].ShouldBe("false");
        result.Options["tlsVerifyCert"].ShouldBe("false");
    }

    [Test]
    public void parses_production_cluster_connection() {
        var connectionString =
            "kurrentdb+discover://admin:changeit@node1.cluster.local:2113,node2.cluster.local:2113,node3.cluster.local:2113?nodePreference=leader";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Scheme.ShouldBe("kurrentdb+discover");
        result.UserInfo.ShouldNotBeNull();
        result.UserInfo.Value.User.ShouldBe("admin");
        result.UserInfo.Value.Password.ShouldBe("changeit");
        result.Hosts.Length.ShouldBe(3);
        result.Hosts[0].Host.ShouldBe("node1.cluster.local");
        result.Hosts[1].Host.ShouldBe("node2.cluster.local");
        result.Hosts[2].Host.ShouldBe("node3.cluster.local");
        result.Options["nodePreference"].ShouldBe("leader");
    }

    [Test]
    public void parses_secure_single_node_connection() {
        var connectionString = "kurrentdb://admin:changeit@kurrentdb.example.com:2113?tls=true&tlsVerifyCert=true";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Scheme.ShouldBe("kurrentdb");
        result.UserInfo.ShouldNotBeNull();
        result.UserInfo.Value.User.ShouldBe("admin");
        result.UserInfo.Value.Password.ShouldBe("changeit");
        result.Hosts.Length.ShouldBe(1);
        result.Hosts[0].Host.ShouldBe("kurrentdb.example.com");
        result.Options["tls"].ShouldBe("true");
        result.Options["tlsVerifyCert"].ShouldBe("true");
    }

    [Test]
    public void parses_connection_with_custom_certificates() {
        var connectionString =
            "kurrentdb://admin:changeit@localhost:2113?tls=true&tlsVerifyCert=true&tlsCaFile=/certs/ca.crt&userCertFile=/certs/client.crt&userKeyFile=/certs/client.key";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Options["tlsCaFile"].ShouldBe("/certs/ca.crt");
        result.Options["userCertFile"].ShouldBe("/certs/client.crt");
        result.Options["userKeyFile"].ShouldBe("/certs/client.key");
    }

    [Test]
    public void parses_connection_with_tuned_timeouts() {
        var connectionString =
            "kurrentdb://localhost:2113?discoveryInterval=500&gossipTimeout=3000&keepAliveInterval=15000&keepAliveTimeout=30000&defaultDeadline=60000";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Options["discoveryInterval"].ShouldBe("500");
        result.Options["gossipTimeout"].ShouldBe("3000");
        result.Options["keepAliveInterval"].ShouldBe("15000");
        result.Options["keepAliveTimeout"].ShouldBe("30000");
        result.Options["defaultDeadline"].ShouldBe("60000");
    }

    [Test]
    public void parses_legacy_esdb_scheme() {
        var connectionString = "esdb://admin:changeit@localhost:2113";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Scheme.ShouldBe("esdb");
        result.UserInfo.ShouldNotBeNull();
        result.UserInfo.Value.User.ShouldBe("admin");
        result.UserInfo.Value.Password.ShouldBe("changeit");
    }

    [Test]
    public void parses_legacy_eventstore_discovery_scheme() {
        var connectionString = "eventstore+discover://admin:changeit@node1:2113,node2:2113";

        var result = KurrentDBConnectionString.Parse(connectionString);

        result.Scheme.ShouldBe("eventstore+discover");
        result.Hosts.Length.ShouldBe(2);
    }

    #endregion
}
