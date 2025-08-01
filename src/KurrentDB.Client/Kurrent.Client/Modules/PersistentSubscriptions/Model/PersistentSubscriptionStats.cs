using Kurrent.Client.Streams;

namespace Kurrent.Client.PersistentSubscriptions;

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
    /// Number of events seen since last measurement on this connection (used as the basis for <see cref="AveragePerSecond"/>).
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
    /// The <see cref="LogPosition"/> of the last checkpoint. This will be null if there are no checkpoints.
    /// </summary>
    public LogPosition LastCheckpointedEventPosition { get; init; }

    /// <summary>
    /// The <see cref="LogPosition"/> of the last known event. This will be undefined if no events have been received yet.
    /// </summary>
    public LogPosition LastKnownEventPosition { get; init; }
}
