using EventStore.Client;
using EventStore.Client.Projections;
using Grpc.Core;
using Kurrent.Client.Legacy;
using KurrentDB.Client;

namespace Kurrent.Client.Projections;

public partial class ProjectionsClient {
	public async ValueTask<Result<Success, UpdateError>> Update(
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

			var resp = await ServiceClient.UpdateAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);

			return resp is not null
				? new Result<Success, UpdateError>()
				: Result.Failure<Success, UpdateError>(new UpdateError());
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, UpdateError>(
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
