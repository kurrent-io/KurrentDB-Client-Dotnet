using System.Runtime.CompilerServices;
using EventStore.Client;
using EventStore.Client.Projections;
using KurrentDB.Client;

namespace Kurrent.Client;

public partial class KurrentProjectionManagementClient {
	/// <summary>
	/// List the <see cref="ProjectionDetails"/> of all one-time projections.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public IAsyncEnumerable<ProjectionDetails> ListOneTimeAsync(CancellationToken cancellationToken = default) =>
		ListInternalAsync(
			new StatisticsReq.Types.Options {
				OneTime = new Empty(),
			},
			cancellationToken
		);

	/// <summary>
	/// List the <see cref="ProjectionDetails"/> of all continuous projections.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public IAsyncEnumerable<ProjectionDetails> ListContinuousAsync(CancellationToken cancellationToken = default) =>
		ListInternalAsync(
			new StatisticsReq.Types.Options {
				Continuous = new Empty()
			},
			cancellationToken
		);

	/// <summary>
	/// Gets the status of a projection.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ProjectionDetails?> GetStatusAsync(string name, CancellationToken cancellationToken = default) =>
		ListInternalAsync(
				new StatisticsReq.Types.Options {
					Name = name
				},
				cancellationToken
			)
			.FirstOrDefaultAsync(cancellationToken).AsTask();

	/// <summary>
	/// List the <see cref="ProjectionDetails"/> of all projections.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public IAsyncEnumerable<ProjectionDetails> ListAllAsync(CancellationToken cancellationToken = default) =>
		ListInternalAsync(
			new StatisticsReq.Types.Options {
				All = new Empty()
			},
			cancellationToken
		);

	async IAsyncEnumerable<ProjectionDetails> ListInternalAsync(
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
