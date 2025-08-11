namespace Kurrent.Client.Projections;

public sealed record ProjectionDetails {
    public static readonly ProjectionDetails None = new();

    public ProjectionName Name    { get; init; } = ProjectionName.None;
    public ProjectionMode Mode    { get; init; } = ProjectionMode.Unspecified;
    public long           Version { get; init; }

    public ProjectionStatus Status       { get; init; }
    public string?          StatusReason { get; init; }

    public string? EffectiveName { get; init; }

    public ProjectionSettings   Settings   { get; init; } = ProjectionSettings.Default;
    public ProjectionStatistics Statistics { get; init; } = ProjectionStatistics.None;

    public bool HasStatistics => Statistics != ProjectionStatistics.None;
}
