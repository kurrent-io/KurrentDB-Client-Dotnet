// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using Grpc.Core;
using Kurrent.Client.Streams;
using static Kurrent.Client.PersistentSubscriptions.PersistentSubscriptionV1Mapper.Requests;

namespace Kurrent.Client.PersistentSubscriptions;

public partial class PersistentSubscriptionsClient {

    public async ValueTask<Result<Success, UpdateSubscriptionError>> UpdateSubscription(
        StreamName stream, string group, PersistentSubscriptionSettings settings, CancellationToken cancellationToken = default
    ) {
        try {
            await EnsureCompatibility(stream, cancellationToken);

            var request = UpdateSubscriptionRequest(stream, group, settings);

            await ServiceClient
                .UpdateAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Success.Instance;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, UpdateSubscriptionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }
}
