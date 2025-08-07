// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using Grpc.Core;
using Kurrent.Client.Streams;
using KurrentDB.Client;
using static Kurrent.Client.PersistentSubscriptions.PersistentSubscriptionV1Mapper.Requests;

namespace Kurrent.Client.PersistentSubscriptions;

partial class PersistentSubscriptionsClient {
    /// <summary>
    /// Creates a persistent subscription to a specified stream.
    /// </summary>
    public async ValueTask<Result<Success, CreateStreamSubscriptionError>> CreateStreamSubscription(
        StreamName stream, string groupName, PersistentSubscriptionSettings settings, CancellationToken cancellationToken = default
    ) {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        try {
            await EnsureCompatibility(stream, cancellationToken);

            var request = CreateSubscriptionRequest(
                stream, groupName, settings,
                HeartbeatOptions.Default, ReadFilter.None
            );

            await ServiceClient
                .CreateAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException rex) {
            return Result.Failure<Success, CreateStreamSubscriptionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.AlreadyExists    => new ErrorDetails.AlreadyExists(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    /// <summary>
    /// Creates a persistent subscription to the $all stream with a specified filter.
    /// </summary>
    public async ValueTask<Result<Success, CreateAllStreamSubscriptionError>> CreateAllStreamSubscription(
        string groupName, ReadFilter filter, PersistentSubscriptionSettings settings, CancellationToken cancellationToken = default
    ) {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        try {
            var request = CreateSubscriptionRequest(
                SystemStreams.AllStream, groupName, settings,
                HeartbeatOptions.Default, filter
            );

            await ServiceClient
                .CreateAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException rex) {
            return Result.Failure<Success, CreateAllStreamSubscriptionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }
}
