// ReSharper disable InconsistentNaming
// ReSharper disable InvertIf

using System.Net;
using System.Net.Sockets;
using Ductus.FluentDocker.Builders;
using Humanizer;
using KurrentDB.Client.Tests.FluentDocker;
using Serilog;
using Serilog.Extensions.Logging;

namespace KurrentDB.Client.Tests;

public class KurrentDBTemporaryTestNode(KurrentDBFixtureOptions? options = null) : TestContainerService {
	KurrentDBFixtureOptions Options { get; } = options ?? DefaultOptions();

	static readonly NetworkPortProvider PortProvider = new(NetworkPortProvider.DefaultPort);

	public static KurrentDBFixtureOptions DefaultOptions() {
		var port = PortProvider.NextAvailablePort;

		var defaultSettings = KurrentDBClientSettings
			.Create(ConnectionString.Replace("{port}", $"{port}"))
			.With(x => x.LoggerFactory = new SerilogLoggerFactory(Log.Logger));

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

/// <summary>
/// Using the default 2113 port assumes that the test is running sequentially.
/// </summary>
/// <param name="port"></param>
internal class NetworkPortProvider(int port = 2114) {
	int _port = port;

	public const int DefaultPort = 2113;

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
					if (socket.Connected) socket.Disconnect(true);
				}
			}
		} finally {
			Semaphore.Release();
		}
	}

	public int NextAvailablePort => GetNextAvailablePort(100.Milliseconds()).GetAwaiter().GetResult();
}
