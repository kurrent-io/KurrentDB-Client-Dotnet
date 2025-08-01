using Humanizer;
using Kurrent.Client.Streams;
using KurrentDB.Client;

namespace Kurrent.Client.PersistentSubscriptions;

/// <summary>
/// Represents the settings for a persistent subscription in the Kurrent client library.
/// </summary>
public sealed record PersistentSubscriptionSettings {
	/// <summary>
	/// Whether the <see cref="PersistentSubscription"></see> should resolve linkTo events to their linked events.
	/// </summary>
	public bool ResolveLinkTos { get; init; }

	/// <summary>
	/// Which event position in the stream or transaction file the subscription should start from.
	/// </summary>
	public LogPosition StartFrom { get; init; } = LogPosition.Unset;

	/// <summary>
	/// Whether to track latency statistics on this subscription.
	/// </summary>
	public bool ExtraStatistics { get; init; }

	/// <summary>
	/// The amount of time after which to consider a message as timed out and retried.
	/// </summary>
	[TimeSpanRange(MinMilliseconds = 0, MaxMilliseconds = int.MaxValue)]
	public TimeSpan MessageTimeout { get; init; } = 30.Seconds();

	/// <summary>
	/// The amount of time to try to checkpoint after.
	/// </summary>
	[TimeSpanRange(MinMilliseconds = 0, MaxMilliseconds = int.MaxValue)]
	public TimeSpan CheckPointAfter { get; init; } = 2.Seconds();

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
	/// The strategy to use for distributing events to client consumers. See <see cref="SystemConsumerStrategies"/> for system supported strategies.
	/// </summary>
	public string ConsumerStrategyName { get; init; } = SystemConsumerStrategies.RoundRobin;
}
