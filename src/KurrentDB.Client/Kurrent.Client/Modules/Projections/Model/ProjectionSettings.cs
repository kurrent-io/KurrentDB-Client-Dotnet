namespace Kurrent.Client.Projections;

public record ProjectionSettings {
    public static readonly ProjectionSettings Unset = new();

    public ProjectionName  Name                { get; internal init; } = ProjectionName.None;
    public ProjectionQuery Query               { get; internal init; } = ProjectionQuery.None;
    public ProjectionMode  Mode                { get; internal init; } = ProjectionMode.Unspecified;
    public bool            TrackEmittedStreams { get; internal init; }

    public void ThrowIfInvalid() {
        if (this == Unset)
            throw new ArgumentException("Projection settings must be set before use.", nameof(ProjectionSettings));
    }

    public static ProjectionSettings OneTime(ProjectionQuery query) =>
        new() { Query = query, Mode = ProjectionMode.OneTime };

    public static ProjectionSettings Continuous(ProjectionName name, ProjectionQuery query, bool trackEmittedStreams = false) =>
        new() { Name = name, Query = query, Mode = ProjectionMode.Continuous, TrackEmittedStreams = trackEmittedStreams };

    public static ProjectionSettings Transient(ProjectionName name, ProjectionQuery query) =>
        new() { Name = name, Query = query, Mode = ProjectionMode.Transient };
}
