namespace Kurrent.Client.Projections;

[PublicAPI]
public record ListProjectionsOptions {
    public static readonly ListProjectionsOptions Default = new();

    public ProjectionMode Mode { get; init; } = ProjectionMode.Unspecified;

    /// <summary>
    /// The type of projections to list.
    /// If not specified, all projections will be listed.
    /// </summary>
    public ProjectionType Type { get; init; } = ProjectionType.Unspecified;

    /// <summary>
    /// Whether to include the projection's statistics.
    /// </summary>
    public bool IncludeStatistics { get; init; }

    /// <summary>
    /// Whether to include the projection's definition.
    /// This is useful for getting the full details of the projection, including its query and other
    /// configuration details.
    /// </summary>
    public bool IncludeDefinition { get; init; }

    /// <summary>
    /// Whether to include the projection's settings.
    /// This is useful for getting the configuration details of the projection, such as checkpointing,
    /// emit settings, and other operational parameters.
    /// </summary>
    public bool IncludeSettings { get; init; }
}
