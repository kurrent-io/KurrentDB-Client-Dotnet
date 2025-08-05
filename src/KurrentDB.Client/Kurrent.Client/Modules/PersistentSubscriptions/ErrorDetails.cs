using static KurrentDB.Protocol.PersistentSubscriptions.V2.PersistentSubscriptionsErrorDetails;
// ReSharper disable CheckNamespace

namespace Kurrent.Client;

public static partial class ErrorDetails {
    /// <summary>
    /// Represents an error indicating that a persistent subscription could not be found.
    /// </summary>
    [KurrentOperationError(typeof(Types.PersistentSubscriptionNotFound))]
    public readonly partial record struct PersistentSubscriptionNotFound;

    /// <summary>
    /// Represents an error indicating that a persistent subscription has been dropped by the server.
    /// </summary>
    [KurrentOperationError(typeof(Types.MaximumSubscribersReached))]
    public readonly partial record struct MaximumSubscribersReached;

    /// <summary>
    /// Represents an error indicating that a persistent subscription has been dropped by the server, typically due to an unexpected condition or configuration change.
    /// </summary>
    [KurrentOperationError(typeof(Types.PersistentSubscriptionDropped))]
    public readonly partial record struct PersistentSubscriptionDropped;
}
