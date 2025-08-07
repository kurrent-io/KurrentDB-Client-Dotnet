using KurrentDB.Client;

namespace Kurrent.Client.PersistentSubscriptions;

public static partial class PersistentSubscriptionsClientExtensions {
    public static ValueTask<Result<Success, DeleteSubscriptionError>> DeleteSubscriptionToAll(this PersistentSubscriptionsClient client, SubscriptionGroupName group, CancellationToken cancellationToken = default) =>
        client.DeleteSubscription(group, SystemStreams.AllStream, cancellationToken);
}
