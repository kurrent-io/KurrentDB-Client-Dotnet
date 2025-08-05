// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Operations;
using KurrentDB.Client;
using KurrentDB.Protocol.Operations.V1;

namespace Kurrent.Client.Admin;

public partial class AdminClient {
	public async ValueTask<Result<DatabaseScavengeResult, StartScavengeError>> StartScavenge(
		int threadCount = 1, int startFromChunk = 0, CancellationToken cancellationToken = default
	) {
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(threadCount);
		ArgumentOutOfRangeException.ThrowIfNegative(startFromChunk);

		try {
			using var call = OperationsServiceClient.StartScavengeAsync(
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
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<DatabaseScavengeResult, StartScavengeError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentException.CreateUnknown(nameof(StartScavenge), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentException.CreateUnknown(nameof(StartScavenge), ex);
		}
	}

	public async ValueTask<Result<DatabaseScavengeResult, StopScavengeError>> StopScavenge(string scavengeId, CancellationToken cancellationToken = default) {
		try {
			var result = await OperationsServiceClient.StopScavengeAsync(
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
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<DatabaseScavengeResult, StopScavengeError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentException.CreateUnknown(nameof(StopScavenge), ex)
				}
			);
		} catch (Exception ex) {
			return Result.Failure<DatabaseScavengeResult, StopScavengeError>(
				ex switch {
					ScavengeNotFoundException => ex.AsScavengeNotFoundError(),
					_                         => throw KurrentException.CreateUnknown(nameof(StopScavenge), ex)
				}
			);
		}
	}
}
