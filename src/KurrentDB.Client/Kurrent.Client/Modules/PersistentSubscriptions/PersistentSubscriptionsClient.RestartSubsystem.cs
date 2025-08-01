using EventStore.Client;
using Grpc.Core;
using Kurrent.Client.Legacy;
using KurrentDB.Client;

namespace Kurrent.Client.PersistentSubscriptions;

partial class PersistentSubscriptionsClient {
    /// <summary>
    /// Restarts the persistent subscriptions subsystem.
    /// </summary>
    public async ValueTask<Result<Success, RestartSubsystemError>> RestartSubsystem(CancellationToken cancellationToken = default) {
        try {
            if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsRestartSubsystem) {
                await ServiceClient
                    .RestartSubsystemAsync(new Empty(), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                return Result.Success<Success, RestartSubsystemError>(Success.Instance);
            }

            await HttpPost(
                    "/subscriptions/restart",
                    "",
                    () => throw new Exception("Unexpected exception while restarting the persistent subscription subsystem."),
                    cancellationToken
                )
                .ConfigureAwait(false);

            return Result.Success<Success, RestartSubsystemError>(Success.Instance);
        }
        catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
            return Result.Failure<Success, RestartSubsystemError>(
                ex switch {
                    AccessDeniedException     => rpcEx.AsAccessDeniedError(),
                    NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
                    _                         => throw KurrentClientException.CreateUnknown(nameof(DeleteToStream), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentClientException.CreateUnknown(nameof(DeleteToStream), ex);
        }
    }
}
