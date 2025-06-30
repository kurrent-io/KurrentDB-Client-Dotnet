using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kurrent.Client.Model;

/// <summary>
/// Represents metadata associated with a stream in KurrentDB.
/// </summary>
public class StreamMetadata {
    public static readonly StreamMetadata None = new();

    /// <summary>
    /// The optional maximum age of events allowed in the stream.
    /// </summary>
    [JsonPropertyName("$maxAge")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(TimeSpanSecondsConverter))]
    public TimeSpan? MaxAge { get; set; }

    /// <summary>
    /// The optional <see cref="StreamRevision"/> from which previous events can be scavenged.
    /// This is used to implement soft-deletion of streams.
    /// </summary>
    [JsonPropertyName("$tb")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(StreamRevisionConverter))]
    public StreamRevision? TruncateBefore { get; set; }

    /// <summary>
    /// The optional amount of time for which the stream head is cacheable.
    /// </summary>
    [JsonPropertyName("$cacheControl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(TimeSpanSecondsConverter))]
    public TimeSpan? CacheControl { get; set; }

    /// <summary>
    /// The optional maximum number of events allowed in the stream.
    /// </summary>
    [JsonPropertyName("$maxCount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxCount { get; set; }

    /// <summary>
    /// The optional <see cref="StreamAcl"/> for the stream.
    /// </summary>
    [JsonPropertyName("$acl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StreamAcl? Acl { get; set; }

    /// <summary>
    /// Custom metadata properties that are serialized as additional JSON properties.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?> CustomProperties { get; set; } = new();

    /// <summary>
    /// The optional <see cref="JsonDocument"/> of user provided metadata.
    /// This represents all custom properties that are not system metadata.
    /// For backward compatibility - prefer using CustomProperties for new code.
    /// </summary>
    [JsonIgnore]
    public JsonDocument? CustomMetadata {
        get {
            if (CustomProperties.Count == 0)
                return null;

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            writer.WriteStartObject();
            foreach (var kvp in CustomProperties) {
                writer.WritePropertyName(kvp.Key);
                JsonSerializer.Serialize(writer, kvp.Value);
            }

            writer.WriteEndObject();
            writer.Flush();

            stream.Position = 0;
            return JsonDocument.Parse(stream);
        }
        set {
            CustomProperties.Clear();
            if (value is null) return;

            foreach (var property in value.RootElement.EnumerateObject()) {
                // Deserialize JsonElement to object
                object? deserializedValue = property.Value.ValueKind switch {
                    JsonValueKind.Null                        => null,
                    JsonValueKind.String                      => property.Value.GetString(),
                    JsonValueKind.Number                      => property.Value.GetDouble(),
                    JsonValueKind.True or JsonValueKind.False => property.Value.GetBoolean(),
                    _                                         => property.Value // Keep as JsonElement for complex types
                };

                CustomProperties[property.Name] = deserializedValue;
            }
        }
    }

    [JsonIgnore] public bool HasMaxAge => MaxAge.HasValue && MaxAge > TimeSpan.Zero;

    [JsonIgnore] public bool HasTruncateBefore => TruncateBefore is not null;

    [JsonIgnore] public bool HasCacheControl => CacheControl.HasValue && CacheControl.Value > TimeSpan.Zero;

    [JsonIgnore] public bool HasMaxCount => MaxCount is > 0;

    [JsonIgnore] public bool HasCustomMetadata => CustomProperties.Count > 0;

    [JsonIgnore] public bool HasAcl => Acl is not null && Acl != StreamAcl.None;

    /// <summary>
    /// Gets a custom metadata property value.
    /// </summary>
    /// <typeparam name="T">The type to cast the value to</typeparam>
    /// <param name="key">The property key</param>
    /// <returns>The property value cast to T, or default if not found or not convertible</returns>
    public T? GetCustomProperty<T>(string key) {
        if (!CustomProperties.TryGetValue(key, out var value)) return default;

        // Handle JsonElement values from deserialization
        if (value is JsonElement element) return element.Deserialize<T>();

        return value is T typed ? typed : default;
    }

    /// <summary>
    /// Creates a new StreamMetadata instance with an additional custom property.
    /// </summary>
    /// <param name="key">The property key</param>
    /// <param name="value">The property value</param>
    /// <returns>A new StreamMetadata instance with the custom property added</returns>
    public StreamMetadata WithCustomProperty(string key, object? value) {
        var newProperties = new Dictionary<string, object?>(CustomProperties);
        if (value is null)
            newProperties.Remove(key);
        else
            newProperties[key] = value;

        return new StreamMetadata {
            MaxAge           = MaxAge,
            TruncateBefore   = TruncateBefore,
            CacheControl     = CacheControl,
            MaxCount         = MaxCount,
            Acl              = Acl,
            CustomProperties = newProperties
        };
    }

    /// <summary>
    /// Creates a new StreamMetadata instance with multiple custom properties added.
    /// </summary>
    /// <param name="customProperties">Dictionary of custom properties to add</param>
    /// <returns>A new StreamMetadata instance with the custom properties added</returns>
    public StreamMetadata WithCustomProperties(IReadOnlyDictionary<string, object?> customProperties) {
        var newProperties = new Dictionary<string, object?>(CustomProperties);

        foreach (var kvp in customProperties)
            if (kvp.Value is null)
                newProperties.Remove(kvp.Key);
            else
                newProperties[kvp.Key] = kvp.Value;

        return new StreamMetadata {
            MaxAge           = MaxAge,
            TruncateBefore   = TruncateBefore,
            CacheControl     = CacheControl,
            MaxCount         = MaxCount,
            Acl              = Acl,
            CustomProperties = newProperties
        };
    }
}

/// <summary>
/// Represents an access control list for a stream
/// </summary>
[PublicAPI]
public readonly record struct StreamAcl() {
    public static readonly StreamAcl None = new();

    /// <summary>
    /// Roles and users permitted to read the stream
    /// </summary>
    [JsonPropertyName("$r")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string[] ReadRoles { get; init; } = [];

    /// <summary>
    /// Roles and users permitted to write to the stream
    /// </summary>
    [JsonPropertyName("$w")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string[] WriteRoles { get; init; } = [];

    /// <summary>
    /// Roles and users permitted to delete the stream
    /// </summary>
    [JsonPropertyName("$d")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string[] DeleteRoles { get; init; } = [];

    /// <summary>
    /// Roles and users permitted to read stream metadata
    /// </summary>
    [JsonPropertyName("$mr")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string[] MetaReadRoles { get; init; } = [];

    /// <summary>
    /// Roles and users permitted to write stream metadata
    /// </summary>
    [JsonPropertyName("$mw")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string[] MetaWriteRoles { get; init; } = [];

    public bool Equals(StreamAcl other) =>
        ReadRoles.SequenceEqual(other.ReadRoles) &&
        WriteRoles.SequenceEqual(other.WriteRoles) &&
        DeleteRoles.SequenceEqual(other.DeleteRoles) &&
        MetaReadRoles.SequenceEqual(other.MetaReadRoles) &&
        MetaWriteRoles.SequenceEqual(other.MetaWriteRoles);

    public override int GetHashCode() {
        var hash = new HashCode();
        foreach (var role in ReadRoles) hash.Add(role);
        foreach (var role in WriteRoles) hash.Add(role);
        foreach (var role in DeleteRoles) hash.Add(role);
        foreach (var role in MetaReadRoles) hash.Add(role);
        foreach (var role in MetaWriteRoles) hash.Add(role);
        return hash.ToHashCode();
    }

    public override string ToString() =>
        $"Read: [{string.Join(",", ReadRoles)}], "
      + $"Write: [{string.Join(",", WriteRoles)}], "
      + $"Delete: [{string.Join(",", DeleteRoles)}], "
      + $"MetaRead: [{string.Join(",", MetaReadRoles)}], "
      + $"MetaWrite: [{string.Join(",", MetaWriteRoles)}]";
}

/// <summary>
/// Converts TimeSpan to/from seconds for JSON serialization
/// </summary>
public class TimeSpanSecondsConverter : JsonConverter<TimeSpan?> {
    public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Number
            ? TimeSpan.FromSeconds(reader.GetInt64())
            : null;

    public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options) {
        if (value.HasValue)
            writer.WriteNumberValue((long)value.Value.TotalSeconds);
        else
            writer.WriteNullValue();
    }
}

/// <summary>
/// Converts StreamRevision to/from long for JSON serialization
/// </summary>
public class StreamRevisionConverter : JsonConverter<StreamRevision?> {
    public override StreamRevision? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType != JsonTokenType.Number) return null;

        var value = reader.GetInt64();
        return value == long.MaxValue ? StreamRevision.Max : StreamRevision.From(value);
    }

    public override void Write(Utf8JsonWriter writer, StreamRevision? value, JsonSerializerOptions options) {
        if (value is not null)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}
