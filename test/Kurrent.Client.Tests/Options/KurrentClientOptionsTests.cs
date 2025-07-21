using System.Net;
using Kurrent.Client.Testing.Shouldly;
using KurrentDB.Client;

namespace Kurrent.Client.Tests.Options;

public class KurrentClientOptionsTests {
    [Test]
    public void default_options_should_be_valid() {
        // Create with default constructor
        var options = new KurrentClientOptions();

        var validationResult = options.ValidateOptions();

        // Ensure validation passes
        validationResult.IsSuccess.ShouldBeTrue("Default options should be valid");
    }

    [Test]
    public void creates_default_options_with_single_endpoint() {
        // Create with default constructor
        var options = new KurrentClientOptions();

        // Verify default values
        options.ConnectionScheme.ShouldBe(KurrentConnectionScheme.Direct);
        options.Endpoints.ShouldHaveSingleItem();
        ShouldlyObjectGraphTestExtensions.ShouldBeEquivalentTo(options.Gossip, KurrentClientGossipOptions.Default);
        options.Security.ShouldBe(KurrentClientSecurityOptions.Default);
        ShouldlyObjectGraphTestExtensions.ShouldBeEquivalentTo(options.Resilience, KurrentClientResilienceOptions.NoResilience);
        ShouldlyObjectGraphTestExtensions.ShouldBeEquivalentTo(options.Schema, KurrentClientSchemaOptions.FullValidation);
        options.Interceptors.ShouldBeEmpty();
    }

    [Test]
    public void creates_options_from_connection_string() {
        // Create from a connection string
        var connectionString = "kurrentdb://admin:changeit@localhost:2113?tls=false";
        var options          = KurrentClientOptions.Parse(connectionString);

        // Verify the parsed options
        options.HasOriginalConnectionString.ShouldBeTrue();
        options.OriginalConnectionString.ShouldBe(connectionString);
        options.ConnectionScheme.ShouldBe(KurrentConnectionScheme.Direct);
        options.Endpoints.Length.ShouldBe(1);
        options.Endpoints[0].Host.ShouldBe("localhost");
        options.Endpoints[0].Port.ShouldBe(2113);
        options.Security.Transport.IsEnabled.ShouldBeFalse();
        options.Security.Authentication.IsBasicCredentials.ShouldBeTrue();

        var credentials = options.Security.Authentication.AsCredentials;
        credentials.Username.ShouldBe("admin");
        credentials.Password.ShouldBe("changeit");
    }

    [Test]
    public void creates_options_from_discovery_connection_string() {
        // Create from a discovery connection string
        var connectionString = "kurrentdb+discover://admin:changeit@node1:2113,node2:2113?tls=true";
        var options          = KurrentClientOptions.Parse(connectionString);

        // Verify the parsed options
        options.ConnectionScheme.ShouldBe(KurrentConnectionScheme.Discover);
        options.Endpoints.Length.ShouldBe(2);
        options.Endpoints[0].Host.ShouldBe("node1");
        options.Endpoints[0].Port.ShouldBe(2113);
        options.Endpoints[1].Host.ShouldBe("node2");
        options.Endpoints[1].Port.ShouldBe(2113);
        options.Security.Transport.IsEnabled.ShouldBeTrue();
    }

    [Test]
    public void generates_connection_string_that_matches_original() {
        // Create from a connection string
        var originalString = "kurrentdb://admin:changeit@localhost:2113?tls=false";
        var options        = KurrentClientOptions.Parse(originalString);

        // Generate the string back
        var generatedString = options.GenerateConnectionString();

        // The generated string should have the same components
        generatedString.ShouldContain("kurrentdb://");
        generatedString.ShouldContain("admin:changeit@");
        generatedString.ShouldContain("localhost:2113");
        generatedString.ShouldContain("tls=false");
    }

    [Test]
    public void handles_special_characters_in_credentials() {
        // Create with special characters in username/password
        var connectionString = "kurrentdb://user%40domain:p%40ssw%3Ard@localhost:2113";
        var options          = KurrentClientOptions.Parse(connectionString);

        // Verify correct handling of special characters
        var credentials = options.Security.Authentication.AsCredentials;
        credentials.Username.ShouldBe("user@domain");
        credentials.Password.ShouldBe("p@ssw:rd");

        // Generate connection string should properly encode these
        var generatedString = options.GenerateConnectionString();
        generatedString.ShouldContain("user%40domain:p%40ssw%3Ard@");
    }

