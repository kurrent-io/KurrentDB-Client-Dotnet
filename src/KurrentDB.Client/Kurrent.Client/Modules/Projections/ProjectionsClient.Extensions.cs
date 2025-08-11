using System.Text.Json;

namespace Kurrent.Client.Projections;

[PublicAPI]
public static class ProjectionsClientExtensions {
    public static ValueTask<Result<Success, CreateProjectionError>> CreateProjection(this ProjectionsClient client, ProjectionName name, ProjectionQuery query, CancellationToken cancellationToken = default) =>
        client.CreateProjection(name, query, ProjectionSettings.Default, cancellationToken);

    public static ValueTask<Result<Success, DeleteProjectionError>> DeleteProjection(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) =>
        client.DeleteProjection(name, DeleteProjectionOptions.Default, cancellationToken);

    public static ValueTask<Result<T, GetProjectionResultError>> GetProjectionResult<T>(this ProjectionsClient client, ProjectionName name, ProjectionPartition partition, CancellationToken cancellationToken = default) where T : notnull =>
        client.GetProjectionResult<T>(name, partition, JsonSerializerOptions.Default, cancellationToken);

    public static ValueTask<Result<T, GetProjectionResultError>> GetProjectionResult<T>(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) where T : notnull =>
        client.GetProjectionResult<T>(name, ProjectionPartition.None, JsonSerializerOptions.Default, cancellationToken);

    public static ValueTask<Result<T, GetProjectionStateError>> GetProjectionState<T>(this ProjectionsClient client, ProjectionName name, ProjectionPartition partition, CancellationToken cancellationToken = default) where T : notnull =>
        client.GetProjectionState<T>(name, partition, JsonSerializerOptions.Default, cancellationToken);

    public static ValueTask<Result<T, GetProjectionStateError>> GetProjectionState<T>(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) where T : notnull =>
        client.GetProjectionState<T>(name, ProjectionPartition.None, JsonSerializerOptions.Default, cancellationToken);
}
