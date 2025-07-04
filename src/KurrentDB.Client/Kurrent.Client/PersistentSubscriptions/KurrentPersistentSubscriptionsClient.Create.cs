// ReSharper disable InconsistentNaming

using EventStore.Client;
using EventStore.Client.PersistentSubscriptions;
using Kurrent.Client.Legacy;
using Kurrent.Client.Model;
using KurrentDB.Client;

namespace Kurrent.Client;

partial class KurrentPersistentSubscriptionsClient {
	static readonly Dictionary<string, CreateReq.Types.ConsumerStrategy> NamedConsumerStrategyToCreateProto
		= new Dictionary<string, CreateReq.Types.ConsumerStrategy> {
			[SystemConsumerStrategies.DispatchToSingle] = CreateReq.Types.ConsumerStrategy.DispatchToSingle,
			[SystemConsumerStrategies.RoundRobin]       = CreateReq.Types.ConsumerStrategy.RoundRobin,
			[SystemConsumerStrategies.Pinned]           = CreateReq.Types.ConsumerStrategy.Pinned
		};

	/// <summary>
	/// Creates a persistent subscription.
	/// </summary>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task CreateToStream(
		string streamName, string groupName, PersistentSubscriptionSettings settings, CancellationToken cancellationToken = default
	) =>
		await CreateInternal(
				streamName, groupName, null, settings, cancellationToken
			)
			.ConfigureAwait(false);

	/// <summary>
	/// Creates a filtered persistent subscription to $all.
	/// </summary>
	public async Task CreateToAll(
		string groupName, ReadFilter filter, PersistentSubscriptionSettings settings, CancellationToken cancellationToken = default
	) =>
		await CreateInternal(SystemStreams.AllStream, groupName, filter, settings, cancellationToken).ConfigureAwait(false);

	/// <summary>
	/// Creates a persistent subscription to $all.
	/// </summary>
	public async Task CreateToAll(string groupName, PersistentSubscriptionSettings settings, CancellationToken cancellationToken = default) =>
		await CreateInternal(SystemStreams.AllStream, groupName, null, settings, cancellationToken).ConfigureAwait(false);

	async Task CreateInternal(
		string streamName, string groupName, ReadFilter? filter, PersistentSubscriptionSettings settings, CancellationToken cancellationToken
	) {
		ArgumentNullException.ThrowIfNull(streamName);
		ArgumentNullException.ThrowIfNull(groupName);
		ArgumentNullException.ThrowIfNull(settings);

		if (settings.ConsumerStrategyName is null)
			throw new ArgumentNullException(nameof(settings.ConsumerStrategyName));

		if (filter is not null && streamName is not SystemStreams.AllStream)
			throw new ArgumentException($"Filters are only supported when subscribing to {SystemStreams.AllStream}");

		if (!NamedConsumerStrategyToCreateProto.TryGetValue(settings.ConsumerStrategyName, out var value))
			throw new ArgumentException("The specified consumer strategy is not supported, specify one of the SystemConsumerStrategies");

		await EnsureCompatibility(streamName, cancellationToken);

		using var call = ServiceClient.CreateAsync(new CreateReq {
				Options = new CreateReq.Types.Options {
					Stream = streamName != SystemStreams.AllStream
						? StreamOptionsForCreateProto(streamName, settings.StartFrom)
						: null,
					All = streamName == SystemStreams.AllStream
						? AllOptionsForCreateProto(settings.StartFrom, filter?.ConvertToEventFilter())
						: null,
#pragma warning disable 612
					StreamIdentifier =
						streamName != SystemStreams.AllStream
							? streamName
							: string.Empty, /*for backwards compatibility*/
#pragma warning restore 612
					GroupName = groupName,
					Settings = new CreateReq.Types.Settings {
#pragma warning disable 612
						Revision = streamName != SystemStreams.AllStream
							? (ulong)settings.StartFrom
							: 0, /*for backwards compatibility*/
#pragma warning restore 612
						CheckpointAfterMs  = (int)settings.CheckPointAfter.TotalMilliseconds,
						ExtraStatistics    = settings.ExtraStatistics,
						MessageTimeoutMs   = (int)settings.MessageTimeout.TotalMilliseconds,
						ResolveLinks       = settings.ResolveLinkTos,
						HistoryBufferSize  = settings.HistoryBufferSize,
						LiveBufferSize     = settings.LiveBufferSize,
						MaxCheckpointCount = settings.CheckPointUpperBound,
						MaxRetryCount      = settings.MaxRetryCount,
						MaxSubscriberCount = settings.MaxSubscriberCount,
						MinCheckpointCount = settings.CheckPointLowerBound,
#pragma warning disable 612
						/*for backwards compatibility*/
						NamedConsumerStrategy = value,
#pragma warning restore 612
						ReadBatchSize = settings.ReadBatchSize
					}
				}
			},
			cancellationToken: cancellationToken
		);

		await call.ResponseAsync.ConfigureAwait(false);
	}

	static CreateReq.Types.StreamOptions StreamOptionsForCreateProto(string streamName, LogPosition position) {
		if (position == LogPosition.Earliest) {
			return new CreateReq.Types.StreamOptions {
				StreamIdentifier = streamName,
				Start            = new Empty()
			};
		}

		if (position == LogPosition.Latest) {
			return new CreateReq.Types.StreamOptions {
				StreamIdentifier = streamName,
				End              = new Empty()
			};
		}

		return new CreateReq.Types.StreamOptions {
			StreamIdentifier = streamName,
			Revision         = (ulong)position.Value
		};
	}

	static CreateReq.Types.AllOptions AllOptionsForCreateProto(LogPosition position, IEventFilter? filter) {
		var allFilter = GetFilterOptions(filter);

		CreateReq.Types.AllOptions allOptions;

		if (position == LogPosition.Earliest) {
			allOptions = new CreateReq.Types.AllOptions {
				Start = new Empty(),
			};
		} else if (position == LogPosition.Latest) {
			allOptions = new CreateReq.Types.AllOptions {
				End = new Empty()
			};
		} else {
			allOptions = new CreateReq.Types.AllOptions {
				Position = new CreateReq.Types.Position {
					CommitPosition  = (ulong)position.Value,
					PreparePosition = (ulong)position.Value
				}
			};
		}

		if (allFilter is null) {
			allOptions.NoFilter = new Empty();
		} else {
			allOptions.Filter = allFilter;
		}

		return allOptions;
	}

	static CreateReq.Types.AllOptions.Types.FilterOptions? GetFilterOptions(IEventFilter? filter) {
		if (filter is null)
			return null;

		var options = filter switch {
			StreamFilter _ => new CreateReq.Types.AllOptions.Types.FilterOptions {
				StreamIdentifier = (filter.Prefixes, filter.Regex) switch {
					(PrefixFilterExpression[] _, RegularFilterExpression _)
						when (filter.Prefixes?.Length ?? 0) == 0 &&
						     filter.Regex != RegularFilterExpression.None =>
						new CreateReq.Types.AllOptions.Types.FilterOptions.Types.Expression
							{ Regex = filter.Regex },
					(PrefixFilterExpression[] _, RegularFilterExpression _)
						when (filter.Prefixes?.Length ?? 0) != 0 &&
						     filter.Regex == RegularFilterExpression.None =>
						new CreateReq.Types.AllOptions.Types.FilterOptions.Types.Expression {
							Prefix = { Array.ConvertAll(filter.Prefixes!, e => e.ToString()) }
						},
					_ => throw new InvalidOperationException()
				}
			},
			EventTypeFilter _ => new CreateReq.Types.AllOptions.Types.FilterOptions {
				EventType = (filter.Prefixes, filter.Regex) switch {
					(PrefixFilterExpression[] _, RegularFilterExpression _)
						when (filter.Prefixes?.Length ?? 0) == 0 &&
						     filter.Regex != RegularFilterExpression.None =>
						new CreateReq.Types.AllOptions.Types.FilterOptions.Types.Expression
							{ Regex = filter.Regex },
					(PrefixFilterExpression[] _, RegularFilterExpression _)
						when (filter.Prefixes?.Length ?? 0) != 0 &&
						     filter.Regex == RegularFilterExpression.None =>
						new CreateReq.Types.AllOptions.Types.FilterOptions.Types.Expression {
							Prefix = { Array.ConvertAll(filter.Prefixes!, e => e.ToString()) }
						},
					_ => throw new InvalidOperationException()
				}
			},
			_ => throw new InvalidOperationException()
		};

		if (filter.MaxSearchWindow.HasValue)
			options.Max = filter.MaxSearchWindow.Value;
		else
			options.Count = new Empty();

		return options;
	}
}
