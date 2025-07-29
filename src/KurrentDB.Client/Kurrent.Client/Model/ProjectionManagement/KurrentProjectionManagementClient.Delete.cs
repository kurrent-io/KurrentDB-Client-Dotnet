using EventStore.Client.Projections;

namespace Kurrent.Client;

public partial class KurrentProjectionManagementClient {
	/// Deletes a projection and optionally its associated streams.
	/// <param name="name">The name of the projection to be deleted.</param>
	/// <param name="deleteEmittedStreams">A boolean indicating whether to delete the emitted streams of the projection.</param>
	/// <param name="deleteStateStream">A boolean indicating whether to delete the state stream of the projection.</param>
	/// <param name="deleteCheckpointStream">A boolean indicating whether to delete the checkpoint stream of the projection. Defaults to false.</param>
	/// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete. Defaults to CancellationToken.None.</param>
	/// <return>A ValueTask that completes when the projection and optional streams have been deleted.</return>
	public async ValueTask Delete(
		string name, bool deleteEmittedStreams = false, bool deleteStateStream = false, bool deleteCheckpointStream = false, CancellationToken cancellationToken = default
	) {
		using var call = ServiceClient.DeleteAsync(
			new DeleteReq {
				Options = new DeleteReq.Types.Options {
					Name                   = name,
					DeleteCheckpointStream = deleteCheckpointStream,
					DeleteEmittedStreams   = deleteEmittedStreams,
					DeleteStateStream      = deleteStateStream,
				}
			}
		  , cancellationToken: cancellationToken
		);

		await call.ResponseAsync.ConfigureAwait(false);
	}
}
