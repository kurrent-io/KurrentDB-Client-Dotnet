namespace Kurrent.Client.Streams;

/// <summary>
/// Represents information about a stream in KurrentDB.
/// </summary>
[PublicAPI]
public record StreamInfo {
    public static readonly StreamInfo None = new();

    /// <summary>
    /// The metadata associated with the stream.
    /// </summary>
    public StreamMetadata Metadata { get; init; } = StreamMetadata.None;

    /// <summary>
    /// The revision of the metadata stream
    /// </summary>
    public StreamRevision MetadataRevision { get; init; } = StreamRevision.Unset;

    /// <summary>
    ///  The last stream revision of the stream.
    /// </summary>
    public StreamRevision LastStreamRevision { get; init; } = StreamRevision.Unset;

    /// <summary>
    /// The last position in the stream.
    /// </summary>
    public LogPosition LastStreamPosition { get; init; } = LogPosition.Unset;

    /// <summary>
    /// The last time the stream was appended.
    /// </summary>
    public DateTime LastStreamAppendTime { get; init; } = DateTime.MinValue;

    /// <summary>
    /// Indicates whether the stream has metadata associated with it.
    /// </summary>
    public bool HasMetadata => Metadata != StreamMetadata.None;

    /// <summary>
    /// The state of the stream, indicating whether it is active, deleted, tombstoned, or missing.
    /// </summary>
    public StreamState State { get; init; } = StreamState.Missing;
}
