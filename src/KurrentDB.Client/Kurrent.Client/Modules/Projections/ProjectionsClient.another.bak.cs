// #pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
//
// // ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
//
// using System.Net;
// using System.Net.Http.Json;
// using System.Text;
// using System.Text.Json;
// using EventStore.Client;
// using Google.Protobuf;
// using Grpc.Core;
// using KurrentDB.Protocol.Projections.V1;
// using Microsoft.Extensions.Logging;
// using Polly;
// using ProjectionsServiceClient = KurrentDB.Protocol.Projections.V1.Projections.ProjectionsClient;
//
// namespace Kurrent.Client.Projections;
//
// public sealed class ProjectionsClient : ClientModuleBase {
//     internal ProjectionsClient(KurrentClient client) : base(client) {
//         ServiceClient = new(client.LegacyCallInvoker);
//     }
//
//     ProjectionsServiceClient ServiceClient { get; }
//
//     static readonly ResiliencePropertyKey<Exception> ExceptionKey = new("LastException");
//     static readonly ResiliencePropertyKey<Success>   SuccessKey   = new("Success");
//
//     public async ValueTask<Result<Success, CreateProjectionError>> Create(ProjectionName name, ProjectionDefinition definition, ProjectionSettings settings, bool autoStart, CancellationToken cancellationToken = default) {
//         name.ThrowIfNone();
//         definition.ThrowIfInvalid();
//         settings.EnsureValid();
//
//         // EMOTIONAL DAMAGE!
//         // This is a workaround for the fact that the v1 projections protocol does not
//         // support configuration of projections and disabling auto-start.
//         // The v2 protocol does, but it is not yet implemented in the server.
//
//         try {
//             // create the projection
//             var content = new ByteArrayContent(Encoding.UTF8.GetBytes(definition))
//                 .With(x => x.Headers.ContentType = new("application/javascript"));
//
//             await BackdoorClient
//                 .PostAsync($"/projections/continuous?type=JS&name={name}&enabled=false", content, cancellationToken)
//                 .ConfigureAwait(false);
//
//             // configure settings
//             if (settings != ProjectionSettings.Default)
//                 await BackdoorClient
//                     .PutAsJsonAsync($"/projection/{name}/config", settings, cancellationToken)
//                     .ConfigureAwait(false);
//
//             // start projection if requested
//             if (autoStart)
//                 await BackdoorClient
//                     .PostAsync($"/projection/{name}/command/enable", null, cancellationToken)
//                     .ConfigureAwait(false);
//
//             return Success.Instance;
//         }
//         catch (HttpRequestException hex) {
//             return Result.Failure<Success, CreateProjectionError>(hex.StatusCode switch {
//                 HttpStatusCode.Unauthorized => new ErrorDetails.AccessDenied(),
//                 HttpStatusCode.Conflict     => new ErrorDetails.AlreadyExists(),
//                 // HttpStatusCode.RequestTimeout => new ErrorDetails.Timeout(),
//                 // HttpStatusCode.Unknown when rex.Status.Detail.Contains("Operation is not valid") => new ErrorDetails.FailedPrecondition(),
//                 _                           => throw hex.WithOriginalCallStack()
//             });
//         }
//     }
//
//     public async ValueTask<Result<Success, UpdateProjectionDefinitionError>> UpdateDefinition(ProjectionName name, ProjectionDefinition definition, CancellationToken cancellationToken = default) {
//         name.ThrowIfNone();
//         definition.ThrowIfInvalid();
//
//         try {
//             var request = new UpdateReq {
//                 Options = new() {
//                     Name          = name,
//                     NoEmitOptions = new Empty(),
//                     Query         = definition
//                 },
//             };
//
//             await ServiceClient
//                 .UpdateAsync(request, cancellationToken: cancellationToken)
//                 .ConfigureAwait(false);
//
//             return Success.Instance;
//         }
//         catch (RpcException rex) {
//             return Result.Failure<Success, UpdateProjectionDefinitionError>(rex.StatusCode switch {
//                 StatusCode.PermissionDenied                                                  => new ErrorDetails.AccessDenied(),
//                 StatusCode.NotFound                                                          => new ErrorDetails.NotFound(),
//                 StatusCode.Unknown when rex.Status.Detail.Contains("Operation is not valid") => new ErrorDetails.FailedPrecondition(),
//                 _                                                                            => throw rex.WithOriginalCallStack()
//             });
//         }
//     }
//
//     public async ValueTask<Result<Success, UpdateProjectionSettingsError>> UpdateSettings(ProjectionName name, ProjectionSettings settings, CancellationToken cancellationToken = default) {
//         name.ThrowIfNone();
//         settings.EnsureValid();
//
//         // EMOTIONAL DAMAGE!
//         // This is a workaround for the fact that the v1 projections protocol does not
//         // support updating projection settings.
//         // The v2 protocol does, but it is not yet implemented in the server.
//
//         try {
//             await BackdoorClient
//                 .PutAsJsonAsync($"/projection/{name}/config", settings, cancellationToken)
//                 .ConfigureAwait(false);
//
//             return Success.Instance;
//         }
//         catch (HttpRequestException rex) {
//             return Result.Failure<Success, UpdateProjectionSettingsError>(rex.StatusCode switch {
//                 HttpStatusCode.Unauthorized => new ErrorDetails.AccessDenied(),
//                 HttpStatusCode.NotFound     => new ErrorDetails.NotFound(),
//                 // HttpStatusCode.Unknown when rex.Status.Detail.Contains("Operation is not valid") => new ErrorDetails.FailedPrecondition(),
//                 _                           => throw rex.WithOriginalCallStack()
//             });
//         }
//     }
//
//     public async ValueTask<Result<Success, DeleteProjectionError>> Delete(ProjectionName name, DeleteProjectionOptions options, CancellationToken cancellationToken = default) {
//         name.ThrowIfNone();
//
//         // TODO SS: I actually truly believe we should only keep the state.
//
//         try {
//             var request = new DeleteReq {
//                 Options = new() {
//                     Name                   = name,
//                     DeleteStateStream      = options.DeleteStateStream,
//                     DeleteCheckpointStream = options.DeleteCheckpointStream,
//                     DeleteEmittedStreams   = options.DeleteEmittedStreams,
//                 }
//             };
//
//             await ServiceClient
//                 .DeleteAsync(request, cancellationToken: cancellationToken)
//                 .ConfigureAwait(false);
//
//             return Success.Instance;
//         }
//         catch (RpcException rex) {
//             return Result.Failure<Success, DeleteProjectionError>(rex.StatusCode switch {
//                 StatusCode.PermissionDenied                                           => new ErrorDetails.AccessDenied(),
//                 StatusCode.NotFound                                                   => new ErrorDetails.NotFound(),
//                 StatusCode.Unknown when rex.Status.Detail.Contains("OperationFailed") => new ErrorDetails.FailedPrecondition(),
//                 _                                                                     => throw rex.WithOriginalCallStack()
//             });
//         }
//     }
//
//     public async ValueTask<Result<Success, EnableProjectionError>> Enable(ProjectionName name, CancellationToken cancellationToken = default) {
//         name.ThrowIfNone();
//
//         try {
//             var request = new EnableReq { Options = new() { Name = name } };
//
//             await ServiceClient
//                 .EnableAsync(request, cancellationToken: cancellationToken)
//                 .ConfigureAwait(false);
//
//             return Success.Instance;
//         }
//         catch (RpcException rex) {
//             return Result.Failure<Success, EnableProjectionError>(
//                 rex.StatusCode switch {
//                     StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//                     StatusCode.NotFound         => new ErrorDetails.NotFound(),
//                     _                           => throw rex.WithOriginalCallStack()
//                 }
//             );
//         }
//     }
//
//     public async ValueTask<Result<Success, DisableProjectionError>> Disable(ProjectionName name, CancellationToken cancellationToken = default) {
//         name.ThrowIfNone();
//
//         try {
//             var request = new DisableReq {
//                 Options = new() {
//                     Name            = name,
//                     WriteCheckpoint = true
//                 }
//             };
//
//             await ServiceClient
//                 .DisableAsync(request, cancellationToken: cancellationToken)
//                 .ConfigureAwait(false);
//
//             return Success.Instance;
//         }
//         catch (RpcException rex) {
//             return Result.Failure<Success, DisableProjectionError>(rex.StatusCode switch {
//                 StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//                 StatusCode.NotFound         => new ErrorDetails.NotFound(),
//                 _                           => throw rex.WithOriginalCallStack()
//             });
//         }
//     }
//
//     /// <summary>
//     /// Resets a projection. This will re-emit events. Streams that are written to, from the projection, will also be soft deleted.
//     /// </summary>
//     public async ValueTask<Result<Success, ResetProjectionError>> Reset(ProjectionName name, CancellationToken cancellationToken = default) {
//         name.ThrowIfNone();
//
//         try {
//             var request = new ResetReq {
//                 Options = new() {
//                     Name            = name,
//                     WriteCheckpoint = true
//                 }
//             };
//
//             await ServiceClient
//                 .ResetAsync(request, cancellationToken: cancellationToken)
//                 .ConfigureAwait(false);
//
//             return Success.Instance;
//         }
//         catch (RpcException rex) {
//             return Result.Failure<Success, ResetProjectionError>(rex.StatusCode switch {
//                 StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//                 StatusCode.NotFound         => new ErrorDetails.NotFound(),
//                 _                           => throw rex.WithOriginalCallStack()
//             });
//         }
//     }
//
//     public async ValueTask<Result<ProjectionSettings, GetProjectionSettingsError>> GetSettings(ProjectionName name, CancellationToken cancellationToken = default) {
//         name.ThrowIfNone();
//
//         // EMOTIONAL DAMAGE!
//         // This is a workaround for the fact that the v1 projections protocol does not
//         // support getting projection settings.
//         // The v2 protocol does, but it is not yet implemented in the server.
//
//         try {
//             return await BackdoorClient
//                 .GetFromJsonAsync<ProjectionSettings>($"/projection/{name}/config", cancellationToken)
//                 .ConfigureAwait(false) ?? ProjectionSettings.Unspecified;
//         }
//         catch (HttpRequestException rex) {
//             return Result.Failure<ProjectionSettings, GetProjectionSettingsError>(rex.StatusCode switch {
//                 HttpStatusCode.Unauthorized => new ErrorDetails.AccessDenied(),
//                 HttpStatusCode.NotFound     => new ErrorDetails.NotFound(),
//                 // HttpStatusCode.Unknown when rex.Status.Detail.Contains("Operation is not valid") => new ErrorDetails.FailedPrecondition(),
//                 _                                                                                => throw rex.WithOriginalCallStack()
//             });
//         }
//     }
//
//     public async ValueTask<Result<ProjectionDetails, GetProjectionDetailsError>> GetDetails(ProjectionName name, GetProjectionDetailsOptions options, CancellationToken cancellationToken = default) {
//         name.ThrowIfNone();
//
//         // EMOTIONAL DAMAGE!
//         // At this point we might as well just use the backdoor HTTP API...
//
//         try {
//             var response = await BackdoorClient
//                 .GetFromJsonAsync<ProjectionsHttpModel.GetProjectionsResponse>($"/projection/{name}/statistics", cancellationToken)
//                 .ConfigureAwait(false);
//
//             if (response.Projections.Length == 0)
//                 return Result.Failure<ProjectionDetails, GetProjectionDetailsError>(new ErrorDetails.NotFound());
//
//             var details = response.Projections[0].MapToProjectionDetails(includeStatistics: options.IncludeStatistics);
//
//             if (details.Status == ProjectionStatus.Deleting || options is { IncludeDefinition: false, IncludeSettings: false })
//                 return details;
//
//             if (options.IncludeDefinition && details.Type != ProjectionType.System)
//                 details = details with {
//                     Definition = await BackdoorClient
//                         .GetStringAsync($"/projection/{name}/query", cancellationToken)
//                         .ConfigureAwait(false)
//                 };
//
//             if (options.IncludeSettings)
//                 details = details with {
//                     Settings = await BackdoorClient
//                         .GetFromJsonAsync<ProjectionSettings>($"/projection/{name}/config", cancellationToken)
//                         .ConfigureAwait(false) ?? ProjectionSettings.Unspecified
//                 };
//
//             return details;
//         }
//         catch (HttpRequestException hex) {
//             return Result.Failure<ProjectionDetails, GetProjectionDetailsError>(hex.StatusCode switch {
//                 HttpStatusCode.Unauthorized => new ErrorDetails.AccessDenied(),
//                 HttpStatusCode.NotFound     => new ErrorDetails.NotFound(),
//                 // HttpStatusCode.Unknown when rex.Status.Detail.Contains("Operation is not valid") => new ErrorDetails.FailedPrecondition(),
//                 _                           => throw hex.WithOriginalCallStack()
//             });
//         }
//     }
//
//     public async ValueTask<Result<List<ProjectionDetails>, ListProjectionsError>> List(ListProjectionsOptions options, CancellationToken cancellationToken = default) {
//         // EMOTIONAL DAMAGE!
//         // At this point we might as well just use the backdoor HTTP API...
//
//         // TODO SS: We should probably remove the mode filter and just return all projections.
//
//         var route = options.Mode switch {
//             ProjectionMode.Unspecified => "/projections/any",
//             ProjectionMode.OneTime     => "/projections/onetime",
//             ProjectionMode.Continuous  => "/projections/continuous",
//             ProjectionMode.Transient   => "/projections/transient"
//         };
//
//         try {
//             var response = await BackdoorClient
//                 .GetFromJsonAsync<ProjectionsHttpModel.GetProjectionsResponse>(route, cancellationToken)
//                 .ConfigureAwait(false);
//
//             if (response.Projections.Length == 0)
//                 return Result.Success<List<ProjectionDetails>, ListProjectionsError>([]);
//
//             var projections = response.Projections
//                 .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
//                 .Select(x => x.MapToProjectionDetails(options.IncludeStatistics))
//                 .Where(x => options.Type == ProjectionType.Unspecified || x.Type == options.Type);
//
//             if (options is { IncludeDefinition: false, IncludeSettings: false })
//                 return projections.ToList();
//
//             return await projections.ToAsyncEnumerable()
//                 .SelectAwaitWithCancellation(EnrichWithDefinition(BackdoorClient, options.IncludeDefinition))
//                 .SelectAwaitWithCancellation(EnrichWithSettings(BackdoorClient, options.IncludeSettings))
//                 .ToListAsync(cancellationToken)
//                 .ConfigureAwait(false);
//         }
//         catch (HttpRequestException hex) {
//             return Result.Failure<List<ProjectionDetails>, ListProjectionsError>(hex.StatusCode switch {
//                 HttpStatusCode.Unauthorized => new ErrorDetails.AccessDenied(),
//                 _                           => throw hex.WithOriginalCallStack()
//             });
//         }
//
//         Func<ProjectionDetails, CancellationToken, ValueTask<ProjectionDetails>> EnrichWithDefinition(HttpClient client, bool includeDefinition) {
//             return async (projection, ct) => {
//                 try {
//                     return includeDefinition && projection.Type != ProjectionType.System && projection.Status != ProjectionStatus.Deleting
//                         ? projection with {
//                             Definition = await client
//                                 .GetStringAsync($"/projection/{projection.Name}/query", ct)
//                                 .ConfigureAwait(false)
//                         }
//                         : projection;
//                 }
//                 catch (Exception ex) {
//                     Logger.LogError(ex, "Failed to enrich projection settings for {ProjectionName}", projection.Name);
//                     throw;
//                 }
//             };
//         }
//
//         Func<ProjectionDetails, CancellationToken, ValueTask<ProjectionDetails>> EnrichWithSettings(HttpClient client, bool includeSettings) {
//             return async (projection, ct) => {
//                 try {
//                     return includeSettings && projection.Status != ProjectionStatus.Deleting
//                         ? projection with {
//                             Settings = await client
//                                 .GetFromJsonAsync<ProjectionSettings>($"/projection/{projection.Name}/config", ct)
//                                 .ConfigureAwait(false)!
//                         }
//                         : projection;
//                 }
//                 catch (Exception ex) {
//                     Logger.LogError(ex, "Failed to enrich projection settings for {ProjectionName}", projection.Name);
//                     throw;
//                 }
//             };
//         }
//     }
//
//     public async ValueTask<Result<T, GetProjectionStateError>> GetState<T>(
//         ProjectionName name,
//         ProjectionPartition partition,
//         JsonSerializerOptions serializerOptions,
//         CancellationToken cancellationToken = default
//     ) where T : notnull {
//         name.ThrowIfNone();
//
//         try {
//             var request = new StateReq {
//                 Options = new() {
//                     Name      = name,
//                     Partition = partition
//                 }
//             };
//
//             var response = await ServiceClient
//                 .StateAsync(request, cancellationToken: cancellationToken)
//                 .ConfigureAwait(false);
//
//             var json = JsonFormatter.Default.Format(response.State);
//
//             return JsonSerializer.Deserialize<T>(json, serializerOptions)
//                 ?? throw new JsonException($"Failed to deserialize projection state for '{name}' with partition '{partition}'");
//         }
//         catch (RpcException rex) {
//             return Result.Failure<T, GetProjectionStateError>(rex.StatusCode switch {
//                 StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//                 StatusCode.NotFound         => new ErrorDetails.NotFound(),
//                 _                           => throw rex.WithOriginalCallStack()
//             });
//         }
//     }
//
//     public async ValueTask<Result<Success, RestartProjectionSubsystemError>> RestartSubsystem(CancellationToken cancellationToken = default) {
//         try {
//             await ServiceClient
//                 .RestartSubsystemAsync(new(), cancellationToken: cancellationToken)
//                 .ConfigureAwait(false);
//
//             return Success.Instance;
//         }
//         catch (RpcException rex) {
//             return Result.Failure<Success, RestartProjectionSubsystemError>(rex.StatusCode switch {
//                 StatusCode.PermissionDenied   => new ErrorDetails.AccessDenied(),
//                 StatusCode.FailedPrecondition => new ErrorDetails.ProjectionsSubsystemRestartFailed(),
//                 _                             => throw rex.WithOriginalCallStack()
//             });
//         }
//     }
// }
