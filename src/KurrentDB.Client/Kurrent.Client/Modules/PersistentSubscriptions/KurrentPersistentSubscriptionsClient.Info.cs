// ReSharper disable InvertIf

using EventStore.Client;
using EventStore.Client.PersistentSubscriptions;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Streams.PersistentSubscriptions;
using KurrentDB.Client;

namespace Kurrent.Client;

partial class KurrentPersistentSubscriptionsClient {
	/// <summary>
	/// Gets the status of a persistent subscription to $all
	/// </summary>
	public async ValueTask<Result<PersistentSubscriptionInfo, GetInfoToAllError>> GetInfoToAll(
		string groupName, CancellationToken cancellationToken = default
	) {
		try {
			if (!LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsGetInfo) {
				throw new NotSupportedException("The server does not support getting persistent subscription details for $all");
			}

			var req = new GetInfoReq {
				Options = new GetInfoReq.Types.Options {
					GroupName = groupName,
					All       = new Empty()
				}
			};

			var info = await GetInfoGrpc(req, cancellationToken).ConfigureAwait(false);

			return Result.Success<PersistentSubscriptionInfo, GetInfoToAllError>(info);
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<PersistentSubscriptionInfo, GetInfoToAllError>(
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

	/// <summary>
	/// Gets the status of a persistent subscription to a stream
	/// </summary>
	public async ValueTask<Result<PersistentSubscriptionInfo, GetInfoToStreamError>> GetInfoToStream(
		string streamName, string groupName, CancellationToken cancellationToken = default
	) {
		try {
			if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsGetInfo) {
				var req = new GetInfoReq {
					Options = new GetInfoReq.Types.Options {
						GroupName        = groupName,
						StreamIdentifier = streamName
					}
				};

				return await GetInfoGrpc(req, cancellationToken).ConfigureAwait(false);
			}

			return await GetInfoHttp(streamName, groupName, cancellationToken)
				.ConfigureAwait(false);
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<PersistentSubscriptionInfo, GetInfoToStreamError>(
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

	async ValueTask<PersistentSubscriptionInfo> GetInfoGrpc(GetInfoReq req, CancellationToken cancellationToken) {
		var result = await ServiceClient
			.GetInfoAsync(req, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return PersistentSubscriptionInfo.From(result.SubscriptionInfo);
	}

	async ValueTask<PersistentSubscriptionInfo> GetInfoHttp(string streamName, string groupName, CancellationToken cancellationToken) {
		var path = $"/subscriptions/{UrlEncode(streamName)}/{UrlEncode(groupName)}/info";
		var result = await HttpGet<PersistentSubscriptionDto>(
				path,
				onNotFound: () => throw new PersistentSubscriptionNotFoundException(streamName, groupName),
				cancellationToken
			)
			.ConfigureAwait(false);

		return PersistentSubscriptionInfo.From(result);
	}
}
