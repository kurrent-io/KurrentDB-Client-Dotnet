namespace Kurrent.Client.Projections;

[PublicAPI]
public record ListProjectionsOptions {
    public static readonly ListProjectionsOptions Default = new();

    public ProjectionMode Mode { get; init; } = ProjectionMode.Unspecified;

    // /// <summary>
    // /// Whether to include the projection's configuration settings.
    // /// </summary>
    // public bool IncludeSettings { get; init; }

    /// <summary>
    /// Whether to include the projection's statistics.
    /// </summary>
    public bool IncludeStatistics { get; init; }
}
