// ReSharper disable InconsistentNaming
// ReSharper disable InvertIf

using Ductus.FluentDocker.Builders;
using KurrentDB.Client.Tests.FluentDocker;
using Serilog;
using Serilog.Extensions.Logging;

namespace KurrentDB.Client.Tests;

public class KurrentDBPermanentTestNode(KurrentDBFixtureOptions? options = null) : TestContainerService {
	KurrentDBFixtureOptions Options { get; } = options ?? DefaultOptions();

	public static KurrentDBFixtureOptions DefaultOptions() {
		const int port = 2113;

		var defaultSettings = KurrentDBClientSettings
			.Create(ConnectionString.Replace("{port}", port.ToString()))
			.With(x => x.LoggerFactory = new SerilogLoggerFactory(Log.Logger));

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
		var builder = CreateContainer(Options.Environment);

		builder.KeepContainer()
			.KeepRunning()
			.ReuseIfExists();

		return AddReadinessCheck(builder);
	}
}
