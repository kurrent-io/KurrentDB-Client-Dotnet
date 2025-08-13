namespace Kurrent.Client.Projections;

public sealed record ProjectionStatistics {
    public static readonly ProjectionStatistics None = new();

    /// <summary>
    /// Amount of time this projection has spent processing events.
    /// </summary>
    public long CoreProcessingTime { get; init; }

    /// <summary>
    /// Projection's epoch. This is incremented when the projection is reset.
    /// </summary>
    public long Epoch { get; init; } = -1;

    /// <summary>
    /// Number of write operations in progress at the time of collecting the stats.
    /// </summary>
    public int WritesInProgress { get; init; }

    /// <summary>
    /// Number of read operations in progress at the time of collecting the stats.
    /// </summary>
    public int ReadsInProgress { get; init; }

    /// <summary>
    /// Number of partitions that have been cached for this projection.
    /// </summary>
    public int PartitionsCached { get; init; }

    /// <summary>
    /// Position of the projection. What this position looks like is determined by the type of selector the projection uses.
    /// </summary>
    public string Position { get; init; } = "C:0/P:-1";

    /// <summary>
    /// Percent completion for the projection.
    /// </summary>
    public float Progress { get; init; }

    /// <summary>
    /// Last checkpoint written by this projection. Like the Position, its shape is determined by the type of selector the projection uses.
    /// </summary>
    public string LastCheckpoint { get; init; } = "C:0/P:-1";

    /// <summary>
    /// Number of events the projection has processed since the last restart of KurrentDB or the projections subsystem.
    /// </summary>
    public long RecordsProcessedAfterRestart { get; init; }

    /// <summary>
    /// Status of the checkpoint writer for the projection.
    /// </summary>
    public ProjectionCheckpointStatus CheckpointStatus { get; init; } = ProjectionCheckpointStatus.Unspecified;

    /// <summary>
    /// Number of events that have been buffered by the projection.
    /// </summary>
    public long BufferedRecords { get; init; }

    /// <summary>
    /// Number of pending events that must be written before a checkpoint can be written.
    /// </summary>
    public int WritePendingRecordsBeforeCheckpoint { get; init; }

    /// <summary>
    /// Number of pending events that are waiting for a checkpoint to be written before they can be processed.
    /// </summary>
    public int WritePendingRecordsAfterCheckpoint { get; init; }

}
