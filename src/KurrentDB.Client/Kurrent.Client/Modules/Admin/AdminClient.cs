using EventStore.Client;
using Grpc.Core;
using KurrentDB.Protocol.Operations.V1;
using OperationsServiceClient = KurrentDB.Protocol.Operations.V1.Operations.OperationsClient;
using FeaturesServiceClient = KurrentDB.Protocol.ServerFeatures.V1.ServerFeatures.ServerFeaturesClient;

namespace Kurrent.Client.Admin;

public partial class AdminClient {
    internal AdminClient(KurrentClient source) {
        Source                = source;
        AdminServiceClient    = new(source.LegacyCallInvoker);
        FeaturesServiceClient = new(source.LegacyCallInvoker);
    }

    KurrentClient Source { get; }

    OperationsServiceClient AdminServiceClient    { get; }
    FeaturesServiceClient   FeaturesServiceClient { get; }

    static readonly Empty EmptyRequest = new Empty();

	public async ValueTask<Result<Success, ShutdownServerError>> ShutdownServer(CancellationToken cancellationToken = default) {
        try {
            await AdminServiceClient
                .ShutdownAsync(EmptyRequest, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Results.Success;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, ShutdownServerError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}


	public async ValueTask<Result<Success, ResignNodeError>> ResignNode(CancellationToken cancellationToken = default) {
		try {
			await AdminServiceClient
                .ResignNodeAsync(EmptyRequest, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Results.Success;
		}
        catch (RpcException rex) {
            return Result.Failure<Success, ResignNodeError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}

	public async ValueTask<Result<Success, SetNodePriorityError>> SetNodePriority(int nodePriority, CancellationToken cancellationToken = default) {
		ArgumentOutOfRangeException.ThrowIfNegative(nodePriority);

        try {
            await AdminServiceClient
                .SetNodePriorityAsync(new SetNodePriorityReq { Priority = nodePriority }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Results.Success;
		}
        catch (RpcException rex) {
            return Result.Failure<Success, SetNodePriorityError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}

    public async ValueTask<Result<Success, MergeIndexesError>> MergeIndexes(CancellationToken cancellationToken = default) {
        try {
            await AdminServiceClient
                .MergeIndexesAsync(EmptyRequest, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Results.Success;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, MergeIndexesError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

	public async ValueTask<Result<Success, RestartPersistentSubscriptionsError>> RestartPersistentSubscriptions(CancellationToken cancellationToken = default) {
		try {
			await AdminServiceClient
                .RestartPersistentSubscriptionsAsync(EmptyRequest, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Results.Success;
		}
        catch (RpcException rex) {
            return Result.Failure<Success, RestartPersistentSubscriptionsError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}
}
