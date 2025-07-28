// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
// ReSharper disable InconsistentNaming

using EventStore.Client;
using EventStore.Client.Operations;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Model;
using KurrentDB.Client;

namespace Kurrent.Client;

public partial class KurrentOperationsClient {
	static readonly Empty EmptyResult = new Empty();

	public async ValueTask<Result<Success, ShutdownError>> Shutdown(CancellationToken cancellationToken = default) {
		try {
			using var call = ServiceClient.ShutdownAsync(EmptyResult, cancellationToken: cancellationToken);
			await call.ResponseAsync.ConfigureAwait(false);

			return new Result<Success, ShutdownError>();
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, ShutdownError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(Shutdown), ex)
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
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, MergeIndexesError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(MergeIndexes), ex)
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
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, ResignNodeError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(ResignNode), ex)
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
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, SetNodePriorityError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(SetNodePriority), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(Shutdown), ex);
		}
	}

	public async ValueTask<Result<Success, RestartPersistentSubscriptionsError>> RestartPersistentSubscriptions(CancellationToken cancellationToken = default) {
		try {
			using var call = ServiceClient.RestartPersistentSubscriptionsAsync(EmptyResult, cancellationToken: cancellationToken);
			await call.ResponseAsync.ConfigureAwait(false);

			return new Result<Success, RestartPersistentSubscriptionsError>();
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, RestartPersistentSubscriptionsError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(RestartPersistentSubscriptions), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(Shutdown), ex);
		}
	}
}
