using Kurrent.Client.Streams;
using Kurrent.Variant;

namespace Kurrent.Client.Connectors;

public enum NodeAffinity {
    Any = 0,

    /// <summary>
    /// The connector prefers to read from the leader node.
    /// </summary>
    Leader = 1,

    /// <summary>
    /// The connector prefers to read from a follower node.
    /// </summary>
    Follower = 2,

    /// <summary>
    /// The connector prefers to read from a read-only replica node.
    /// </summary>
    ReadonlyReplica = 3,
}

public enum ConnectorType {
    Unspecified = 0,
    Sink        = 1,
    Source      = 2,
}

/// <summary>
/// Enum representing the various states a connector can be in.
/// </summary>
public enum ConnectorState {
    /// <summary>
    /// The state of the connector is unknown.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// The connector is in the process of being activated.
    /// </summary>
    Activating = 1,
    /// <summary>
    /// The connector is currently running.
    /// </summary>
    Running = 2,
    /// <summary>
    /// The connector is in the process of being deactivated.
    /// </summary>
    Deactivating = 3,
    /// <summary>
    /// The connector is currently stopped.
    /// </summary>
   Stopped = 4,
}

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

/// <summary>
/// Options for listing connectors.
/// </summary>
public record ConnectorListOptions {
    /// <summary>
    /// Filter connectors by their operational state.
    /// </summary>
    public IReadOnlyList<ConnectorState> States { get; init; } = [];

    /// <summary>
    /// Filter connectors by instance type names.
    /// </summary>
    public IReadOnlyList<string> InstanceTypeNames { get; init; } = [];

    /// <summary>
    /// Filter connectors by specific connector IDs.
    /// </summary>
    public IReadOnlyList<string> ConnectorIds { get; init; } = [];

    /// <summary>
    /// Include connector settings in the response.
    /// </summary>
    public bool IncludeSettings { get; init; }

    /// <summary>
    /// Include deleted connectors in the response.
    /// </summary>
    public bool ShowDeleted { get; init; }

    /// <summary>
    /// Page number for pagination (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of items per page for pagination.
    /// </summary>
    public int PageSize { get; init; } = 50;
}

/// <summary>
/// Result of a connector list operation.
/// </summary>
public record ConnectorListResult {
    /// <summary>
    /// List of connectors matching the filter criteria.
    /// </summary>
    public required IReadOnlyList<ConnectorDetails> Items { get; init; }

    /// <summary>
    /// Total number of connectors matching the filter criteria.
    /// </summary>
    public int TotalSize { get; init; }

    /// <summary>
    /// Gets a value indicating whether there are more pages available.
    /// </summary>
    public bool HasMorePages => Items.Count > 0 && TotalSize > Items.Count;
}

/// <summary>
/// Connector settings with metadata.
/// </summary>
public record ConnectorSettingsResult {
    /// <summary>
    /// Configuration settings for the connector.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Settings { get; init; }

    /// <summary>
    /// Timestamp when the settings were last updated.
    /// </summary>
    public DateTimeOffset SettingsUpdateTime { get; init; }
}

[PublicAPI]
public readonly partial record struct CreateConnectorError : IVariantResultError<
    ErrorDetails.AccessDenied
>;

[PublicAPI]
public readonly partial record struct ReconfigureConnectorError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.ConnectorNotFound
>;

[PublicAPI]
public readonly partial record struct DeleteConnectorError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.ConnectorNotFound
>;

[PublicAPI]
public readonly partial record struct StartConnectorError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.ConnectorNotFound
>;

[PublicAPI]
public readonly partial record struct ResetConnectorError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.ConnectorNotFound
>;

[PublicAPI]
public readonly partial record struct StopConnectorError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.ConnectorNotFound
>;

[PublicAPI]
public readonly partial record struct RenameConnectorError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.ConnectorNotFound
>;

[PublicAPI]
public readonly partial record struct ListConnectorsError : IVariantResultError<
    ErrorDetails.AccessDenied
>;

[PublicAPI]
public readonly partial record struct GetConnectorSettingsError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.ConnectorNotFound
>;
