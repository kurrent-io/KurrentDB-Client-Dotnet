using Grpc.Core;
using EventStore.Client;
using EventStore.Client.PersistentSubscriptions;
using KurrentDB.Client;
using NotSupportedException = System.NotSupportedException;

namespace Kurrent.Client;

partial class KurrentPersistentSubscriptionsClient {
	/// <summary>
	/// Retry the parked messages of the persistent subscription
	/// </summary>
	public async Task ReplayParkedMessagesToAllAsync(
		string groupName, long? stopAt = null, TimeSpan? deadline = null,
		UserCredentials? userCredentials = null, CancellationToken cancellationToken = default
	) {
		var channelInfo = await LegacyClient.GetChannelInfo(cancellationToken);
		if (channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsReplayParked) {
			var req = new ReplayParkedReq() {
				Options = new ReplayParkedReq.Types.Options {
					GroupName = groupName,
					All       = new Empty()
				},
			};

			await ReplayParkedGrpcAsync(
					req, stopAt, deadline,
					userCredentials, channelInfo.CallInvoker, cancellationToken
				)
				.ConfigureAwait(false);

			return;
		}

		if (channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsToAll) {
			await ReplayParkedHttpAsync(
					SystemStreams.AllStream, groupName, stopAt,
					channelInfo,
					deadline, userCredentials, cancellationToken
				)
				.ConfigureAwait(false);

			return;
		}

		throw new NotSupportedException("The server does not support persistent subscriptions to $all.");
	}

	/// <summary>
	/// Retry the parked messages of the persistent subscription
	/// </summary>
	public async Task ReplayParkedMessagesToStreamAsync(
		string streamName, string groupName, long? stopAt = null,
		TimeSpan? deadline = null, UserCredentials? userCredentials = null, CancellationToken cancellationToken = default
	) {
		var channelInfo = await LegacyClient.GetChannelInfo(cancellationToken);
		if (channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsReplayParked) {
			var req = new ReplayParkedReq() {
				Options = new ReplayParkedReq.Types.Options {
					GroupName        = groupName,
					StreamIdentifier = streamName
				},
			};

			await ReplayParkedGrpcAsync(
					req, stopAt, deadline,
					userCredentials, channelInfo.CallInvoker, cancellationToken
				)
				.ConfigureAwait(false);

			return;
		}

		await ReplayParkedHttpAsync(
				streamName, groupName, stopAt,
				channelInfo, deadline, userCredentials,
				cancellationToken
			)
			.ConfigureAwait(false);
	}

	async Task ReplayParkedGrpcAsync(
		ReplayParkedReq req, long? numberOfEvents, TimeSpan? deadline,
		UserCredentials? userCredentials, CallInvoker callInvoker, CancellationToken cancellationToken
	) {
		if (numberOfEvents.HasValue) {
			req.Options.StopAt = numberOfEvents.Value;
		} else {
			req.Options.NoLimit = new Empty();
		}

		await ServiceClient
			.ReplayParkedAsync(req, cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}

	async Task ReplayParkedHttpAsync(
		string streamName,
		string groupName,
		long? numberOfEvents,
		ChannelInfo channelInfo,
		TimeSpan? deadline,
		UserCredentials? userCredentials,
		CancellationToken cancellationToken
	) {
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
