// ReSharper disable InconsistentNaming

using KurrentDB.Client;

using static Kurrent.Client.Model.PersistentSubscription.PersistentSubscriptionV1Mapper.Requests;

namespace Kurrent.Client;

public partial class KurrentPersistentSubscriptionsClient {
	/// <summary>
	/// Updates a persistent subscription on a specific stream with the given group name and subscription settings.
	/// </summary>
	/// <param name="streamName">The name of the stream to which the persistent subscription applies.</param>
	/// <param name="groupName">The name of the subscriber group for the persistent subscription.</param>
	/// <param name="settings">The settings for configuring the persistent subscription.</param>
	/// <param name="cancellationToken">An optional token to cancel the asynchronous operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async ValueTask UpdateToStream(string streamName, string groupName, PersistentSubscriptionSettings settings, CancellationToken cancellationToken = default) {
		await EnsureCompatibility(streamName, cancellationToken);
		var request = UpdateSubscriptionRequest(streamName, groupName, settings);
		using var call = ServiceClient.UpdateAsync(request, cancellationToken: cancellationToken);
		await call.ResponseAsync.ConfigureAwait(false);
	}

	/// <summary>
	/// Updates a persistent subscription on the global "$all" stream with the specified group name and subscription settings.
	/// </summary>
	/// <param name="groupName">The name of the subscriber group for the persistent subscription.</param>
	/// <param name="settings">The settings for configuring the persistent subscription.</param>
	/// <param name="cancellationToken">An optional token to cancel the asynchronous operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async ValueTask UpdateToAll(string groupName, PersistentSubscriptionSettings settings, CancellationToken cancellationToken = default) =>
		await UpdateToStream(SystemStreams.AllStream, groupName, settings, cancellationToken).ConfigureAwait(false);
}
