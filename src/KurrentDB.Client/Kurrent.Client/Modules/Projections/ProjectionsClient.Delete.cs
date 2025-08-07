// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using Grpc.Core;
using KurrentDB.Protocol.Projections.V1;

namespace Kurrent.Client.Projections;

public partial class ProjectionsClient {
	public async ValueTask<Result<Success, DeleteProjectionError>> DeleteProjection(
		string name, bool deleteEmittedStreams = false, bool deleteStateStream = false, bool deleteCheckpointStream = false,
		CancellationToken cancellationToken = default
	) {
		try {
            var request = new DeleteReq {
                Options = new() {
                    Name                   = name,
                    DeleteCheckpointStream = deleteCheckpointStream,
                    DeleteEmittedStreams   = deleteEmittedStreams,
                    DeleteStateStream      = deleteStateStream,
                }
            };

			await ServiceClient
                .DeleteAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException rex) {
            return Result.Failure<Success, DeleteProjectionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}
}
