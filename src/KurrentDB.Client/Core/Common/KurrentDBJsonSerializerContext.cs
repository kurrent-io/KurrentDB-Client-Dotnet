using System.Text.Json.Serialization;
using KurrentDB.Diagnostics.Tracing;

namespace KurrentDB.Client;

/// <summary>
/// Source-generated JSON metadata for the types serialized internally by the client.
/// Keeps JSON serialization trimming-safe.
/// </summary>
[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	Converters = [typeof(StreamMetadataJsonConverter), typeof(SystemSettingsJsonConverter)]
)]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(TracingMetadata))]
[JsonSerializable(typeof(StreamMetadata))]
[JsonSerializable(typeof(SystemSettings))]
[JsonSerializable(typeof(PersistentSubscriptionDto))]
[JsonSerializable(typeof(IList<PersistentSubscriptionDto>))]
partial class KurrentDBJsonSerializerContext : JsonSerializerContext;