    [Test]
    public void generates_proper_http_uri_based_on_transport_security() {
        // Create with TLS enabled
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new("localhost", 2113)],
            Security         = KurrentClientSecurityOptions.Default // TLS enabled
        };

        // Check HTTP scheme
        options.HttpUriScheme.ShouldBe(Uri.UriSchemeHttps);

        // Change to no TLS
        options = options with { Security = KurrentClientSecurityOptions.Insecure };
        options.HttpUriScheme.ShouldBe(Uri.UriSchemeHttp);
    }

    [Test]
    public void generates_address_correctly_for_direct_connection() {
        // Create a direct connection
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new("localhost", 2113)],
            Security         = KurrentClientSecurityOptions.Default
        };

        // Address should be a HTTPS URI
        options.Address.ShouldNotBeNull();
        options.Address.Scheme.ShouldBe("https");
        options.Address.Host.ShouldBe("localhost");
        options.Address.Port.ShouldBe(2113);
    }

    [Test]
    public void generates_guid_address_for_discovery_connection() {
        // Create a discovery connection
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Discover,
            Endpoints        = [new("node1", 2113), new("node2", 2113)]
        };

        // Address should be a discovery URI with a GUID
        options.Address.ShouldNotBeNull();
        options.Address.Scheme.ShouldBe("kurrentdb+discover");
        options.Address.Host.ShouldNotBeNullOrEmpty();

        // The host should be a valid GUID
        Guid.TryParse(options.Address.Host, out _).ShouldBeTrue();
    }

    [Test]
    public void preserves_gossip_options_in_connection_string() {
        // Create with custom gossip options
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Discover,
            Endpoints        = [new("localhost", 2113)],
            Gossip = new() {
                MaxDiscoverAttempts = 15,
                DiscoveryInterval   = TimeSpan.FromMilliseconds(200),
                Timeout             = TimeSpan.FromSeconds(10),
                ReadPreference      = NodePreference.Leader
            }
        };

        // Generate connection string
        var connectionString = options.GenerateConnectionString();

        // Should include gossip settings
        connectionString.ShouldContain("maxDiscoverAttempts=15");
        connectionString.ShouldContain("discoveryInterval=200");
        connectionString.ShouldContain("gossipTimeout=10");
        connectionString.ShouldContain("nodePreference=leader");
    }

    [Test]
    public void preserves_resilience_options_in_connection_string() {
        // Create with custom resilience options
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new("localhost", 2113)],
            Resilience = new() {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                KeepAliveTimeout  = TimeSpan.FromSeconds(60),
                Deadline          = TimeSpan.FromSeconds(45)
            }
        };

        // Generate connection string
        var connectionString = options.GenerateConnectionString();

        // Should include resilience settings
        connectionString.ShouldContain("keepAliveInterval=120");
        connectionString.ShouldContain("keepAliveTimeout=60");
        connectionString.ShouldContain("defaultDeadline=45000");
    }

    [Test]
    public void direct_connection_with_multiple_hosts_is_not_valid() {
        // Create direct connection with multiple hosts
        // In practice, this is not a recommended configuration, but the client should handle it gracefully
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints = [
                new DnsEndPoint("first.example.com", 2113),
                new DnsEndPoint("second.example.com", 2113),
                new DnsEndPoint("third.example.com", 2113)
            ]
        };

        var result = options.ValidateOptions();

        result.IsSuccess.ShouldBe(false, "Direct connection with multiple hosts must not be valid.");
        //
        // Assert.Fail(" Direct connection with multiple hosts should not be allowed, but it was created successfully.");
    }

    [Test]
    public void discovery_connection_with_many_hosts_preserves_all_hosts() {
        // Create discovery connection with many hosts
        var endpoints = new[] {
            new DnsEndPoint("node1.example.com", 2113),
            new DnsEndPoint("node2.example.com", 2113),
            new DnsEndPoint("node3.example.com", 2113),
            new DnsEndPoint("node4.example.com", 2113),
            new DnsEndPoint("node5.example.com", 2113)
        };

        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Discover,
            Endpoints        = endpoints
        };

        // All hosts should be preserved in the connection string
        var connectionString = options.GenerateConnectionString();

        // Verify all hosts are included
        foreach (var endpoint in endpoints)
            connectionString.ShouldContain($"{endpoint.Host}:{endpoint.Port}");
    }

    [Test]
    public void discovery_connection_with_different_node_preferences() {
        // Test all node preferences with discovery connections
        var nodePreferences = new[] {
            NodePreference.Leader,
            NodePreference.Follower,
            NodePreference.Random,
            NodePreference.ReadOnlyReplica
        };

        foreach (var preference in nodePreferences) {
            // Create options with this preference
            var options = new KurrentClientOptions {
                ConnectionScheme = KurrentConnectionScheme.Discover,
                Endpoints        = [new DnsEndPoint("node1.example.com", 2113)],
                Gossip = new KurrentClientGossipOptions {
                    ReadPreference = preference
                }
            };

            // Generate connection string and verify preference is included correctly
            var connectionString = options.GenerateConnectionString();

            if (preference != NodePreference.Random) // Random is default, so it may not appear in string
                connectionString.ShouldContain($"nodePreference={preference.ToString().ToLowerInvariant()}");
        }
    }

    [Test]
    public void connection_string_with_custom_connection_name() {
        // Create options with custom connection name
        var options = new KurrentClientOptions {
            ConnectionName   = "custom-connection-1",
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new DnsEndPoint("localhost", 2113)]
        };

        // Generate connection string
        var connectionString = options.GenerateConnectionString();

        // Verify connection name is present
        connectionString.ShouldContain("connectionName=custom-connection-1");
    }

    [Test]
    public void connection_string_with_auto_generated_connection_name_omits_parameter() {
        // Create options with auto-generated connection name (starts with conn-)
        var options = new KurrentClientOptions {
            // Default constructor creates name like "conn-{Guid}"
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new DnsEndPoint("localhost", 2113)]
        };

        // Verify name follows expected pattern
        options.ConnectionName.ShouldStartWith("conn-");

        // Generate connection string
        var connectionString = options.GenerateConnectionString();

        // Connection name parameter should be omitted (since it's auto-generated)
        connectionString.ShouldNotContain("connectionName=");
    }

    [Test]
    public void parses_connection_string_with_empty_query_parameters() {
        // Connection string with parameter without value
        var connectionString = "kurrentdb://localhost:2113?tlsVerifyCert=";
        var options          = KurrentClientOptions.Parse(connectionString);

        // Should default to true when empty
        options.Security.Transport.VerifyServerCertificate.ShouldBeTrue();
    }

    [Test]
    public void client_certificate_auth_is_preserved_in_connection_string() {
        // Create options with certificate authentication from files
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new DnsEndPoint("localhost", 2113)],
            Security = KurrentClientSecurityOptions.MutualTls(
                Uri.EscapeDataString("/path/to/client.crt"),
                Uri.EscapeDataString("/path/to/client.key")
            )
        };

        // Generate connection string
        var connectionString = options.GenerateConnectionString();

        // Verify certificate paths are included
        connectionString.ShouldContain($"{Uri.EscapeDataString("/path/to/client.crt")}");
        connectionString.ShouldContain($"{Uri.EscapeDataString("/path/to/client.key")}");
    }

    [Test]
    public void resilience_with_infinite_timeout_values_creates_valid_connection_string() {
        // Create options with Timeout.InfiniteTimeSpan values
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new DnsEndPoint("localhost", 2113)],
            Resilience = new KurrentClientResilienceOptions {
                KeepAliveInterval = Timeout.InfiniteTimeSpan,
                KeepAliveTimeout  = Timeout.InfiniteTimeSpan,
                Deadline          = Timeout.InfiniteTimeSpan
            }
        };

        // Generate connection string - should not throw any exceptions
        var connectionString = options.GenerateConnectionString();

        // Connection string should be valid and not contain invalid values
        connectionString.ShouldNotBeNullOrEmpty();
    }
}
