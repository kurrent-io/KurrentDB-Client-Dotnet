// ReSharper disable InvertIf

using EventStore.Client;
using EventStore.Client.PersistentSubscriptions;
using KurrentDB.Client;

namespace Kurrent.Client;

partial class KurrentPersistentSubscriptionsClient {
	/// <summary>
	/// Gets the status of a persistent subscription to $all
	/// </summary>
	public async ValueTask<PersistentSubscriptionInfo> GetInfoToAll(string groupName, CancellationToken cancellationToken = default) {
		if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsGetInfo) {
			var req = new GetInfoReq {
				Options = new GetInfoReq.Types.Options {
					GroupName = groupName,
					All       = new Empty()
				}
			};

			return await GetInfoGrpc(req, cancellationToken).ConfigureAwait(false);
		}

		throw new NotSupportedException("The server does not support getting persistent subscription details for $all");
	}

	/// <summary>
	/// Gets the status of a persistent subscription to a stream
	/// </summary>
	public async ValueTask<PersistentSubscriptionInfo> GetInfoToStream(
		string streamName, string groupName, CancellationToken cancellationToken = default
	) {
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
