using System.Text.Json;

namespace Kurrent.Client.Projections;

[PublicAPI]
public static class ProjectionsClientIdiomaticExtensions {
    public static async ValueTask CreateAsync(this ProjectionsClient client, ProjectionName name, ProjectionDefinition definition, ProjectionSettings settings, bool autoStart, CancellationToken cancellationToken = default) {
        _ = await client.Create(name, definition, settings, autoStart, cancellationToken).ThrowOnFailureAsync().ConfigureAwait(false);
    }

    public static ValueTask CreateAsync(this ProjectionsClient client, ProjectionName name, ProjectionDefinition definition, ProjectionSettings settings, CancellationToken cancellationToken = default) =>
        CreateAsync(client, name, definition, ProjectionSettings.Default, false, cancellationToken);

    public static ValueTask CreateAsync(this ProjectionsClient client, ProjectionName name, ProjectionDefinition definition, CancellationToken cancellationToken = default) =>
        CreateAsync(client, name, definition, ProjectionSettings.Default, cancellationToken);

    public static async ValueTask DeleteAsync(this ProjectionsClient client, ProjectionName name, DeleteProjectionOptions options, CancellationToken cancellationToken = default) {
         _ = await client.Delete(name, options, cancellationToken).ThrowOnFailureAsync().ConfigureAwait(false);
    }

    public static ValueTask DeleteAsync(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) =>
        DeleteAsync(client, name, DeleteProjectionOptions.Default, cancellationToken);

    public static async ValueTask EnableAsync(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) {
         _ = await client.Enable(name, cancellationToken).ThrowOnFailureAsync().ConfigureAwait(false);
    }

    public static async ValueTask DisableAsync(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) {
         _ = await client.Disable(name, cancellationToken).ThrowOnFailureAsync().ConfigureAwait(false);
    }

    public static async ValueTask ResetAsync(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) {
         _ = await client.Reset(name, cancellationToken).ThrowOnFailureAsync().ConfigureAwait(false);
    }

    public static ValueTask<ProjectionSettings> GetSettingsAsync(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) =>
        client.GetSettings(name, cancellationToken).ThrowOnFailureAsync();

    public static ValueTask<ProjectionDetails> GetDetailsAsync(this ProjectionsClient client, ProjectionName name, GetProjectionDetailsOptions options, CancellationToken cancellationToken = default) =>
        client.GetDetails(name, options, cancellationToken).ThrowOnFailureAsync();

    public static ValueTask<ProjectionDetails> GetDetailsAsync(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) =>
        GetDetailsAsync(client, name, GetProjectionDetailsOptions.Default, cancellationToken);

    public static ValueTask<List<ProjectionDetails>> ListAsync(this ProjectionsClient client, ListProjectionsOptions options, CancellationToken cancellationToken = default) =>
        client.List(options, cancellationToken).ThrowOnFailureAsync();

    public static ValueTask<List<ProjectionDetails>> ListAsync(this ProjectionsClient client, CancellationToken cancellationToken = default) =>
        ListAsync(client, ListProjectionsOptions.Default, cancellationToken);

    public static ValueTask<T> GetStateAsync<T>(this ProjectionsClient client, ProjectionName name,  ProjectionPartition partition, JsonSerializerOptions serializerOptions, CancellationToken cancellationToken = default) where T : notnull =>
        client.GetState<T>(name, partition, serializerOptions, cancellationToken).ThrowOnFailureAsync();

    public static ValueTask<T> GetStateAsync<T>(this ProjectionsClient client, ProjectionName name, ProjectionPartition partition, CancellationToken cancellationToken = default) where T : notnull =>
        GetStateAsync<T>(client, name, partition, JsonSerializerOptions.Default, cancellationToken);

    public static ValueTask<T> GetStateAsync<T>(this ProjectionsClient client, ProjectionName name, CancellationToken cancellationToken = default) where T : notnull =>
        GetStateAsync<T>(client, name, ProjectionPartition.None, JsonSerializerOptions.Default, cancellationToken);

    public static async ValueTask RestartSubsystemAsync(this ProjectionsClient client, CancellationToken cancellationToken = default) {
        _ = await client.RestartSubsystem(cancellationToken).ThrowOnFailureAsync().ConfigureAwait(false);
    }
}
