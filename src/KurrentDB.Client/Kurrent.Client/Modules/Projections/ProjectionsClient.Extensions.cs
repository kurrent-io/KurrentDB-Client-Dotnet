using System.Text.Json;

namespace Kurrent.Client.Projections;

[PublicAPI]
public static class ProjectionsClientExtensions {
    public static ValueTask<Result<Success, CreateProjectionError>> Create(this ProjectionsClient client, ProjectionName name, ProjectionDefinition definition, ProjectionSettings settings, CancellationToken cancellationToken = default) =>
        client.Create(name, definition, settings, false, cancellationToken);

    public static ValueTask<Result<Success, CreateProjectionError>> Create(this ProjectionsClient client, ProjectionName name, ProjectionDefinition definition, CancellationToken cancellationToken = default) =>
        client.Create(name, definition, ProjectionSettings.Default, false, cancellationToken);

    public static ValueTask<Result<Success, DeleteProjectionError>> Delete(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) =>
        client.Delete(name, DeleteProjectionOptions.Default, cancellationToken);

    public static ValueTask<Result<ProjectionDetails, GetProjectionDetailsError>> GetDetails(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) =>
        client.GetDetails(name, GetProjectionDetailsOptions.Default, cancellationToken);

    public static ValueTask<Result<T, GetProjectionStateError>> GetState<T>(this ProjectionsClient client, ProjectionName name, ProjectionPartition partition, CancellationToken cancellationToken = default) where T : notnull =>
        client.GetState<T>(name, partition, JsonSerializerOptions.Default, cancellationToken);

    public static ValueTask<Result<T, GetProjectionStateError>> GetState<T>(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) where T : notnull =>
        client.GetState<T>(name, ProjectionPartition.None, JsonSerializerOptions.Default, cancellationToken);
}
