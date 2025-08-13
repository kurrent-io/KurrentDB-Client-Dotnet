namespace Kurrent.Client.Projections;

[PublicAPI]
public record DeleteProjectionOptions {
    public static readonly DeleteProjectionOptions Default = new();

    public bool DeleteStateStream      { get; init; } = true;
    public bool DeleteCheckpointStream { get; init; } = true;
    public bool DeleteEmittedStreams   { get; init; } = true;
}
