using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kurrent.Client.Model;

/// <summary>
/// Represents metadata associated with a stream in KurrentDB.
/// </summary>
[JsonConverter(typeof(StreamMetadataJsonConverter))]
public record StreamMetadata {
    public static readonly StreamMetadata None = new();

    /// <summary>
    /// The optional maximum age of events allowed in the stream.
    /// </summary>
    public TimeSpan? MaxAge { get; init; }

    /// <summary>
    /// The optional <see cref="StreamRevision"/> from which previous events can be scavenged.
    /// This is used to implement soft-deletion of streams.
    /// </summary>
    public StreamRevision? TruncateBefore { get; init; }

    /// <summary>
    /// The optional amount of time for which the stream head is cacheable.
    /// </summary>
    public TimeSpan? CacheControl { get; init; }

    /// <summary>
    /// The optional maximum number of events allowed in the stream.
    /// </summary>
    public int? MaxCount { get; init; }

    /// <summary>
    /// The optional <see cref="JsonDocument"/> of user provided metadata.
    /// </summary>
    public JsonDocument? CustomMetadata { get; init; }

    /// <summary>
    /// The optional <see cref="StreamAcl"/> for the stream.
    /// </summary>
    public StreamAcl? Acl { get; init; }

    public bool HasMaxAge         => MaxAge.HasValue && MaxAge > TimeSpan.Zero;
    public bool HasTruncateBefore => TruncateBefore is not null;
    public bool HasCacheControl   => CacheControl.HasValue && CacheControl.Value > TimeSpan.Zero;
    public bool HasMaxCount       => MaxCount is > 0;
    public bool HasCustomMetadata => CustomMetadata is not null && CustomMetadata.RootElement.ValueKind != JsonValueKind.Undefined;
    public bool HasAcl            => Acl is not null && Acl != StreamAcl.None;
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
    public string[] ReadRoles { get; init; } = [];

    /// <summary>
    /// Roles and users permitted to write to the stream
    /// </summary>
    public string[] WriteRoles { get; init; } = [];

    /// <summary>
    /// Roles and users permitted to delete the stream
    /// </summary>
    public string[] DeleteRoles { get; init; } = [];

    /// <summary>
    /// Roles and users permitted to read stream metadata
    /// </summary>
    public string[] MetaReadRoles { get; init; } = [];

    /// <summary>
    /// Roles and users permitted to write stream metadata
    /// </summary>
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

class StreamMetadataJsonConverter : JsonConverter<StreamMetadata> {
    const string MaxAgeKey         = "$maxAge";
    const string MaxCountKey       = "$maxCount";
    const string TruncateBeforeKey = "$tb";
    const string CacheControlKey   = "$cacheControl";
    const string AclKey            = "$acl";

    public static readonly StreamMetadataJsonConverter Instance = new();

    public override StreamMetadata Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        int?            maxCount       = null;
        TimeSpan?       maxAge         = null;
        TimeSpan?       cacheControl = null;
        StreamRevision? truncateBefore = null;
        StreamAcl?      acl            = null;

        using var stream               = new MemoryStream();
        using var customMetadataWriter = new Utf8JsonWriter(stream);

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new InvalidOperationException();

        customMetadataWriter.WriteStartObject();

        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) throw new InvalidOperationException();

