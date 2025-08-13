using Kurrent.Client.Streams;

namespace Kurrent.Client.Connectors;

/// <summary>
/// Represents detailed information about a connector in KurrentDB.
/// </summary>
public record ConnectorDetails {
    /// <summary>
    /// Unique identifier for the connector.
    /// </summary>
    public required string ConnectorId { get; init; }

    /// <summary>
    /// Display name of the connector instance type.
    /// </summary>
    public required string InstanceTypeName { get; init; }

    /// <summary>
    /// Human-readable name for the connector.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Current operational state of the connector.
    /// </summary>
    public ConnectorState State { get; init; } = ConnectorState.Unknown;

    /// <summary>
    /// Timestamp when the connector state was last updated.
    /// </summary>
    public DateTimeOffset StateUpdateTime { get; init; }

    /// <summary>
    /// Configuration settings for the connector.
    /// </summary>
    public IReadOnlyDictionary<string, string> Settings { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Timestamp when the connector settings were last updated.
    /// </summary>
    public DateTimeOffset SettingsUpdateTime { get; init; }

    /// <summary>
    /// Current processing position of the connector.
    /// </summary>
    public LogPosition Position { get; init; } = LogPosition.Unset;

    /// <summary>
    /// Timestamp when the connector position was last updated.
    /// </summary>
    public DateTimeOffset? PositionUpdateTime { get; init; }

    /// <summary>
    /// Timestamp when the connector was created.
    /// </summary>
    public DateTimeOffset CreateTime { get; init; }

    /// <summary>
    /// Timestamp when the connector was last updated.
    /// </summary>
    public DateTimeOffset UpdateTime { get; init; }

    /// <summary>
    /// Timestamp when the connector was deleted, if applicable.
    /// </summary>
    public DateTimeOffset? DeleteTime { get; init; }

    /// <summary>
    /// Error details if the connector is in an error state.
    /// </summary>
    public string? ErrorDetails { get; init; }

    /// <summary>
    /// Gets a value indicating whether the connector is currently running.
    /// </summary>
    public bool IsRunning => State == ConnectorState.Running;

    /// <summary>
    /// Gets a value indicating whether the connector is stopped.
    /// </summary>
    public bool IsStopped => State == ConnectorState.Stopped;

    /// <summary>
    /// Gets a value indicating whether the connector has been deleted.
    /// </summary>
    public bool IsDeleted => DeleteTime.HasValue;
}
