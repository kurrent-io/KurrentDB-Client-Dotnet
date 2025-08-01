// ReSharper disable InconsistentNaming

using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Streams.PersistentSubscriptions;
using Kurrent.Client.Streams;
using KurrentDB.Client;
using static Kurrent.Client.Streams.PersistentSubscription.PersistentSubscriptionV1Mapper.Requests;

namespace Kurrent.Client;

partial class KurrentPersistentSubscriptionsClient {
	/// <summary>
	/// Creates a persistent subscription to a specified stream.
	/// </summary>
	/// <param name="streamName">The name of the stream to subscribe to.</param>
	/// <param name="groupName">The name of the subscription group.</param>
	/// <param name="settings">The settings for the persistent subscription.</param>
	/// <param name="cancellationToken">A cancellation token used to observe cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async ValueTask<Result<Success, CreateToStreamError>> CreateToStream(string streamName, string groupName, PersistentSubscriptionSettings settings, CancellationToken cancellationToken = default) {
		try {
			await CreateInternal(
				streamName, groupName, settings,
				ReadFilter.None, cancellationToken
			).ConfigureAwait(false);

			return new Result<Success, CreateToStreamError>();
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, CreateToStreamError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(CreateToStream), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(CreateToStream), ex);
		}
	}

	/// <summary>
	/// Creates a persistent subscription to the $all stream with a specified filter.
	/// </summary>
	/// <param name="groupName">The name of the subscription group.</param>
	/// <param name="filter">The read filter specifying scope and type for the subscription.</param>
	/// <param name="settings">The settings for the persistent subscription.</param>
	/// <param name="cancellationToken">A cancellation token used to observe cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async ValueTask<Result<Success, CreateToAllError>> CreateToAll(string groupName, ReadFilter filter, PersistentSubscriptionSettings settings, CancellationToken cancellationToken = default) {
		try {
			await CreateInternal(
				SystemStreams.AllStream, groupName, settings,
				filter, cancellationToken
			).ConfigureAwait(false);

			return new Result<Success, CreateToAllError>();
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, CreateToAllError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(CreateToAll), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(CreateToAll), ex);
		}
	}

	/// <summary>
	/// Creates a persistent subscription to the $all stream.
	/// </summary>
	/// <param name="groupName">The name of the subscription group.</param>
	/// <param name="settings">The settings for the persistent subscription.</param>
	/// <param name="cancellationToken">A cancellation token used to observe cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async ValueTask<Result<Success, CreateToAllError>> CreateToAll(string groupName, PersistentSubscriptionSettings settings, CancellationToken cancellationToken = default) {
		try {
			await CreateInternal(
				SystemStreams.AllStream, groupName, settings,
				ReadFilter.None, cancellationToken
			).ConfigureAwait(false);

			return new Result<Success, CreateToAllError>();
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, CreateToAllError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(CreateToAll), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(CreateToAll), ex);
		}
	}

	async ValueTask CreateInternal(
		string streamName,
		string groupName,
		PersistentSubscriptionSettings settings,
		ReadFilter filter,
		CancellationToken cancellationToken
	) {
		await EnsureCompatibility(streamName, cancellationToken);
		var request = CreateSubscriptionRequest(streamName, groupName, settings, HeartbeatOptions.Default, filter);
		using var call = ServiceClient.CreateAsync(request, cancellationToken: cancellationToken);
		await call.ResponseAsync.ConfigureAwait(false);
	}
}
