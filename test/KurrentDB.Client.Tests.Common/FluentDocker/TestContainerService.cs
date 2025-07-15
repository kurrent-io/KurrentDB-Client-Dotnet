// ReSharper disable InconsistentNaming

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;

namespace KurrentDB.Client.Tests.FluentDocker;

public abstract partial class TestContainerService : TestService<IContainerService, ContainerBuilder> {
	internal static Version? _version;

	public static Version Version => _version ?? throw new InvalidOperationException("Version has not been initialized.");

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

	[GeneratedRegex(@"\b(?:KurrentDB|EventStore)\s+version\s+([0-9]+(?:\.[0-9]+)*)")]
	private static partial Regex VersionRegex();
}
