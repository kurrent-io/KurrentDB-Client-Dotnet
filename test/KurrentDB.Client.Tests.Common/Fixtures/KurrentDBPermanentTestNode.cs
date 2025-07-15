// ReSharper disable InconsistentNaming
// ReSharper disable InvertIf

using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;
using Humanizer;
using KurrentDB.Client.Tests.FluentDocker;
using Serilog;
using Serilog.Extensions.Logging;

namespace KurrentDB.Client.Tests;

public class KurrentDBPermanentTestNode(KurrentDBFixtureOptions? options = null) : TestContainerService {
	KurrentDBFixtureOptions Options { get; } = options ?? DefaultOptions();

	public static KurrentDBFixtureOptions DefaultOptions() {
		const string connString = "kurrentdb://admin:changeit@localhost:{port}/?tlsVerifyCert=false";

		const int port = 2113;

		var defaultSettings = KurrentDBClientSettings
			.Create(connString.Replace("{port}", $"{port}"))
			.With(x => x.LoggerFactory = new SerilogLoggerFactory(Log.Logger))
			.With(x => x.RetrySettings = KurrentDBClientRetrySettings.Default);

		var defaultEnvironment = new Dictionary<string, string?>(GlobalEnvironment.Variables) {
			["EVENTSTORE_MEM_DB"]                              = "true",
			["EVENTSTORE_CERTIFICATE_FILE"]                    = "/etc/eventstore/certs/node/node.crt",
			["EVENTSTORE_CERTIFICATE_PRIVATE_KEY_FILE"]        = "/etc/eventstore/certs/node/node.key",
			["EVENTSTORE_TRUSTED_ROOT_CERTIFICATES_PATH"]      = "/etc/eventstore/certs/ca",
			["EVENTSTORE__PLUGINS__USERCERTIFICATES__ENABLED"] = "true",
			["EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE"]        = "10000",
			["EVENTSTORE_STREAM_INFO_CACHE_CAPACITY"]          = "10000",
			["EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP"]           = "true",
			["EVENTSTORE_LOG_LEVEL"]                           = "Default",
			["EVENTSTORE_DISABLE_LOG_FILE"]                    = "true",
			["EVENTSTORE_START_STANDARD_PROJECTIONS"]          = "true",
			["EVENTSTORE_RUN_PROJECTIONS"]                     = "All",
			["EVENTSTORE_ADVERTISE_HTTP_PORT_TO_CLIENT_AS"]    = "2113",
			["EVENTSTORE_ADVERTISE_NODE_PORT_TO_CLIENT_AS"]    = "2113",
			["EVENTSTORE_NODE_PORT"]                           = "2113",
			["EVENTSTORE_MAX_APPEND_SIZE"]                     = $"{4.Megabytes().Bytes}"
		};

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

					var versionOutput = service.ExecuteCommand("/opt/kurrentdb/kurrentd --version");
					if (versionOutput.Success && TryParseVersion(versionOutput.Log.FirstOrDefault(), out var version))
						_version ??= version;
				}
			);
	}
}
