using EventStore.Client;
using Grpc.Core;
using KurrentDB.Protocol.Projections.V1;
using ProjectionsServiceClient = KurrentDB.Protocol.Projections.V1.Projections.ProjectionsClient;

namespace Kurrent.Client.Projections;

public sealed partial class ProjectionsClient {
    internal ProjectionsClient(KurrentClient source) =>
        ServiceClient  = new(source.LegacyCallInvoker);

    internal ProjectionsServiceClient ServiceClient  { get; }

    public async ValueTask<Result<Success, CreateProjectionError>> CreateProjection(ProjectionSettings settings, CancellationToken cancellationToken = default) {
        settings.ThrowIfInvalid();

        try {
            var request = new CreateReq { Options = new() { Query = settings.Query } };

            if (settings.Mode == ProjectionMode.OneTime)
                request.Options.OneTime = new Empty();
            else if (settings.Mode == ProjectionMode.Transient)
                request.Options = new() { Transient = new() { Name = settings.Name } };
            else if (settings.Mode == ProjectionMode.Continuous)
                request.Options.Continuous = new() {
                    Name                = settings.Name,
                    TrackEmittedStreams = settings.TrackEmittedStreams
                };

            await ServiceClient
                .CreateAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException rex) {
            return Result.Failure<Success, CreateProjectionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.AlreadyExists    => new ErrorDetails.AlreadyExists(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    // update projection makes no sense because each type has different options.
    public async ValueTask<Result<Success, UpdateProjectionError>> UpdateProjection(
        ProjectionName name, ProjectionQuery query, bool? emitEnabled = null, CancellationToken cancellationToken = default
    ) {
        try {
            var options = new UpdateReq.Types.Options {
                Name  = name,
                Query = query
            };

            if (emitEnabled.HasValue)
                options.EmitEnabled = emitEnabled.Value;
            else
                options.NoEmitOptions = new Empty();

            var request = new UpdateReq {
                Options = options
            };

            var resp = await ServiceClient
                .UpdateAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException rex) {
            return Result.Failure<Success, UpdateProjectionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    public async ValueTask<Result<Success, DeleteProjectionError>> DeleteProjection(
        ProjectionName name,
        bool deleteEmittedStreams = false,
        bool deleteStateStream = false,
        bool deleteCheckpointStream = false,
        CancellationToken cancellationToken = default
    ) {
        try {
            var request = new DeleteReq {
                Options = new() {
                    Name                   = name,
                    DeleteCheckpointStream = deleteCheckpointStream,
                    DeleteEmittedStreams   = deleteEmittedStreams,
                    DeleteStateStream      = deleteStateStream,
                }
            };

            await ServiceClient
                .DeleteAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException rex) {
            return Result.Failure<Success, DeleteProjectionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

      public async ValueTask<Result<Success, EnableProjectionError>> EnableProjection(string name, CancellationToken cancellationToken = default) {
        try {
            var request = new EnableReq {
                Options = new() {
                    Name = name
                }
            };

           await ServiceClient
               .EnableAsync(request , cancellationToken: cancellationToken)
               .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException rex) {
            return Result.Failure<Success, EnableProjectionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    /// <summary>
    /// Resets a projection. This will re-emit events. Streams that are written to from the projection will also be soft deleted.
    /// </summary>
    public async ValueTask<Result<Success, ResetProjectionError>> ResetProjection(ProjectionName name, CancellationToken cancellationToken = default) {
        try {
            var request = new ResetReq {
                Options = new ResetReq.Types.Options {
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
        try {
            var request = new DisableReq {
                Options = new DisableReq.Types.Options {
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
        try {
            var request = new DisableReq {
                Options = new DisableReq.Types.Options {
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
                .RestartSubsystemAsync(new Empty(), cancellationToken: cancellationToken)
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
                Options =  new StatisticsReq.Types.Options {
                    Name = name
                }
            };

            using var call = ServiceClient.Statistics(request, cancellationToken: cancellationToken);

            var result = await call.ResponseStream
                .ReadAllAsync(cancellationToken)
                .Select(MapToProjectionDetails)
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
                ProjectionMode.Unspecified => new() { All        = new Empty() },
                ProjectionMode.OneTime     => new() { OneTime    = new Empty() },
                ProjectionMode.Continuous  => new() { Continuous = new Empty() },
                ProjectionMode.Transient   => new() { Transient  = new Empty() }
            }
        };

        try {
            using var call = ServiceClient.Statistics(request, cancellationToken: cancellationToken);

            var result = await call.ResponseStream
                .ReadAllAsync(cancellationToken)
                .Select(MapToProjectionDetails)
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

	static ProjectionDetails MapToProjectionDetails(StatisticsResp response) {
		var details = response.Details;

		return new ProjectionDetails(
			details.CoreProcessingTime,
            details.Version,
            details.Epoch,
			details.EffectiveName,
            details.WritesInProgress,
            details.ReadsInProgress,
			details.PartitionsCached,
			details.Status,
            details.StateReason,
            details.Name,
			details.Mode,
            details.Position,
            details.Progress,
			details.LastCheckpoint,
            details.EventsProcessedAfterRestart,
            details.CheckpointStatus,
			details.BufferedEvents,
            details.WritePendingEventsBeforeCheckpoint,
			details.WritePendingEventsAfterCheckpoint
		);
	}
}
