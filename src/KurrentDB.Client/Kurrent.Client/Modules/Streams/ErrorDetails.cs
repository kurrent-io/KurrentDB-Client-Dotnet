// ReSharper disable CheckNamespace

using Types = KurrentDB.Protocol.Streams.V2.StreamsErrorDetails.Types;

namespace Kurrent.Client;

static class LegacyExceptions {
		public const string ExceptionKey = "exception";

		public const string AccessDenied                    = "access-denied";
		public const string InvalidTransaction              = "invalid-transaction";
		public const string StreamDeleted                   = "stream-deleted";
		public const string WrongExpectedVersion            = "wrong-expected-version";
		public const string StreamNotFound                  = "stream-not-found";
		public const string MaximumAppendSizeExceeded       = "maximum-append-size-exceeded";
		public const string MissingRequiredMetadataProperty = "missing-required-metadata-property";
		public const string NotLeader                       = "not-leader";

		public const string PersistentSubscriptionFailed       = "persistent-subscription-failed";
		public const string PersistentSubscriptionDoesNotExist = "persistent-subscription-does-not-exist";
		public const string PersistentSubscriptionExists       = "persistent-subscription-exists";
		public const string MaximumSubscribersReached          = "maximum-subscribers-reached";
		public const string PersistentSubscriptionDropped      = "persistent-subscription-dropped";

		public const string UserNotFound = "user-not-found";
		public const string UserConflict = "user-conflict";

		public const string ScavengeNotFound = "scavenge-not-found";
	}

public static partial class ErrorDetails {

    public static readonly Dictionary<string, string> Map = new() {
        [LegacyExceptions.AccessDenied]              = nameof(AccessDenied),
        [LegacyExceptions.InvalidTransaction]        = nameof(TransactionMaxSizeExceeded),
        [LegacyExceptions.StreamDeleted]             = nameof(StreamDeleted),
        [LegacyExceptions.WrongExpectedVersion]      = nameof(StreamRevisionConflict),
        [LegacyExceptions.StreamNotFound]            = nameof(StreamNotFound),
        [LegacyExceptions.MaximumAppendSizeExceeded] = nameof(TransactionMaxSizeExceeded),
        [LegacyExceptions.PersistentSubscriptionFailed]       = nameof(PersistentSubscriptionDropped),
        [LegacyExceptions.PersistentSubscriptionDoesNotExist] = nameof(PersistentSubscriptionNotFound),
        [LegacyExceptions.PersistentSubscriptionExists]       = nameof(PersistentSubscriptionDropped),
        [LegacyExceptions.MaximumSubscribersReached]          = nameof(MaximumSubscribersReached),
        [LegacyExceptions.UserNotFound]                       = nameof(UserNotFound),
        [LegacyExceptions.ScavengeNotFound]                   = nameof(ScavengeNotFound),

        // [LegacyExceptions.MissingRequiredMetadataProperty]    = nameof(MissingRequiredMetadataProperty),
        // [LegacyExceptions.NotLeader]                          = nameof(NotLeader),
        // [LegacyExceptions.UserConflict]                       = nameof(UserConflict)
    };
}

public static partial class ErrorDetails {
    /// <summary>
    /// Represents an error indicating that the specified stream could not be found.
    /// </summary>
    [KurrentOperationError(typeof(Types.StreamNotFound))]
    public readonly partial record struct StreamNotFound;

    /// <summary>
    /// Represents an error indicating that the specified stream has been deleted.
    /// </summary>
    [KurrentOperationError(typeof(Types.StreamDeleted))]
    public readonly partial record struct StreamDeleted;

    /// <summary>
    /// Represents an error indicating that the specified stream has been tombstoned, meaning it is no longer available for appending.
    /// </summary>
    [KurrentOperationError(typeof(Types.StreamTombstoned))]
    public readonly partial record struct StreamTombstoned;

    /// <summary>
    /// Indicates an error where the maximum allowable size of a transaction has been exceeded.
    /// </summary>
    [KurrentOperationError(typeof(Types.TransactionMaxSizeExceeded))]
    public readonly partial record struct TransactionMaxSizeExceeded;

    /// <summary>
    /// Represents a failure due to an unexpected revision conflict during an append operation.
    /// </summary>
    [KurrentOperationError(typeof(Types.StreamRevisionConflict))]
    public readonly partial record struct StreamRevisionConflict;

    /// <summary>
    /// Represents an error indicating that the specified log position could not be found in the transaction log.
    /// </summary>
    [KurrentOperationError(typeof(Types.LogPositionNotFound))]
    public readonly partial record struct LogPositionNotFound;
}
