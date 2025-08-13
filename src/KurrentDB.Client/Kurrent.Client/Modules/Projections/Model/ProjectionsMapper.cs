using System.Diagnostics;

namespace Kurrent.Client.Projections;

static class ProjectionsHttpMapper {
    public static ProjectionDetails MapToProjectionDetails(this ProjectionsHttpModel.Projection source, bool includeStatistics) {
        var mode   = MapProjectionMode(source);
        var status = MapProjectionStatus(source);

        var stats = includeStatistics
            ? MapToProjectionStatistics(source)
            : ProjectionStatistics.None;

        return new() {
            Name          = source.Name,
            Mode          = mode,
            Version       = source.Version,
            Status        = status,
            FaultReason  = source.StateReason,
            EffectiveName = source.EffectiveName,
            Statistics    = stats,
        };
    }

    static ProjectionStatistics MapToProjectionStatistics(this ProjectionsHttpModel.Projection source) {
        return new ProjectionStatistics {
            CoreProcessingTime                  = source.CoreProcessingTime,
            Epoch                               = source.Epoch,
            WritesInProgress                    = source.WritesInProgress,
            ReadsInProgress                     = source.ReadsInProgress,
            PartitionsCached                    = source.PartitionsCached,
            Position                            = source.Position,
            Progress                            = source.Progress,
            LastCheckpoint                      = source.LastCheckpoint,
            RecordsProcessedAfterRestart        = source.EventsProcessedAfterRestart,
            BufferedRecords                     = source.BufferedEvents,
            CheckpointStatus                    = source.MapProjectionCheckpointStatus(),
            WritePendingRecordsBeforeCheckpoint = source.WritePendingEventsBeforeCheckpoint,
            WritePendingRecordsAfterCheckpoint  = source.WritePendingEventsAfterCheckpoint
        };
    }

    static ProjectionCheckpointStatus MapProjectionCheckpointStatus(this ProjectionsHttpModel.Projection source) {
        return source.CheckpointStatus switch {
            null or ""  => ProjectionCheckpointStatus.Unspecified,
            "Requested" => ProjectionCheckpointStatus.Requested,
            "Writting"  => ProjectionCheckpointStatus.Writting,
            _           => throw new UnreachableException($"Unknown or invalid projection checkpoint status: {source.CheckpointStatus}")
        };
    }

    static ProjectionMode MapProjectionMode(this ProjectionsHttpModel.Projection source) {
        return source.Mode switch {
            "Continuous" => ProjectionMode.Continuous,
            "Transient"  => ProjectionMode.Transient,
            "OneTime"    => ProjectionMode.OneTime,
            _            => throw new UnreachableException($"Unknown or invalid projection mode: {source.Mode}")
        };
    }

