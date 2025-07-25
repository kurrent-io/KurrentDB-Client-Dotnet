using EventStore.Client;
using EventStore.Client.PersistentSubscriptions;
using KurrentDB.Client;
using NotSupportedException = System.NotSupportedException;

namespace Kurrent.Client;

partial class KurrentPersistentSubscriptionsClient {
	/// <summary>
	/// Retry the parked messages of the persistent subscription
	/// </summary>
	public async ValueTask ReplayParkedMessagesToAll(
		string groupName, long? stopAt = null, CancellationToken cancellationToken = default
	) {
		if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsReplayParked) {
			var request = new ReplayParkedReq {
				Options = new ReplayParkedReq.Types.Options {
					GroupName = groupName,
					All       = new Empty()
				},
			};

			await ReplayParkedGrpc(request, stopAt, cancellationToken).ConfigureAwait(false);

			return;
		}

		if (!LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsToAll)
			throw new NotSupportedException("The server does not support persistent subscriptions to $all.");

		await ReplayParkedHttp(SystemStreams.AllStream, groupName, stopAt, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Retry the parked messages of the persistent subscription
	/// </summary>
	public async ValueTask ReplayParkedMessagesToStream(
		string streamName, string groupName, long? stopAt = null, CancellationToken cancellationToken = default
	) {
		if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsReplayParked) {
			var req = new ReplayParkedReq {
				Options = new ReplayParkedReq.Types.Options {
					GroupName        = groupName,
					StreamIdentifier = streamName
				},
			};

			await ReplayParkedGrpc(req, stopAt, cancellationToken)
				.ConfigureAwait(false);

			return;
		}

		await ReplayParkedHttp(streamName, groupName, stopAt, cancellationToken).ConfigureAwait(false);
	}

	async ValueTask ReplayParkedGrpc(
		ReplayParkedReq request, long? numberOfEvents, CancellationToken cancellationToken
	) {
		if (numberOfEvents.HasValue)
			request.Options.StopAt = numberOfEvents.Value;
		else
			request.Options.NoLimit = new Empty();

		await ServiceClient
			.ReplayParkedAsync(request, cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}

	async ValueTask ReplayParkedHttp(string streamName, string groupName, long? numberOfEvents, CancellationToken cancellationToken) {
		var path  = $"/subscriptions/{UrlEncode(streamName)}/{UrlEncode(groupName)}/replayParked";
		var query = numberOfEvents.HasValue ? $"stopAt={numberOfEvents.Value}" : "";

		await HttpPost(
				path, query,
				onNotFound: () => throw new PersistentSubscriptionNotFoundException(streamName, groupName),
				cancellationToken
			)
			.ConfigureAwait(false);
	}
}
