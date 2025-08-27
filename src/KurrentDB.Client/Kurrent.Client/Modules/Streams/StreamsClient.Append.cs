// ReSharper disable RedundantCatchClause

#pragma warning disable CS8509

using System.Diagnostics;
using Grpc.Core;
using KurrentDB.Diagnostics;
using KurrentDB.Diagnostics.Tracing;
using static KurrentDB.Protocol.Streams.V2.MultiStreamAppendResponse;
using Contracts = KurrentDB.Protocol.Streams.V2;

namespace Kurrent.Client.Streams;

public partial class StreamsClient {
    public async ValueTask<Result<AppendStreamSuccesses, AppendStreamFailures>> Append(IAsyncEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default) {
        var tags = Tags.WithRequiredTag(TraceConstants.Tags.DatabaseOperationName, TraceConstants.Operations.Append);

        var activity = KurrentActivitySource.StartAppendActivity(tags);

        try {
            using var session = ServiceClient.MultiStreamAppendSession(cancellationToken: cancellationToken);

            await foreach (var request in requests.WithCancellation(cancellationToken)) {
                var records = await request.Messages
                    .Map(request.Stream, SerializerProvider, cancellationToken)
                    .ToArrayAsync(cancellationToken)
                    .ConfigureAwait(false);

                var serviceRequest = new Contracts.AppendStreamRequest {
                    Stream           = request.Stream,
                    ExpectedRevision = request.ExpectedState,
                    Records          = { records }
                };

                // Cancellation of stream writes is not supported by this gRPC implementation.
                // To cancel the operation, we should cancel the entire session.
                await session.RequestStream
                    .WriteAsync(serviceRequest, CancellationToken.None)
                    .ConfigureAwait(false);
            }

            await session.RequestStream.CompleteAsync();

            var response = await session.ResponseAsync;

            if (response.ResultCase is ResultOneofCase.Failure) {
	            var failures = response.Failure.Map();
	            activity.FailActivity(failures);
	            return failures;
            }

            activity.CompleteActivity();
            return response.Success.Map();
        }
        catch (RpcException rex) {
	        activity.FailActivity(rex);
            throw;

            // we have a problem here cause the error result must contain a list of failures or permission denied or others...
            // return Result.Failure<AppendStreamSuccesses, AppendStreamFailures>(rex.StatusCode switch {
            //     StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
            //     _                           => throw rex.WithOriginalCallStack()
            // });
        }
    }
}

#region tracing

static class AppendActivityExtensions {
	public static void FailActivity(this Activity? activity, RpcException exception) {
		if (activity is null)
			return;

		if (activity.IsAllDataRequested) {
			activity.SetStatus(ActivityStatusCode.Error);
			activity.AddException(exception);
		}

		activity.Dispose();
	}

	public static void FailActivity(this Activity? activity, AppendStreamFailures failures) {
		if (activity is null)
			return;

		if (activity.IsAllDataRequested) {
			activity.SetStatus(ActivityStatusCode.Error);
			failures.ForEach(failure => activity.AddException(failure.CreateException()));
		}

		activity.Dispose();
	}

	public static void CompleteActivity(this Activity? activity) {
		if (activity is null)
			return;

		if (activity.IsAllDataRequested)
			activity.SetStatus(ActivityStatusCode.Ok);

		activity.Dispose();
	}
}

#endregion
