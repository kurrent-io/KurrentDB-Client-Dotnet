// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using EventStore.Client;
using Grpc.Core;

namespace Kurrent.Client.PersistentSubscriptions;

partial class PersistentSubscriptionsClient {
    public async ValueTask<Result<Success, RestartPersistentSubscriptionsSubsystemError>> RestartPersistentSubscriptionsSubsystem(CancellationToken cancellationToken = default) {
        try {
            if (LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsRestartSubsystem) {
                await ServiceClient
                    .RestartSubsystemAsync(new Empty(), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            else {
                await HttpPost(
                        "/subscriptions/restart", "",
                        () => throw new Exception("Unexpected exception while restarting the persistent subscription subsystem."),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

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
