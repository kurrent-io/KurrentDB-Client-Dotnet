namespace Kurrent.Client.Legacy;

static class LegacyErrorCodes {
    // public const string InvalidTransaction        = "invalid-transaction";
    public const string StreamDeleted        = "stream-deleted";
    public const string WrongExpectedVersion = "wrong-expected-version";
    // public const string StreamNotFound            = "stream-not-found";
    public const string MaximumAppendSizeExceeded = "maximum-append-size-exceeded";

    public const string MissingRequiredMetadataProperty = "missing-required-metadata-property";

    public const string NotLeader                       = "not-leader";

    // public const string UserNotFound     = "user-not-found";
    // public const string UserConflict     = "user-conflict";
    // public const string ScavengeNotFound = "scavenge-not-found";

    public const string PersistentSubscriptionFailed       = "persistent-subscription-failed";
    public const string PersistentSubscriptionDoesNotExist = "persistent-subscription-does-not-exist";
    public const string PersistentSubscriptionExists       = "persistent-subscription-exists";
    public const string MaximumSubscribersReached          = "maximum-subscribers-reached";
    public const string PersistentSubscriptionDropped      = "persistent-subscription-dropped";
}
