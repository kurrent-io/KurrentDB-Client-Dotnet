using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;
using JetBrains.Annotations;
using Kurrent.Client.Testing.Containers.FluentDocker;
using Microsoft.Extensions.Configuration;
using static System.StringComparer;

namespace Kurrent.Client.Testing.Containers.KurrentDB;

public static class KurrentDBConfiguration {
    /// <summary>
    /// Default insecure configuration for KurrentDB.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string?> Insecure = new Dictionary<string, string?> {
        ["EVENTSTORE_TELEMETRY_OPTOUT"]          = "true",
        ["EVENTSTORE_ALLOW_UNKNOWN_OPTIONS"]     = "false",
        ["EVENTSTORE_MEM_DB"]                    = "false",
        ["EVENTSTORE_DISABLE_LOG_FILE"]          = "true",
        ["EVENTSTORE_LOG_LEVEL"]                 = "Verbose",
        ["EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP"] = "true",
        ["EVENTSTORE_RUN_PROJECTIONS"]            = "All",
        ["EVENTSTORE_START_STANDARD_PROJECTIONS"] = "true",
        ["EVENTSTORE_NODE_PORT"]                  = "2113",
        ["EVENTSTORE_INSECURE"]                   = "true",

        ["EVENTSTORE_USER_CERTIFICATES__ENABLED"]          = "false",
        ["EVENTSTORE__PLUGINS__USERCERTIFICATES__ENABLED"] = "false",
    };

    /// <summary>
    /// Default secure configuration for KurrentDB.
    /// This configuration is used when TLS is enabled and user certificates are required.
    /// It includes settings for trusted root certificates and node certificates.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string?> Secure = new Dictionary<string, string?>(Insecure) {
        ["EVENTSTORE_INSECURE"]                            = "false",
        // TODO: This is causing some issues in secure tests
        // ["EVENTSTORE_USER_CERTIFICATES__ENABLED"]          = "true",
        // ["EVENTSTORE__PLUGINS__USERCERTIFICATES__ENABLED"] = "true",
        ["EVENTSTORE_TRUSTED_ROOT_CERTIFICATES_PATH"]      = "/etc/eventstore/certs/ca",
        ["EVENTSTORE_CERTIFICATE_FILE"]                    = "/etc/eventstore/certs/node/node.crt",
        ["EVENTSTORE_CERTIFICATE_PRIVATE_KEY_FILE"]        = "/etc/eventstore/certs/node/node.key",
    };

    /// <summary>
    /// Returns the KurrentDB configuration from the application context.
    /// It combines the default insecure configuration with any overrides specified in the appsettings or environment variables.
    /// </summary>
    public static IReadOnlyDictionary<string, string?> FromContext(Dictionary<string, string?>? baseConfiguration = null) {
        var fromContext = ApplicationContext.Configuration.AsEnumerable()
            .Where(x => x.Key.StartsWith("EVENTSTORE_"))
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value);

        return (baseConfiguration ?? Insecure)
            .Concat(fromContext)
            .GroupBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Last().Value, OrdinalIgnoreCase);
    }
}

[PublicAPI]
public class KurrentDBTestContainer : TestContainer {
    public const int    DefaultPort     = 2113;
    public const string DefaultUsername = "admin";
    public const string DefaultPassword = "changeit";

    static string DefaultImage => ApplicationContext.Configuration["Kurrent:Client:Tests:ContainerImage"]
                               ?? Environment.GetEnvironmentVariable("TESTCONTAINER_KURRENTDB_IMAGE")
                               ?? "docker.cloudsmith.io/eventstore/kurrent-staging/kurrentdb:ci";

    public KurrentDBTestContainer(Dictionary<string, string?> settings) : base(DefaultImage) {
        ActiveConfiguration = KurrentDBConfiguration.FromContext(settings).ToDictionary();

        IsSecure = ActiveConfiguration.TryGetValue("EVENTSTORE_INSECURE", out var value) && value == "false";

        if (IsSecure) {
            AuthenticatedConnectionString = "kurrentdb://admin:changeit@localhost:2113/?tls=true&tlsVerifyCert=false";
            AnonymousConnectionString     = "kurrentdb://localhost:2113/?tls=true&tlsVerifyCert=false";
        }
        else {
            AuthenticatedConnectionString = "kurrentdb://admin:changeit@localhost:2113/?tls=false";
            AnonymousConnectionString     = "kurrentdb://localhost:2113/?tls=false";
        }
    }

    public KurrentDBTestContainer(bool insecure = true)
        : this(insecure ? KurrentDBConfiguration.Insecure.ToDictionary() : KurrentDBConfiguration.Secure.ToDictionary()) { }

    public Dictionary<string, string?> ActiveConfiguration           { get; }
    public string                      AuthenticatedConnectionString { get; }
    public string                      AnonymousConnectionString     { get; }

    bool IsSecure { get; }

    protected override ContainerBuilder ConfigureContainer(ContainerBuilder builder) {
        var healthCheckCommand = IsSecure
            ? "curl -u admin:changeit --cacert /etc/eventstore/certs/ca/ca.crt https://localhost:2113/health/live"
            : "curl -o - -I http://admin:changeit@localhost:2113/health/live";

        if (IsSecure) {
            var certsPath = Path.Combine(Environment.CurrentDirectory, "certs");
            CertificatesManager.CheckCertificates(certsPath);
            builder = builder.MountVolume(certsPath, "/etc/eventstore/certs", MountType.ReadOnly);
        }

        return builder
            .WithPublicEndpointResolver()
            .WithEnvironment(ActiveConfiguration)
            .ExposePort(DefaultPort, DefaultPort)
            .KeepContainer().KeepRunning().ReuseIfExists()
            .WaitUntilReadyWithConstantBackoff(1_000, 60, service => {
                var output = service.ExecuteCommand(healthCheckCommand);
                if (!output.Success) throw new(output.Error);
            });
    }
}
