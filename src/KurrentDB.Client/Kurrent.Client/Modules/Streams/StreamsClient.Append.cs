#pragma warning disable CS8509

using Kurrent.Client.Model;
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
                ResultOneofCase.Success => StreamsClientV2Mapper.Map((Types.Success)response.Success),
                ResultOneofCase.Failure => StreamsClientV2Mapper.Map((Types.Failure)response.Failure),
            };
        }
        catch (Exception ex) {
            throw KurrentClientException.CreateUnknown(nameof(Append), ex);
        }
    }
}
