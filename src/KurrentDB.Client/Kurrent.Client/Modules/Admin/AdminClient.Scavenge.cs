#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.

using Grpc.Core;
using KurrentDB.Protocol.Operations.V1;

namespace Kurrent.Client.Admin;

public partial class AdminClient {
	public async ValueTask<Result<ScavengeResult, StartScavengeError>> StartScavenge(
		int threadCount = 1, int startFromChunk = 0, CancellationToken cancellationToken = default
	) {
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(threadCount);
		ArgumentOutOfRangeException.ThrowIfNegative(startFromChunk);

		try {
            var request = new StartScavengeReq {
                Options = new StartScavengeReq.Types.Options {
                    ThreadCount    = threadCount,
                    StartFromChunk = startFromChunk
                }
            };

            var response = await AdminServiceClient
                .StartScavengeAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

			var result = response.ScavengeResult switch {
				ScavengeResp.Types.ScavengeResult.Started    => ScavengeResult.Started(response.ScavengeId),
				ScavengeResp.Types.ScavengeResult.Stopped    => ScavengeResult.Stopped(response.ScavengeId),   // TODO SS: what?! why would it return STOPPED on START?
				ScavengeResp.Types.ScavengeResult.InProgress => ScavengeResult.InProgress(response.ScavengeId)
			};

			return result;
		}
        catch (RpcException rex) {
            return Result.Failure<ScavengeResult, StartScavengeError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}

	public async ValueTask<Result<ScavengeResult, StopScavengeError>> StopScavenge(string scavengeId, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(scavengeId);

        try {
            var request = new StopScavengeReq {
                Options = new StopScavengeReq.Types.Options {
                    ScavengeId = scavengeId
                }
            };

			var response = await AdminServiceClient
                .StopScavengeAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

			var result = response.ScavengeResult switch {
				ScavengeResp.Types.ScavengeResult.Started    => ScavengeResult.Started(response.ScavengeId), // TODO SS: what?! why would it return STARTED ON STOP?
				ScavengeResp.Types.ScavengeResult.Stopped    => ScavengeResult.Stopped(response.ScavengeId),
				ScavengeResp.Types.ScavengeResult.InProgress => ScavengeResult.InProgress(response.ScavengeId)
			};

			return result;
		}
        catch (RpcException rex) {
            return Result.Failure<ScavengeResult, StopScavengeError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}
}
