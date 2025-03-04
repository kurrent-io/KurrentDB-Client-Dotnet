using EventStore.Client;
using EventStore.Client.PersistentSubscriptions;

#nullable enable
namespace KurrentDb.Client {
	partial class KurrentDBPersistentSubscriptionsClient {
		/// <summary>
		/// Restarts the persistent subscriptions subsystem.
		/// </summary>
		public async Task RestartSubsystemAsync(TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			if (channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsRestartSubsystem) {
				await new PersistentSubscriptions.PersistentSubscriptionsClient(channelInfo.CallInvoker)
					.RestartSubsystemAsync(new Empty(), KurrentCallOptions
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
