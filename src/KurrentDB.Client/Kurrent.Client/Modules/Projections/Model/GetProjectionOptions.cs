namespace Kurrent.Client.Projections;

public record GetProjectionDetailsOptions {
    public static readonly GetProjectionDetailsOptions Default = new();

    public bool IncludeDefinition { get; init; } = true;
    public bool IncludeSettings   { get; init; } = true;
    public bool IncludeStatistics { get; init; } = true;
}
