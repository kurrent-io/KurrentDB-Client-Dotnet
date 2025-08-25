namespace Kurrent.Client.Connectors;

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
