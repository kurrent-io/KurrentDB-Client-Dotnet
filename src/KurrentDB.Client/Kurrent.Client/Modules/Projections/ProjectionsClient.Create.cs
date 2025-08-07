// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using EventStore.Client;
using Grpc.Core;
using KurrentDB.Protocol.Projections.V1;

namespace Kurrent.Client.Projections;

public partial class ProjectionsClient {
	public ValueTask<Result<Success, CreateProjectionError>> CreateOneTimeProjection(string query, CancellationToken cancellationToken = default) {
        var request = new CreateReq {
            Options = new() {
                OneTime = new Empty(),
                Query   = query
            }
        };

        return CreateProjection(request, cancellationToken);
	}

	public ValueTask<Result<Success, CreateProjectionError>> CreateContinuousProjection(
		string name, string query, bool trackEmittedStreams = false, CancellationToken cancellationToken = default
	) {
        var request = new CreateReq {
            Options = new CreateReq.Types.Options {
                Continuous = new() {
                    Name                = name,
                    TrackEmittedStreams = trackEmittedStreams
                },
                Query = query
            }
        };

        return CreateProjection(request, cancellationToken);
	}

	public ValueTask<Result<Success, CreateProjectionError>> CreateTransientProjection(string name, string query, CancellationToken cancellationToken = default) {
        var request = new CreateReq {
            Options = new CreateReq.Types.Options {
                Transient = new() {
                    Name = name
                },
                Query = query
            }
        };

        return CreateProjection(request, cancellationToken);
	}

    async ValueTask<Result<Success, CreateProjectionError>> CreateProjection(CreateReq request, CancellationToken cancellationToken = default) {
        try {
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
}
