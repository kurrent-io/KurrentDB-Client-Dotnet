// #pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
//
// // ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
//
// using System.Net;
// using System.Net.Http.Json;
// using System.Text;
// using System.Text.Json;
// using Google.Protobuf;
// using Grpc.Core;
// using KurrentDB.Protocol.Projections.V1;
// using ProjectionsServiceClient = KurrentDB.Protocol.Projections.V1.Projections.ProjectionsClient;
//
// namespace Kurrent.Client.Projections;
//
// public sealed class ProjectionsClient {
//     internal ProjectionsClient(KurrentClient source) {
//         ServiceClient         = new(source.LegacyCallInvoker);
//         GetBackdoorHttpClient = source.GetBackdoorHttpClient;
//     }
//
//     ProjectionsServiceClient ServiceClient         { get; }
//     Func<HttpClient>         GetBackdoorHttpClient { get; }
//
//     // public async ValueTask<Result<Success, CreateProjectionError>> CreateProjectionGrpcLOL(ProjectionName name, ProjectionDefinition definition, ProjectionSettings settings, CancellationToken cancellationToken = default) {
//     //     name.ThrowIfNone();
//     //     definition.ThrowIfInvalid();
//     //     settings.EnsureValid();
//     //
//     //     try {
//     //         var request = new CreateReq {
//     //             Options = new() {
//     //                 Continuous = new() {
//     //                     Name                = name,
//     //                     EmitEnabled         = settings.EmitEnabled,
//     //                     TrackEmittedStreams = settings.TrackEmittedStreams
//     //                 },
//     //                 Query = definition
//     //             }
//     //         };
//     //
//     //         await ServiceClient
//     //             .CreateAsync(request, cancellationToken: cancellationToken)
//     //             .ConfigureAwait(false);
//     //
//     //         return Success.Instance;
//     //     }
//     //     catch (RpcException rex) {
//     //         return Result.Failure<Success, CreateProjectionError>(rex.StatusCode switch {
//     //             StatusCode.PermissionDenied                                    => new ErrorDetails.AccessDenied(),
//     //             StatusCode.Unknown when rex.Status.Detail.Contains("Conflict") => new ErrorDetails.AlreadyExists(),
//     //             _                                                              => throw rex.WithOriginalCallStack()
//     //         });
//     //     }
//     // }
//
//     public async ValueTask<Result<Success, CreateProjectionError>> CreateProjection(ProjectionName name, ProjectionDefinition definition, ProjectionSettings settings, bool autoStart, CancellationToken cancellationToken = default) {
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
//             var client = GetBackdoorHttpClient();
//
//             // create the projection
//
//             var content = new ByteArrayContent(Encoding.UTF8.GetBytes(definition));
//
//             content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/javascript");
//
//             var response = await client
//                 .PostAsync($"/projections/continuous?type=JS&name={name}&enabled=false", content, cancellationToken)
//                 .ConfigureAwait(false);
//
//             response.EnsureSuccessStatusCode();
//
//             // configure settings
//             response = await client
//                 .PutAsJsonAsync($"/projection/{name}/config", settings, cancellationToken)
//                 .ConfigureAwait(false);
//
//             response.EnsureSuccessStatusCode();
//
//             // start projection if requested
//             if (autoStart) {
//                 response = await client
//                     .PostAsync($"/projection/{name}/command/enable", null, cancellationToken)
//                     .ConfigureAwait(false);
//
//                 response.EnsureSuccessStatusCode();
//             }
//
//             return Success.Instance;
//         }
//         catch (HttpRequestException rex) {
//             return Result.Failure<Success, CreateProjectionError>(rex.StatusCode switch {
//                 HttpStatusCode.Unauthorized => new ErrorDetails.AccessDenied(),
//                 HttpStatusCode.NotFound     => new ErrorDetails.AlreadyExists(),
//                 // HttpStatusCode.Unknown when rex.Status.Detail.Contains("Operation is not valid") => new ErrorDetails.FailedPrecondition(),
//                 _                                                                                => throw rex.WithOriginalCallStack()
//             });
//         }
//     }
//
//     public async ValueTask<Result<Success, UpdateProjectionDefinitionError>> UpdateProjectionDefinition(ProjectionName name, ProjectionDefinition definition, CancellationToken cancellationToken = default) {
//         name.ThrowIfNone();
//         definition.ThrowIfInvalid();
//
//         // EMOTIONAL DAMAGE!
//         // At this point v1 protocol is so bad I might as well just use the backdoor HTTP API.
//
//         try {
//             var response = await GetBackdoorHttpClient()
//                 .PutAsJsonAsync($"/projection/{name}/query", definition, cancellationToken)
//                 .ConfigureAwait(false);
//
//             response.EnsureSuccessStatusCode();
//
//             return Success.Instance;
//         }
//         catch (HttpRequestException rex) {
//             return Result.Failure<Success, UpdateProjectionDefinitionError>(rex.StatusCode switch {
//                 HttpStatusCode.Unauthorized => new ErrorDetails.AccessDenied(),
//                 HttpStatusCode.NotFound     => new ErrorDetails.NotFound(),
//                 // HttpStatusCode.Unknown when rex.Status.Detail.Contains("Operation is not valid") => new ErrorDetails.FailedPrecondition(),
//                 _                                                                                => throw rex.WithOriginalCallStack()
//             });
//         }
//
//         // try {
//         //     var request = new UpdateReq {
//         //         Options = new() {
//         //             Name          = name,
//         //             NoEmitOptions = new Empty(),
//         //             Query         = definition
//         //         },
//         //     };
//         //
//         //     await ServiceClient
//         //         .UpdateAsync(request, cancellationToken: cancellationToken)
//         //         .ConfigureAwait(false);
//         //
//         //     return Success.Instance;
//         // }
//         // catch (RpcException rex) {
//         //     return Result.Failure<Success, UpdateProjectionDefinitionError>(rex.StatusCode switch {
//         //         StatusCode.PermissionDenied                                                  => new ErrorDetails.AccessDenied(),
//         //         StatusCode.NotFound                                                          => new ErrorDetails.NotFound(),
//         //         StatusCode.Unknown when rex.Status.Detail.Contains("Operation is not valid") => new ErrorDetails.FailedPrecondition(),
//         //         _                                                                            => throw rex.WithOriginalCallStack()
//         //     });
//         // }
//     }
//
//     public async ValueTask<Result<ProjectionDefinition, UpdateProjectionDefinitionError>> GetProjectionDefinition(ProjectionName name, CancellationToken cancellationToken = default) {
//         name.ThrowIfNone();
//
//         // EMOTIONAL DAMAGE!
//         // This is a workaround for the fact that the v1 projections protocol does not
//         // support getting projection definition.
//         // The v2 protocol does, but it is not yet implemented in the server.
//
//         try {
//             return await GetBackdoorHttpClient()
//                 .GetFromJsonAsync<ProjectionDefinition>($"/projection/{name}/query", cancellationToken)
//                 .ConfigureAwait(false);
//         }
//         catch (HttpRequestException rex) {
//             return Result.Failure<ProjectionDefinition, UpdateProjectionDefinitionError>(rex.StatusCode switch {
//                 HttpStatusCode.Unauthorized => new ErrorDetails.AccessDenied(),
//                 HttpStatusCode.NotFound     => new ErrorDetails.NotFound(),
//                 // HttpStatusCode.Unknown when rex.Status.Detail.Contains("Operation is not valid") => new ErrorDetails.FailedPrecondition(),
//                 _                                                                                => throw rex.WithOriginalCallStack()
//             });
//         }
//     }
//
//     public async ValueTask<Result<Success, UpdateProjectionSettingsError>> UpdateProjectionSettings(ProjectionName name, ProjectionSettings settings, CancellationToken cancellationToken = default) {
//         name.ThrowIfNone();
//         settings.EnsureValid();
//
//         // EMOTIONAL DAMAGE!
//         // This is a workaround for the fact that the v1 projections protocol does not
//         // support updating projection settings.
//         // The v2 protocol does, but it is not yet implemented in the server.
//
//         try {
//             var response = await GetBackdoorHttpClient()
//                 .PutAsJsonAsync($"/projection/{name}/config", settings, cancellationToken)
//                 .ConfigureAwait(false);
//
//             response.EnsureSuccessStatusCode();
//
//             return Success.Instance;
//         }
//         catch (HttpRequestException rex) {
//             return Result.Failure<Success, UpdateProjectionSettingsError>(rex.StatusCode switch {
//                 HttpStatusCode.Unauthorized                                                      => new ErrorDetails.AccessDenied(),
//                 HttpStatusCode.NotFound                                                          => new ErrorDetails.NotFound(),
//                 // HttpStatusCode.Unknown when rex.Status.Detail.Contains("Operation is not valid") => new ErrorDetails.FailedPrecondition(),
//                 _                                                                                => throw rex.WithOriginalCallStack()
//             });
//         }
//     }
//
//     public async ValueTask<Result<ProjectionSettings, UpdateProjectionSettingsError>> GetProjectionSettings(ProjectionName name, CancellationToken cancellationToken = default) {
//         name.ThrowIfNone();
//
//         // EMOTIONAL DAMAGE!
//         // This is a workaround for the fact that the v1 projections protocol does not
//         // support getting projection settings.
//         // The v2 protocol does, but it is not yet implemented in the server.
//
//         try {
//             return await GetBackdoorHttpClient()
//                 .GetFromJsonAsync<ProjectionSettings>($"/projection/{name}/config", cancellationToken)
//                 .ConfigureAwait(false)!;
//         }
//         catch (HttpRequestException rex) {
//             return Result.Failure<ProjectionSettings, UpdateProjectionSettingsError>(rex.StatusCode switch {
//                 HttpStatusCode.Unauthorized => new ErrorDetails.AccessDenied(),
//                 HttpStatusCode.NotFound     => new ErrorDetails.NotFound(),
//                 // HttpStatusCode.Unknown when rex.Status.Detail.Contains("Operation is not valid") => new ErrorDetails.FailedPrecondition(),
//                 _                                                                                => throw rex.WithOriginalCallStack()
//             });
//         }
//     }
//
//     public async ValueTask<Result<Success, DeleteProjectionError>> DeleteProjection(ProjectionName name, DeleteProjectionOptions options, CancellationToken cancellationToken = default) {
//         name.ThrowIfNone();
//
//         try {
//             var request = new DeleteReq {
//                 Options = new() {
//                     Name                   = name,
//                     DeleteStateStream      = options.DeleteStateStream,
//                     DeleteCheckpointStream = options.DeleteCheckpointStream,
//                     DeleteEmittedStreams   = options.DeleteCheckpointStream,
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
//     public async ValueTask<Result<Success, EnableProjectionError>> EnableProjection(ProjectionName name, CancellationToken cancellationToken = default) {
//         name.ThrowIfNone();
//
//         try {
//             var request = new EnableReq {
//                 Options = new() {
//                     Name = name
//                 }
//             };
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
//     /// <summary>
//     /// Disables a projection. Saves the projection's checkpoint.
//     /// </summary>
//     public async ValueTask<Result<Success, DisableProjectionError>> DisableProjection(ProjectionName name, CancellationToken cancellationToken = default) {
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
//     public async ValueTask<Result<Success, ResetProjectionError>> ResetProjection(ProjectionName name, CancellationToken cancellationToken = default) {
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
//     /// <summary>
//     /// Aborts a projection. Does not save the projection's checkpoint.
//     /// </summary>
//     /// <param name="name"></param>
//     /// <param name="cancellationToken"></param>
//     /// <returns></returns>
//     public async ValueTask<Result<Success, AbortProjectionError>> AbortProjection(ProjectionName name, CancellationToken cancellationToken = default) {
//         name.ThrowIfNone();
//
//         try {
//             var request = new DisableReq {
//                 Options = new() {
//                     Name            = name,
//                     WriteCheckpoint = false
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
//             return Result.Failure<Success, AbortProjectionError>(rex.StatusCode switch {
//                 StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//                 StatusCode.NotFound         => new ErrorDetails.NotFound(),
//                 _                           => throw rex.WithOriginalCallStack()
//             });
//         }
//     }
//
//     // public async ValueTask<Result<ProjectionDetails, GetProjectionError>> GetProjectionGRPC_LOL(ProjectionName name, CancellationToken cancellationToken = default) {
//     //     name.ThrowIfNone();
//     //
//     //     try {
//     //         var request = new StatisticsReq {
//     //             Options =  new() {
//     //                 Name = name
//     //             }
//     //         };
//     //
//     //         using var call = ServiceClient.Statistics(request, cancellationToken: cancellationToken);
//     //
//     //         var result = await call.ResponseStream
//     //             .ReadAllAsync(cancellationToken)
//     //             .Select(static rsp => rsp.MapProjectionDetails(includeStatistics: true))
//     //             .FirstAsync(cancellationToken)
//     //             .ConfigureAwait(false);
//     //
//     //         // get definition
//     //
//     //         // get settings
//     //
//     //
//     //         return result;
//     //     }
//     //     catch (RpcException rex) {
//     //         return Result.Failure<ProjectionDetails, GetProjectionError>(rex.StatusCode switch {
//     //             StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//     //             StatusCode.NotFound         => new ErrorDetails.NotFound(),
//     //             _                           => throw rex.WithOriginalCallStack()
//     //         });
//     //     }
//     // }
//
//     public record GetProjectionOptions {
//         public bool IncludeDefinition { get; init; } = true;
//         public bool IncludeSettings   { get; init; } = true;
//         public bool IncludeStatistics { get; init; } = true;
//     }
//
//     // public async ValueTask<Result<ProjectionDetails, GetProjectionError>> GetProjection(ProjectionName name, CancellationToken cancellationToken = default) {
//     //     name.ThrowIfNone();
//     //
//     //     try {
//     //         var http = GetBackdoorHttpClient();
//     //
//     //         var temp = await http.GetStringAsync($"/projections/any", cancellationToken);
//     //
//     //         var getStats      = http.GetFromJsonAsync<ProjectionsHttpModel.GetProjectionsResponse>($"/projection/{name}/statistics", cancellationToken);
//     //         var getDefinition = http.GetStringAsync($"/projection/{name}/query", cancellationToken);
//     //         var getSettings   = http.GetFromJsonAsync<ProjectionSettings>($"/projection/{name}/config", cancellationToken);
//     //
//     //         await Task.WhenAll(getStats, getDefinition, getSettings).ConfigureAwait(false);
//     //
//     //         var details = getStats.Result.Projections[0].MapToProjectionDetails(true);
//     //
//     //         ProjectionDefinition definition = getDefinition.Result;
//     //
//     //         var settings = getSettings.Result;
//     //
//     //         return details with {
//     //             Definition = definition,
//     //             Settings   = settings!,
//     //         };
//     //     }
//     //     catch (HttpRequestException rex) {
//     //         return Result.Failure<ProjectionDetails, GetProjectionError>(rex.StatusCode switch {
//     //             HttpStatusCode.Unauthorized => new ErrorDetails.AccessDenied(),
//     //             HttpStatusCode.NotFound     => new ErrorDetails.NotFound(),
//     //             // HttpStatusCode.Unknown when rex.Status.Detail.Contains("Operation is not valid") => new ErrorDetails.FailedPrecondition(),
//     //             _                                                                                => throw rex.WithOriginalCallStack()
//     //         });
//     //     }
//     // }
//
//     public async ValueTask<Result<ProjectionDetails, GetProjectionError>> GetProjection(ProjectionName name, CancellationToken cancellationToken = default) {
//         name.ThrowIfNone();
//
//         // EMOTIONAL DAMAGE!
//         // At this point we might as well just use the backdoor HTTP API...
//
//         try {
//             var client = GetBackdoorHttpClient();
//
//             var response = await client
//                 .GetFromJsonAsync<ProjectionsHttpModel.GetProjectionsResponse>($"/projection/{name}/statistics", cancellationToken)
//                 .ConfigureAwait(false);
//
//             if (response.Projections.Length == 0)
//                 return Result.Failure<ProjectionDetails, GetProjectionError>(new ErrorDetails.NotFound());
//
//             var getDefinition = client.GetStringAsync($"/projection/{name}/query", cancellationToken);
//             var getSettings   = client.GetFromJsonAsync<ProjectionSettings>($"/projection/{name}/config", cancellationToken) ;
//
//             await Task.WhenAll(getDefinition, getSettings).ConfigureAwait(false);
//
//             return response.Projections[0].MapToProjectionDetails(includeStatistics: true) with {
//                 Definition = await getDefinition,
//                 Settings   = (await getSettings)!
//             };
//         }
//         catch (HttpRequestException hex) {
//             return Result.Failure<ProjectionDetails, GetProjectionError>(hex.StatusCode switch {
//                 HttpStatusCode.Unauthorized => new ErrorDetails.AccessDenied(),
//                 HttpStatusCode.NotFound     => new ErrorDetails.NotFound(),
//                 // HttpStatusCode.Unknown when rex.Status.Detail.Contains("Operation is not valid") => new ErrorDetails.FailedPrecondition(),
//                 _                           => throw hex.WithOriginalCallStack()
//             });
//         }
//     }
//
//     public async ValueTask<Result<List<ProjectionDetails>, ListProjectionsError>> ListProjections(ListProjectionsOptions options, CancellationToken cancellationToken = default) {
//         // EMOTIONAL DAMAGE!
//         // At this point we might as well just use the backdoor HTTP API...
//
//         var route = options.Mode switch {
//             ProjectionMode.Unspecified => "/projections/any",
//             ProjectionMode.OneTime     => "/projections/onetime",
//             ProjectionMode.Continuous  => "/projections/continuous",
//             ProjectionMode.Transient   => "/projections/transient"
//         };
//
//         try {
//             var client = GetBackdoorHttpClient();
//
//             var response = await client
//                 .GetFromJsonAsync<ProjectionsHttpModel.GetProjectionsResponse>(route, cancellationToken)
//                 .ConfigureAwait(false);
//
//             if (response.Projections.Length == 0)
//                 return Result.Success<List<ProjectionDetails>, ListProjectionsError>([]);
//
//             var projections = options.IncludeSystemProjections
//                 ? response.Projections
//                     .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
//                     .Select(x => x.MapToProjectionDetails(options.IncludeStatistics))
//                 : response.Projections
//                     .Where(static x => !x.Name.StartsWith('$'))
//                     .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
//                     .Select(x => x.MapToProjectionDetails(options.IncludeStatistics));
//
//             if (options is { IncludeDefinition: false, IncludeSettings: false })
//                 return projections.ToList();
//
//             return await projections
//                 .ToAsyncEnumerable()
//                 .SelectAwait(EnrichDetails(client, options, cancellationToken))
//                 .ToListAsync(cancellationToken);
//         }
//         catch (HttpRequestException hex) {
//             return Result.Failure<List<ProjectionDetails>, ListProjectionsError>(hex.StatusCode switch {
//                 HttpStatusCode.Unauthorized => new ErrorDetails.AccessDenied(),
//                 _                           => throw hex.WithOriginalCallStack()
//             });
//         }
//
//         static Func<ProjectionDetails, ValueTask<ProjectionDetails>> EnrichDetails(HttpClient client, ListProjectionsOptions options, CancellationToken ct) {
//             return async projection => {
//                 if (options.IncludeDefinition)
//                     projection = projection with {
//                         Definition = await client
//                             .GetStringAsync($"/projection/{projection.Name}/query", ct)
//                             .ConfigureAwait(false)
//                     };
//
//                 if (options.IncludeSettings)
//                     projection = projection with {
//                         Settings = await client
//                             .GetFromJsonAsync<ProjectionSettings>($"/projection/{projection.Name}/config", ct)
//                             .ConfigureAwait(false)!
//                     };
//
//                 return projection;
//             };
//         }
//     }
//
//     // public async ValueTask<Result<List<ProjectionDetails>, ListProjectionsError>> ListProjections(ListProjectionsOptions options, CancellationToken cancellationToken = default) {
//     //     // EMOTIONAL DAMAGE!
//     //     // At this point we might as well just use the backdoor HTTP API...
//     //
//     //     var route = options.Mode switch {
//     //         ProjectionMode.Unspecified => "/projections/any",
//     //         ProjectionMode.OneTime     => "/projections/onetime",
//     //         ProjectionMode.Continuous  => "/projections/continuous",
//     //         ProjectionMode.Transient   => "/projections/transient"
//     //     };
//     //
//     //     try {
//     //         var http = GetBackdoorHttpClient();
//     //
//     //         var response = await http
//     //             .GetFromJsonAsync<ProjectionsHttpModel.GetProjectionsResponse>(route, cancellationToken)
//     //             .ConfigureAwait(false);
//     //
//     //         if (response.Projections.Length == 0)
//     //             return new List<ProjectionDetails>();
//     //
//     //         if (!options.IncludeSystemProjections && response.Projections.Length != 0) {
//     //             response = new(response.Projections.Where(x => !x.Name.StartsWith('$')).ToArray());
//     //
//     //             if (response.Projections.Length == 0)
//     //                 return new List<ProjectionDetails>();
//     //         }
//     //
//     //         var result = await response.Projections
//     //             .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
//     //             .ToAsyncEnumerable()
//     //             .SelectAwait((projection, idx) => ProcessProjection(projection, http))
//     //             .ToListAsync(cancellationToken);
//     //
//     //         return result;
//     //     }
//     //     catch (HttpRequestException hex) {
//     //         return Result.Failure<List<ProjectionDetails>, ListProjectionsError>(hex.StatusCode switch {
//     //             HttpStatusCode.Unauthorized => new ErrorDetails.AccessDenied(),
//     //             _                           => throw hex.WithOriginalCallStack()
//     //         });
//     //     }
//     //
//     //
//     //     async ValueTask<ProjectionDetails> ProcessProjection(ProjectionsHttpModel.Projection projection, HttpClient http) {
//     //         var details = projection.MapToProjectionDetails(options.IncludeStatistics);
//     //
//     //         if (options.IncludeDefinition) {
//     //             ProjectionDefinition definition = await http
//     //                 .GetStringAsync($"/projection/{projection.Name}/query", cancellationToken)
//     //                 .ConfigureAwait(false);
//     //
//     //             details = details with { Definition = definition };
//     //         }
//     //
//     //         if (options.IncludeSettings) {
//     //             var settings = await http
//     //                 .GetFromJsonAsync<ProjectionSettings>($"/projection/{projection.Name}/config", cancellationToken)
//     //                 .ConfigureAwait(false)!;
//     //
//     //             details = details with { Settings = settings };
//     //         }
//     //
//     //         return details;
//     //     }
//     // }
//
//     // public async ValueTask<Result<List<ProjectionDetails>, ListProjectionsError>> ListProjections(ListProjectionsOptions options, CancellationToken cancellationToken = default) {
//     //     var request = new StatisticsReq {
//     //         Options = options.Mode switch {
//     //             ProjectionMode.Unspecified => new() { All        = new() },
//     //             ProjectionMode.OneTime     => new() { OneTime    = new() },
//     //             ProjectionMode.Continuous  => new() { Continuous = new() },
//     //             ProjectionMode.Transient   => new() { Transient  = new() }
//     //         }
//     //     };
//     //
//     //     try {
//     //         using var call = ServiceClient.Statistics(request, cancellationToken: cancellationToken);
//     //
//     //         return await call.ResponseStream
//     //             .ReadAllAsync(cancellationToken)
//     //             .SelectAwait(async rsp => {
//     //                 var details = rsp.MapProjectionDetails(options.IncludeStatistics);
//     //
//     //                 if (options.IncludeDefinition) {
//     //                     var definition = await GetProjectionDefinition(details.Name, cancellationToken)
//     //                         .ThrowOnFailureAsync()
//     //                         .ConfigureAwait(false);
//     //
//     //                     details = details with { Definition = definition };
//     //                 }
//     //
//     //                 if (options.IncludeSettings) {
//     //                     var settings =  await GetProjectionSettings(details.Name, cancellationToken)
//     //                         .ThrowOnFailureAsync()
//     //                         .ConfigureAwait(false);
//     //
//     //                     details = details with { Settings = settings };
//     //                 }
//     //
//     //                 return details;
//     //             })
//     //             .ToListAsync(cancellationToken)
//     //             .ConfigureAwait(false);
//     //     }
//     //     catch (RpcException rex) {
//     //         return Result.Failure<List<ProjectionDetails>, ListProjectionsError>(
//     //             rex.StatusCode switch {
//     //                 StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//     //                 _                           => throw rex.WithOriginalCallStack()
//     //             }
//     //         );
//     //     }
//     // }
//
//
//     // public async ValueTask<Result<ProjectionDetails, GetProjectionError>> GetProjectionGRPC_LOL(ProjectionName name, CancellationToken cancellationToken = default) {
//     //     name.ThrowIfNone();
//     //
//     //     try {
//     //         var request = new StatisticsReq {
//     //             Options =  new() {
//     //                 Name = name
//     //             }
//     //         };
//     //
//     //         using var call = ServiceClient.Statistics(request, cancellationToken: cancellationToken);
//     //
//     //         var result = await call.ResponseStream
//     //             .ReadAllAsync(cancellationToken)
//     //             .Select(static rsp => rsp.MapProjectionDetails(includeStatistics: true))
//     //             .FirstAsync(cancellationToken)
//     //             .ConfigureAwait(false);
//     //
//     //         // get definition
//     //
//     //         // get settings
//     //
//     //
//     //         return result;
//     //     }
//     //     catch (RpcException rex) {
//     //         return Result.Failure<ProjectionDetails, GetProjectionError>(rex.StatusCode switch {
//     //             StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//     //             StatusCode.NotFound         => new ErrorDetails.NotFound(),
//     //             _                           => throw rex.WithOriginalCallStack()
//     //         });
//     //     }
//     // }
//
//     // public async ValueTask<Result<List<ProjectionDetails>, ListProjectionsError>> ListProjections(
//     //     ListProjectionsOptions options, CancellationToken cancellationToken = default
//     // ) {
//     //     var request = new StatisticsReq {
//     //         Options = options.Mode switch {
//     //             ProjectionMode.Unspecified => new() { All        = new() },
//     //             ProjectionMode.OneTime     => new() { OneTime    = new() },
//     //             ProjectionMode.Continuous  => new() { Continuous = new() },
//     //             ProjectionMode.Transient   => new() { Transient  = new() }
//     //         }
//     //     };
//     //
//     //     try {
//     //         using var call = ServiceClient.Statistics(request, cancellationToken: cancellationToken);
//     //
//     //         return await call.ResponseStream
//     //             .ReadAllAsync(cancellationToken)
//     //             .Select(rsp => rsp.MapProjectionDetails(options.IncludeStatistics))
//     //             .ToListAsync(cancellationToken)
//     //             .ConfigureAwait(false);
//     //     }
//     //     catch (RpcException rex) {
//     //         return Result.Failure<List<ProjectionDetails>, ListProjectionsError>(
//     //             rex.StatusCode switch {
//     //                 StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//     //                 _                           => throw rex.WithOriginalCallStack()
//     //             }
//     //         );
//     //     }
//     // }
//
//     public async ValueTask<Result<T, GetProjectionResultError>> GetProjectionResult<T>(
//         ProjectionName name,
//         ProjectionPartition partition,
//         JsonSerializerOptions serializerOptions,
//         CancellationToken cancellationToken = default
//     ) where T : notnull {
//         name.ThrowIfNone();
//
//         try {
//             var request = new ResultReq {
//                 Options = new() {
//                     Name      = name,
//                     Partition = partition
//                 }
//             };
//
//             var response = await ServiceClient
//                 .ResultAsync(request, cancellationToken: cancellationToken)
//                 .ConfigureAwait(false);
//
//             var json = JsonFormatter.Default.Format(response.Result);
//
//             var result = JsonSerializer.Deserialize<T>(json, serializerOptions)
//                       ?? throw new JsonException($"Failed to deserialize projection result for '{name}' with partition '{partition}'");
//
//             return result;
//         }
//         catch (RpcException rex) {
//             return Result.Failure<T, GetProjectionResultError>(
//                 rex.StatusCode switch {
//                     StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//                     StatusCode.NotFound         => new ErrorDetails.NotFound(),
//                     _                           => throw rex.WithOriginalCallStack()
//                 }
//             );
//         }
//     }
//
//     public async ValueTask<Result<T, GetProjectionStateError>> GetProjectionState<T>(
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
//             var result = JsonSerializer.Deserialize<T>(json, serializerOptions)
//                       ?? throw new JsonException($"Failed to deserialize projection state for '{name}' with partition '{partition}'");
//
//             return result;
//         }
//         catch (RpcException rex) {
//             return Result.Failure<T, GetProjectionStateError>(
//                 rex.StatusCode switch {
//                     StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
//                     StatusCode.NotFound         => new ErrorDetails.NotFound(),
//                     _                           => throw rex.WithOriginalCallStack()
//                 }
//             );
//         }
//     }
//
//     public async ValueTask<Result<Success, RestartProjectionSubsystemError>> RestartProjectionSubsystem(CancellationToken cancellationToken = default) {
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
