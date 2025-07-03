using EventStore.Client;
using EventStore.Client.PersistentSubscriptions;
using KurrentDB.Client;

namespace Kurrent.Client;

partial class KurrentPersistentSubscriptionsClient {
	/// <summary>
	/// Lists persistent subscriptions to $all.
	/// </summary>
	public async Task<IEnumerable<PersistentSubscriptionInfo>> ListToAllAsync(CancellationToken cancellationToken = default) {
		if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsList) {
			var req = new ListReq {
				Options = new ListReq.Types.Options {
					ListForStream = new ListReq.Types.StreamOption {
						All = new Empty()
					}
				}
			};

			return await ListGrpcAsync(req, cancellationToken).ConfigureAwait(false);
		}

		throw new NotSupportedException("The server does not support listing the persistent subscriptions.");
	}

	/// <summary>
	/// Lists persistent subscriptions to the specified stream.
	/// </summary>
	public async Task<IEnumerable<PersistentSubscriptionInfo>> ListToStreamAsync(string streamName, CancellationToken cancellationToken = default) {
		if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsList) {
			var req = new ListReq {
				Options = new ListReq.Types.Options {
					ListForStream = new ListReq.Types.StreamOption {
						Stream = streamName
					}
				}
			};

			return await ListGrpcAsync(req, cancellationToken).ConfigureAwait(false);
		}

		return await ListHttpAsync().ConfigureAwait(false);

		async Task<IEnumerable<PersistentSubscriptionInfo>> ListHttpAsync( ) {
			var path = $"/subscriptions/{UrlEncode(streamName)}";
			var result = await HttpGet<IList<PersistentSubscriptionDto>>(
					path,
					onNotFound: () => throw new PersistentSubscriptionNotFoundException(streamName, string.Empty),
					cancellationToken
				)
				.ConfigureAwait(false);

			return result.Select(PersistentSubscriptionInfo.From);
		}
	}

	/// <summary>
	/// Lists all persistent subscriptions.
	/// </summary>
	public async Task<IEnumerable<PersistentSubscriptionInfo>> ListAllAsync(CancellationToken cancellationToken = default) {
		if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsList) {
			var req = new ListReq {
				Options = new ListReq.Types.Options {
					ListAllSubscriptions = new Empty()
				}
			};

			return await ListGrpcAsync(req, cancellationToken).ConfigureAwait(false);
		}

		var result = await HttpGet<IList<PersistentSubscriptionDto>>(
				path: "/subscriptions",
				onNotFound: () => throw new PersistentSubscriptionNotFoundException(string.Empty, string.Empty),
				cancellationToken
			)
			.ConfigureAwait(false);

		return result.Select(PersistentSubscriptionInfo.From);
	}

	async Task<IEnumerable<PersistentSubscriptionInfo>> ListGrpcAsync(ListReq req, CancellationToken cancellationToken) {
		using var call = ServiceClient.ListAsync(req, cancellationToken: cancellationToken);
		var response = await call.ResponseAsync.ConfigureAwait(false);
		return response.Subscriptions.Select(PersistentSubscriptionInfo.From);
	}

}
