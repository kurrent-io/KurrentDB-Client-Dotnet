// ReSharper disable InconsistentNaming

using System.Text.RegularExpressions;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using System.Net;
using System.Net.Sockets;
using Ductus.FluentDocker.Extensions;
using Humanizer;
using Ductus.FluentDocker.Model.Builders;
using Ductus.FluentDocker.Services.Extensions;

namespace KurrentDB.Client.Tests.FluentDocker;

public abstract class TestContainerService : TestService<IContainerService, ContainerBuilder> {
	static Version? _version;
	// Define the regex pattern as a static readonly field instead of using GeneratedRegex
	static readonly Regex _versionRegex = new Regex(@"\b(?:KurrentDB|EventStore)\s+version\s+([0-9]+(?:\.[0-9]+)*)", RegexOptions.Compiled);

	public static Version Version => _version ??= GetVersion();

	internal static string ConnectionString => "kurrentdb://admin:changeit@localhost:{port}/?tlsVerifyCert=false";

	protected static ContainerBuilder CreateContainer(
		IDictionary<string, string?> environment, string containerName = "dotnet-client-test", int port = 2113
	) {
		var env = environment.Select(pair => $"{pair.Key}={pair.Value}").ToArray();

		var certsPath = Path.Combine(Environment.CurrentDirectory, "certs");

		CertificatesManager.VerifyCertificatesExist(certsPath);

		return new Builder()
			.UseContainer()
			.UseImage(environment["TESTCONTAINER_KURRENTDB_IMAGE"])
			.WithName(containerName)
			.WithPublicEndpointResolver()
			.WithEnvironment(env)
			.MountVolume(certsPath, "/etc/eventstore/certs", MountType.ReadOnly)
			.ExposePort(port, 2113);
	}

	protected static ContainerBuilder AddReadinessCheck(ContainerBuilder builder) {
		return builder.WaitUntilReadyWithConstantBackoff(
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

	/// Attempts to parse a version number from the given input string.
	/// This method looks for a version pattern within the input string, such as
	/// "KurrentDB version 25.1.0.2299-nightly" or "EventStore version 25.1.0.2299-nightly",
	/// and tries to extract and parse the version information.
	/// <param name="input">The input span containing the version string to parse.</param>
	/// <param name="version">When the method returns, contains the parsed <see cref="Version"/> object, or null if parsing failed.</param>
	/// <returns><c>true</c> if the version could be successfully parsed; otherwise, <c>false</c>.</returns>
	static bool TryParseVersion(ReadOnlySpan<char> input, out Version? version) {
		version = null;

		if (input.IsEmpty) {
			return false;
		}

		try {
			// Use the static regex field instead of calling VersionRegex() method
			var match = _versionRegex.Match(input.ToString());

			if (!match.Success || match.Groups.Count < 2)
				return false;

			return Version.TryParse(match.Groups[1].Value, out version);
		} catch (Exception ex) {
			throw new Exception($"Failed to parse version from: {input.ToString()}", ex);
		}
	}

	// static Version GetVersion() {
	// 	using var cts = new CancellationTokenSource(30.Seconds());
	// 	using var database = new Builder().UseContainer()
	// 		.UseImage(GlobalEnvironment.DockerImage)
	// 		.Command("--version")
	// 		.Build()
	// 		.Start();
	//
	// 	using var log  = database.Logs(true, cts.Token);
	//
	// 	foreach (var line in log.ReadToEnd())
	// 		if (TryParseVersion(line, out var version))
	// 			return version;
	//
	// 	throw new InvalidOperationException("Could not determine server version from logs");
	// }

	static Version GetVersion() {
		const string versionPrefix     = "KurrentDB version";
		const string esdbVersionPrefix = "EventStoreDB version";

		using var cts = new CancellationTokenSource(30.Seconds());
		using var eventstore = new Builder().UseContainer()
			.UseImage(GlobalEnvironment.DockerImage)
			.Command("--version")
			.Build()
			.Start();

		using var log  = eventstore.Logs(true, cts.Token);
		var       logs = log.ReadToEnd();
		foreach (var line in logs) {
			if (line.StartsWith(versionPrefix) &&
			    Version.TryParse(new string(ReadVersion(line[(versionPrefix.Length + 1)..]).ToArray()), out var version)) {
				return version;
			}

			if (line.StartsWith(esdbVersionPrefix) &&
			    Version.TryParse(new string(ReadVersion(line[(esdbVersionPrefix.Length + 1)..]).ToArray()), out var esdbVersion)) {
				return esdbVersion;
			}
		}

		throw new InvalidOperationException($"Could not determine server version from logs: {string.Join(Environment.NewLine, logs)}");

		IEnumerable<char> ReadVersion(string s) {
			foreach (var c in s.TakeWhile(c => c == '.' || char.IsDigit(c))) {
				yield return c;
			}
		}
	}

}
