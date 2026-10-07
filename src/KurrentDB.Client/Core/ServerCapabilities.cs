using System.Text.RegularExpressions;

namespace KurrentDB.Client {
#pragma warning disable 1591
	public partial record ServerCapabilities(
		bool SupportsBatchAppend = false,
		bool SupportsPersistentSubscriptionsToAll = false,
		bool SupportsPersistentSubscriptionsGetInfo = false,
		bool SupportsPersistentSubscriptionsRestartSubsystem = false,
		bool SupportsPersistentSubscriptionsReplayParked = false,
		bool SupportsMultiStreamAppend = false,
		bool SupportsPersistentSubscriptionsList = false,
		bool SupportsAppendRecords = false,
		bool SupportsSubscriptionFellBehind = false) {
		// first server version that sends FellBehind
		static readonly Version SubscriptionFellBehindVersion = new(26, 2, 2);

		// major.minor.patch, optionally followed by a pre-release suffix. anything else is unsupported.
		const string ServerVersionPattern = @"^([0-9]+)\.([0-9]+)\.([0-9]+)";

#if NET8_0_OR_GREATER
		[GeneratedRegex(ServerVersionPattern, RegexOptions.CultureInvariant)]
		private static partial Regex ServerVersion();
#else
		static readonly Regex ServerVersionRegex = new(ServerVersionPattern, RegexOptions.CultureInvariant | RegexOptions.Compiled);

		static Regex ServerVersion() => ServerVersionRegex;
#endif

		internal static bool VersionSupportsSubscriptionFellBehind(string? serverVersion) =>
			ServerVersion().Match(serverVersion ?? "") is { Success: true } match
			&& int.TryParse(match.Groups[1].Value, out var major)
			&& int.TryParse(match.Groups[2].Value, out var minor)
			&& int.TryParse(match.Groups[3].Value, out var patch)
			&& new Version(major, minor, patch) >= SubscriptionFellBehindVersion;
	}
}
