using EventStore.Client;
using EventStore.Client.PersistentSubscriptions;
using Grpc.Core;
using Kurrent.Client.Legacy;
using KurrentDB.Client;

namespace Kurrent.Client.PersistentSubscriptions;

partial class PersistentSubscriptionsClient {
	/// <summary>
	/// Deletes a persistent subscription.
	/// </summary>
	public async ValueTask<Result<Success, DeleteToStreamError>> DeleteToStream(
		string streamName, string groupName, CancellationToken cancellationToken = default
	) {
		try {
			await EnsureCompatibility(streamName, cancellationToken);

			var deleteOptions = new DeleteReq.Types.Options {
				GroupName = groupName
			};

			if (streamName is SystemStreams.AllStream)
				deleteOptions.All = new Empty();
			else
				deleteOptions.StreamIdentifier = streamName;

			var call = ServiceClient.DeleteAsync(new DeleteReq { Options = deleteOptions }, cancellationToken: cancellationToken);

			await call.ResponseAsync.ConfigureAwait(false);

			return new Result<Success, DeleteToStreamError>();
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, DeleteToStreamError>(
				ex switch {
					AccessDeniedException                        => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException                    => rpcEx.AsNotAuthenticatedError(),
					PersistentSubscriptionNotFoundException psEx => rpcEx.AsPersistentSubscriptionNotFoundError(psEx.StreamName, psEx.GroupName),
					_                                            => throw KurrentClientException.CreateUnknown(nameof(DeleteToStream), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(DeleteToStream), ex);
		}
	}

	/// <summary>
	/// Deletes a persistent subscription to $all.
	/// </summary>
	public async ValueTask<Result<Success, DeleteToAllError>> DeleteToAll(string groupName, CancellationToken cancellationToken = default) {
		try {
			await DeleteToStream(SystemStreams.AllStream, groupName, cancellationToken).ConfigureAwait(false);
			return new Result<Success, DeleteToAllError>();
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, DeleteToAllError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(DeleteToStream), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(DeleteToStream), ex);
		}
	}
}
