using System.Diagnostics;
using KurrentDB.Protocol.Projections.V1;

namespace Kurrent.Client.Projections;

static class ProjectionsMapper {
    public static ProjectionDetails MapProjectionDetails(this StatisticsResp response, bool includeStatistics) {
        var details = response.Details;

        var mode = response.Details.MapProjectionMode();

        var status = response.Details.MapProjectionStatus();

        var stats = includeStatistics
            ? response.MapProjectionStatistics()
            : ProjectionStatistics.None;

        return new() {
            Name          = details.Name,
            Mode          = mode,
            Version       = details.Version,
            Status        = status,
            StatusReason  = details.StateReason,
            EffectiveName = details.EffectiveName,
            Statistics    = stats,
        };
    }

    public static ProjectionStatistics MapProjectionStatistics(this StatisticsResp response) {
        var details = response.Details;

        return new ProjectionStatistics {
            CoreProcessingTime                  = details.CoreProcessingTime,
            Epoch                               = details.Epoch,
            WritesInProgress                    = details.WritesInProgress,
            ReadsInProgress                     = details.ReadsInProgress,
            PartitionsCached                    = details.PartitionsCached,
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


    public static ProjectionMode MapProjectionMode(this StatisticsResp.Types.Details details) {
        return details.Mode switch {
            "Continuous" => ProjectionMode.Continuous,
            "Transient"  => ProjectionMode.Transient,
            "OneTime"    => ProjectionMode.OneTime,
            _            => throw new UnreachableException($"Unknown or invalid projection mode: {details.Mode}")
        };
    }

    public static ProjectionStatus MapProjectionStatus(this StatisticsResp.Types.Details details) {
        return details.Status switch {
            "Starting"        => ProjectionStatus.Starting,
            "Running"         => ProjectionStatus.Running,
            "Stopping"        => ProjectionStatus.Stopping,
            "Stopped"         => ProjectionStatus.Stopped,
            "Aborted/Stopped" => ProjectionStatus.Stopped,
            "Faulted"         => ProjectionStatus.Faulted,
            "Suspended"       => ProjectionStatus.Suspended,
            _                 => throw new UnreachableException($"Unknown or invalid projection status: {details.Status}")
        };
    }
}
