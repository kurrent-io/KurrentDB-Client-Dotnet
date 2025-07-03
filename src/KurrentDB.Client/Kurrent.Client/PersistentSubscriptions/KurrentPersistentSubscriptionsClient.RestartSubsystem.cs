using EventStore.Client;
using KurrentDB.Client;

namespace Kurrent.Client;

partial class KurrentPersistentSubscriptionsClient {
	/// <summary>
	/// Restarts the persistent subscriptions subsystem.
	/// </summary>
	public async Task RestartSubsystemAsync(
		TimeSpan? deadline = null, UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default
	) {
		var channelInfo = await LegacyClient.GetChannelInfo(cancellationToken);
		if (channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsRestartSubsystem) {
			await ServiceClient
				.RestartSubsystemAsync(new Empty(), cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			return;
		}

		await HttpPost(
				path: "/subscriptions/restart",
				query: "",
				onNotFound: () => throw new Exception("Unexpected exception while restarting the persistent subscription subsystem."),
				cancellationToken
			)
			.ConfigureAwait(false);
	}
}
