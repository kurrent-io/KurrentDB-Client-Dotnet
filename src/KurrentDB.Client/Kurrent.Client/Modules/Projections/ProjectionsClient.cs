#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.

// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using System.Text.Json;
using Google.Protobuf;
using Grpc.Core;
using KurrentDB.Protocol.Projections.V1;
using ProjectionsServiceClient = KurrentDB.Protocol.Projections.V1.Projections.ProjectionsClient;

namespace Kurrent.Client.Projections;

public sealed partial class ProjectionsClient {
    internal ProjectionsClient(KurrentClient source) =>
        ServiceClient  = new(source.LegacyCallInvoker);

    internal ProjectionsServiceClient ServiceClient  { get; }

    public async ValueTask<Result<Success, CreateProjectionError>> CreateProjection(ProjectionName name, ProjectionQuery query, ProjectionSettings settings, CancellationToken cancellationToken = default) {
        name.ThrowIfNone();
        query.ThrowIfInvalid();
        settings.ThrowIfInvalid();

        try {
            var request = new CreateReq {
                Options = new() {
                    Continuous = new() {
                        Name                = name,
                        EmitEnabled         = settings.EmitEnabled,
                        TrackEmittedStreams = settings.TrackEmittedStreams
                    },
                    Query = query
                }
            };

            await ServiceClient
                .CreateAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Success.Instance;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, CreateProjectionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied                                               => new ErrorDetails.AccessDenied(),
                StatusCode.AlreadyExists                                                  => new ErrorDetails.AlreadyExists(),
                StatusCode.Unknown when rex.Message.Contains("Projection already exists") => new ErrorDetails.AlreadyExists(),
                _                                                                         => throw rex.WithOriginalCallStack()
            });
        }
    }

    public async ValueTask<Result<Success, RenameProjectionError>> RenameProjection(ProjectionName name, CancellationToken cancellationToken = default) {
        name.ThrowIfNone();

        try {
            var request = new UpdateReq {
                Options = new() {
                    Name = name
                }
            };

            await ServiceClient
                .UpdateAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Success.Instance;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, RenameProjectionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    public async ValueTask<Result<Success, UpdateProjectionQueryError>> UpdateProjectionQuery(ProjectionName name, ProjectionQuery query, CancellationToken cancellationToken = default) {
        name.ThrowIfNone();
        query.ThrowIfInvalid();

        try {
            var request = new UpdateReq {
                Options = new() {
                    Name          = name,
                    Query         = query,
                    NoEmitOptions = new()
                }
            };

            await ServiceClient
                .UpdateAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Success.Instance;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, UpdateProjectionQueryError>(
                rex.StatusCode switch {
                    StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                    StatusCode.NotFound         => new ErrorDetails.NotFound(),
                    _                           => throw rex.WithOriginalCallStack()
                }
            );
        }
    }

    public async ValueTask<Result<Success, DeleteProjectionError>> DeleteProjection(ProjectionName name, DeleteProjectionOptions options, CancellationToken cancellationToken = default) {
        name.ThrowIfNone();

        try {
            var request = new DeleteReq {
                Options = new() {
                    Name                   = name,
                    DeleteStateStream      = options.DeleteStateStream,
                    DeleteCheckpointStream = options.DeleteCheckpointStream,
                    DeleteEmittedStreams   = options.DeleteCheckpointStream,
                }
            };

            await ServiceClient
                .DeleteAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Success.Instance;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, DeleteProjectionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    public async ValueTask<Result<Success, EnableProjectionError>> EnableProjection(ProjectionName name, CancellationToken cancellationToken = default) {
        name.ThrowIfNone();

        try {
            var request = new EnableReq {
                Options = new() {
                    Name = name
                }
            };

            await ServiceClient
                .EnableAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException rex) {
            return Result.Failure<Success, EnableProjectionError>(
                rex.StatusCode switch {
                    StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                    StatusCode.NotFound         => new ErrorDetails.NotFound(),
                    _                           => throw rex.WithOriginalCallStack()
                }
            );
        }
    }

    /// <summary>
    /// Resets a projection. This will re-emit events. Streams that are written to from the projection will also be soft deleted.
    /// </summary>
    public async ValueTask<Result<Success, ResetProjectionError>> ResetProjection(ProjectionName name, CancellationToken cancellationToken = default) {
        name.ThrowIfNone();

        try {
            var request = new ResetReq {
                Options = new() {
                    Name            = name,
                    WriteCheckpoint = true
                }
            };

            await ServiceClient
                .ResetAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException rex) {
            return Result.Failure<Success, ResetProjectionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    /// <summary>
    /// Aborts a projection. Does not save the projection's checkpoint.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<Result<Success, AbortProjectionError>> AbortProjection(ProjectionName name, CancellationToken cancellationToken = default) {
        name.ThrowIfNone();

        try {
            var request = new DisableReq {
                Options = new() {
                    Name            = name,
                    WriteCheckpoint = false
                }
            };

            await ServiceClient
                .DisableAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException rex) {
            return Result.Failure<Success, AbortProjectionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    /// <summary>
    /// Disables a projection. Saves the projection's checkpoint.
    /// </summary>
    public async ValueTask<Result<Success, DisableProjectionError>> DisableProjection(ProjectionName name, CancellationToken cancellationToken = default) {
        name.ThrowIfNone();

        try {
            var request = new DisableReq {
                Options = new() {
                    Name            = name,
                    WriteCheckpoint = true
                }
            };

            await ServiceClient
                .DisableAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException rex) {
            return Result.Failure<Success, DisableProjectionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    public async ValueTask<Result<Success, RestartProjectionSubsystemError>> RestartProjectionSubsystem(CancellationToken cancellationToken = default) {
        try {
            await ServiceClient
                .RestartSubsystemAsync(new(), cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException rex) {
            return Result.Failure<Success, RestartProjectionSubsystemError>(rex.StatusCode switch {
                StatusCode.PermissionDenied   => new ErrorDetails.AccessDenied(),
                StatusCode.FailedPrecondition => new ErrorDetails.ProjectionsSubsystemRestartFailed(),
                _                             => throw rex.WithOriginalCallStack()
            });
        }
    }

    public async ValueTask<Result<ProjectionDetails, GetProjectionError>> GetProjection(ProjectionName name, CancellationToken cancellationToken = default) {
        try {
            var request = new StatisticsReq {
                Options =  new() {
                    Name = name
                }
            };

            using var call = ServiceClient.Statistics(request, cancellationToken: cancellationToken);

            var result = await call.ResponseStream
                .ReadAllAsync(cancellationToken)
                .Select(static rsp => rsp.MapToProjectionDetails())
                .FirstAsync(cancellationToken);

            return result;
        }
        catch (RpcException rex) {
            return Result.Failure<ProjectionDetails, GetProjectionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

	public async ValueTask<Result<List<ProjectionDetails>, ListProjectionsError>> ListProjections(ListProjectionsOptions options, CancellationToken cancellationToken = default) {
        var request = new StatisticsReq {
            Options = options.Mode switch {
                ProjectionMode.Unspecified => new() { All        = new() },
                ProjectionMode.OneTime     => new() { OneTime    = new() },
                ProjectionMode.Continuous  => new() { Continuous = new() },
                ProjectionMode.Transient   => new() { Transient  = new() }
            }
        };

        try {
            using var call = ServiceClient.Statistics(request, cancellationToken: cancellationToken);

            var result = await call.ResponseStream
                .ReadAllAsync(cancellationToken)
                .Select(static rsp => rsp.MapToProjectionDetails())
                .ToListAsync(cancellationToken);

            return result;
        }
        catch (RpcException rex) {
            return Result.Failure<List<ProjectionDetails>, ListProjectionsError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}

     public async ValueTask<Result<T, GetProjectionResultError>> GetProjectionResult<T>(
        ProjectionName name,
        ProjectionPartition partition,
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellationToken = default
    ) where T : notnull {
        name.ThrowIfNone();

        try {
            var request = new ResultReq {
                Options = new() {
                    Name      = name,
                    Partition = partition
                }
            };

            var response = await ServiceClient
                .ResultAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var json = JsonFormatter.Default.Format(response.Result);

            var result = JsonSerializer.Deserialize<T>(json, serializerOptions)
                      ?? throw new JsonException($"Failed to deserialize projection result for '{name}' with partition '{partition}'");

            return result;
        }
        catch (RpcException rex) {
            return Result.Failure<T, GetProjectionResultError>(
                rex.StatusCode switch {
                    StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                    StatusCode.NotFound         => new ErrorDetails.NotFound(),
                    _                           => throw rex.WithOriginalCallStack()
                }
            );
        }
    }

    public async ValueTask<Result<T, GetProjectionStateError>> GetProjectionState<T>(
        ProjectionName name,
        ProjectionPartition partition,
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellationToken = default
    ) where T : notnull {
        name.ThrowIfNone();

        try {
            var request = new StateReq {
                Options = new() {
                    Name      = name,
                    Partition = partition
                }
            };

            var response = await ServiceClient
                .StateAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var json = JsonFormatter.Default.Format(response.State);

            var result = JsonSerializer.Deserialize<T>(json, serializerOptions)
                      ?? throw new JsonException($"Failed to deserialize projection state for '{name}' with partition '{partition}'");

            return result;
        }
        catch (RpcException rex) {
            return Result.Failure<T, GetProjectionStateError>(
                rex.StatusCode switch {
                    StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                    StatusCode.NotFound         => new ErrorDetails.NotFound(),
                    _                           => throw rex.WithOriginalCallStack()
                }
            );
        }
    }
}
