using System.Net;
using Kurrent.Client.Testing.Shouldly;
using KurrentDB.Client;

namespace Kurrent.Client.Tests.Options;

public class KurrentClientOptionsTests {
    [Test]
    public void default_options_should_be_valid() {
        var options = new KurrentClientOptions();

        var validationResult = options.ValidateOptions();

        validationResult.IsSuccess.ShouldBeTrue("Default options should be valid");
    }

    [Test]
    public void creates_default_options_with_single_endpoint() {
        var options = new KurrentClientOptions();

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
        var connectionString = "kurrentdb://admin:changeit@localhost:2113?tls=false";
        var options          = KurrentClientOptions.Parse(connectionString);

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
        var connectionString = "kurrentdb+discover://admin:changeit@node1:2113,node2:2113?tls=true";
        var options          = KurrentClientOptions.Parse(connectionString);

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
        var originalString = "kurrentdb://admin:changeit@localhost:2113?tls=false";
        var options        = KurrentClientOptions.Parse(originalString);

        var generatedString = options.GenerateConnectionString();

        generatedString.ShouldContain("kurrentdb://");
        generatedString.ShouldContain("admin:changeit@");
        generatedString.ShouldContain("localhost:2113");
        generatedString.ShouldContain("tls=false");
    }

    [Test]
    public void handles_special_characters_in_credentials() {
        var connectionString = "kurrentdb://user%40domain:p%40ssw%3Ard@localhost:2113";
        var options          = KurrentClientOptions.Parse(connectionString);

        var credentials = options.Security.Authentication.AsCredentials;
        credentials.Username.ShouldBe("user@domain");
        credentials.Password.ShouldBe("p@ssw:rd");

        var generatedString = options.GenerateConnectionString();
        generatedString.ShouldContain("user%40domain:p%40ssw%3Ard@");
    }

