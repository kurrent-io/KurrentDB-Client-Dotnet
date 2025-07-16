// ReSharper disable InconsistentNaming

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using System.Net;
using System.Net.Sockets;
using Humanizer;

namespace KurrentDB.Client.Tests.FluentDocker;

public abstract partial class TestContainerService : TestService<IContainerService, ContainerBuilder> {
	internal static Version? _version;

	public static Version Version => _version;

	[GeneratedRegex(@"\b(?:KurrentDB|EventStore)\s+version\s+([0-9]+(?:\.[0-9]+)*)")]
	private static partial Regex VersionRegex();

	/// Attempts to parse a version number from the given input string.
	/// This method looks for a version pattern within the input string, such as
	/// "KurrentDB version 25.1.0.2299-nightly" or "EventStore version 25.1.0.2299-nightly",
	/// and tries to extract and parse the version information.
	/// <param name="input">The input span containing the version string to parse.</param>
	/// <param name="version">When the method returns, contains the parsed <see cref="Version"/> object, or null if parsing failed.</param>
	/// <returns><c>true</c> if the version could be successfully parsed; otherwise, <c>false</c>.</returns>
	internal static bool TryParseVersion(ReadOnlySpan<char> input, [MaybeNullWhen(false)] out Version version) {
		version = null!;

		if (input.IsEmpty)
			return false;

		try {
			var match = VersionRegex().Match(input.ToString());

			if (!match.Success || match.Groups.Count < 2)
				return false;

			return Version.TryParse(match.Groups[1].Value, out version);
		} catch (Exception ex) {
			throw new Exception($"Failed to parse version from: {input.ToString()}", ex);
		}
	}

	/// <summary>
	/// Using the default 2113 port assumes that the test is running sequentially.
	/// </summary>
	/// <param name="port"></param>
	internal class NetworkPortProvider(int port = 2114) {
		int _port = port;

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
}
