#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.

// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using EventStore.Client;
using Grpc.Core;
using KurrentDB.Protocol.Projections.V1;

namespace Kurrent.Client.Projections;

public partial class ProjectionsClient {
	public async ValueTask<Result<List<ProjectionDetails>, ListProjectionsError>> ListProjections(ListProjectionsOptions options, CancellationToken cancellationToken = default) {
        var request = new StatisticsReq {
            Options = options.Mode switch {
                ProjectionMode.Unspecified => new() { All        = new Empty() },
                ProjectionMode.OneTime     => new() { OneTime    = new Empty() },
                ProjectionMode.Continuous  => new() { Continuous = new Empty() },
                ProjectionMode.Transient   => new() { Transient  = new Empty() }
            }
        };

        try {
            using var call = ServiceClient.Statistics(request, cancellationToken: cancellationToken);

            var result = await call.ResponseStream
                .ReadAllAsync(cancellationToken)
                .Select(MapToProjectionDetails)
                .ToListAsync(cancellationToken);

            return result;
        }
        catch (RpcException rex) {
            return Result.Failure<List<ProjectionDetails>, ListProjectionsError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}

	public async ValueTask<Result<ProjectionDetails, GetProjectionError>> GetProjection(string name, CancellationToken cancellationToken = default) {
        try {
            var request = new StatisticsReq {
                Options =  new StatisticsReq.Types.Options {
                    Name = name
                }
            };

            using var call = ServiceClient.Statistics(request, cancellationToken: cancellationToken);

            var result = await call.ResponseStream
                .ReadAllAsync(cancellationToken)
                .Select(MapToProjectionDetails)
                .FirstAsync(cancellationToken);

            return result;
        }
        catch (RpcException rex) {
            return Result.Failure<ProjectionDetails, GetProjectionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}

	static ProjectionDetails MapToProjectionDetails(StatisticsResp response) {
		var details = response.Details;

		return new ProjectionDetails(
			details.CoreProcessingTime,
            details.Version,
            details.Epoch,
			details.EffectiveName,
            details.WritesInProgress,
            details.ReadsInProgress,
			details.PartitionsCached,
			details.Status,
            details.StateReason,
            details.Name,
			details.Mode,
            details.Position,
            details.Progress,
			details.LastCheckpoint,
            details.EventsProcessedAfterRestart,
            details.CheckpointStatus,
			details.BufferedEvents,
            details.WritePendingEventsBeforeCheckpoint,
			details.WritePendingEventsAfterCheckpoint
		);
	}
}
