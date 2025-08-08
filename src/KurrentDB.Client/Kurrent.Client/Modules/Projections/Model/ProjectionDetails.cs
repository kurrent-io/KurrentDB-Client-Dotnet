using KurrentDB.Protocol.Projections.V1;

namespace Kurrent.Client.Projections;

public sealed record ProjectionDetails {
    /// <summary>
    /// Amount of time this projection has spent processing events.
    /// </summary>
    public long CoreProcessingTime { get; init; }

    /// <summary>
    /// Version of the projection. This is incremented when the projection is edited or reset.
    /// </summary>
    public long Version { get; init; }

    /// <summary>
    /// Projection's epoch. This is incremented when the projection is reset.
    /// </summary>
    public long Epoch { get; init; }

    /// <summary>
    /// Effective name of the projection.
    /// </summary>
    public string EffectiveName { get; init; } = string.Empty;

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
    /// Current status of the projection.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Reason for the projection's current status. This will usually be set if the projection has faulted for some reason.
    /// </summary>
    public string StateReason { get; init; } = string.Empty;

    /// <summary>
    /// Name of the projection.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Mode that the projection is running in (Transient, Continuous, or OneTime).
    /// </summary>
    public ProjectionMode Mode { get; init; }

    /// <summary>
    /// Position of the projection. What this position looks like is determined by the type of selector the projection uses.
    /// </summary>
    public string Position { get; init; } = string.Empty;

    /// <summary>
    /// Percent completion for the projection.
    /// </summary>
    public float Progress { get; init; }

    /// <summary>
    /// Last checkpoint written by this projection. Like the Position, its shape is determined by the type of selector the projection uses.
    /// </summary>
    public string LastCheckpoint { get; init; } = string.Empty;

    /// <summary>
    /// Number of events the projection has processed since the last restart of KurrentDB or the projections subsystem.
    /// </summary>
    public long RecordsProcessedAfterRestart { get; init; }

    /// <summary>
    /// Status of the checkpoint writer for the projection.
    /// </summary>
    public string CheckpointStatus { get; init; } = string.Empty;

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

    /// <summary>Represents an empty or uninitialized projection details instance.</summary>
    public static readonly ProjectionDetails None = new() {
        CoreProcessingTime                  = 0,
        Version                             = 0,
        Epoch                               = 0,
        EffectiveName                       = string.Empty,
        WritesInProgress                    = 0,
        ReadsInProgress                     = 0,
        PartitionsCached                    = 0,
        Status                              = string.Empty,
        StateReason                         = string.Empty,
        Name                                = string.Empty,
        Mode                                = ProjectionMode.Unspecified,
        Position                            = string.Empty,
        Progress                            = 0.0f,
        LastCheckpoint                      = string.Empty,
        RecordsProcessedAfterRestart        = 0,
        CheckpointStatus                    = string.Empty,
        BufferedRecords                     = 0,
        WritePendingRecordsBeforeCheckpoint = 0,
        WritePendingRecordsAfterCheckpoint  = 0
    };
}

static class Mapper {
    internal static ProjectionDetails MapToProjectionDetails(this StatisticsResp response) {
        var details = response.Details;

        var mode = response.Details.Mode switch {
            "Continuous" => ProjectionMode.Continuous,
            "Transient"  => ProjectionMode.Transient,
            "OneTime"    => ProjectionMode.OneTime,
            _            => throw new InvalidOperationException($"Unknown projection mode: {response.Details.Mode}")
        };

        return new ProjectionDetails {
            CoreProcessingTime                  = details.CoreProcessingTime,
            Version                             = details.Version,
            Epoch                               = details.Epoch,
            EffectiveName                       = details.EffectiveName,
            WritesInProgress                    = details.WritesInProgress,
            ReadsInProgress                     = details.ReadsInProgress,
            PartitionsCached                    = details.PartitionsCached,
            Status                              = details.Status,
            StateReason                         = details.StateReason,
            Name                                = details.Name,
            Mode                                = mode,
            Position                            = details.Position,
            Progress                            = details.Progress,
            LastCheckpoint                      = details.LastCheckpoint,
            RecordsProcessedAfterRestart        = details.EventsProcessedAfterRestart,
            CheckpointStatus                    = details.CheckpointStatus,
            BufferedRecords                     = details.BufferedEvents,
            WritePendingRecordsBeforeCheckpoint = details.WritePendingEventsBeforeCheckpoint,
            WritePendingRecordsAfterCheckpoint  = details.WritePendingEventsAfterCheckpoint
        };
    }
}
