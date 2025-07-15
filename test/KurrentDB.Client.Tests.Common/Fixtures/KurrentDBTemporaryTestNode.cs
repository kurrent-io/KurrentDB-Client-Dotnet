// ReSharper disable InconsistentNaming
// ReSharper disable InvertIf

using System.Net;
using System.Net.Sockets;
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
			["EVENTSTORE_ADVERTISE_NODE_PORT_TO_CLIENT_AS"]    = $"{port}",
			["EVENTSTORE_ADVERTISE_HTTP_PORT_TO_CLIENT_AS"]    = $"{port}",
			["EVENTSTORE_MAX_APPEND_SIZE"]                     = $"{4.Megabytes().Bytes}"
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
				}
			);

		return builder;
	}
}

/// <summary>
/// Using the default 2113 port assumes that the test is running sequentially.
/// </summary>
/// <param name="port"></param>
class NetworkPortProvider(int port = 2114) {
	int _port            = port;

	public const int DefaultEsdbPort = 2113;

	static readonly SemaphoreSlim Semaphore = new(1, 1);

	async Task<int> GetNextAvailablePort(TimeSpan delay = default) {
		await Semaphore.WaitAsync();

		try {
			using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			while (true) {
				var nexPort = Interlocked.Increment(ref _port);

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

	public int NextAvailablePort => GetNextAvailablePort(100.Milliseconds()).GetAwaiter().GetResult();
}
