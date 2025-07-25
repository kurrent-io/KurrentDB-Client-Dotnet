// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
// ReSharper disable InconsistentNaming

using EventStore.Client;
using EventStore.Client.Operations;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Model;

namespace Kurrent.Client;

public partial class KurrentOperationsClient {
	static readonly Empty EmptyResult = new Empty();

	public async ValueTask<Result<Success, ShutdownError>> Shutdown(CancellationToken cancellationToken = default) {
		try {
			using var call = ServiceClient.ShutdownAsync(EmptyResult, cancellationToken: cancellationToken);
			await call.ResponseAsync.ConfigureAwait(false);

			return new Result<Success, ShutdownError>();
		} catch (RpcException ex) {
			return Result.Failure<Success, ShutdownError>(
				ex.StatusCode switch {
					StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
					StatusCode.Unauthenticated  => ex.AsNotAuthenticatedError(),
					_                           => throw KurrentClientException.CreateUnknown(nameof(Shutdown), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(Shutdown), ex);
		}
	}

	public async ValueTask<Result<Success, MergeIndexesError>> MergeIndexes(CancellationToken cancellationToken = default) {
		try {
			using var call = ServiceClient.MergeIndexesAsync(EmptyResult, cancellationToken: cancellationToken);
			await call.ResponseAsync.ConfigureAwait(false);

			return new Result<Success, MergeIndexesError>();
		} catch (RpcException ex) {
			return Result.Failure<Success, MergeIndexesError>(
				ex.StatusCode switch {
					StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
					StatusCode.Unauthenticated  => ex.AsNotAuthenticatedError(),
					_                           => throw KurrentClientException.CreateUnknown(nameof(MergeIndexes), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(Shutdown), ex);
		}
	}

	public async ValueTask<Result<Success, ResignNodeError>> ResignNode(CancellationToken cancellationToken = default) {
		try {
			using var call = ServiceClient.ResignNodeAsync(EmptyResult, cancellationToken: cancellationToken);
			await call.ResponseAsync.ConfigureAwait(false);

			return new Result<Success, ResignNodeError>();
		} catch (RpcException ex) {
			return Result.Failure<Success, ResignNodeError>(
				ex.StatusCode switch {
					StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
					StatusCode.Unauthenticated  => ex.AsNotAuthenticatedError(),
					_                           => throw KurrentClientException.CreateUnknown(nameof(ResignNode), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(Shutdown), ex);
		}
	}

	public async ValueTask<Result<Success, SetNodePriorityError>> SetNodePriority(int nodePriority, CancellationToken cancellationToken = default) {
		try {
			using var call = ServiceClient.SetNodePriorityAsync(new SetNodePriorityReq { Priority = nodePriority }, cancellationToken: cancellationToken);
			await call.ResponseAsync.ConfigureAwait(false);

			return new Result<Success, SetNodePriorityError>();
		} catch (RpcException ex) {
			return Result.Failure<Success, SetNodePriorityError>(
				ex.StatusCode switch {
					StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
					StatusCode.Unauthenticated  => ex.AsNotAuthenticatedError(),
					_                           => throw KurrentClientException.CreateUnknown(nameof(SetNodePriority), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(Shutdown), ex);
		}
	}

	public async ValueTask<Result<Success, RestartPersistentSubscriptionsError>> RestartPersistentSubscriptions(
		CancellationToken cancellationToken = default
	) {
		try {
			using var call = ServiceClient.RestartPersistentSubscriptionsAsync(EmptyResult, cancellationToken: cancellationToken);
			await call.ResponseAsync.ConfigureAwait(false);

			return new Result<Success, RestartPersistentSubscriptionsError>();
		} catch (RpcException ex) {
			return Result.Failure<Success, RestartPersistentSubscriptionsError>(
				ex.StatusCode switch {
					StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
					StatusCode.Unauthenticated  => ex.AsNotAuthenticatedError(),
					_                           => throw KurrentClientException.CreateUnknown(nameof(RestartPersistentSubscriptions), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(Shutdown), ex);
		}
	}
}
