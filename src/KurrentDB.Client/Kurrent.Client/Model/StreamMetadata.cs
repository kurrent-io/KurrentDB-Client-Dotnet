using System.Text.Json;

namespace Kurrent.Client.Model;

/// <summary>
/// Represents metadata associated with a stream in KurrentDB.
/// </summary>
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

    // /// <summary>
    // /// The optional <see cref="StreamAcl"/> for the stream.
    // /// </summary>
    // public StreamAcl? Acl { get; init; }

    public bool HasMaxAge         => MaxAge.HasValue && MaxAge > TimeSpan.Zero;
    public bool HasTruncateBefore => TruncateBefore is not null;
    public bool HasCacheControl   => CacheControl.HasValue && CacheControl.Value > TimeSpan.Zero;
    public bool HasMaxCount       => MaxCount is > 0;
    public bool HasCustomMetadata => CustomMetadata is not null && CustomMetadata.RootElement.ValueKind != JsonValueKind.Undefined;
}