    static ProjectionStatus MapProjectionStatus(this ProjectionsHttpModel.Projection source) {
        // shame... shame... shame...
        return source.Status switch {
            // special cases
            "Stopped (Enabled)" => ProjectionStatus.Stopped,
            "Faulted (Enabled)" => ProjectionStatus.Faulted,

            // catch-all for statuses that start with a specific prefix
            _ when source.Status.StartsWith("Creating")       => ProjectionStatus.Creating,
            _ when source.Status.StartsWith("Loading")        => ProjectionStatus.Loading,
            _ when source.Status.StartsWith("Loaded")         => ProjectionStatus.Loaded,
            _ when source.Status.StartsWith("Preparing")      => ProjectionStatus.Preparing,
            _ when source.Status.StartsWith("Prepared")       => ProjectionStatus.Prepared,
            _ when source.Status.StartsWith("Starting")       => ProjectionStatus.Starting,
            _ when source.Status.StartsWith("LoadingStopped") => ProjectionStatus.LoadingStopped,
            _ when source.Status.StartsWith("Running")        => ProjectionStatus.Running,
            _ when source.Status.StartsWith("Stopping")       => ProjectionStatus.Stopping,
            _ when source.Status.StartsWith("Aborting")       => ProjectionStatus.Aborting,
            _ when source.Status.StartsWith("Stopped")        => ProjectionStatus.Stopped,
            _ when source.Status.StartsWith("Completed")      => ProjectionStatus.Completed,
            _ when source.Status.StartsWith("Aborted")        => ProjectionStatus.Aborted,
            _ when source.Status.StartsWith("Faulted")        => ProjectionStatus.Faulted,
            _ when source.Status.StartsWith("Deleting")       => ProjectionStatus.Deleting,

            // // regular cases
            // "Creating"       => ProjectionStatus.Creating,
            // "Loading"        => ProjectionStatus.Loading,
            // "Loaded"         => ProjectionStatus.Loaded,
            // "Preparing"      => ProjectionStatus.Preparing,
            // "Prepared"       => ProjectionStatus.Prepared,
            // "Starting"       => ProjectionStatus.Starting,
            // "LoadingStopped" => ProjectionStatus.LoadingStopped,
            // "Running"        => ProjectionStatus.Running,
            // "Stopping"       => ProjectionStatus.Stopping,
            // "Aborting"       => ProjectionStatus.Aborting,
            // "Stopped"        => ProjectionStatus.Stopped,
            // "Completed"      => ProjectionStatus.Completed,
            // "Aborted"        => ProjectionStatus.Aborted,
            // "Faulted"        => ProjectionStatus.Faulted,
            // "Deleting"       => ProjectionStatus.Deleting,

            // // catch-all for statuses that end with a specific suffix
            // _ when source.Status.EndsWith("/Creating")       => ProjectionStatus.Creating,
            // _ when source.Status.EndsWith("/Loading")        => ProjectionStatus.Loading,
            // _ when source.Status.EndsWith("/Loaded")         => ProjectionStatus.Loaded,
            // _ when source.Status.EndsWith("/Preparing")      => ProjectionStatus.Preparing,
            // _ when source.Status.EndsWith("/Prepared")       => ProjectionStatus.Prepared,
            // _ when source.Status.EndsWith("/Starting")       => ProjectionStatus.Starting,
            // _ when source.Status.EndsWith("/LoadingStopped") => ProjectionStatus.LoadingStopped,
            // _ when source.Status.EndsWith("/Running")        => ProjectionStatus.Running,
            // _ when source.Status.EndsWith("/Stopping")       => ProjectionStatus.Stopping,
            // _ when source.Status.EndsWith("/Aborting")       => ProjectionStatus.Aborting,
            // _ when source.Status.EndsWith("/Stopped")        => ProjectionStatus.Stopped,
            // _ when source.Status.EndsWith("/Completed")      => ProjectionStatus.Completed,
            // _ when source.Status.EndsWith("/Aborted")        => ProjectionStatus.Aborted,
            // _ when source.Status.EndsWith("/Faulted")        => ProjectionStatus.Faulted,
            // _ when source.Status.EndsWith("/Deleting")       => ProjectionStatus.Deleting,

            // // catch-all for statuses that end with a specific prefix
            // _ when source.Status.StartsWith("Creating/")       => ProjectionStatus.Creating,
            // _ when source.Status.StartsWith("Loading/")        => ProjectionStatus.Loading,
            // _ when source.Status.StartsWith("Loaded/")         => ProjectionStatus.Loaded,
            // _ when source.Status.StartsWith("Preparing/")      => ProjectionStatus.Preparing,
            // _ when source.Status.StartsWith("Prepared/")       => ProjectionStatus.Prepared,
            // _ when source.Status.StartsWith("Starting/")       => ProjectionStatus.Starting,
            // _ when source.Status.StartsWith("LoadingStopped/") => ProjectionStatus.LoadingStopped,
            // _ when source.Status.StartsWith("Running/")        => ProjectionStatus.Running,
            // _ when source.Status.StartsWith("Stopping/")       => ProjectionStatus.Stopping,
            // _ when source.Status.StartsWith("Aborting/")       => ProjectionStatus.Aborting,
            // _ when source.Status.StartsWith("Stopped/")        => ProjectionStatus.Stopped,
            // _ when source.Status.StartsWith("Completed/")      => ProjectionStatus.Completed,
            // _ when source.Status.StartsWith("Aborted/")        => ProjectionStatus.Aborted,
            // _ when source.Status.StartsWith("Faulted/")        => ProjectionStatus.Faulted,
            // _ when source.Status.StartsWith("Deleting/")       => ProjectionStatus.Deleting,

            _ => throw new UnreachableException($"Unknown or invalid projection status: {source.Status}")
        };

        // these are possible second states of each main status... tick tick boom...
        // private enum State : uint {
        //     Initial            = 0x80000000,
        //     LoadStateRequested = 0x2,
        //     StateLoaded        = 0x4,
        //     Subscribed         = 0x8,
        //     Running            = 0x10,
        //     Stopping           = 0x40,
        //     Stopped            = 0x80,
        //     FaultedStopping    = 0x100,
        //     Faulted            = 0x200,
        //     CompletingPhase    = 0x400,
        //     PhaseCompleted     = 0x800,
        //     Suspended          = 0x900,
        // }
    }
}

static class ProjectionsHttpModel {
    internal readonly record struct GetProjectionsResponse(Projection[] Projections);

    internal readonly record struct Projection {
        public string Name          { get; init; }
        public string Mode          { get; init; }
        public long   Version       { get; init; }
        public string Status        { get; init; }
        public string StateReason   { get; init; }
        public string EffectiveName { get; init; }

        public long   CoreProcessingTime                 { get; init; }
        public long   Epoch                              { get; init; }
        public int    WritesInProgress                   { get; init; }
        public int    ReadsInProgress                    { get; init; }
        public int    PartitionsCached                   { get; init; }
        public string Position                           { get; init; }
        public float  Progress                           { get; init; }
        public string LastCheckpoint                     { get; init; }
        public long   EventsProcessedAfterRestart        { get; init; }
        public string CheckpointStatus                   { get; init; }
        public long   BufferedEvents                     { get; init; }
        public int    WritePendingEventsBeforeCheckpoint { get; init; }
        public int    WritePendingEventsAfterCheckpoint  { get; init; }
    }
}
