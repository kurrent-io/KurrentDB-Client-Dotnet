using System.Text.Json;

namespace Kurrent.Client.Projections;

[PublicAPI]
public static class ProjectionsClientIdiomaticExtensions {
    public static async ValueTask CreateProjectionAsync(this ProjectionsClient client, ProjectionName name, ProjectionQuery query, ProjectionSettings settings, CancellationToken cancellationToken = default) {
         _ = await client.CreateProjection(name, query, settings, cancellationToken).ThrowOnFailureAsync().ConfigureAwait(false);
    }

    public static ValueTask CreateProjectionAsync(this ProjectionsClient client, ProjectionName name, ProjectionQuery query, CancellationToken cancellationToken = default) =>
        CreateProjectionAsync(client, name, query, ProjectionSettings.Default, cancellationToken);

    public static async ValueTask DeleteProjectionAsync(this ProjectionsClient client, ProjectionName name, DeleteProjectionOptions options, CancellationToken cancellationToken = default) {
         _ = await client.DeleteProjection(name, options, cancellationToken).ThrowOnFailureAsync().ConfigureAwait(false);
    }

    public static ValueTask DeleteProjectionAsync(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) =>
        DeleteProjectionAsync(client, name, DeleteProjectionOptions.Default, cancellationToken);

    public static async ValueTask EnableProjectionAsync(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) {
         _ = await client.EnableProjection(name, cancellationToken).ThrowOnFailureAsync().ConfigureAwait(false);
    }

    public static async ValueTask DisableProjectionAsync(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) {
         _ = await client.DisableProjection(name, cancellationToken).ThrowOnFailureAsync().ConfigureAwait(false);
    }

    public static async ValueTask ResetProjectionAsync(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) {
         _ = await client.ResetProjection(name, cancellationToken).ThrowOnFailureAsync().ConfigureAwait(false);
    }

    public static async ValueTask AbortProjectionAsync(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) {
        _ = await client.AbortProjection(name, cancellationToken).ThrowOnFailureAsync().ConfigureAwait(false);
    }

    public static ValueTask<ProjectionDetails> GetProjectionAsync(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) =>
        client.GetProjection(name, cancellationToken).ThrowOnFailureAsync();

    public static  ValueTask<List<ProjectionDetails>> ListProjectionsAsync(this ProjectionsClient client, ListProjectionsOptions options, CancellationToken cancellationToken = default) =>
        client.ListProjections(options, cancellationToken).ThrowOnFailureAsync();

    public static ValueTask<T> GetProjectionResultAsync<T>(this ProjectionsClient client, ProjectionName name,  ProjectionPartition partition, JsonSerializerOptions serializerOptions, CancellationToken cancellationToken = default) where T : notnull =>
        client.GetProjectionResult<T>(name, partition, serializerOptions, cancellationToken).ThrowOnFailureAsync();

    public static ValueTask<T> GetProjectionResultAsync<T>(this ProjectionsClient client, ProjectionName name, ProjectionPartition partition, CancellationToken cancellationToken = default) where T : notnull =>
        GetProjectionResultAsync<T>(client, name, partition, JsonSerializerOptions.Default, cancellationToken);

    public static ValueTask<T> GetProjectionResultAsync<T>(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) where T : notnull =>
        GetProjectionResultAsync<T>(client, name, ProjectionPartition.None, JsonSerializerOptions.Default, cancellationToken);

    public static ValueTask<T> GetProjectionStateAsync<T>(this ProjectionsClient client, ProjectionName name,  ProjectionPartition partition, JsonSerializerOptions serializerOptions, CancellationToken cancellationToken = default) where T : notnull =>
        client.GetProjectionState<T>(name, partition, serializerOptions, cancellationToken).ThrowOnFailureAsync();

    public static ValueTask<T> GetProjectionStateAsync<T>(this ProjectionsClient client, ProjectionName name, ProjectionPartition partition, CancellationToken cancellationToken = default) where T : notnull =>
        GetProjectionStateAsync<T>(client, name, partition, JsonSerializerOptions.Default, cancellationToken);

    public static ValueTask<T> GetProjectionStateAsync<T>(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) where T : notnull =>
        GetProjectionStateAsync<T>(client, name, ProjectionPartition.None, JsonSerializerOptions.Default, cancellationToken);

    public static async ValueTask RestartSubsystemAsync(this ProjectionsClient client, CancellationToken cancellationToken = default) {
        _ = await client.RestartProjectionSubsystem(cancellationToken).ThrowOnFailureAsync().ConfigureAwait(false);
    }
}
