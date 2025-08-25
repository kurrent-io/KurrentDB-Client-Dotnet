namespace Kurrent.Client.Connectors;

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
