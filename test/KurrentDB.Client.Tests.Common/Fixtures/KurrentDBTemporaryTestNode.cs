using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;
using KurrentDB.Client.Tests.FluentDocker;
using Serilog;
using Serilog.Extensions.Logging;
using static System.TimeSpan;

namespace KurrentDB.Client.Tests.TestNode;

public class KurrentDBTemporaryTestNode(KurrentDBFixtureOptions? options = null) : TestContainerService {
	static readonly NetworkPortProvider NetworkPortProvider = new(NetworkPortProvider.DefaultEsdbPort);

	KurrentDBFixtureOptions Options { get; } = options ?? DefaultOptions();

	static Version? _version;

	public static Version Version => _version;

	public static KurrentDBFixtureOptions DefaultOptions() {
		const string connString = "kurrentdb://admin:changeit@localhost:{port}/?tlsVerifyCert=false";

		var port = NetworkPortProvider.NextAvailablePort;

		var defaultSettings = KurrentDBClientSettings
			.Create(connString.Replace("{port}", $"{port}"))
			.With(x => x.LoggerFactory = new SerilogLoggerFactory(Log.Logger))
			.With(x => x.RetrySettings = KurrentDBClientRetrySettings.Default);
			// .With(x => x.DefaultDeadline = Application.DebuggerIsAttached ? null : FromSeconds(30))
			// .With(x => x.ConnectivitySettings.MaxDiscoverAttempts = 20)
			// .With(x => x.ConnectivitySettings.DiscoveryInterval = FromSeconds(1));

		var defaultEnvironment = new Dictionary<string, string?>(GlobalEnvironment.Variables) {
			["EVENTSTORE_MEM_DB"]                              = "true",
			["EVENTSTORE_CERTIFICATE_FILE"]                    = "/etc/kurrentdb/certs/node/node.crt",
			["EVENTSTORE_CERTIFICATE_PRIVATE_KEY_FILE"]        = "/etc/kurrentdb/certs/node/node.key",
			["EVENTSTORE_TRUSTED_ROOT_CERTIFICATES_PATH"]      = "/etc/kurrentdb/certs/ca",
			["EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE"]        = "10000",
			["EVENTSTORE_STREAM_INFO_CACHE_CAPACITY"]          = "10000",
			["EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP"]           = "true",
			["EVENTSTORE_LOG_LEVEL"]                           = "Default", // required to use serilog settings
			["EVENTSTORE_DISABLE_LOG_FILE"]                    = "true",
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

		var containerName = $"kurrentdb-dotnet-test-{port}-{Guid.NewGuid().ToString()[30..]}";

		CertificatesManager.VerifyCertificatesExist(certsPath);

		var builder = new Builder()
			.UseContainer()
			.UseImage(Options.Environment["ES_DOCKER_IMAGE"])
			.WithName(containerName)
			.WithPublicEndpointResolver()
			.WithEnvironment(env)
			.MountVolume(certsPath, "/etc/kurrentdb/certs", MountType.ReadOnly)
			.ExposePort(port, 2113)
			.WaitUntilReadyWithConstantBackoff(
				1_000, 60, service => {
					var output = service.ExecuteCommand("curl -u admin:changeit --cacert /etc/kurrentdb/certs/ca/ca.crt https://localhost:2113/health/live");
					if (!output.Success) {
						var connectionError = output.Log.FirstOrDefault(x => x.Contains("curl:"));
						throw connectionError is not null
							? new Exception($"KurrentDB health check failed: {connectionError}")
							: new Exception($"KurrentDB is not running or not reachable. {Environment.NewLine}{output.Error}");
					}

					var versionOutput = service.ExecuteCommand("/opt/kurrentdb/kurrentd --version");
					if (versionOutput.Success && TryParseKurrentDBVersion(versionOutput.Log.FirstOrDefault(), out var version))
						_version ??= version;
				}
			);

		return builder;
	}

	static bool TryParseKurrentDBVersion(ReadOnlySpan<char> input, [MaybeNullWhen(false)] out Version version) {
		version = null!;

		if (input.IsEmpty)
			return false;

		var kurrentPrefix    = "KurrentDB version ".AsSpan();
		var eventStorePrefix = "EventStore version ".AsSpan();

		ReadOnlySpan<char> versionPart;
		if (input.StartsWith(kurrentPrefix))
			versionPart = input[kurrentPrefix.Length..];
		else if (input.StartsWith(eventStorePrefix))
			versionPart = input[eventStorePrefix.Length..];
		else
			return false;

		try {
			var spaceIndex  = versionPart.IndexOf(' ');
			var versionText = spaceIndex >= 0 ? versionPart[..spaceIndex] : versionPart;

			Span<int> components = stackalloc int[4];

			var componentCount = 0;
			var start          = 0;

			for (var i = 0; i <= versionText.Length; i++) {
				if (i != versionText.Length && versionText[i] != '.') continue;

				if (componentCount >= 4)
					break;

				var componentSpan = versionText.Slice(start, i - start);

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
		} catch (Exception ex) {
			throw new Exception($"Failed to parse version from: {input.ToString()}", ex);
		}
	}
}

/// <summary>
/// Using the default 2113 port assumes that the test is running sequentially.
/// </summary>
/// <param name="port"></param>
class NetworkPortProvider(int port = 2114) {
	public const int DefaultEsdbPort = 2113;

	static readonly SemaphoreSlim Semaphore = new(1, 1);

	async Task<int> GetNextAvailablePort(TimeSpan delay = default) {
		await Semaphore.WaitAsync();

		try {
			using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			while (true) {
				var nexPort = Interlocked.Increment(ref port);

				try {
					await socket.ConnectAsync(IPAddress.Any, nexPort);
				} catch (SocketException ex) {
					if (ex.SocketErrorCode is SocketError.ConnectionRefused or not SocketError.IsConnected) {
						return nexPort;
					}

					await Task.Delay(delay);
				} finally {
					if (socket.Connected) await socket.DisconnectAsync(true);
				}
			}
		} finally {
			Semaphore.Release();
		}
	}

	public int NextAvailablePort => GetNextAvailablePort(FromMilliseconds(100)).GetAwaiter().GetResult();
}
