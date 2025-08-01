using Kurrent.Client.Model;

namespace Kurrent.Client.Streams;

/// <summary>
/// Represents information about a stream in KurrentDB.
/// </summary>
[PublicAPI]
public record StreamInfo {
    /// <summary>
    /// The metadata associated with the stream.
    /// </summary>
    public StreamMetadata Metadata { get; init; } = StreamMetadata.None;

    /// <summary>
    /// The revision of the metadata stream
    /// </summary>
    public StreamRevision MetadataRevision { get; init; } = StreamRevision.Unset;

    /// <summary>
    /// Indicates whether the stream has been deleted.
    /// </summary>
    public bool IsDeleted { get; init; }

    /// <summary>
    /// Indicates whether the stream has been tombstoned.
    /// </summary>
    public bool IsTombstoned { get; init; }

    /// <summary>
    ///  The last stream revision of the stream.
    /// </summary>
    public StreamRevision LastStreamRevision { get; init; } = StreamRevision.Unset;

    /// <summary>
    /// The last position in the stream.
    /// </summary>
    public LogPosition LastStreamPosition { get; init; } = LogPosition.Unset;

    /// <summary>
    /// The last time the stream was updated.
    /// </summary>
    public DateTimeOffset LastStreamUpdate { get; init; } = DateTimeOffset.MinValue;

    /// <summary>
    /// Indicates whether the stream has metadata associated with it.
    /// </summary>
    public bool HasMetadata => Metadata != StreamMetadata.None;

    public Streams.StreamsClient.StreamState State { get; init; } = Streams.StreamsClient.StreamState.NotFound;
}
