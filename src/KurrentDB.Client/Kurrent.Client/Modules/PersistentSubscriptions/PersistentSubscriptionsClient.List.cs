// ReSharper disable InvertIf

using EventStore.Client;
using Grpc.Core;
using KurrentDB.Client;
using KurrentDB.Protocol.PersistentSubscriptions.V1;

namespace Kurrent.Client.PersistentSubscriptions;

partial class PersistentSubscriptionsClient {
    /// <summary>
    /// Lists persistent subscriptions to $all.
    /// </summary>
    public async ValueTask<Result<List<PersistentSubscriptionInfo>, ListToAllError>> ListToAll(CancellationToken cancellationToken = default) {
        try {
            if (!LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsList)
                throw new NotSupportedException("The server does not support listing the persistent subscriptions.");

            var req = new ListReq {
                Options = new ListReq.Types.Options {
                    ListForStream = new ListReq.Types.StreamOption {
                        All = new Empty()
                    }
                }
            };

            var info = await ListGrpc(req, cancellationToken).ConfigureAwait(false);

            return info;
        }
        catch (RpcException rex) {
            return Result.Failure<List<PersistentSubscriptionInfo>, ListToAllError>(
                rex.StatusCode switch {
                    StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                    _                           => throw rex.WithOriginalCallStack()
                }
            );
        }
    }

    /// <summary>
    /// Lists persistent subscriptions to the specified stream.
    /// </summary>
    public async ValueTask<Result<List<PersistentSubscriptionInfo>, ListToStreamError>> ListToStream(
        string streamName, CancellationToken cancellationToken = default
    ) {
        try {
            if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsList) {
                var req = new ListReq {
                    Options = new ListReq.Types.Options {
                        ListForStream = new ListReq.Types.StreamOption {
                            Stream = streamName
                        }
                    }
                };

                return await ListGrpc(req, cancellationToken).ConfigureAwait(false);
            }

            var info = await ListHttpAsync().ConfigureAwait(false);

            return info;
        }
        catch (RpcException rex) {
            return Result.Failure<List<PersistentSubscriptionInfo>, ListToStreamError>(
                rex.StatusCode switch {
                    StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                    _                           => throw rex.WithOriginalCallStack()
                }
            );
        }

        async ValueTask<List<PersistentSubscriptionInfo>> ListHttpAsync() {
            var path = $"/subscriptions/{UrlEncode(streamName)}";
            var result = await HttpGet<IList<PersistentSubscriptionDto>>(
                    path,
                    () => throw new PersistentSubscriptionNotFoundException(streamName, string.Empty),
                    cancellationToken
                )
                .ConfigureAwait(false);

            return result.Select(PersistentSubscriptionInfo.From).ToList();
        }
    }

    /// <summary>
    /// Lists all persistent subscriptions to $all and streams.
    /// </summary>
    public async ValueTask<Result<List<PersistentSubscriptionInfo>, ListAllError>> ListAll(CancellationToken cancellationToken = default) {
        try {
            if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsList) {
                var req = new ListReq {
                    Options = new ListReq.Types.Options {
                        ListAllSubscriptions = new Empty()
                    }
                };

                return await ListGrpc(req, cancellationToken).ConfigureAwait(false);
            }

            var result = await HttpGet<IList<PersistentSubscriptionDto>>(
                    "/subscriptions",
                    () => throw new PersistentSubscriptionNotFoundException(string.Empty, string.Empty),
                    cancellationToken
                )
                .ConfigureAwait(false);

            return result.Select(PersistentSubscriptionInfo.From).ToList();
        }
        catch (RpcException rex) {
            return Result.Failure<List<PersistentSubscriptionInfo>, ListAllError>(
                rex.StatusCode switch {
                    StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                    _                           => throw rex.WithOriginalCallStack()
                }
            );
        }
    }

    async ValueTask<List<PersistentSubscriptionInfo>> ListGrpc(ListReq req, CancellationToken cancellationToken) {
        var response = await ServiceClient
            .ListAsync(req, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return response.Subscriptions.Select(PersistentSubscriptionInfo.From).ToList();
    }
}
