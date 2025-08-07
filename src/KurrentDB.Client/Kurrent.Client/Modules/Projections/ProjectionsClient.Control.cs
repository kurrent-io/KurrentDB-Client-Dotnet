// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using EventStore.Client;
using Grpc.Core;
using KurrentDB.Protocol.Projections.V1;

namespace Kurrent.Client.Projections;

public partial class ProjectionsClient {
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
    public async ValueTask<Result<Success, ResetProjectionError>> ResetProjection(string name, CancellationToken cancellationToken = default) {
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
    public async ValueTask<Result<Success, AbortProjectionError>> AbortProjection(string name, CancellationToken cancellationToken = default) {
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
    public async ValueTask<Result<Success, DisableProjectionError>> DisableProjection(string name, CancellationToken cancellationToken = default) {
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
}