    [Test]
    public void generates_proper_http_uri_based_on_transport_security() {
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new("localhost", 2113)],
            Security         = KurrentClientSecurityOptions.Default
        };

        options.HttpUriScheme.ShouldBe(Uri.UriSchemeHttps);

        options = options with { Security = KurrentClientSecurityOptions.Insecure };
        options.HttpUriScheme.ShouldBe(Uri.UriSchemeHttp);
    }

    [Test]
    public void generates_address_correctly_for_direct_connection() {
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new("localhost", 2113)],
            Security         = KurrentClientSecurityOptions.Default
        };

        options.Address.ShouldNotBeNull();
        options.Address.Scheme.ShouldBe("https");
        options.Address.Host.ShouldBe("localhost");
        options.Address.Port.ShouldBe(2113);
    }

    [Test]
    public void generates_guid_address_for_discovery_connection() {
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Discover,
            Endpoints        = [new("node1", 2113), new("node2", 2113)]
        };

        options.Address.ShouldNotBeNull();
        options.Address.Scheme.ShouldBe("kurrentdb+discover");
        options.Address.Host.ShouldNotBeNullOrEmpty();

        Guid.TryParse(options.Address.Host, out _).ShouldBeTrue();
    }

    [Test]
    public void preserves_gossip_options_in_connection_string() {
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

        var connectionString = options.GenerateConnectionString();

        connectionString.ShouldContain("maxDiscoverAttempts=15");
        connectionString.ShouldContain("discoveryInterval=200");
        connectionString.ShouldContain("gossipTimeout=10");
        connectionString.ShouldContain("nodePreference=leader");
    }

    [Test]
    public void preserves_resilience_options_in_connection_string() {
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new("localhost", 2113)],
            Resilience = new() {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                KeepAliveTimeout  = TimeSpan.FromSeconds(60),
                Deadline          = TimeSpan.FromSeconds(45)
            }
        };

        var connectionString = options.GenerateConnectionString();

        connectionString.ShouldContain("keepAliveInterval=120");
        connectionString.ShouldContain("keepAliveTimeout=60");
        connectionString.ShouldContain("defaultDeadline=45000");
    }

    [Test]
    public void direct_connection_with_multiple_hosts_is_not_valid() {
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
    }

    [Test]
    public void discovery_connection_with_many_hosts_preserves_all_hosts() {
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

        var connectionString = options.GenerateConnectionString();

        foreach (var endpoint in endpoints)
            connectionString.ShouldContain($"{endpoint.Host}:{endpoint.Port}");
    }

    [Test]
    public void discovery_connection_with_different_node_preferences() {
        var nodePreferences = new[] {
            NodePreference.Leader,
            NodePreference.Follower,
            NodePreference.Random,
            NodePreference.ReadOnlyReplica
        };

        foreach (var preference in nodePreferences) {
            var options = new KurrentClientOptions {
                ConnectionScheme = KurrentConnectionScheme.Discover,
                Endpoints        = [new DnsEndPoint("node1.example.com", 2113)],
                Gossip = new KurrentClientGossipOptions {
                    ReadPreference = preference
                }
            };

            var connectionString = options.GenerateConnectionString();

            if (preference != NodePreference.Random)
                connectionString.ShouldContain($"nodePreference={preference.ToString().ToLowerInvariant()}");
        }
    }

    [Test]
    public void connection_string_with_custom_connection_name() {
        var options = new KurrentClientOptions {
            ConnectionName   = "custom-connection-1",
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new DnsEndPoint("localhost", 2113)]
        };

        var connectionString = options.GenerateConnectionString();

        connectionString.ShouldContain("connectionName=custom-connection-1");
    }

    [Test]
    public void connection_string_with_auto_generated_connection_name_omits_parameter() {
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new DnsEndPoint("localhost", 2113)]
        };

        options.ConnectionName.ShouldStartWith("conn-");

        var connectionString = options.GenerateConnectionString();

        connectionString.ShouldNotContain("connectionName=");
    }

    [Test]
    public void parses_connection_string_with_empty_query_parameters() {
        var connectionString = "kurrentdb://localhost:2113?tlsVerifyCert=";
        var options          = KurrentClientOptions.Parse(connectionString);

        options.Security.Transport.VerifyServerCertificate.ShouldBeTrue();
    }

    [Test]
    public void client_certificate_auth_is_preserved_in_connection_string() {
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new DnsEndPoint("localhost", 2113)],
            Security = KurrentClientSecurityOptions.MutualTls(
                Uri.EscapeDataString("/path/to/client.crt"),
                Uri.EscapeDataString("/path/to/client.key")
            )
        };

        var connectionString = options.GenerateConnectionString();

        connectionString.ShouldContain($"{Uri.EscapeDataString("/path/to/client.crt")}");
        connectionString.ShouldContain($"{Uri.EscapeDataString("/path/to/client.key")}");
    }

    [Test]
    public void resilience_with_infinite_timeout_values_creates_valid_connection_string() {
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new DnsEndPoint("localhost", 2113)],
            Resilience = new KurrentClientResilienceOptions {
                KeepAliveInterval = Timeout.InfiniteTimeSpan,
                KeepAliveTimeout  = Timeout.InfiniteTimeSpan,
                Deadline          = Timeout.InfiniteTimeSpan
            }
        };

        var connectionString = options.GenerateConnectionString();

        connectionString.ShouldNotBeNullOrEmpty();
    }

    #region Error Handling and Edge Cases

    [Test]
    public void throws_when_parsing_connection_string_with_invalid_node_preference() {
        var connectionString = "kurrentdb+discover://localhost?nodePreference=invalidPreference";

        Should.Throw<Exception>(() => KurrentClientOptions.Parse(connectionString));
    }

    [Test]
    public void throws_when_parsing_connection_string_with_invalid_integer_parameters() {
        var connectionString = "kurrentdb://localhost?maxDiscoverAttempts=notAnInteger";

        Should.Throw<Exception>(() => KurrentClientOptions.Parse(connectionString));
    }

    [Test]
    public void handles_negative_timeout_values_in_connection_string() {
        var connectionString = "kurrentdb://localhost?keepAliveInterval=-500";

        var options = KurrentClientOptions.Parse(connectionString);

        options.Resilience.KeepAliveInterval.TotalMilliseconds.ShouldBe(-500);
    }

    [Test]
    public void handles_connection_string_with_zero_timeout_values() {
        var connectionString = "kurrentdb://localhost?keepAliveInterval=0&keepAliveTimeout=0";

        var options = KurrentClientOptions.Parse(connectionString);

        options.Resilience.KeepAliveInterval.ShouldBe(TimeSpan.Zero);
        options.Resilience.KeepAliveTimeout.ShouldBe(TimeSpan.Zero);
    }

    [Test]
    public void handles_connection_string_with_invalid_boolean_parameters() {
        var connectionString = "kurrentdb://localhost?tls=maybe&tlsVerifyCert=perhaps";

        var options = KurrentClientOptions.Parse(connectionString);

        options.Security.Transport.IsEnabled.ShouldBeTrue();
        options.Security.Transport.VerifyServerCertificate.ShouldBeTrue();
    }

    #endregion

    #region Builder Functionality Tests

    [Test]
    public void get_builder_returns_functional_builder() {
        var originalOptions = new KurrentClientOptions {
            ConnectionName   = "test-connection",
            ConnectionScheme = KurrentConnectionScheme.Discover,
            Endpoints        = [new DnsEndPoint("test-host", 2113)]
        };

        var builder = originalOptions.GetBuilder();

        builder.ShouldNotBeNull();

        var newOptions = builder.Build();
        newOptions.ConnectionName.ShouldBe(originalOptions.ConnectionName);
        newOptions.ConnectionScheme.ShouldBe(originalOptions.ConnectionScheme);
        newOptions.Endpoints.Length.ShouldBe(originalOptions.Endpoints.Length);
    }

    [Test]
    public void static_build_property_returns_builder_instance() {
        var builder = KurrentClientOptions.Build;

        builder.ShouldNotBeNull();

        var options = builder.Build();
        options.ShouldNotBeNull();
        options.ConnectionScheme.ShouldBe(KurrentConnectionScheme.Direct);
    }

    #endregion

    #region Complex Property Logic Tests

    [Test]
    public void address_property_handles_discovery_scheme_correctly() {
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Discover,
            Endpoints        = [new DnsEndPoint("node1", 2113), new DnsEndPoint("node2", 2113)]
        };

        var address = options.Address;
        address.Scheme.ShouldBe("kurrentdb+discover");

        Guid.TryParse(address.Host, out _).ShouldBeTrue();
    }

    [Test]
    public void address_property_uses_https_for_secure_transport() {
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new DnsEndPoint("secure-host", 2113)],
            Security         = KurrentClientSecurityOptions.Default
        };

        var address = options.Address;
        address.Scheme.ShouldBe("https");
        address.Host.ShouldBe("secure-host");
        address.Port.ShouldBe(2113);
    }

    [Test]
    public void address_property_uses_http_for_insecure_transport() {
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new DnsEndPoint("insecure-host", 2113)],
            Security         = KurrentClientSecurityOptions.Insecure
        };

        var address = options.Address;
        address.Scheme.ShouldBe("http");
        address.Host.ShouldBe("insecure-host");
        address.Port.ShouldBe(2113);
    }

    [Test]
    public void has_original_connection_string_returns_false_for_manually_created_options() {
        var options = new KurrentClientOptions {
            ConnectionName = "manual-connection"
        };

        options.HasOriginalConnectionString.ShouldBeFalse();
        options.OriginalConnectionString.ShouldBeEmpty();
    }

    [Test]
    public void has_original_connection_string_returns_true_for_parsed_options() {
        var connectionString = "kurrentdb://localhost";
        var options = KurrentClientOptions.Parse(connectionString);

        options.HasOriginalConnectionString.ShouldBeTrue();
        options.OriginalConnectionString.ShouldBe(connectionString);
    }

    [Test]
    public void to_string_calls_generate_connection_string() {
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = [new DnsEndPoint("localhost", 2113)]
        };

        var toString = options.ToString();
        var generated = options.GenerateConnectionString();

        toString.ShouldBe(generated);
        toString.ShouldContain("kurrentdb://");
        toString.ShouldContain("localhost:2113");
    }

    #endregion

    #region Authentication Configuration Edge Cases

    [Test]
    public void handles_mixed_authentication_configuration_priority() {
        var connectionString = "kurrentdb://user:pass@localhost?userCertFile=/cert.crt&userKeyFile=/key.key";

        var options = KurrentClientOptions.Parse(connectionString);

        options.Security.Authentication.IsCertificateFileCredentials.ShouldBeTrue();
        options.Security.Authentication.IsBasicCredentials.ShouldBeFalse();
    }

    [Test]
    public void handles_basic_authentication_from_connection_string() {
        var connectionString = "kurrentdb://testuser:testpass@localhost";

        var options = KurrentClientOptions.Parse(connectionString);

        options.Security.Authentication.IsBasicCredentials.ShouldBeTrue();
        var credentials = options.Security.Authentication.AsCredentials;
        credentials.Username.ShouldBe("testuser");
        credentials.Password.ShouldBe("testpass");
    }

    [Test]
    public void handles_no_authentication_configuration() {
        var connectionString = "kurrentdb://localhost";

        var options = KurrentClientOptions.Parse(connectionString);

        options.Security.Authentication.IsNoCredentials.ShouldBeTrue();
    }

    #endregion

    #region Transport Security Combinations

    [Test]
    public void handles_tls_enabled_with_certificate_verification_disabled() {
        var connectionString = "kurrentdb://localhost?tls=true&tlsVerifyCert=false";

        var options = KurrentClientOptions.Parse(connectionString);

        options.Security.Transport.IsEnabled.ShouldBeTrue();
        options.Security.Transport.VerifyServerCertificate.ShouldBeFalse();
    }

    [Test]
    public void handles_tls_disabled_configuration() {
        var connectionString = "kurrentdb://localhost?tls=false";

        var options = KurrentClientOptions.Parse(connectionString);

        options.Security.Transport.IsEnabled.ShouldBeFalse();
    }

    [Test]
    public void handles_custom_ca_certificate_configuration() {
        var connectionString = "kurrentdb://localhost?tls=true&tlsCaFile=/path/to/ca.crt";

        var options = KurrentClientOptions.Parse(connectionString);

        options.Security.Transport.IsEnabled.ShouldBeTrue();
        options.Security.Transport.IsFileCertificateTls.ShouldBeTrue();
    }

    #endregion

    #region Validation Integration Tests

    [Test]
    public void validation_fails_for_direct_connection_with_multiple_endpoints() {
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints = [
                new DnsEndPoint("host1", 2113),
                new DnsEndPoint("host2", 2113)
            ]
        };

        var result = options.ValidateOptions();
        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public void validation_succeeds_for_discovery_connection_with_multiple_endpoints() {
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Discover,
            Endpoints = [
                new DnsEndPoint("host1", 2113),
                new DnsEndPoint("host2", 2113)
            ]
        };

        var result = options.ValidateOptions();
        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void validation_fails_for_empty_endpoints_array() {
        var options = new KurrentClientOptions {
            ConnectionScheme = KurrentConnectionScheme.Direct,
            Endpoints        = []
        };

        var result = options.ValidateOptions();
        result.IsSuccess.ShouldBeFalse();
    }

    #endregion

    #region Connection String Generation Edge Cases

    [Test]
    public void generates_minimal_connection_string_for_default_options() {
        var options = new KurrentClientOptions();

        var connectionString = options.GenerateConnectionString();

        connectionString.ShouldContain("kurrentdb://");
        connectionString.ShouldContain("localhost:2113");
        connectionString.ShouldNotContain("connectionName=conn-");
    }

    [Test]
    public void generates_connection_string_with_all_custom_options() {
        var options = new KurrentClientOptions {
            ConnectionName   = "custom-conn",
            ConnectionScheme = KurrentConnectionScheme.Discover,
            Endpoints        = [new DnsEndPoint("custom-host", 2113)],
            Security         = KurrentClientSecurityOptions.Insecure,
            Gossip = new KurrentClientGossipOptions {
                MaxDiscoverAttempts = 20,
                DiscoveryInterval   = TimeSpan.FromMilliseconds(300),
                Timeout             = TimeSpan.FromSeconds(15),
                ReadPreference      = NodePreference.Leader
            },
            Resilience = new KurrentClientResilienceOptions {
                KeepAliveInterval = TimeSpan.FromMinutes(2),
                KeepAliveTimeout  = TimeSpan.FromMinutes(1),
                Deadline          = TimeSpan.FromSeconds(45)
            }
        };

        var connectionString = options.GenerateConnectionString();

        connectionString.ShouldNotBeNullOrEmpty();
        connectionString.ShouldContain("kurrentdb+discover://");
        connectionString.ShouldContain("custom-host:2113");
        connectionString.ShouldContain("tls=false");
        connectionString.ShouldContain("connectionName=custom-conn");
        connectionString.ShouldContain("nodePreference=leader");
        connectionString.ShouldContain("maxDiscoverAttempts=20");
        connectionString.ShouldContain("discoveryInterval=300");
        connectionString.ShouldContain("gossipTimeout=15");
        connectionString.ShouldContain("keepAliveInterval=120");
        connectionString.ShouldContain("keepAliveTimeout=60");
        connectionString.ShouldContain("defaultDeadline=45000");
    }

    #endregion
}
