using KurrentDB.Client;

namespace Kurrent.Client.Model;

/// <summary>
/// Represents the settings of a persistent subscription.
/// </summary>
public record PersistentSubscriptionSettings {
    /// <summary>
    /// Whether the persistent subscription should resolve linkTo events to their linked events.
    /// </summary>
    public bool ResolveLinkTos { get; init; }

    /// <summary>
    /// Which event position in the stream or transaction file the subscription should start from.
    /// </summary>
    public LogPosition StartFrom { get; init; }

    /// <summary>
    /// Whether to track latency statistics on this subscription.
    /// </summary>
    public bool ExtraStatistics { get; init; }

    /// <summary>
    /// The amount of time after which to consider a message as timed out and retried.
    /// </summary>
    public TimeSpan MessageTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The maximum number of retries (due to timeout) before a message is considered to be parked.
    /// </summary>
    public int MaxRetryCount { get; init; } = 10;

    /// <summary>
    /// The size of the buffer (in-memory) listening to live messages as they happen before paging occurs.
    /// </summary>
    public int LiveBufferSize { get; init; } = 500;

    /// <summary>
    /// The number of events read at a time when paging through history.
    /// </summary>
    public int ReadBatchSize { get; init; } = 20;

    /// <summary>
    /// The number of events to cache when paging through history.
    /// </summary>
    public int HistoryBufferSize { get; init; } = 500;

    /// <summary>
    /// The amount of time to try to checkpoint after.
    /// </summary>
    public TimeSpan CheckPointAfter { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The minimum number of messages to process before a checkpoint may be written.
    /// </summary>
    public int CheckPointLowerBound { get; init; } = 10;

    /// <summary>
    /// The maximum number of messages not checkpointed before forcing a checkpoint.
    /// </summary>
    public int CheckPointUpperBound { get; init; } = 1000;

    /// <summary>
    /// The maximum number of subscribers allowed.
    /// </summary>
    public int MaxSubscriberCount { get; init; }

    /// <summary>
    /// The strategy to use for distributing events to client consumers.
    /// See <see cref="ConsumerStrategyNames"/> for system supported strategies.
    /// </summary>
    public string ConsumerStrategyName { get; init; } = SystemConsumerStrategies.RoundRobin;

    /// <summary>
    /// Gets a value indicating whether message timeout is configured.
    /// </summary>
    public bool HasMessageTimeout => MessageTimeout > TimeSpan.Zero;

    /// <summary>
    /// Gets a value indicating whether retry logic is enabled.
    /// </summary>
    public bool HasRetryLogic => MaxRetryCount > 0;

    /// <summary>
    /// Gets a value indicating whether checkpointing is configured.
    /// </summary>
    public bool HasCheckpointing => CheckPointAfter > TimeSpan.Zero;

    /// <summary>
    /// Gets a value indicating whether subscriber limits are enforced.
    /// </summary>
    public bool HasSubscriberLimits => MaxSubscriberCount > 0;

    /// <summary>
    /// Gets a value indicating whether buffering is configured for live events.
    /// </summary>
    public bool HasLiveBuffering => LiveBufferSize > 0;

    /// <summary>
    /// Gets a value indicating whether buffering is configured for historical events.
    /// </summary>
    public bool HasHistoryBuffering => HistoryBufferSize > 0;
}
