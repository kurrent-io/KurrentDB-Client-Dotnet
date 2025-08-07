// ReSharper disable CheckNamespace

using Grpc.Core;
using KurrentDB.Client;
using static KurrentDB.Protocol.PersistentSubscriptions.V2.PersistentSubscriptionsErrorDetails;

namespace Kurrent.Client;

public static partial class ErrorDetails {
    /// <summary>
    /// Represents an error indicating that a persistent subscription has been dropped by the server.
    /// </summary>
    [KurrentOperationError(typeof(Types.PersistentSubscriptionMaximumSubscribersReached))]
    public readonly partial record struct MaximumSubscribersReached;

    /// <summary>
    /// Represents an error indicating that a persistent subscription has been dropped by the server, typically due to an unexpected condition or configuration change.
    /// </summary>
    [KurrentOperationError(typeof(Types.PersistentSubscriptionDropped))]
    public readonly partial record struct PersistentSubscriptionDropped;
}

public static partial class ErrorDetails {
    // public static PersistentSubscriptionNotFound AsPersistentSubscriptionNotFoundError(this Exception ex, string streamName, string groupName) =>
    //     ex is RpcException
    //         ? new(x => x.With("Stream", streamName).With("group", groupName))
    //         : throw new InvalidCastException($"Expected {nameof(PersistentSubscriptionNotFoundException)} but got {ex.GetType().Name} while mapping to {nameof(PersistentSubscriptionNotFound)}.", ex);
    //
    // public static MaximumSubscribersReached AsMaximumSubscribersReachedError(this Exception ex, string streamName, string groupName) =>
    //     ex is RpcException
    //         ? new(x => x.With("Stream", streamName).With("group", groupName))
    //         : throw new InvalidCastException($"Expected {nameof(MaximumSubscribersReachedException)} but got {ex.GetType().Name} while mapping to {nameof(MaximumSubscribersReached)}.", ex);
    //
    // public static PersistentSubscriptionDropped AsPersistentSubscriptionDroppedError(this Exception ex, string streamName, string groupName) =>
    //     ex is RpcException
    //         ? new(x => x.With("Stream", streamName).With("group", groupName))
    //         : throw new InvalidCastException($"Expected {nameof(PersistentSubscriptionDroppedByServerException)} but got {ex.GetType().Name} while mapping to {nameof(PersistentSubscriptionDropped)}.", ex);


    // public static StreamRevisionConflict AsStreamRevisionConflictError(this Exception ex) {
    //     return ex.MapToResultError(
    //         LegacyErrorCodes.WrongExpectedVersion,
    //         static rex => new StreamRevisionConflict(x => x
    //             .With<StreamName>("Stream", rex.Trailers.GetValue("stream-name") ?? StreamName.None)
    //             .With("ExpectedRevision", GetStreamRevision(rex.Trailers, "expected-version"))
    //             .With("ActualRevision", GetStreamRevision(rex.Trailers, "actual-version"))
    //         )
    //     );
    //
    //     static StreamRevision GetStreamRevision(global::Grpc.Core.Metadata metadata, string key) =>
    //         metadata.GetValue(key) is { } val && long.TryParse(val, out var revision)
    //             ? StreamRevision.From(revision) : ExpectedStreamState.NoStream;
    // }
}
