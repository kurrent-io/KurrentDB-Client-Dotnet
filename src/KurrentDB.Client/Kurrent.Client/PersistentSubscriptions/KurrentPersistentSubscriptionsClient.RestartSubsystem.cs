using EventStore.Client;
using KurrentDB.Client;

namespace Kurrent.Client;

partial class KurrentPersistentSubscriptionsClient {
	/// <summary>
	/// Restarts the persistent subscriptions subsystem.
	/// </summary>
	public async Task RestartSubsystemAsync(CancellationToken cancellationToken = default) {
		if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsRestartSubsystem) {
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
