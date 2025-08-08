namespace Kurrent.Client.Projections;

[PublicAPI]
public record ProjectionSettings {
    public static readonly ProjectionSettings Default = new();

    /// <summary>
    /// Whether the projection can emit events. If this is false then the projection will fault if linkTo or emit are used in the projection query.
    /// </summary>
    public bool EmitEnabled { get; init; }

    /// <summary>
    /// Whether the projection should keep track of the emitted streams that it creates.
    /// This causes write amplification and should only be used if the projection has to be deleted in the future.
    /// </summary>
    public bool TrackEmittedStreams { get; init; }

    // /// <summary>
    // /// The time after which a projection checkpoint should be written.
    // /// This is the minimum time after which a checkpoint will be written, even if the other thresholds are not met.
    // /// </summary>
    // public TimeSpan CheckpointAfter { get; init; } = TimeSpan.FromSeconds(0);
    //
    // /// <summary>
    // /// The number of events that a projection can handle before attempting to write a checkpoint.
    // /// This is the minimum number of events that must be handled before a checkpoint will be written,
    // /// even if the other thresholds are not met.
    // /// </summary>
    // public int CheckpointHandledThreshold { get; init; } = 4000;
    //
    // /// <summary>
    // /// The number of bytes that a projection can process before attempting to write a checkpoint.
    // /// This is the minimum number of bytes that must be processed before a checkpoint will be written,
    // /// even if the other thresholds are not met.
    // /// </summary>
    // public int CheckpointUnhandledBytesThreshold { get; init; } = 10_000_000;
    //
    // /// <summary>
    // /// The number of events that can be pending before the projection readers are temporarily paused.
    // /// This is to prevent the projection from falling too far behind and consuming too much memory.
    // /// If the number of pending events exceeds this threshold, the projection will stop reading new events
    // /// until the number of pending events falls below this threshold.
    // /// </summary>
    // public int PendingEventsThreshold { get; init; } = 5000;
    //
    // /// <summary>
    // /// The maximum number of events that the projection can write in a batch at a time.
    // /// This is to prevent the projection from writing too many events at once and overwhelming the system.
    // /// If the number of events to be written exceeds this threshold, the projection will split the
    // /// write operation into multiple batches, each containing at most this many events.
    // /// This is to ensure that the projection can write events efficiently without overwhelming the system.
    // /// </summary>
    // public int MaxWriteBatchLength { get; init; } = 500;
    //
    // /// <summary>
    // /// The maximum number of concurrent write operations allowed for a projection.
    // /// This is to prevent the projection from overwhelming the system with too many concurrent write operations.
    // /// If the number of concurrent write operations exceeds this threshold, the projection will
    // /// wait until the number of in-flight write operations falls below this threshold before starting new ones.
    // /// This is to ensure that the projection can write events efficiently without overwhelming the system.
    // /// </summary>
    // public int MaxAllowedWritesInFlight { get; init; }

    public void ThrowIfInvalid() {
        if (EmitEnabled && !TrackEmittedStreams)
            throw new ArgumentException("EmitEnabled must be true if TrackEmittedStreams is true.");

        // if (CheckpointAfter < TimeSpan.Zero)
        //     throw new ArgumentOutOfRangeException(nameof(CheckpointAfter), "CheckpointAfter must be non-negative.");
        // if (CheckpointHandledThreshold <= 0)
        //     throw new ArgumentOutOfRangeException(nameof(CheckpointHandledThreshold), "CheckpointHandledThreshold must be greater than zero.");
        // if (CheckpointUnhandledBytesThreshold <= 0)
        //     throw new ArgumentOutOfRangeException(nameof(CheckpointUnhandledBytesThreshold), "CheckpointUnhandledBytesThreshold must be greater than zero.");
        // if (PendingEventsThreshold <= 0)
        //     throw new ArgumentOutOfRangeException(nameof(PendingEventsThreshold), "PendingEventsThreshold must be greater than zero.");
        // if (MaxWriteBatchLength <= 0)
        //     throw new ArgumentOutOfRangeException(nameof(MaxWriteBatchLength), "MaxWriteBatchLength must be greater than zero.");
        // if (MaxAllowedWritesInFlight <= 0)
        //     throw new ArgumentOutOfRangeException(nameof(MaxAllowedWritesInFlight), "MaxAllowedWritesInFlight must be greater than zero.");
    }
}
