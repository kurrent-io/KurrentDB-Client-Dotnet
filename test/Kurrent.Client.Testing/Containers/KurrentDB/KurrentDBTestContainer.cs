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
        ["KURRENTDB_TELEMETRY_OPTOUT"]          = "true",
        ["KURRENTDB_ALLOW_UNKNOWN_OPTIONS"]     = "false",
        ["KURRENTDB_MEM_DB"]                    = "false",
        ["KURRENTDB_DISABLE_LOG_FILE"]          = "true",
        ["KURRENTDB_LOG_LEVEL"]                 = "Verbose",
        ["KURRENTDB_ENABLE_ATOM_PUB_OVER_HTTP"] = "true",
        ["KURRENTDB_RUN_PROJECTIONS"]            = "All",
        ["KURRENTDB_START_STANDARD_PROJECTIONS"] = "true",
        ["KURRENTDB_NODE_PORT"]                  = "2113",
        ["KURRENTDB_INSECURE"]                   = "true",

        ["KURRENTDB_USER_CERTIFICATES__ENABLED"]          = "false",
        ["KURRENTDB__PLUGINS__USERCERTIFICATES__ENABLED"] = "false",
    };

    /// <summary>
    /// Default secure configuration for KurrentDB.
    /// This configuration is used when TLS is enabled and user certificates are required.
    /// It includes settings for trusted root certificates and node certificates.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string?> Secure = new Dictionary<string, string?>(Insecure) {
        ["KURRENTDB_INSECURE"]                            = "false",
        ["KURRENTDB_USER_CERTIFICATES__ENABLED"]          = "true",
        ["KURRENTDB__PLUGINS__USERCERTIFICATES__ENABLED"] = "true",
        ["KURRENTDB_TRUSTED_ROOT_CERTIFICATES_PATH"]      = "/etc/kurrentdb/certs/ca",
        ["KURRENTDB_CERTIFICATE_FILE"]                    = "/etc/kurrentdb/certs/node/node.crt",
        ["KURRENTDB_CERTIFICATE_PRIVATE_KEY_FILE"]        = "/etc/kurrentdb/certs/node/node.key",
    };

    /// <summary>
    /// Returns the KurrentDB configuration from the application context.
    /// It combines the default insecure configuration with any overrides specified in the appsettings or environment variables.
    /// </summary>
    public static IReadOnlyDictionary<string, string?> FromContext(Dictionary<string, string?>? baseConfiguration = null) {
        var fromContext = ApplicationContext.Configuration.AsEnumerable()
            .Where(x => x.Key.StartsWith("KURRENTDB_"))
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

    static string DefaultImage => "docker.kurrent.io/kurrent-preview/kurrentdb:25.0.1-experimental-arm64-8.0-jammy";

    public KurrentDBTestContainer(Dictionary<string, string?> settings) : base(DefaultImage) {
        ActiveConfiguration = KurrentDBConfiguration.FromContext(settings).ToDictionary();

        IsSecure = ActiveConfiguration.TryGetValue("KURRENTDB_INSECURE", out var value) && value == "false";

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
            ? "curl -u admin:changeit --cacert /etc/kurrentdb/certs/ca/ca.crt https://localhost:2113/health/live"
            : "curl -o - -I http://admin:changeit@localhost:2113/health/live";

        if (IsSecure) {
            var certsPath = Path.Combine(Environment.CurrentDirectory, "certs");
            CertificatesManager.CheckCertificates(certsPath);
            builder = builder.MountVolume(certsPath, "/etc/kurrentdb/certs", MountType.ReadOnly);
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

    // protected override ValueTask OnStarted() {
    //     // ----
    //     // the idea is to get the public endpoint from the service, but it doesn't work as expected
    //     // at a given point it worked, but I have no idea why it stopped working.
    //     // builder.WithPublicEndpointResolver()
    //     // ----
    //     // var endpoint   = Service.GetPublicEndpoint(DefaultPort);
    //     // var uriBuilder = new UriBuilder("kurrentdb", endpoint.Address.ToString(), endpoint.Port) { Query = "tls=false" };
    //     // AnonymousConnectionString     = uriBuilder.ToString();
    //     // AuthenticatedConnectionString = uriBuilder
    //     //     .With(x => x.UserName = DefaultUsername)
    //     //     .With(x => x.Password = DefaultPassword)
    //     //     .ToString();
    //
    //     return ValueTask.CompletedTask;
    // }
}



//
// [PublicAPI]
// public class KurrentDBTestContainer : TestContainer {
//     public const int    DefaultPort     = 2113;
//     public const string DefaultUsername = "admin";
//     public const string DefaultPassword = "changeit";
//
//         // "docker.kurrent.io/kurrent-latest/kurrentdb:latest"
//         // "docker.kurrent.io/kurrent-preview/kurrentdb:25.0.1-experimental-arm64-8.0-jammy"
//         // "docker.kurrent.io/eventstore/eventstoredb-ee:lts"
//
//     public KurrentDBTestContainer(Dictionary<string, string?> settings) : base("docker.kurrent.io/kurrent-preview/kurrentdb:25.0.1-experimental-arm64-8.0-jammy") {
//         ActiveConfiguration = KurrentDBConfiguration.FromContext(settings).ToDictionary();
//
//         IsSecure = ActiveConfiguration.TryGetValue("KURRENTDB_INSECURE", out var value) && value == "false";
//
//         if (IsSecure) {
//             AuthenticatedConnectionString = "kurrentdb://admin:changeit@localhost:2113/?tls=true&tlsVerifyCert=false";
//             AnonymousConnectionString     = "kurrentdb://localhost:2113/?tls=true&tlsVerifyCert=false";
//         }
//         else {
//             AuthenticatedConnectionString = "kurrentdb://admin:changeit@localhost:2113/?tls=false";
//             AnonymousConnectionString     = "kurrentdb://localhost:2113/?tls=false";
//         }
//     }
//
//     public KurrentDBTestContainer(bool insecure = true)
//         : this(insecure ? KurrentDBConfiguration.Insecure.ToDictionary() : KurrentDBConfiguration.Secure.ToDictionary()) { }
//
//     public Dictionary<string, string?> ActiveConfiguration           { get; }
//     public string                      AuthenticatedConnectionString { get; }
//     public string                      AnonymousConnectionString     { get; }
//
//     bool IsSecure { get; }
//
//     protected override ContainerBuilder ConfigureContainer(ContainerBuilder builder) {
//         var healthCheckCommand = IsSecure
//             ? "curl -u admin:changeit --cacert /etc/kurrentdb/certs/ca/ca.crt https://localhost:2113/health/live"
//             : "curl -o - -I http://admin:changeit@localhost:2113/health/live";
//
//         if (IsSecure) {
//             var certsPath = Path.Combine(Environment.CurrentDirectory, "certs");
//             CertificatesManager.CheckCertificates(certsPath);
//             builder = builder.MountVolume(certsPath, "/etc/kurrentdb/certs", MountType.ReadOnly);
//         }
//
//         return builder
//             .WithPublicEndpointResolver()
//             .WithEnvironment(ActiveConfiguration)
//             .ExposePort(DefaultPort, DefaultPort)
//             .KeepContainer().KeepRunning().ReuseIfExists()
//             .WaitUntilReadyWithConstantBackoff(1_000, 60, service => {
//                 var output = service.ExecuteCommand(healthCheckCommand);
//                 if (!output.Success) throw new(output.Error);
//             });
//     }
//
//     // protected override ValueTask OnStarted() {
//     //     // ----
//     //     // the idea is to get the public endpoint from the service, but it doesn't work as expected
//     //     // at a given point it worked, but I have no idea why it stopped working.
//     //     // builder.WithPublicEndpointResolver()
//     //     // ----
//     //     // var endpoint   = Service.GetPublicEndpoint(DefaultPort);
//     //     // var uriBuilder = new UriBuilder("kurrentdb", endpoint.Address.ToString(), endpoint.Port) { Query = "tls=false" };
//     //     // AnonymousConnectionString     = uriBuilder.ToString();
//     //     // AuthenticatedConnectionString = uriBuilder
//     //     //     .With(x => x.UserName = DefaultUsername)
//     //     //     .With(x => x.Password = DefaultPassword)
//     //     //     .ToString();
//     //
//     //     return ValueTask.CompletedTask;
//     // }
// }
