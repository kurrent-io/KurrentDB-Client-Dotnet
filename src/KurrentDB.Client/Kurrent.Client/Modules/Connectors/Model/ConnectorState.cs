namespace Kurrent.Client.Connectors;

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
