using KurrentDB.Protocol.Projections.V1;

namespace Kurrent.Client.Projections;

static class ProjectionsMapper {
    // internal static ProjectionStatistics MapToProjectionStatistics(this StatisticsResp response) {
    //     var details = response.Details;
    //
    //     var mode = response.Details.Mode switch {
    //         "Continuous" => ProjectionMode.Continuous,
    //         "Transient"  => ProjectionMode.Transient,
    //         "OneTime"    => ProjectionMode.OneTime,
    //         _            => throw new InvalidOperationException($"Unknown projection mode: {response.Details.Mode}")
    //     };
    //
    //     return new ProjectionStatistics {
    //         CoreProcessingTime                  = details.CoreProcessingTime,
    //         Version                             = details.Version,
    //         Epoch                               = details.Epoch,
    //         EffectiveName                       = details.EffectiveName,
    //         WritesInProgress                    = details.WritesInProgress,
    //         ReadsInProgress                     = details.ReadsInProgress,
    //         PartitionsCached                    = details.PartitionsCached,
    //         Status                              = details.Status,
    //         StateReason                         = details.StateReason,
    //         Name                                = details.Name,
    //         Mode                                = mode,
    //         Position                            = details.Position,
    //         Progress                            = details.Progress,
    //         LastCheckpoint                      = details.LastCheckpoint,
    //         RecordsProcessedAfterRestart        = details.EventsProcessedAfterRestart,
    //         CheckpointStatus                    = details.CheckpointStatus,
    //         BufferedRecords                     = details.BufferedEvents,
    //         WritePendingRecordsBeforeCheckpoint = details.WritePendingEventsBeforeCheckpoint,
    //         WritePendingRecordsAfterCheckpoint  = details.WritePendingEventsAfterCheckpoint
    //     };
    // }

    internal static ProjectionStatistics MapToProjectionStatistics(this StatisticsResp response) {
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

    internal static ProjectionDetails MapToProjectionDetails(this StatisticsResp response) {
        var details = response.Details;

        var mode = response.Details.Mode switch {
            "Continuous" => ProjectionMode.Continuous,
            "Transient"  => ProjectionMode.Transient,
            "OneTime"    => ProjectionMode.OneTime,
            _            => throw new InvalidOperationException($"Unknown or invalid projection mode: {response.Details.Mode}")
        };

        var status = response.Details.Status switch {
            "Starting"   => ProjectionStatus.Starting,
            "Running"    => ProjectionStatus.Running,
            "Stopping"   => ProjectionStatus.Stopping,
            "Stopped"    => ProjectionStatus.Stopped,
            "Faulted"    => ProjectionStatus.Faulted,
            "Suspended"  => ProjectionStatus.Suspended,
            _            => throw new InvalidOperationException($"Unknown or invalid projection status: {response.Details.Status}")
        };

        return new ProjectionDetails {
            Name          = details.Name,
            Mode          = mode,
            Version       = details.Version,
            Status        = status,
            StatusReason  = details.StateReason,
            EffectiveName = details.EffectiveName,
            Statistics    = response.MapToProjectionStatistics(),
        };
    }
}
