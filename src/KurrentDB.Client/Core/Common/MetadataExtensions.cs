using System.Text.Json;
using Grpc.Core;

namespace KurrentDB.Client;

static class MetadataExtensions {
	/// <summary>
	/// Encodes a dictionary of string key-value pairs as JSON metadata bytes.
	/// </summary>
	/// <param name="metadata">The dictionary to encode.</param>
	/// <returns>UTF-8 encoded JSON bytes representing the metadata.</returns>
	public static ReadOnlyMemory<byte> Encode(this Dictionary<string, string> metadata) =>
		JsonSerializer.SerializeToUtf8Bytes(metadata);

	/// <summary>
	/// Encodes an anonymous object as JSON metadata bytes.
	/// </summary>
	/// <param name="metadata">The object to encode.</param>
	/// <returns>UTF-8 encoded JSON bytes representing the metadata.</returns>
	public static ReadOnlyMemory<byte> Encode(this object metadata) =>
		JsonSerializer.SerializeToUtf8Bytes(metadata);

	/// <summary>
	/// Decodes JSON metadata bytes as a dictionary of string key-value pairs.
	/// </summary>
	/// <param name="metadata">The metadata bytes to decode.</param>
	/// <returns>A dictionary of string key-value pairs, or <c>null</c> if the metadata is empty.</returns>
	public static Dictionary<string, string>? Decode(this ReadOnlyMemory<byte> metadata) {
		if (metadata.IsEmpty)
			return null;

		return JsonSerializer.Deserialize<Dictionary<string, string>>(metadata.Span);
	}

	/// <summary>
	/// Decodes the metadata from an event record as a dictionary of string key-value pairs.
	/// </summary>
	/// <param name="eventRecord">The event record containing metadata to decode.</param>
	/// <returns>A dictionary of string key-value pairs, or <c>null</c> if the metadata is empty.</returns>
	public static Dictionary<string, string>? Decode(this EventRecord eventRecord) =>
		eventRecord.Metadata.Decode();

	/// <summary>
	/// Decodes the metadata from a resolved event as a dictionary of string key-value pairs.
	/// </summary>
	/// <param name="resolvedEvent">The resolved event containing metadata to decode.</param>
	/// <returns>A dictionary of string key-value pairs, or <c>null</c> if the metadata is empty.</returns>
	public static Dictionary<string, string>? Decode(this ResolvedEvent resolvedEvent) =>
		resolvedEvent.OriginalEvent.Metadata.Decode();

	/// <summary>
	/// Attempts to retrieve a value from the gRPC metadata by key.
	/// </summary>
	/// <param name="metadata">The gRPC metadata collection.</param>
	/// <param name="key">The key to search for.</param>
	/// <param name="value">When this method returns, contains the value associated with the key, if found; otherwise, <c>null</c>.</param>
	/// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
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

	/// <summary>
	/// Retrieves a stream state value from the gRPC metadata by key.
	/// </summary>
	/// <param name="metadata">The gRPC metadata collection.</param>
	/// <param name="key">The key to search for.</param>
	/// <returns>The parsed stream state value if found and valid; otherwise, <see cref="StreamState.NoStream"/>.</returns>
	public static StreamState GetStreamState(this Metadata metadata, string key) =>
		metadata.TryGetValue(key, out var s) && ulong.TryParse(s, out var value)
			? value
			: StreamState.NoStream;

	/// <summary>
	/// Retrieves an integer value from the gRPC metadata by key.
	/// </summary>
	/// <param name="metadata">The gRPC metadata collection.</param>
	/// <param name="key">The key to search for.</param>
	/// <returns>The parsed integer value if found and valid; otherwise, <c>0</c>.</returns>
	public static int GetIntValueOrDefault(this Metadata metadata, string key) =>
		metadata.TryGetValue(key, out var s) && int.TryParse(s, out var value)
			? value
			: default;
}
