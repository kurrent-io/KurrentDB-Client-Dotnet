// ReSharper disable InconsistentNaming
// ReSharper disable InvertIf

using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;
using KurrentDB.Client.Tests.FluentDocker;
using Serilog;
using Serilog.Extensions.Logging;

namespace KurrentDB.Client.Tests;

public class KurrentDBPermanentTestNode(KurrentDBFixtureOptions? options = null) : TestContainerService {
	KurrentDBFixtureOptions Options { get; } = options ?? DefaultOptions();

	public static KurrentDBFixtureOptions DefaultOptions() {
		const string uri = "kurrentdb://admin:changeit@localhost:{port}/?tlsVerifyCert=false";

		const int port = 2113;

		var defaultSettings = KurrentDBClientSettings
			.Create(uri.Replace("{port}", port.ToString()))
			.With(x => x.LoggerFactory = new SerilogLoggerFactory(Log.Logger))
			.With(x => x.RetrySettings = KurrentDBClientRetrySettings.Default);

		var defaultEnvironment = new Dictionary<string, string?>(GlobalEnvironment.Variables) {
			["EVENTSTORE_START_STANDARD_PROJECTIONS"]       = "true",
			["EVENTSTORE_RUN_PROJECTIONS"]                  = "All",
			["EVENTSTORE_ADVERTISE_HTTP_PORT_TO_CLIENT_AS"] = port.ToString(),
			["EVENTSTORE_ADVERTISE_NODE_PORT_TO_CLIENT_AS"] = port.ToString(),
			["EVENTSTORE_NODE_PORT"]                        = port.ToString(),
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
			.WithName("dotnet-client-test")
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
					if (!versionOutput.Success)
						versionOutput = service.ExecuteCommand("/opt/eventstore/eventstored --version");

					if (versionOutput.Success && TryParseVersion(versionOutput.Log.FirstOrDefault(), out var version))
						_version ??= version;
				}
			);
	}
}
