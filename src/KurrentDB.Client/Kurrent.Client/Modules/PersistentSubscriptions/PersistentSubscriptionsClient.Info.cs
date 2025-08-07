// ReSharper disable InvertIf

using EventStore.Client;
using Grpc.Core;
using Kurrent.Client.Legacy;
using KurrentDB.Client;
using KurrentDB.Protocol.PersistentSubscriptions.V1;

namespace Kurrent.Client.PersistentSubscriptions;

partial class PersistentSubscriptionsClient {
    /// <summary>
    /// Gets the status of a persistent subscription to $all
    /// </summary>
    public async ValueTask<Result<PersistentSubscriptionInfo, GetInfoToAllError>> GetInfoToAll(
        string groupName, CancellationToken cancellationToken = default
    ) {
        try {
            if (!LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsGetInfo)
                throw new NotSupportedException("The server does not support getting persistent subscription details for $all");

            var req = new GetInfoReq {
                Options = new GetInfoReq.Types.Options {
                    GroupName = groupName,
                    All       = new Empty()
                }
            };

            var info = await GetInfoGrpc(req, cancellationToken).ConfigureAwait(false);

            return info;
        }
        catch (RpcException rex) {
            return Result.Failure<PersistentSubscriptionInfo, GetInfoToAllError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    /// <summary>
    /// Gets the status of a persistent subscription to a stream
    /// </summary>
    public async ValueTask<Result<PersistentSubscriptionInfo, GetInfoToStreamError>> GetInfoToStream(
        string streamName, string groupName, CancellationToken cancellationToken = default
    ) {
        try {
            if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsGetInfo) {
                var req = new GetInfoReq {
                    Options = new GetInfoReq.Types.Options {
                        GroupName        = groupName,
                        StreamIdentifier = streamName
                    }
                };

                return await GetInfoGrpc(req, cancellationToken).ConfigureAwait(false);
            }

            return await GetInfoHttp(streamName, groupName, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (RpcException rex) {
            return Result.Failure<PersistentSubscriptionInfo, GetInfoToStreamError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    async ValueTask<PersistentSubscriptionInfo> GetInfoGrpc(GetInfoReq req, CancellationToken cancellationToken) {
        var result = await ServiceClient
            .GetInfoAsync(req, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return PersistentSubscriptionInfo.From(result.SubscriptionInfo);
    }

    async ValueTask<PersistentSubscriptionInfo> GetInfoHttp(string streamName, string groupName, CancellationToken cancellationToken) {
        var path = $"/subscriptions/{UrlEncode(streamName)}/{UrlEncode(groupName)}/info";
        var result = await HttpGet<PersistentSubscriptionDto>(
                path,
                () => throw new PersistentSubscriptionNotFoundException(streamName, groupName),
                cancellationToken
            )
            .ConfigureAwait(false);

        return PersistentSubscriptionInfo.From(result);
    }
}
