namespace Kurrent.Client.Admin;

/// <summary>
/// An enumeration that represents the state of a subsystem.
/// </summary>
public enum SubsystemState {
    NotReady,
    Ready,
    Starting,
    Started,
    Stopping,
    Stopped
}
