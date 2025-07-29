// ReSharper disable InconsistentNaming

using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Model;
using Kurrent.Client.Model.PersistentSubscriptions;
using KurrentDB.Client;
using static Kurrent.Client.Model.PersistentSubscription.PersistentSubscriptionV1Mapper.Requests;

namespace Kurrent.Client;

public partial class KurrentPersistentSubscriptionsClient {
	/// <summary>
	/// Updates a persistent subscription on a specific stream with the given group name and subscription settings.
	/// </summary>
	/// <param name="streamName">The name of the stream to which the persistent subscription applies.</param>
	/// <param name="groupName">The name of the subscriber group for the persistent subscription.</param>
	/// <param name="settings">The settings for configuring the persistent subscription.</param>
	/// <param name="cancellationToken">An optional token to cancel the asynchronous operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async ValueTask<Result<Success, UpdateToStreamError>> UpdateToStream(
		string streamName, string groupName, PersistentSubscriptionSettings settings, CancellationToken cancellationToken = default
	) {
		try {
			await EnsureCompatibility(streamName, cancellationToken);
			var request = UpdateSubscriptionRequest(streamName, groupName, settings);

			using var call = ServiceClient.UpdateAsync(request, cancellationToken: cancellationToken);
			await call.ResponseAsync.ConfigureAwait(false);

			return Result.Success<Success, UpdateToStreamError>(Success.Instance);
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, UpdateToStreamError>(
				ex switch {
					AccessDeniedException                       => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException                   => rpcEx.AsNotAuthenticatedError(),
					PersistentSubscriptionNotFoundException pEx => rpcEx.AsPersistentSubscriptionNotFoundError(pEx.StreamName, pEx.GroupName),
					_                                           => throw KurrentClientException.CreateUnknown(nameof(DeleteToStream), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(DeleteToStream), ex);
		}
	}

	/// <summary>
	/// Updates a persistent subscription on the global "$all" stream with the specified group name and subscription settings.
	/// </summary>
	/// <param name="groupName">The name of the subscriber group for the persistent subscription.</param>
	/// <param name="settings">The settings for configuring the persistent subscription.</param>
	/// <param name="cancellationToken">An optional token to cancel the asynchronous operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async ValueTask<Result<Success, UpdateToAllError>> UpdateToAll(
		string groupName, PersistentSubscriptionSettings settings, CancellationToken cancellationToken = default
	) {
		try {
			await UpdateToStream(
				SystemStreams.AllStream, groupName, settings,
				cancellationToken
			).ConfigureAwait(false);

			return Result.Success<Success, UpdateToAllError>(Success.Instance);
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, UpdateToAllError>(
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
