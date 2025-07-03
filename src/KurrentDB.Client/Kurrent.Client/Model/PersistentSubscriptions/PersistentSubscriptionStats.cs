namespace Kurrent.Client.Model;

/// <summary>
/// Provides the statistics of a persistent subscription.
/// </summary>
public record PersistentSubscriptionStats {
    /// <summary>
    /// Average number of events per second.
    /// </summary>
    public int AveragePerSecond { get; init; }

    /// <summary>
    /// Total number of events processed by subscription.
    /// </summary>
    public long TotalItems { get; init; }

    /// <summary>
    /// Number of events seen since last measurement (used as the basis for AveragePerSecond).
    /// </summary>
    public long CountSinceLastMeasurement { get; init; }

    /// <summary>
    /// Number of events in the read buffer.
    /// </summary>
    public int ReadBufferCount { get; init; }

    /// <summary>
    /// Number of events in the live buffer.
    /// </summary>
    public long LiveBufferCount { get; init; }

    /// <summary>
    /// Number of events in the retry buffer.
    /// </summary>
    public int RetryBufferCount { get; init; }

    /// <summary>
    /// Current in flight messages across all connections.
    /// </summary>
    public int TotalInFlightMessages { get; init; }

    /// <summary>
    /// Current number of outstanding messages.
    /// </summary>
    public int OutstandingMessagesCount { get; init; }

    /// <summary>
    /// The current number of parked messages.
    /// </summary>
    public long ParkedMessageCount { get; init; }

    /// <summary>
    /// The position of the last checkpoint. This will be null if there are no checkpoints.
    /// </summary>
    public LogPosition? LastCheckpointedEventPosition { get; init; }

    /// <summary>
    /// The position of the last known event. This will be null if no events have been received yet.
    /// </summary>
    public LogPosition? LastKnownEventPosition { get; init; }

    /// <summary>
    /// Gets a value indicating whether the subscription is actively processing events.
    /// </summary>
    public bool IsProcessingEvents => AveragePerSecond > 0;

    /// <summary>
    /// Gets a value indicating whether there are any parked messages.
    /// </summary>
    public bool HasParkedMessages => ParkedMessageCount > 0;

    /// <summary>
    /// Gets a value indicating whether there are any retry messages pending.
    /// </summary>
    public bool HasRetryMessages => RetryBufferCount > 0;

    /// <summary>
    /// Gets a value indicating whether the subscription has processed any events.
    /// </summary>
    public bool HasProcessedEvents => TotalItems > 0;

    /// <summary>
    /// Gets a value indicating whether the subscription has been checkpointed.
    /// </summary>
    public bool HasCheckpoint => LastCheckpointedEventPosition is not null;

    /// <summary>
    /// Gets the total number of buffered events across all buffers.
    /// </summary>
    public long TotalBufferedEvents => ReadBufferCount + LiveBufferCount + RetryBufferCount;
}
