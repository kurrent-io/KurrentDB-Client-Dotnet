using EventStore.Client;
using EventStore.Client.PersistentSubscriptions;
using KurrentDB.Client;

namespace Kurrent.Client;

partial class KurrentPersistentSubscriptionsClient {
	/// <summary>
	/// Gets the status of a persistent subscription to $all
	/// </summary>
	public async Task<PersistentSubscriptionInfo> GetInfoToAllAsync(
		string groupName, CancellationToken cancellationToken = default
	) {
		if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsGetInfo) {
			var req = new GetInfoReq {
				Options = new GetInfoReq.Types.Options {
					GroupName = groupName,
					All       = new Empty()
				}
			};

			return await GetInfoGrpcAsync(req, cancellationToken).ConfigureAwait(false);
		}

		throw new NotSupportedException("The server does not support getting persistent subscription details for $all");
	}

	/// <summary>
	/// Gets the status of a persistent subscription to a stream
	/// </summary>
	public async Task<PersistentSubscriptionInfo> GetInfoToStreamAsync(
		string streamName, string groupName, CancellationToken cancellationToken = default
	) {
		if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsGetInfo) {
			var req = new GetInfoReq {
				Options = new GetInfoReq.Types.Options {
					GroupName        = groupName,
					StreamIdentifier = streamName
				}
			};

			return await GetInfoGrpcAsync(req, cancellationToken).ConfigureAwait(false);
		}

		return await GetInfoHttpAsync(streamName, groupName, cancellationToken)
			.ConfigureAwait(false);
	}

	async ValueTask<PersistentSubscriptionInfo> GetInfoGrpcAsync(GetInfoReq req, CancellationToken cancellationToken) {
		var result = await ServiceClient
			.GetInfoAsync(req, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return PersistentSubscriptionInfo.From(result.SubscriptionInfo);
	}

	async Task<PersistentSubscriptionInfo> GetInfoHttpAsync(string streamName, string groupName, CancellationToken cancellationToken) {
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
