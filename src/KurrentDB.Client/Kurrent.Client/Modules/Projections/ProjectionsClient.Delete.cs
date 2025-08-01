using EventStore.Client.Projections;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Model;
using KurrentDB.Client;

namespace Kurrent.Client.Projections;

public partial class ProjectionsClient {
	/// Deletes a projection and optionally its associated streams.
	/// <param name="name">The name of the projection to be deleted.</param>
	/// <param name="deleteEmittedStreams">A boolean indicating whether to delete the emitted streams of the projection.</param>
	/// <param name="deleteStateStream">A boolean indicating whether to delete the state stream of the projection.</param>
	/// <param name="deleteCheckpointStream">A boolean indicating whether to delete the checkpoint stream of the projection. Defaults to false.</param>
	/// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete. Defaults to CancellationToken.None.</param>
	/// <return>A ValueTask that completes when the projection and optional streams have been deleted.</return>
	public async ValueTask<Result<Success, DeleteProjectionError>> Delete(
		string name, bool deleteEmittedStreams = false, bool deleteStateStream = false, bool deleteCheckpointStream = false,
		CancellationToken cancellationToken = default
	) {
		try {
			using var call = ServiceClient.DeleteAsync(
				new DeleteReq {
					Options = new DeleteReq.Types.Options {
						Name                   = name,
						DeleteCheckpointStream = deleteCheckpointStream,
						DeleteEmittedStreams   = deleteEmittedStreams,
						DeleteStateStream      = deleteStateStream,
					}
				},
				cancellationToken: cancellationToken
			);

			await call.ResponseAsync.ConfigureAwait(false);

			return new Result<Success, DeleteProjectionError>();
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, DeleteProjectionError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(Delete), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(Delete), ex);
		}
	}
}
