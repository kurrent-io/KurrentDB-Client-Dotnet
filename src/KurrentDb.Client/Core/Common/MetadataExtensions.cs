using Grpc.Core;

namespace KurrentDB.Client;

static class MetadataExtensions {
	public static bool TryGetValue(this Metadata metadata, string key, out string? value) {
		value = default;

		foreach (var entry in metadata) {
			if (entry.Key != key)
				continue;

			value = entry.Value;
			return true;
		}

		return false;
	}

	public static StreamState GetStreamState(this Metadata metadata, string key) =>
		metadata.TryGetValue(key, out var s) && ulong.TryParse(s, out var value)
			? StreamState.StreamRevision(value)
			: StreamState.NoStream;

	public static int GetIntValueOrDefault(this Metadata metadata, string key) =>
		metadata.TryGetValue(key, out var s) && int.TryParse(s, out var value)
			? value
			: default;
}
