// ReSharper disable InconsistentNaming
// ReSharper disable InvertIf

using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;
using Humanizer;
using KurrentDB.Client.Tests.FluentDocker;
using Serilog;
using Serilog.Extensions.Logging;

namespace KurrentDB.Client.Tests;

public class KurrentDBTemporaryTestNode(KurrentDBFixtureOptions? options = null) : TestContainerService {
	static readonly NetworkPortProvider NetworkPortProvider = new(NetworkPortProvider.DefaultEsdbPort);

	KurrentDBFixtureOptions Options { get; } = options ?? DefaultOptions();

	public static KurrentDBFixtureOptions DefaultOptions() {
		const string connString = "kurrentdb://admin:changeit@localhost:{port}/?tlsVerifyCert=false";

		var port = NetworkPortProvider.NextAvailablePort;

		var defaultSettings = KurrentDBClientSettings
			.Create(connString.Replace("{port}", $"{port}"))
			.With(x => x.LoggerFactory = new SerilogLoggerFactory(Log.Logger))
			.With(x => x.RetrySettings = KurrentDBClientRetrySettings.Default);

		var defaultEnvironment = new Dictionary<string, string?>(GlobalEnvironment.Variables) {
			["EVENTSTORE_ADVERTISE_NODE_PORT_TO_CLIENT_AS"] = $"{port}",
			["EVENTSTORE_ADVERTISE_HTTP_PORT_TO_CLIENT_AS"] = $"{port}",
		};

		return new(defaultSettings, defaultEnvironment);
	}

	protected override ContainerBuilder Configure() {
		var env  = Options.Environment.Select(pair => $"{pair.Key}={pair.Value}").ToArray();
		var port = Options.DBClientSettings.ConnectivitySettings.ResolvedAddressOrDefault.Port;

		var certsPath = Path.Combine(Environment.CurrentDirectory, "certs");

		var containerName = $"dotnet-client-test-{port}-{Guid.NewGuid().ToString()[30..]}";

		CertificatesManager.VerifyCertificatesExist(certsPath);

		var builder = new Builder()
			.UseContainer()
			.UseImage(Options.Environment["ES_DOCKER_IMAGE"])
			.WithName(containerName)
			.WithPublicEndpointResolver()
			.WithEnvironment(env)
			.MountVolume(certsPath, "/etc/eventstore/certs", MountType.ReadOnly)
			.ExposePort(port, 2113)
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

		return builder;
	}
}
