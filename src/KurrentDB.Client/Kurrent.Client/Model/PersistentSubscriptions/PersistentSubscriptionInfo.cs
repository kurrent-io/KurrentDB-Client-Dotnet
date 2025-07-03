namespace Kurrent.Client.Model;

/// <summary>
/// Provides details about a persistent subscription, including status, connections, settings, and live statistics.
/// </summary>
public record PersistentSubscriptionInfo {
    /// <summary>
    /// The source of events for the subscription (stream name or $all).
    /// </summary>
    public required string EventSource { get; init; }

    /// <summary>
    /// The group name assigned to the subscription.
    /// </summary>
    public required string GroupName { get; init; }

    /// <summary>
    /// The current status of the subscription.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// The active connections to this subscription.
    /// </summary>
    public IReadOnlyList<PersistentSubscriptionConnectionInfo> Connections { get; init; } = [];

    /// <summary>
    /// The settings used to create or configure the subscription.
    /// </summary>
    public PersistentSubscriptionSettings? Settings { get; init; }

    /// <summary>
    /// Live statistics for the persistent subscription.
    /// </summary>
    public required PersistentSubscriptionStats Stats { get; init; }

    /// <summary>
    /// Indicates whether the subscription has any active connections.
    /// </summary>
    public bool HasActiveConnections => Connections.Count > 0;

    /// <summary>
    /// Indicates whether the subscription is currently running.
    /// </summary>
    public bool IsRunning => Status.Equals("Running", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Indicates whether the subscription is for the $all stream.
    /// </summary>
    public bool IsAllStream => EventSource == "$all";

    /// <summary>
    /// Gets the total number of in-flight messages across all connections.
    /// </summary>
    public int TotalInFlightMessages => Connections.Sum(c => c.InFlightMessages);

    /// <summary>
    /// Gets the total number of available slots across all connections.
    /// </summary>
    public int TotalAvailableSlots => Connections.Sum(c => c.AvailableSlots);
}
