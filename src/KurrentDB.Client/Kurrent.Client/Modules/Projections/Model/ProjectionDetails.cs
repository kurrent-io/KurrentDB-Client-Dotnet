namespace Kurrent.Client.Projections;

public sealed record ProjectionDetails {
    public static readonly ProjectionDetails None = new();

    public ProjectionName       Name       { get; init; } = ProjectionName.None;
    public ProjectionDefinition Definition { get; init; } = ProjectionDefinition.None;
    public ProjectionMode       Mode       { get; init; } = ProjectionMode.Unspecified;

    public long Version { get; init; }

    public ProjectionStatus Status      { get; init; } = ProjectionStatus.Unspecified;
    public string?          FaultReason { get; init; }

    public string EffectiveName { get; init; } = ""; // we should just remove it

    public ProjectionSettings   Settings   { get; init; } = ProjectionSettings.Unspecified;
    public ProjectionStatistics Statistics { get; init; } = ProjectionStatistics.None;

    public ProjectionType Type =>
        Name == ProjectionName.None
            ? ProjectionType.Unspecified
            : Name.IsSystemProjection
                ? ProjectionType.System
                : ProjectionType.User;

    public bool HasDefinition => Definition != ProjectionDefinition.None;
    public bool HasSettings   => Settings != ProjectionSettings.Unspecified;
    public bool HasStatistics => Statistics != ProjectionStatistics.None;
}
