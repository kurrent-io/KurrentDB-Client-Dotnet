namespace Kurrent.Client.Model;

/// <summary>
/// Provides the details of a persistent subscription connection.
/// </summary>
public record PersistentSubscriptionConnectionInfo {
    /// <summary>
    /// Origin of this connection (IP address or hostname).
    /// </summary>
    public required string From { get; init; }

    /// <summary>
    /// Connection username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// The name of the connection.
    /// </summary>
    public required string ConnectionName { get; init; }

    /// <summary>
    /// Average events per second on this connection.
    /// </summary>
    public int AverageItemsPerSecond { get; init; }

    /// <summary>
    /// Total items processed on this connection.
    /// </summary>
    public long TotalItems { get; init; }

    /// <summary>
    /// Number of items seen since last measurement on this connection (used as the basis for AverageItemsPerSecond).
    /// </summary>
    public long CountSinceLastMeasurement { get; init; }

    /// <summary>
    /// Number of available slots for processing messages.
    /// </summary>
    public int AvailableSlots { get; init; }

    /// <summary>
    /// Number of in flight messages on this connection.
    /// </summary>
    public int InFlightMessages { get; init; }

    /// <summary>
    /// Timing measurements for the connection. Can be enabled with the ExtraStatistics setting.
    /// </summary>
    public IReadOnlyDictionary<string, long> ExtraStatistics { get; init; } = new Dictionary<string, long>();

    /// <summary>
    /// Gets a value indicating whether this connection is actively processing events.
    /// </summary>
    public bool IsProcessingEvents => AverageItemsPerSecond > 0;

    /// <summary>
    /// Gets a value indicating whether this connection has available capacity.
    /// </summary>
    public bool HasAvailableCapacity => AvailableSlots > 0;

    /// <summary>
    /// Gets a value indicating whether this connection has in-flight messages.
    /// </summary>
    public bool HasInFlightMessages => InFlightMessages > 0;

    /// <summary>
    /// Gets a value indicating whether this connection has processed any events.
    /// </summary>
    public bool HasProcessedEvents => TotalItems > 0;

    /// <summary>
    /// Gets a value indicating whether extra statistics are available for this connection.
    /// </summary>
    public bool HasExtraStatistics => ExtraStatistics.Count > 0;

    /// <summary>
    /// Gets the connection utilization as a percentage (0-100).
    /// </summary>
    public double UtilizationPercentage {
        get {
            var totalSlots = AvailableSlots + InFlightMessages;
            return totalSlots == 0 ? 0 : (InFlightMessages / (double)totalSlots) * 100;
        }
    }
}
