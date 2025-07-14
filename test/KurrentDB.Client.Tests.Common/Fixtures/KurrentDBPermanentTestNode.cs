using System.Diagnostics.CodeAnalysis;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;
using KurrentDB.Client;
using KurrentDB.Client.Tests.FluentDocker;
using Serilog;
using Serilog.Extensions.Logging;

public class KurrentDBPermanentTestNode(KurrentDBFixtureOptions? options = null) : TestContainerService {
	KurrentDBFixtureOptions Options { get; } = options ?? DefaultOptions();

	static Version? _version;

	public static Version Version => _version;

	public static KurrentDBFixtureOptions DefaultOptions() {
		const string connString = "kurrentdb://admin:changeit@localhost:{port}/?tlsVerifyCert=false";

		var port = 2113; // NetworkPortProvider.NextAvailablePort;

		var defaultSettings = KurrentDBClientSettings
			.Create(connString.Replace("{port}", $"{port}"))
			.With(x => x.LoggerFactory = new SerilogLoggerFactory(Log.Logger))
			.With(x => x.RetrySettings = KurrentDBClientRetrySettings.Default);
			// .With(x => x.DefaultDeadline = Application.DebuggerIsAttached ? null : FromSeconds(30))
			// .With(x => x.ConnectivitySettings.MaxDiscoverAttempts = 20)
			// .With(x => x.ConnectivitySettings.DiscoveryInterval = FromSeconds(1));

		var defaultEnvironment = new Dictionary<string, string?>(GlobalEnvironment.Variables) {
			["EVENTSTORE_MEM_DB"]                              = "true",
			["EVENTSTORE_CERTIFICATE_FILE"]                    = "/etc/eventstore/certs/node/node.crt",
			["EVENTSTORE_CERTIFICATE_PRIVATE_KEY_FILE"]        = "/etc/eventstore/certs/node/node.key",
			["EVENTSTORE_TRUSTED_ROOT_CERTIFICATES_PATH"]      = "/etc/eventstore/certs/ca",
			["EVENTSTORE__PLUGINS__USERCERTIFICATES__ENABLED"] = "true",
			["EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE"]        = "10000",
			["EVENTSTORE_STREAM_INFO_CACHE_CAPACITY"]          = "10000",
			["EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP"]           = "true",
			["EVENTSTORE_LOG_LEVEL"]                           = "Default", // required to use serilog settings
			["EVENTSTORE_DISABLE_LOG_FILE"]                    = "true",
			["EVENTSTORE_START_STANDARD_PROJECTIONS"]          = "true",
			["EVENTSTORE_RUN_PROJECTIONS"]                     = "All",
			["EVENTSTORE_ADVERTISE_HTTP_PORT_TO_CLIENT_AS"]    = "2113",
			["EVENTSTORE_ADVERTISE_NODE_PORT_TO_CLIENT_AS"]    = "2113",
			["EVENTSTORE_NODE_PORT"]                           = "2113",
			["EVENTSTORE_MAX_APPEND_SIZE"]                     = "4194304" // Sets the limit to 4MB
		};

		if (GlobalEnvironment.DockerImage.Contains("commercial")) {
			defaultEnvironment["EVENTSTORE_CERTIFICATE_FILE"]                    = "/etc/eventstore/certs/node/node.crt";
			defaultEnvironment["EVENTSTORE_CERTIFICATE_PRIVATE_KEY_FILE"]        = "/etc/eventstore/certs/node/node.key";
			defaultEnvironment["EVENTSTORE_TRUSTED_ROOT_CERTIFICATES_PATH"]      = "/etc/eventstore/certs/ca";
			defaultEnvironment["EventStore__Plugins__UserCertificates__Enabled"] = "true";
		}

		return new(defaultSettings, defaultEnvironment);
	}

	protected override ContainerBuilder Configure() {
		var env  = Options.Environment.Select(pair => $"{pair.Key}={pair.Value}").ToArray();
		var port = Options.DBClientSettings.ConnectivitySettings.Address?.Port ?? KurrentDBClientConnectivitySettings.DefaultPort;

		var certsPath = Path.Combine(Environment.CurrentDirectory, "certs");

		CertificatesManager.VerifyCertificatesExist(certsPath);

		return new Builder()
			.UseContainer()
			.UseImage(Options.Environment["ES_DOCKER_IMAGE"])
			.WithName("kurrentdb-dotnet-test")
			.WithPublicEndpointResolver()
			.WithEnvironment(env)
			.MountVolume(certsPath, "/etc/eventstore/certs", MountType.ReadOnly)
			.ExposePort(port, 2113)
			.KeepContainer().KeepRunning().ReuseIfExists()
			.WaitUntilReadyWithConstantBackoff(
				1_000,
				60,
				service => {
					var output = service.ExecuteCommand("curl -u admin:changeit --cacert /etc/eventstore/certs/ca/ca.crt https://localhost:2113/health/live");
					if (!output.Success)
						throw new Exception(output.Error);
				}
			);
	}
}
