// ReSharper disable InconsistentNaming
// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using EventStore.Client;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Registry;
using Kurrent.Client.Schema.Serialization;
using Kurrent.Client.Streams;
using KurrentDB.Protocol.PersistentSubscriptions.V1;
using Microsoft.Extensions.Logging;

using PersistentSubscriptionsServiceClient = KurrentDB.Protocol.PersistentSubscriptions.V1.PersistentSubscriptions.PersistentSubscriptionsClient;

using static Kurrent.Client.PersistentSubscriptions.PersistentSubscriptionV1Mapper.Requests;

namespace Kurrent.Client.PersistentSubscriptions;

public sealed partial class PersistentSubscriptionsClient : SubClientBase {
    internal PersistentSubscriptionsClient(KurrentClient client) : base(client) =>
        ServiceClient = new(client.LegacyCallInvoker);

    PersistentSubscriptionsServiceClient ServiceClient { get; }

    public async ValueTask<Result<Success, CreateSubscriptionError>> CreateSubscription(
        SubscriptionGroupName group, StreamName stream, ReadFilter filter, PersistentSubscriptionSettings settings, CancellationToken cancellationToken = default
    ) {
        group.ThrowIfInvalid();
        stream.ThrowIfInvalid();

        if (stream.IsAllStream && !ServerCapabilities.SupportsPersistentSubscriptionsToAll)
            throw new NotSupportedException("The server does not support persistent subscriptions to $all.");

        if (!stream.IsAllStream && filter != ReadFilter.None)
            throw new NotSupportedException("Filtering is only supported for persistent subscriptions to $all.");

        try {
            var request = CreateSubscriptionRequest(
                stream, group, settings,
                HeartbeatOptions.Default,
                filter
            );

            await ServiceClient
                .CreateAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Success.Instance;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, CreateSubscriptionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.AlreadyExists    => new ErrorDetails.AlreadyExists(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    public async ValueTask<Result<Success, UpdateSubscriptionError>> UpdateSubscription(
        SubscriptionGroupName group, StreamName stream, PersistentSubscriptionSettings settings, CancellationToken cancellationToken = default
    ) {
        group.ThrowIfInvalid();
        stream.ThrowIfInvalid();

        if (stream.IsAllStream && !ServerCapabilities.SupportsPersistentSubscriptionsToAll)
            throw new NotSupportedException("The server does not support persistent subscriptions to $all.");

        try {
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

    public async ValueTask<Result<Success, DeleteSubscriptionError>> DeleteSubscription(SubscriptionGroupName group, StreamName stream, CancellationToken cancellationToken = default) {
        group.ThrowIfInvalid();
        stream.ThrowIfInvalid();

        if (stream.IsAllStream && !ServerCapabilities.SupportsPersistentSubscriptionsToAll)
            throw new NotSupportedException("The server does not support persistent subscriptions to $all.");

        try {
            var request = new DeleteReq {
                Options = new DeleteReq.Types.Options {
                    GroupName = group
                }
            };

            if (stream.IsAllStream)
                request.Options.All = new Empty();
            else
                request.Options.StreamIdentifier = stream.Value;

            await ServiceClient
                .DeleteAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Success.Instance;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, DeleteSubscriptionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    public async ValueTask<Result<PersistentSubscriptionDetails, GetSubscriptionError>> GetSubscription(
        SubscriptionGroupName group, StreamName stream, CancellationToken cancellationToken = default
    ) {
        group.ThrowIfInvalid();
        stream.ThrowIfInvalid();

        if (stream.IsAllStream && !ServerCapabilities.SupportsPersistentSubscriptionsToAll)
            throw new NotSupportedException("The server does not support persistent subscriptions to $all.");

        try {
            var request = new GetInfoReq {
                Options = new() {
                    GroupName = group
                }
            };

            if (stream.IsAllStream)
                request.Options.All = new Empty();
            else
                request.Options.StreamIdentifier = stream.Value;

            var response = await ServiceClient
                .GetInfoAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return PersistentSubscriptionDetails.From(response.SubscriptionInfo);
        }
        catch (RpcException rex) {
            return Result.Failure<PersistentSubscriptionDetails, GetSubscriptionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    /// <summary>
    /// Lists all persistent subscriptions.
    /// If the <see cref="ListPersistentSubscriptionsOptions.Stream"/> is set to <see cref="StreamName.None"/>, it lists all persistent subscriptions across all streams.
    /// If the <see cref="ListPersistentSubscriptionsOptions.Stream"/> is set to a specific stream, it lists all persistent subscriptions for that stream.
    /// </summary>
    public async ValueTask<Result<List<PersistentSubscriptionDetails>, ListSubscriptionsError>> ListSubscriptions(ListPersistentSubscriptionsOptions options, CancellationToken cancellationToken = default) {
        try {
            if (options.Stream.IsAllStream && ServerCapabilities.SupportsPersistentSubscriptionsList)
                throw new NotSupportedException("The server does not support persistent subscriptions to $all.");

            var request = new ListReq { Options = new() };

            if(options.Stream == StreamName.None)
                request.Options.ListAllSubscriptions = new Empty();
            else if (options.Stream.IsAllStream)
                request.Options.ListForStream = new() { All = new Empty() };
            else
                request.Options.ListForStream = new() { Stream = options.Stream.Value };

            var response = await ServiceClient
                .ListAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var result = response.Subscriptions
                .Select(PersistentSubscriptionDetails.From)
                .ToList();

            return result;
        }
        catch (RpcException rex) {
            return Result.Failure<List<PersistentSubscriptionDetails>, ListSubscriptionsError>(
                rex.StatusCode switch {
                    StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                    _                           => throw rex.WithOriginalCallStack()
                }
            );
        }
    }

    public async ValueTask<Result<Success, ReplaySubscriptionParkedMessagesError>> ReplaySubscriptionParkedMessages(
        SubscriptionGroupName group, StreamName stream, long? stopAt = null, CancellationToken cancellationToken = default
    ) {
        group.ThrowIfInvalid();
        stream.ThrowIfInvalid();

        if (stream.IsAllStream && ServerCapabilities.SupportsPersistentSubscriptionsList)
            throw new NotSupportedException("The server does not support persistent subscriptions to $all.");

        try {
            var request = new ReplayParkedReq {
                Options = new ReplayParkedReq.Types.Options {
                    GroupName = group,
                }
            };

            if (stream.IsAllStream)
                request.Options.All = new Empty();
            else
                request.Options.StreamIdentifier = stream.Value;

            if (stopAt.HasValue)
                request.Options.StopAt = stopAt.Value;
            else
                request.Options.NoLimit = new Empty();

            await ServiceClient
                .ReplayParkedAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Success.Instance;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, ReplaySubscriptionParkedMessagesError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    public async ValueTask<Result<Success, RestartPersistentSubscriptionsSubsystemError>> RestartPersistentSubscriptionsSubsystem(CancellationToken cancellationToken = default) {
        try {
            if (ServerCapabilities.SupportsPersistentSubscriptionsRestartSubsystem)
                throw new NotSupportedException("The server does not support restarting the persistent subscriptions subsystem.");

            await ServiceClient
                .RestartSubsystemAsync(new Empty(), cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Success.Instance;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, RestartPersistentSubscriptionsSubsystemError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }
}