            switch (reader.GetString()) {
                case MaxCountKey:
                    if (!reader.Read()) throw new InvalidOperationException();
                    maxCount = reader.GetInt32();
                    break;

                case MaxAgeKey:
                    if (!reader.Read()) throw new InvalidOperationException();
                    var int64 = reader.GetInt64();
                    maxAge = TimeSpan.FromSeconds(int64);
                    break;

                case CacheControlKey:
                    if (!reader.Read()) throw new InvalidOperationException();
                    cacheControl = TimeSpan.FromSeconds(reader.GetInt64());
                    break;

                case TruncateBeforeKey:
                    if (!reader.Read()) throw new InvalidOperationException();
                    var value = reader.GetInt64();
                    truncateBefore = value == long.MaxValue
                        ? StreamRevision.Max
                        : StreamRevision.From(value);

                    break;

                case AclKey:
                    if (!reader.Read()) throw new InvalidOperationException();
                    acl = StreamAclJsonConverter.Instance.Read(ref reader, typeof(StreamAcl), options);
                    break;

                default:
                    customMetadataWriter.WritePropertyName(reader.GetString()!);
                    reader.Read();
                    switch (reader.TokenType) {
                        case JsonTokenType.Comment:
                            customMetadataWriter.WriteCommentValue(reader.GetComment());
                            break;

                        case JsonTokenType.String:
                            customMetadataWriter.WriteStringValue(reader.GetString());
                            break;

                        case JsonTokenType.Number:
                            customMetadataWriter.WriteNumberValue(reader.GetDouble());
                            break;

                        case JsonTokenType.True:
                        case JsonTokenType.False:
                            customMetadataWriter.WriteBooleanValue(reader.GetBoolean());
                            break;

                        case JsonTokenType.Null:
                            reader.Read();
                            customMetadataWriter.WriteNullValue();
                            break;

                        default:
                            throw new JsonException(
                                $"Unexpected token type {reader.TokenType} for property '{reader.GetString()}' in stream metadata.");
                    }

                    break;
            }
        }

        customMetadataWriter.WriteEndObject();
        customMetadataWriter.Flush();

        stream.Position = 0;

        return new StreamMetadata {
            MaxCount       = maxCount,
            MaxAge         = maxAge,
            TruncateBefore = truncateBefore,
            CacheControl   = cacheControl,
            Acl            = acl,
            CustomMetadata = JsonDocument.Parse(stream)
        };
    }

    public override void Write(Utf8JsonWriter writer, StreamMetadata value, JsonSerializerOptions options) {
        writer.WriteStartObject();

        if (value.MaxCount.HasValue)
            writer.WriteNumber(MaxCountKey, value.MaxCount.Value);

        if (value.MaxAge.HasValue)
            writer.WriteNumber(MaxAgeKey, (long)value.MaxAge.Value.TotalSeconds);

        if (value.TruncateBefore != null && value.TruncateBefore != StreamRevision.Unset)
            writer.WriteNumber(TruncateBeforeKey, value.TruncateBefore);

        if (value.CacheControl.HasValue)
            writer.WriteNumber(CacheControlKey, (long)value.CacheControl.Value.TotalSeconds);

        if (value.Acl is not null) {
            writer.WritePropertyName(AclKey);
            StreamAclJsonConverter.Instance.Write(writer, value.Acl.Value, options);
        }

        if (value.CustomMetadata is not null)
            foreach (var property in value.CustomMetadata.RootElement.EnumerateObject())
                property.WriteTo(writer);

        writer.WriteEndObject();
    }
}

class StreamAclJsonConverter : JsonConverter<StreamAcl> {
    const string AclRead      = "$r";
    const string AclWrite     = "$w";
    const string AclDelete    = "$d";
    const string AclMetaRead  = "$mr";
    const string AclMetaWrite = "$mw";

    public static readonly StreamAclJsonConverter Instance = new();

    public override StreamAcl Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        string[]? read      = null;
        string[]? write     = null;
        string[]? delete    = null;
        string[]? metaRead  = null;
        string[]? metaWrite = null;

        if (reader.TokenType != JsonTokenType.StartObject) throw new InvalidOperationException();

        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new InvalidOperationException();

            switch (reader.GetString()) {
                case AclRead:
                    read = ReadRoles(ref reader);
                    break;

                case AclWrite:
                    write = ReadRoles(ref reader);
                    break;

                case AclDelete:
                    delete = ReadRoles(ref reader);
                    break;

                case AclMetaRead:
                    metaRead = ReadRoles(ref reader);
                    break;

                case AclMetaWrite:
                    metaWrite = ReadRoles(ref reader);
                    break;
            }
        }

        return new StreamAcl {
            ReadRoles      = read ?? [],
            WriteRoles     = write ?? [],
            DeleteRoles    = delete ?? [],
            MetaReadRoles  = metaRead ?? [],
            MetaWriteRoles = metaWrite ?? []
        };
    }

    static string[]? ReadRoles(ref Utf8JsonReader reader) {
        if (!reader.Read())
            throw new InvalidOperationException();

        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
            return [reader.GetString()!];

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new InvalidOperationException();

        var roles = new List<string>();

        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndArray)
                return roles.Count == 0 ? [] : roles.ToArray();

            if (reader.TokenType != JsonTokenType.String)
                throw new InvalidOperationException();

            roles.Add(reader.GetString()!);
        }

        return roles.ToArray();
    }

    public override void Write(Utf8JsonWriter writer, StreamAcl value, JsonSerializerOptions options) {
        writer.WriteStartObject();

        WriteRoles(writer, AclRead, value.ReadRoles);
        WriteRoles(writer, AclWrite, value.WriteRoles);
        WriteRoles(writer, AclDelete, value.DeleteRoles);
        WriteRoles(writer, AclMetaRead, value.MetaReadRoles);
        WriteRoles(writer, AclMetaWrite, value.MetaWriteRoles);

        writer.WriteEndObject();
    }

    static void WriteRoles(Utf8JsonWriter writer, string name, string[]? roles) {
        if (roles is null)
            return;

        writer.WritePropertyName(name);
        writer.WriteStartArray();

        foreach (var role in roles)
            writer.WriteStringValue(role);

        writer.WriteEndArray();
    }
}
