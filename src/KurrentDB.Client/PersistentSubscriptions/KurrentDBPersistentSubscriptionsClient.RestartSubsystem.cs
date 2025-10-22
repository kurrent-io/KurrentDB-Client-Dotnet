#nullable enable
using EventStore.Client;
using static KurrentDB.Protocol.PersistentSubscriptions.V1.PersistentSubscriptions;

namespace KurrentDB.Client {
	partial class KurrentDBPersistentSubscriptionsClient {
		/// <summary>
		/// Restarts the persistent subscriptions subsystem.
		/// </summary>
		public async Task RestartSubsystemAsync(TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			if (channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsRestartSubsystem) {
				await new PersistentSubscriptionsClient(channelInfo.CallInvoker)
					.RestartSubsystemAsync(new Empty(), KurrentDBCallOptions
						.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken))
					.ConfigureAwait(false);
				return;
			}

			await HttpPost(
				path: "/subscriptions/restart",
				query: "",
				onNotFound: () =>
					throw new Exception("Unexpected exception while restarting the persistent subscription subsystem."),
				channelInfo, deadline, userCredentials, cancellationToken)
			.ConfigureAwait(false);
		}
	}
}
