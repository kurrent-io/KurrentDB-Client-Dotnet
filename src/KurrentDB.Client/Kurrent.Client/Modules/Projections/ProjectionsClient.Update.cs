// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using EventStore.Client;
using Grpc.Core;
using KurrentDB.Protocol.Projections.V1;

namespace Kurrent.Client.Projections;

public partial class ProjectionsClient {
	public async ValueTask<Result<Success, UpdateProjectionError>> UpdateProjection(
		string name, string query, bool? emitEnabled = null, CancellationToken cancellationToken = default
	) {
		try {
			var options = new UpdateReq.Types.Options {
				Name  = name,
				Query = query
			};

			if (emitEnabled.HasValue)
				options.EmitEnabled = emitEnabled.Value;
			else
				options.NoEmitOptions = new Empty();

			var request = new UpdateReq {
				Options = options
			};

			var resp = await ServiceClient
                .UpdateAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

			return new Success();
		}
        catch (RpcException rex) {
            return Result.Failure<Success, UpdateProjectionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}
}
