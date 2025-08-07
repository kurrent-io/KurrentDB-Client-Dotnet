using EventStore.Client;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Streams;
using KurrentDB.Client;
using KurrentDB.Protocol.PersistentSubscriptions.V1;

namespace Kurrent.Client.PersistentSubscriptions;

partial class PersistentSubscriptionsClient {
    /// <summary>
    /// Deletes a persistent subscription.
    /// </summary>
    public async ValueTask<Result<Success, DeleteSubscription>> DeleteSubscription(StreamName stream, string groupName, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        try {
            await EnsureCompatibility(stream, cancellationToken);

            var request = new DeleteReq {
                Options = new DeleteReq.Types.Options {
                    GroupName = groupName
                }
            };

            if (stream.IsAllStream)
                request.Options.All = new Empty();
            else
                request.Options.StreamIdentifier = stream.Value;

            await ServiceClient
                .DeleteAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException rex) {
            return Result.Failure<Success, DeleteSubscription>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

	public ValueTask<Result<Success, DeleteSubscription>> DeleteStreamSubscription(StreamName stream, string groupName, CancellationToken cancellationToken = default) =>
        DeleteSubscription(stream, groupName, cancellationToken);

	public ValueTask<Result<Success, DeleteSubscription>> DeleteAllStreamSubscription(string groupName, CancellationToken cancellationToken = default) =>
        DeleteSubscription(SystemStreams.AllStream, groupName, cancellationToken);
}
