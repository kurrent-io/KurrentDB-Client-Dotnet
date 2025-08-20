// ReSharper disable CheckNamespace

using static KurrentDB.Protocol.PersistentSubscriptions.V2.PersistentSubscriptionsErrorDetails;

namespace Kurrent.Client;

public static partial class ErrorDetails {
    /// <summary>
    /// Represents an error indicating that a persistent subscription has been dropped by the server.
    /// </summary>
    [KurrentOperationError(typeof(Types.PersistentSubscriptionMaximumSubscribersReached))]
    public partial record MaximumSubscribersReached;

    /// <summary>
    /// Represents an error indicating that a persistent subscription has been dropped by the server, typically due to an unexpected condition or configuration change.
    /// </summary>
    [KurrentOperationError(typeof(Types.PersistentSubscriptionDropped))]
    public partial record PersistentSubscriptionDropped;
}
