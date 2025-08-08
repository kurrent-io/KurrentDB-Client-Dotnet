namespace Kurrent.Client.Admin;

/// <summary>
/// A structure representing the result of a scavenge operation.
/// </summary>
public readonly record struct ScavengeResult {
    ScavengeResult(string scavengeId, ScavengeStatus status) {
        ScavengeId = scavengeId;
        Status     = status;
    }

    /// <summary>
    /// The ID of the scavenge operation.
    /// </summary>
    public string ScavengeId { get; }

    /// <summary>
    /// The <see cref="ScavengeStatus"/> of the scavenge operation.
    /// </summary>
    public ScavengeStatus Status { get; }

    /// <summary>
    /// A scavenge operation that has started.
    /// </summary>
    /// <param name="scavengeId"></param>
    /// <returns></returns>
    public static ScavengeResult Started(string scavengeId) =>
        new(scavengeId, ScavengeStatus.Started);

    /// <summary>
    /// A scavenge operation that has stopped.
    /// </summary>
    /// <param name="scavengeId"></param>
    /// <returns></returns>
    public static ScavengeResult Stopped(string scavengeId) =>
        new(scavengeId, ScavengeStatus.Stopped);

    /// <summary>
    /// A scavenge operation that is currently in progress.
    /// </summary>
    /// <param name="scavengeId"></param>
    /// <returns></returns>
    public static ScavengeResult InProgress(string scavengeId) =>
        new(scavengeId, ScavengeStatus.InProgress);

    /// <summary>
    /// A scavenge operation whose state is unknown.
    /// </summary>
    /// <param name="scavengeId"></param>
    /// <returns></returns>
    public static ScavengeResult Unknown(string scavengeId) =>
        new(scavengeId, ScavengeStatus.Unknown);
}
