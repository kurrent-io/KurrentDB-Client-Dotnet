using System.Runtime.CompilerServices;
using EventStore.Client;
using EventStore.Client.Projections;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Model;
using KurrentDB.Client;

namespace Kurrent.Client;

public partial class KurrentProjectionManagementClient {
	/// <summary>
	/// List the <see cref="ProjectionDetails"/> of all one-time projections.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Result<IAsyncEnumerable<ProjectionDetails>, ListOneTimeError> ListOneTime(CancellationToken cancellationToken = default) {
		try {
			var details = ListInternal(
				new StatisticsReq.Types.Options {
					OneTime = new Empty(),
				},
				cancellationToken
			);

			return Result.Success<IAsyncEnumerable<ProjectionDetails>, ListOneTimeError>(details);
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<IAsyncEnumerable<ProjectionDetails>, ListOneTimeError>(
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

	/// <summary>
	/// List the <see cref="ProjectionDetails"/> of all continuous projections.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Result<IAsyncEnumerable<ProjectionDetails>, ListContinuousError> ListContinuous(CancellationToken cancellationToken = default) {
		try {
			var details = ListInternal(
				new StatisticsReq.Types.Options {
					Continuous = new Empty()
				},
				cancellationToken
			);

			return Result.Success<IAsyncEnumerable<ProjectionDetails>, ListContinuousError>(details);
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<IAsyncEnumerable<ProjectionDetails>, ListContinuousError>(
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

	/// <summary>
	/// Gets the status of a projection.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async ValueTask<Result<ProjectionDetails, GetStatusError>> GetStatus(string name, CancellationToken cancellationToken = default) {
		try {
			var details = await ListInternal(
					new StatisticsReq.Types.Options {
						Name = name
					},
					cancellationToken
				)
				.FirstOrDefaultAsync(cancellationToken).AsTask();

			return Result.Success<ProjectionDetails, GetStatusError>(details ?? ProjectionDetails.None);
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<ProjectionDetails, GetStatusError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(GetStatus), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(GetStatus), ex);
		}
	}

	/// <summary>
	/// List the <see cref="ProjectionDetails"/> of all projections.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Result<IAsyncEnumerable<ProjectionDetails>, ListAllError> ListAll(CancellationToken cancellationToken = default) {
		try {
			var details = ListInternal(
				new StatisticsReq.Types.Options {
					All = new Empty()
				},
				cancellationToken
			);

			return Result.Success<IAsyncEnumerable<ProjectionDetails>, ListAllError>(details);
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<IAsyncEnumerable<ProjectionDetails>, ListAllError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(ListAll), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(Delete), ex);
		}
	}

	async IAsyncEnumerable<ProjectionDetails> ListInternal(
		StatisticsReq.Types.Options options,
		[EnumeratorCancellation] CancellationToken cancellationToken
	) {
		using var call = ServiceClient.Statistics(
			new StatisticsReq {
				Options = options
			},
			cancellationToken: cancellationToken
		);

		await foreach (var projectionDetails in call.ResponseStream.ReadAllAsync(cancellationToken)
			               .Select(ConvertToProjectionDetails)
			               .WithCancellation(cancellationToken)
			               .ConfigureAwait(false)) {
			yield return projectionDetails;
		}
	}

	static ProjectionDetails ConvertToProjectionDetails(StatisticsResp response) {
		var details = response.Details;

		return new ProjectionDetails(
			details.CoreProcessingTime, details.Version, details.Epoch,
			details.EffectiveName, details.WritesInProgress, details.ReadsInProgress,
			details.PartitionsCached,
			details.Status, details.StateReason, details.Name,
			details.Mode, details.Position, details.Progress,
			details.LastCheckpoint, details.EventsProcessedAfterRestart, details.CheckpointStatus,
			details.BufferedEvents, details.WritePendingEventsBeforeCheckpoint,
			details.WritePendingEventsAfterCheckpoint
		);
	}
}
