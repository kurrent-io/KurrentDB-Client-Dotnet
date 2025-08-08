using EventStore.Client;
using Grpc.Core;
using KurrentDB.Protocol.Operations.V1;
using OperationsServiceClient = KurrentDB.Protocol.Operations.V1.Operations.OperationsClient;
using FeaturesServiceClient = KurrentDB.Protocol.ServerFeatures.V1.ServerFeatures.ServerFeaturesClient;

namespace Kurrent.Client.Admin;

public partial class AdminClient {
    internal AdminClient(KurrentClient source) {
        OperationsServiceClient = new(source.LegacyCallInvoker);
        FeaturesServiceClient   = new(source.LegacyCallInvoker);
    }

    OperationsServiceClient OperationsServiceClient { get; }
    FeaturesServiceClient   FeaturesServiceClient   { get; }

    static readonly Empty EmptyRequest = new Empty();

	public async ValueTask<Result<Success, ShutdownServerError>> ShutdownServer(CancellationToken cancellationToken = default) {
        try {
            await OperationsServiceClient
                .ShutdownAsync(EmptyRequest, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException rex) {
            return Result.Failure<Success, ShutdownServerError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}

	public async ValueTask<Result<Success, MergeIndexesError>> MergeIndexes(CancellationToken cancellationToken = default) {
		try {
            await OperationsServiceClient
                .MergeIndexesAsync(EmptyRequest, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException rex) {
            return Result.Failure<Success, MergeIndexesError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

	public async ValueTask<Result<Success, ResignNodeError>> ResignNode(CancellationToken cancellationToken = default) {
		try {
			await OperationsServiceClient
                .ResignNodeAsync(EmptyRequest, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
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
            await OperationsServiceClient
                .SetNodePriorityAsync(new SetNodePriorityReq { Priority = nodePriority }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
		}
        catch (RpcException rex) {
            return Result.Failure<Success, SetNodePriorityError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}

	public async ValueTask<Result<Success, RestartPersistentSubscriptionsError>> RestartPersistentSubscriptions(CancellationToken cancellationToken = default) {
		try {
			await OperationsServiceClient
                .RestartPersistentSubscriptionsAsync(EmptyRequest, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
		}
        catch (RpcException rex) {
            return Result.Failure<Success, RestartPersistentSubscriptionsError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}
}
