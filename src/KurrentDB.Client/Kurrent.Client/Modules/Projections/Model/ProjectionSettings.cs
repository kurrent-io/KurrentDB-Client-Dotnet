using System.Text.Json.Serialization;

namespace Kurrent.Client.Projections;

[PublicAPI]
public record ProjectionSettings {
    public static readonly ProjectionSettings Unspecified = new();

    public static readonly ProjectionSettings Default = new() {
        EmitEnabled                       = true,
        TrackEmittedStreams               = false,
        CheckpointAfter                   = 0,
        CheckpointHandledThreshold        = 4000,
        CheckpointUnhandledBytesThreshold = 10_000_000,
        PendingRecordsThreshold           = 5000,
        MaxWriteBatchLength               = 500,
        MaxAllowedWritesInFlight          = 0, // Unbounded
        ProjectionExecutionTimeout        = 250
    };

    /// <summary>
    /// Whether the projection can emit events. <para/>
    /// If this is false then the projection will fault if linkTo or emit are used in the projection query.
    /// </summary>
    public bool EmitEnabled { get; init; }

    /// <summary>
    /// Whether the projection should keep track of the emitted streams that it creates.
    /// This causes write amplification and should only be used if the projection has to be deleted in the future.
    /// <remarks>
    /// By default, KurrentDB disables the trackemittedstreams setting for projections. <para/>
    /// When enabled, an event appended records the stream name (in $projections-{projection_name}-emittedstreams) of each event emitted by the projection. <para/>
    /// This means that write amplification is a possibility, as each event that the projection emits appends a separate event. <para/>
    /// As such, this option is not recommended for projections that emit a lot of events, and you should enable only where necessary.
    /// </remarks>
    /// </summary>
    public bool TrackEmittedStreams { get; init; }

    /// <summary>
    /// The time after which a projection checkpoint should be written. <para/>
    /// This is the minimum time after which a checkpoint will be written, even if the other thresholds are not met.
    /// </summary>
    [JsonPropertyName("CheckpointAfterMs")]
    public int CheckpointAfter { get; init; }

    /// <summary>
    /// The number of events that a projection can handle before attempting to write a checkpoint. <para/>
    /// This is the minimum number of events that must be handled before a checkpoint will be written,
    /// even if the other thresholds are not met.
    /// </summary>
    public int CheckpointHandledThreshold { get; init; }

    /// <summary>
    /// The number of bytes that a projection can process before attempting to write a checkpoint. <para/>
    /// This is the minimum number of bytes that must be processed before a checkpoint will be written,
    /// even if the other thresholds are not met.
    /// </summary>
    public int CheckpointUnhandledBytesThreshold { get; init; }

    /// <summary>
    /// The number of records that can be pending before the projection readers are temporarily paused. <para/>
    /// This is to prevent the projection from falling too far behind and consuming too much memory. <para/>
    /// If the number of pending records exceeds this threshold, the projection will stop reading new records
    /// until the number of pending records falls below this threshold.
    /// </summary>
    [JsonPropertyName("PendingEventsThreshold")]
    public int PendingRecordsThreshold { get; init; }

    /// <summary>
    /// The maximum number of events that the projection can write in a batch at a time. <para/>
    /// This is to prevent the projection from writing too many events at once and overwhelming the system. <para/>
    /// If the number of events to be written exceeds this threshold, the projection will split the
    /// write operation into multiple batches, each containing at most this many events. <para/>
    /// This is to ensure that the projection can write events efficiently without overwhelming the system.
    /// </summary>
    public int MaxWriteBatchLength { get; init; }

    /// <summary>
    /// The maximum number of concurrent write operations allowed for a projection. <para/>
    /// This is to prevent the projection from overwhelming the system with too many concurrent write operations. <para/>
    /// If the number of concurrent write operations exceeds this threshold, the projection will
    /// wait until the number of in-flight write operations falls below this threshold before starting new ones. <para/>
    /// This is to ensure that the projection can write events efficiently without overwhelming the system. <para/>
    /// Zero is unbounded, meaning no limit on the number of concurrent writes.
    /// </summary>
    public int MaxAllowedWritesInFlight { get; init; }

    /// <summary>
    /// The timeout for executing the projection query. <para/>
    /// This is the maximum time that the projection will wait for the query to complete before timing out. <para/>
    /// If the query does not complete within this time, the projection will fault and stop processing. <para/>
    /// This is to ensure that the projection does not hang indefinitely and can recover from long-running queries.
    /// </summary>
    public int ProjectionExecutionTimeout { get; init; }

    public void EnsureValid() {
        if (TrackEmittedStreams && !EmitEnabled)
            throw new ArgumentException("EmitEnabled must be true if TrackEmittedStreams is true.");

        if (CheckpointAfter < 0)
            throw new ArgumentOutOfRangeException(nameof(CheckpointAfter), "CheckpointAfter must be non-negative.");

        if (CheckpointHandledThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(CheckpointHandledThreshold), "CheckpointHandledThreshold must be greater than zero.");

        if (CheckpointUnhandledBytesThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(CheckpointUnhandledBytesThreshold), "CheckpointUnhandledBytesThreshold must be greater than zero.");

        if (PendingRecordsThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(PendingRecordsThreshold), "PendingRecordsThreshold must be greater than zero.");

        if (MaxWriteBatchLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxWriteBatchLength), "MaxWriteBatchLength must be greater than zero.");

        if (MaxAllowedWritesInFlight < 0)
            throw new ArgumentOutOfRangeException(nameof(MaxAllowedWritesInFlight), "MaxAllowedWritesInFlight must be non-negative.");

        if (ProjectionExecutionTimeout <= 0)
            throw new ArgumentOutOfRangeException(nameof(ProjectionExecutionTimeout), "ProjectionExecutionTimeout must be greater than zero.");
    }
}
