using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;
using JetBrains.Annotations;
using KurrentDB.Client.Testing.Containers.FluentDocker;

namespace KurrentDB.Client.Testing.Containers.KurrentDB;

[PublicAPI]
public class KurrentDBTestContainer(bool insecure = true) : TestContainer("docker.kurrent.io/kurrent-latest/kurrentdb:latest") {
	public const int    DefaultPort     = 2113;
	public const string DefaultUsername = "admin";
	public const string DefaultPassword = "changeit";

    protected override ContainerBuilder ConfigureContainer(ContainerBuilder builder) {
        var environment = new Dictionary<string, string?> {
	        ["KURRENTDB_TELEMETRY_OPTOUT"]      = "true",
	        ["KURRENTDB_ALLOW_UNKNOWN_OPTIONS"] = "true",
	        ["KURRENTDB_MEM_DB"]                = "false",

	        ["KURRENTDB_LOG_LEVEL"]        = "Default", // required to use serilog settings
	        ["KURRENTDB_DISABLE_LOG_FILE"] = "true",

	        ["KURRENTDB_RUN_PROJECTIONS"]            = "All",
	        ["KURRENTDB_START_STANDARD_PROJECTIONS"] = "true",

	        ["KURRENTDB_INSECURE"]                            = insecure.ToString(),
	        ["KURRENTDB__PLUGINS__USERCERTIFICATES__ENABLED"] = (!insecure).ToString(),

	        ["KURRENTDB_TRUSTED_ROOT_CERTIFICATES"]           = "/etc/kurrentdb/certs/ca/ca.crt",
	        ["KURRENTDB_TRUSTED_ROOT_CERTIFICATES_PATH"]      = "/etc/kurrentdb/certs/ca",
	        ["KURRENTDB_CERTIFICATE_FILE"]                    = "/etc/kurrentdb/certs/node/node.crt",
	        ["KURRENTDB_CERTIFICATE_PRIVATE_KEY_FILE"]        = "/etc/kurrentdb/certs/node/node.key",

	        // ["KURRENTDB_STREAM_EXISTENCE_FILTER_SIZE"] = "10000",
	        // ["KURRENTDB_STREAM_INFO_CACHE_CAPACITY"]   = "10000",

	        ["KURRENTDB_ENABLE_ATOM_PUB_OVER_HTTP"] = "false", // required to use legacy UI

            // ["KURRENTDB_ADVERTISE_HTTP_PORT_TO_CLIENT_AS"] = $"{DefaultPort}",
            // ["KURRENTDB_MAX_APPEND_SIZE"]                  = "4194304"  // Sets the limit to 4MB
        };

        var certsPath = Path.Combine(Environment.CurrentDirectory, "certs");

        return builder
	        .WithPublicEndpointResolver()
            .WithEnvironment(environment)
            .MountVolume(certsPath, "/etc/kurrentdb/certs", MountType.ReadOnly)
            .ExposePort(DefaultPort, DefaultPort)
            .KeepContainer().KeepRunning().ReuseIfExists()
            .WaitUntilReadyWithConstantBackoff(1_000, 60, service => {
                var output = service.ExecuteCommand("curl -o - -I http://admin:changeit@localhost:2113/health/live");
//                var output = service.ExecuteCommand("curl -u admin:changeit --cacert /etc/kurrentdb/certs/ca/ca.crt https://localhost:2113/health/live");
                if (!output.Success) throw new(output.Error);
            });
    }

    public string AuthenticatedConnectionString { get; private set; } = null!;
    public string AnonymousConnectionString     { get; private set; } = null!;

    protected override ValueTask OnStarted() {
        var endpoint   = Service.GetPublicEndpoint(DefaultPort);
        var uriBuilder = new UriBuilder("kurrentdb", endpoint.Address.ToString(), endpoint.Port) {
            Query = "tls=false"
        };

        AnonymousConnectionString = uriBuilder.ToString();

        AuthenticatedConnectionString = uriBuilder
            .With(x => x.UserName = DefaultUsername)
            .With(x => x.Password = DefaultPassword)
            .ToString();

        return ValueTask.CompletedTask;
    }

    // public KurrentDBClientSettings GetClientSettings() => KurrentDBClientSettings
    //     .Create(AuthenticatedConnectionString)
    //     .With(x => {
    //         x.LoggerFactory = new SerilogLoggerFactory(Log.Logger);
    //         x.ConnectivitySettings.KeepAliveInterval = TimeSpan.FromSeconds(60);
    //         x.ConnectivitySettings.KeepAliveTimeout  = TimeSpan.FromSeconds(30);
    //         //x.CreateHttpMessageHandler = () => CreateOptimizedHandler(x);
    //     });
    //
    // public KurrentDBClientSettings GetAnonymousClientSettings() => KurrentDBClientSettings
    //     .Create(AnonymousConnectionString)
    //     .With(x => {
    //         x.LoggerFactory                          = new SerilogLoggerFactory(Log.Logger);
    //         x.ConnectivitySettings.KeepAliveInterval = TimeSpan.FromSeconds(60);
    //         x.ConnectivitySettings.KeepAliveTimeout  = TimeSpan.FromSeconds(30);
    //        // x.CreateHttpMessageHandler               = () => CreateOptimizedHandler(x);
    //     });
    //
    // static SocketsHttpHandler CreateOptimizedHandler(KurrentDBClientSettings settings) {
    //     var handler = new SocketsHttpHandler {
    //         EnableMultipleHttp2Connections = true,
    //         KeepAlivePingDelay             = settings.ConnectivitySettings.KeepAliveInterval,
    //         KeepAlivePingTimeout           = settings.ConnectivitySettings.KeepAliveTimeout,
    //         PooledConnectionIdleTimeout    = Timeout.InfiniteTimeSpan,
    //         KeepAlivePingPolicy            = HttpKeepAlivePingPolicy.Always,
    //
    //         SslOptions = new() {
    //             EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
    //         }
    //     };
    //
    //     if (settings.ConnectivitySettings.Insecure)
    //         return handler;
    //
    //     if (settings.ConnectivitySettings.ClientCertificate != null)
    //         handler.SslOptions.ClientCertificates = new() {
    //             settings.ConnectivitySettings.ClientCertificate
    //         };
    //
    //     handler.SslOptions.RemoteCertificateValidationCallback = settings.ConnectivitySettings.TlsVerifyCert
    //         ? settings.ConnectivitySettings.TlsCaFile is null
    //             ? null
    //             : (_, certificate, chain, _) => {
    //                 if (certificate is not X509Certificate2 certificate2 || chain is null)
    //                     return false;
    //
    //                 chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
    //                 chain.ChainPolicy.CustomTrustStore.Add(settings.ConnectivitySettings.TlsCaFile);
    //                 return chain.Build(certificate2);
    //             }
    //         : (_, _, _, _) => true;
    //
    //     return handler;
    // }
    //
    // public KurrentDBClient GetClient(string? connectionName = null) =>
	   //  new(GetClientSettings().With(x => x.ConnectionName = connectionName));
    //
    // public KurrentDBClient GetAnonymousClient(string? connectionName = null) =>
	   //  new(GetAnonymousClientSettings().With(x => x.ConnectionName = connectionName));
}
