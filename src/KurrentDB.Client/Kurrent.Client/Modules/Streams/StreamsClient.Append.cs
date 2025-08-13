#pragma warning disable CS8509

using Grpc.Core;
using Kurrent.Variant;
using static KurrentDB.Protocol.Streams.V2.MultiStreamAppendResponse;
using Contracts = KurrentDB.Protocol.Streams.V2;

namespace Kurrent.Client.Streams;

public partial class StreamsClient {
    public async ValueTask<Result<AppendStreamSuccesses, AppendStreamFailures>> Append(IAsyncEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default) {
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

            return response.ResultCase switch {
                ResultOneofCase.Success => response.Success.Map(),
                ResultOneofCase.Failure => response.Failure.Map(),
            };
        }
        catch (RpcException rex) {
            throw;

            // we have a problem here cause the error result must contain a list of failures or permission denied or others...
            // return Result.Failure<AppendStreamSuccesses, AppendStreamFailures>(rex.StatusCode switch {
            //     StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
            //     _                           => throw rex.WithOriginalCallStack()
            // });
        }
    }
}
