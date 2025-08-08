namespace Kurrent.Client.Projections;

[PublicAPI]
public record ListProjectionsOptions {
    public ProjectionMode Mode { get; init; } = ProjectionMode.Unspecified;
}
