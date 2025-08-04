namespace Kurrent.Client.Connectors;

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
