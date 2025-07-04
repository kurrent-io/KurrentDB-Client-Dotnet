// ReSharper disable InconsistentNaming

// using Ductus.FluentDocker.Builders;
// using Ductus.FluentDocker.Model.Builders;
// using KurrentDB.Client.Tests.FluentDocker;
//
// namespace KurrentDB.Client.Tests;
//
// public class EventStorePermanentTestNode(EventStoreFixtureOptions? options = null) : BaseTestNode(options) {
// 	protected override ContainerBuilder ConfigureContainer(ContainerBuilder builder) {
// 		var port      = Options.ClientSettings.ConnectivitySettings.ResolvedAddressOrDefault.Port;
// 		var certsPath = Path.Combine(Environment.CurrentDirectory, "certs");
//
// 		var containerName = "es-client-dotnet-test";
//
// 		return builder
// 			.UseImage(Options.Environment["ES_DOCKER_IMAGE"])
// 			.WithName(containerName)
// 			.WithPublicEndpointResolver()
// 			.MountVolume(certsPath, "/etc/eventstore/certs", MountType.ReadOnly)
// 			.ExposePort(port, 2113)
// 			.KeepContainer().KeepRunning().ReuseIfExists()
// 			.WaitUntilReadyWithConstantBackoff(
// 				1_000,
// 				60,
// 				service => {
// 					var output = service.ExecuteCommand("curl -u admin:changeit --cacert /etc/eventstore/certs/ca/ca.crt https://localhost:2113/health/live");
// 					if (!output.Success)
// 						throw new Exception(output.Error);
// 				}
// 			);
// 	}
// }

using System.Diagnostics.CodeAnalysis;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;
using Ductus.FluentDocker.Model.Containers;
using Ductus.FluentDocker.Services;
using KurrentDB.Client;
using KurrentDB.Client.Tests.FluentDocker;
using Serilog;
using Serilog.Extensions.Logging;
using static System.TimeSpan;

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
			["KURRENTDB_MEM_DB"]                              = "true",
			["KURRENTDB_CERTIFICATE_FILE"]                    = "/etc/kurrentdb/certs/node/node.crt",
			["KURRENTDB_CERTIFICATE_PRIVATE_KEY_FILE"]        = "/etc/kurrentdb/certs/node/node.key",
			["KURRENTDB_TRUSTED_ROOT_CERTIFICATES_PATH"]      = "/etc/kurrentdb/certs/ca",
			["KURRENTDB__PLUGINS__USERCERTIFICATES__ENABLED"] = "true",
			["KURRENTDB_STREAM_EXISTENCE_FILTER_SIZE"]        = "10000",
			["KURRENTDB_STREAM_INFO_CACHE_CAPACITY"]          = "10000",
			["KURRENTDB_ENABLE_ATOM_PUB_OVER_HTTP"]           = "true",
			["KURRENTDB_LOG_LEVEL"]                           = "Default", // required to use serilog settings
			["KURRENTDB_DISABLE_LOG_FILE"]                    = "true",
			["KURRENTDB_START_STANDARD_PROJECTIONS"]          = "true",
			["KURRENTDB_RUN_PROJECTIONS"]                     = "All",
			["KURRENTDB_ADVERTISE_HTTP_PORT_TO_CLIENT_AS"]    = "2113",
			["KURRENTDB_ADVERTISE_NODE_PORT_TO_CLIENT_AS"]    = "2113",
			["KURRENTDB_NODE_PORT"]                           = "2113",
			["KURRENTDB_MAX_APPEND_SIZE"]                     = "4194304" // Sets the limit to 4MB
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
			.MountVolume(certsPath, "/etc/kurrentdb/certs", MountType.ReadOnly)
			.ExposePort(port, 2113)
			.KeepContainer().KeepRunning().ReuseIfExists()
			.WaitUntilReadyWithConstantBackoff(500, 10, service => {
                var output = service.ExecuteCommand("curl -u admin:changeit --cacert /etc/kurrentdb/certs/ca/ca.crt https://localhost:2113/health/live");
                if (!output.Success) {
	                var connectionError = output.Log.FirstOrDefault(x => x.Contains("curl:"));
	                throw connectionError is not null
		                ? new Exception($"KurrentDB health check failed: {connectionError}")
		                : new Exception(
			                $"KurrentDB is not running or not reachable. {Environment.NewLine}{output.Error}"
		                );
                }

                var versionOutput = service.ExecuteCommand("/opt/kurrentdb/kurrentd --version");
                if (versionOutput.Success && TryParseKurrentDBVersion(versionOutput.Log.FirstOrDefault(), out var version))
	                _version ??= version;
            });
	}

    static bool TryParseKurrentDBVersion(ReadOnlySpan<char> input, [MaybeNullWhen(false)] out Version version) {
        version = null!;

        if (input.IsEmpty)
            return false;

        var versionPrefix = "KurrentDB version ".AsSpan();

        if (!input.StartsWith(versionPrefix))
            return false;

        try {
            // Skip past "KurrentDB version "
            var versionPart = input[versionPrefix.Length..];

            // Extract the version portion (everything before the first space)
            var spaceIndex  = versionPart.IndexOf(' ');
            var versionText = spaceIndex >= 0 ? versionPart[..spaceIndex] : versionPart;

            // Parse version components by finding dots
            Span<int> components = stackalloc int[4];

            var componentCount = 0;
            var start          = 0;

            for (var i = 0; i <= versionText.Length; i++) {
                if (i != versionText.Length && versionText[i] != '.') continue;

                if (componentCount >= 4)
                    break;

                var componentSpan = versionText.Slice(start, i - start);

                // Extract only the numeric part of the component
                var numericEnd = 0;
                while (numericEnd < componentSpan.Length && char.IsDigit(componentSpan[numericEnd]))
	                numericEnd++;

                if (numericEnd == 0 || !int.TryParse(componentSpan[..numericEnd], out components[componentCount]))
	                return false;

                componentCount++;
                start = i + 1;

            }

            if (componentCount < 2)
                return false;

            // Create Version object with appropriate constructor
            version = componentCount switch {
                2 => new Version(components[0], components[1]),
                3 => new Version(components[0], components[1], components[2]),
                >= 4 => new Version(
                    components[0], components[1], components[2],
                    components[3]
                ),
                _ => null
            };

            return version is not null;
        }
        catch (Exception ex) {
	        throw new Exception($"Failed to parse KurrentDB version from: {input.ToString()}", ex);
        }
    }
}
