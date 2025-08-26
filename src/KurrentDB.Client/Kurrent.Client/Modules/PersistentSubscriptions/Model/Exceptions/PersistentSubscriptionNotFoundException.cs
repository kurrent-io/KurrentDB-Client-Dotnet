namespace Kurrent.Client.PersistentSubscriptions;

/// <summary>
/// The exception that is thrown when a persistent subscription is not found.
/// </summary>
class PersistentSubscriptionNotFoundException : KurrentException {
    /// <summary>
    /// The group name.
    /// </summary>
    public readonly string GroupName;

    /// <summary>
    /// The stream name.
    /// </summary>
    public readonly string StreamName;

    /// <summary>
    /// Constructs a new <see cref="PersistentSubscriptionNotFoundException"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public PersistentSubscriptionNotFoundException(string streamName, string groupName, Exception? exception = null)
        : base($"Subscription group '{groupName}' on stream '{streamName}' does not exist.", exception) {
        if (streamName == null) throw new ArgumentNullException(nameof(streamName));
        if (groupName == null) throw new ArgumentNullException(nameof(groupName));

        StreamName = streamName;
        GroupName  = groupName;
    }
}
