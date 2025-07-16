// ReSharper disable InconsistentNaming
// ReSharper disable InvertIf

using Ductus.FluentDocker.Builders;
using Humanizer;
using KurrentDB.Client.Tests.FluentDocker;
using Serilog;
using Serilog.Extensions.Logging;

namespace KurrentDB.Client.Tests;

public class KurrentDBTemporaryTestNode(KurrentDBFixtureOptions? options = null) : TestContainerService {
	static readonly NetworkPortProvider NetworkPortProvider = new(NetworkPortProvider.DefaultEsdbPort);

	KurrentDBFixtureOptions Options { get; } = options ?? DefaultOptions();

	public static KurrentDBFixtureOptions DefaultOptions() {
		var port = NetworkPortProvider.NextAvailablePort;

		var defaultSettings = KurrentDBClientSettings
			.Create(ConnectionString.Replace("{port}", $"{port}"))
			.With(x => x.LoggerFactory = new SerilogLoggerFactory(Log.Logger))
			.With(x => x.RetrySettings = KurrentDBClientRetrySettings.Default);

		var defaultEnvironment = new Dictionary<string, string?>(GlobalEnvironment.Variables) {
			["EVENTSTORE_ADVERTISE_NODE_PORT_TO_CLIENT_AS"] = $"{port}",
			["EVENTSTORE_ADVERTISE_HTTP_PORT_TO_CLIENT_AS"] = $"{port}",
		};

		return new(defaultSettings, defaultEnvironment);
	}

	protected override ContainerBuilder Configure() {
		var port = Options.DBClientSettings.ConnectivitySettings.ResolvedAddressOrDefault.Port;

		var containerName = $"dotnet-client-test-{port}-{Guid.NewGuid():N}";

		var builder = CreateContainer(Options.Environment, containerName, port);

		return AddReadinessCheck(builder);
	}
}
