using EventStore.Client.Projections;

namespace Kurrent.Client;

public partial class KurrentProjectionManagementClient {
	public async ValueTask Delete(
		string name, bool deleteEmittedStreams, bool deleteStateStream, bool deleteCheckpointStream = false, CancellationToken cancellationToken = default
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
