// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using EventStore.Client.Operations;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Model;
using KurrentDB.Client;

namespace Kurrent.Client;

public partial class KurrentOperationsClient {
	public async ValueTask<Result<DatabaseScavengeResult, StartScavengeError>> StartScavenge(
		int threadCount = 1, int startFromChunk = 0, CancellationToken cancellationToken = default
	) {
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(threadCount);
		ArgumentOutOfRangeException.ThrowIfNegative(startFromChunk);

		try {
			using var call = ServiceClient.StartScavengeAsync(
				new StartScavengeReq {
					Options = new StartScavengeReq.Types.Options {
						ThreadCount    = threadCount,
						StartFromChunk = startFromChunk
					}
				},
				cancellationToken: cancellationToken
			);

			var result = await call.ResponseAsync.ConfigureAwait(false);

			var scavengeResult = result.ScavengeResult switch {
				ScavengeResp.Types.ScavengeResult.Started    => DatabaseScavengeResult.Started(result.ScavengeId),
				ScavengeResp.Types.ScavengeResult.Stopped    => DatabaseScavengeResult.Stopped(result.ScavengeId),
				ScavengeResp.Types.ScavengeResult.InProgress => DatabaseScavengeResult.InProgress(result.ScavengeId),
				_                                            => DatabaseScavengeResult.Unknown(result.ScavengeId)
			};

			return Result.Success<DatabaseScavengeResult, StartScavengeError>(scavengeResult);
		} catch (RpcException ex) {
			return Result.Failure<DatabaseScavengeResult, StartScavengeError>(
				ex.StatusCode switch {
					StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
					StatusCode.Unauthenticated  => ex.AsNotAuthenticatedError(),
					_                           => throw KurrentClientException.CreateUnknown(nameof(Shutdown), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(StartScavenge), ex);
		}
	}

	public async ValueTask<Result<DatabaseScavengeResult, StopScavengeError>> StopScavenge(string scavengeId, CancellationToken cancellationToken = default) {
		try {
			var result = await ServiceClient.StopScavengeAsync(
					new StopScavengeReq {
						Options = new StopScavengeReq.Types.Options {
							ScavengeId = scavengeId
						}
					}
				  , cancellationToken: cancellationToken
				)
				.ResponseAsync.ConfigureAwait(false);

			var scavengeResult = result.ScavengeResult switch {
				ScavengeResp.Types.ScavengeResult.Started    => DatabaseScavengeResult.Started(result.ScavengeId),
				ScavengeResp.Types.ScavengeResult.Stopped    => DatabaseScavengeResult.Stopped(result.ScavengeId),
				ScavengeResp.Types.ScavengeResult.InProgress => DatabaseScavengeResult.InProgress(result.ScavengeId),
				_                                            => DatabaseScavengeResult.Unknown(result.ScavengeId)
			};

			return Result.Success<DatabaseScavengeResult, StopScavengeError>(scavengeResult);
		} catch (RpcException ex) {
			return Result.Failure<DatabaseScavengeResult, StopScavengeError>(
				ex.StatusCode switch {
					StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
					StatusCode.Unauthenticated  => ex.AsNotAuthenticatedError(),
					_                           => throw KurrentClientException.CreateUnknown(nameof(Shutdown), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(StopScavenge), ex);
		}
	}
}
