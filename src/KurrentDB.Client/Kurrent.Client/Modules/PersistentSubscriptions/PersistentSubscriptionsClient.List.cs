// ReSharper disable InvertIf

using EventStore.Client;
using Grpc.Core;
using Kurrent.Client.Legacy;
using KurrentDB.Client;
using KurrentDB.Protocol.PersistentSubscriptions.V1;

namespace Kurrent.Client.PersistentSubscriptions;

partial class PersistentSubscriptionsClient {
    /// <summary>
    /// Lists persistent subscriptions to $all.
    /// </summary>
    public async ValueTask<Result<IEnumerable<PersistentSubscriptionInfo>, ListToAllError>> ListToAll(CancellationToken cancellationToken = default) {
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

            return Result.Success<IEnumerable<PersistentSubscriptionInfo>, ListToAllError>(info);
        }
        catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
            return Result.Failure<IEnumerable<PersistentSubscriptionInfo>, ListToAllError>(
                ex switch {
                    AccessDeniedException     => rpcEx.AsAccessDeniedError(),
                    NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
                    _                         => throw KurrentException.CreateUnknown(nameof(DeleteToStream), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentException.CreateUnknown(nameof(DeleteToStream), ex);
        }
    }

    /// <summary>
    /// Lists persistent subscriptions to the specified stream.
    /// </summary>
    public async ValueTask<Result<IEnumerable<PersistentSubscriptionInfo>, ListToStreamError>> ListToStream(
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

                return Result.Success<IEnumerable<PersistentSubscriptionInfo>, ListToStreamError>(await ListGrpc(req, cancellationToken).ConfigureAwait(false));
            }

            var info = await ListHttpAsync().ConfigureAwait(false);

            return Result.Success<IEnumerable<PersistentSubscriptionInfo>, ListToStreamError>(info);
        }
        catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
            return Result.Failure<IEnumerable<PersistentSubscriptionInfo>, ListToStreamError>(
                ex switch {
                    AccessDeniedException                       => rpcEx.AsAccessDeniedError(),
                    NotAuthenticatedException                   => rpcEx.AsNotAuthenticatedError(),
                    PersistentSubscriptionNotFoundException pEx => rpcEx.AsPersistentSubscriptionNotFoundError(pEx.StreamName, pEx.GroupName),
                    _                                           => throw KurrentException.CreateUnknown(nameof(DeleteToStream), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentException.CreateUnknown(nameof(DeleteToStream), ex);
        }

        async ValueTask<IEnumerable<PersistentSubscriptionInfo>> ListHttpAsync() {
            var path = $"/subscriptions/{UrlEncode(streamName)}";
            var result = await HttpGet<IList<PersistentSubscriptionDto>>(
                    path,
                    () => throw new PersistentSubscriptionNotFoundException(streamName, string.Empty),
                    cancellationToken
                )
                .ConfigureAwait(false);

            return result.Select(PersistentSubscriptionInfo.From);
        }
    }

    /// <summary>
    /// Lists all persistent subscriptions to $all and streams.
    /// </summary>
    public async ValueTask<Result<IEnumerable<PersistentSubscriptionInfo>, ListAllError>> ListAll(CancellationToken cancellationToken = default) {
        try {
            if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsList) {
                var req = new ListReq {
                    Options = new ListReq.Types.Options {
                        ListAllSubscriptions = new Empty()
                    }
                };

                return Result.Success<IEnumerable<PersistentSubscriptionInfo>, ListAllError>(await ListGrpc(req, cancellationToken).ConfigureAwait(false));
            }

            var result = await HttpGet<IList<PersistentSubscriptionDto>>(
                    "/subscriptions",
                    () => throw new PersistentSubscriptionNotFoundException(string.Empty, string.Empty),
                    cancellationToken
                )
                .ConfigureAwait(false);

            return Result.Success<IEnumerable<PersistentSubscriptionInfo>, ListAllError>(result.Select(PersistentSubscriptionInfo.From));
        }
        catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
            return Result.Failure<IEnumerable<PersistentSubscriptionInfo>, ListAllError>(
                ex switch {
                    AccessDeniedException     => rpcEx.AsAccessDeniedError(),
                    NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
                    _                         => throw KurrentException.CreateUnknown(nameof(DeleteToStream), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentException.CreateUnknown(nameof(DeleteToStream), ex);
        }
    }

    async ValueTask<IEnumerable<PersistentSubscriptionInfo>> ListGrpc(ListReq req, CancellationToken cancellationToken) {
        using var call     = ServiceClient.ListAsync(req, cancellationToken: cancellationToken);
        var       response = await call.ResponseAsync.ConfigureAwait(false);
        return response.Subscriptions.Select(PersistentSubscriptionInfo.From);
    }
}
