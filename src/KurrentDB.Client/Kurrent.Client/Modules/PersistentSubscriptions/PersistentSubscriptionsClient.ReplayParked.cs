// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using EventStore.Client;
using Grpc.Core;
using KurrentDB.Client;
using KurrentDB.Protocol.PersistentSubscriptions.V1;
using NotSupportedException = System.NotSupportedException;

namespace Kurrent.Client.PersistentSubscriptions;

partial class PersistentSubscriptionsClient {
    /// <summary>
    /// Retry the parked messages of the persistent subscription
    /// </summary>
    public async ValueTask<Result<Success, ReplayParkedMessagesToAllError>> ReplayParkedMessagesToAll(
        string groupName, long? stopAt = null, CancellationToken cancellationToken = default
    ) {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        try {
            if (!LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsToAll)
                throw new NotSupportedException("The server does not support persistent subscriptions to $all.");

            if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsReplayParked) {
                var request = new ReplayParkedReq {
                    Options = new ReplayParkedReq.Types.Options {
                        GroupName = groupName,
                        All       = new Empty()
                    }
                };

                await ReplayParkedGrpc(request, stopAt, cancellationToken).ConfigureAwait(false);

                return Success.Instance;
            }

            await ReplayParkedHttp(SystemStreams.AllStream, groupName, stopAt, cancellationToken)
                .ConfigureAwait(false);

            return Success.Instance;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, ReplayParkedMessagesToAllError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    /// <summary>
    /// Retry the parked messages of the persistent subscription
    /// </summary>
    public async ValueTask<Result<Success, ReplayParkedMessagesToStreamError>> ReplayParkedMessagesToStream(
        string streamName, string groupName, long? stopAt = null, CancellationToken cancellationToken = default
    ) {
        try {
            if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsReplayParked) {
                var req = new ReplayParkedReq {
                    Options = new ReplayParkedReq.Types.Options {
                        GroupName        = groupName,
                        StreamIdentifier = streamName
                    }
                };

                await ReplayParkedGrpc(req, stopAt, cancellationToken)
                    .ConfigureAwait(false);

                return Result.Success<Success, ReplayParkedMessagesToStreamError>(Success.Instance);
            }

            await ReplayParkedHttp(
                streamName, groupName, stopAt,
                cancellationToken
            ).ConfigureAwait(false);

            return Result.Success<Success, ReplayParkedMessagesToStreamError>(Success.Instance);
        }
        catch (RpcException rex) {
            return Result.Failure<Success, ReplayParkedMessagesToStreamError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    async ValueTask ReplayParkedGrpc(
        ReplayParkedReq request, long? numberOfEvents, CancellationToken cancellationToken
    ) {
        if (numberOfEvents.HasValue)
            request.Options.StopAt = numberOfEvents.Value;
        else
            request.Options.NoLimit = new Empty();

        await ServiceClient
            .ReplayParkedAsync(request, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    async ValueTask ReplayParkedHttp(string streamName, string groupName, long? numberOfEvents, CancellationToken cancellationToken) {
        var path  = $"/subscriptions/{UrlEncode(streamName)}/{UrlEncode(groupName)}/replayParked";
        var query = numberOfEvents.HasValue ? $"stopAt={numberOfEvents.Value}" : "";

        await HttpPost(
                path, query,
                () => throw new PersistentSubscriptionNotFoundException(streamName, groupName),
                cancellationToken
            )
            .ConfigureAwait(false);
    }
}
