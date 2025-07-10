using EventStore.Client;
using EventStore.Client.PersistentSubscriptions;
using KurrentDB.Client;

namespace Kurrent.Client;

partial class KurrentPersistentSubscriptionsClient {
	/// <summary>
	/// Deletes a persistent subscription.
	/// </summary>
	public async Task DeleteToStream(string streamName, string groupName, CancellationToken cancellationToken = default) {
		await EnsureCompatibility(streamName, cancellationToken);

		var deleteOptions = new DeleteReq.Types.Options {
			GroupName = groupName
		};

		if (streamName is SystemStreams.AllStream)
			deleteOptions.All = new Empty();
		else
			deleteOptions.StreamIdentifier = streamName;

		var call = ServiceClient.DeleteAsync(new DeleteReq { Options = deleteOptions }, cancellationToken: cancellationToken);

		await call.ResponseAsync.ConfigureAwait(false);
	}

	/// <summary>
	/// Deletes a persistent subscription to $all.
	/// </summary>
	public async Task DeleteToAll(string groupName, CancellationToken cancellationToken = default) =>
		await DeleteToStream(SystemStreams.AllStream, groupName, cancellationToken).ConfigureAwait(false);
}
