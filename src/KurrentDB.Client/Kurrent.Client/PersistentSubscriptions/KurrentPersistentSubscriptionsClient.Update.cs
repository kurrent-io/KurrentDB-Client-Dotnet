// ReSharper disable InconsistentNaming

using EventStore.Client;
using EventStore.Client.PersistentSubscriptions;
using Kurrent.Client.Model;
using KurrentDB.Client;

namespace Kurrent.Client;

public partial class KurrentPersistentSubscriptionsClient {
	static readonly Dictionary<string, UpdateReq.Types.ConsumerStrategy> NamedConsumerStrategyToUpdateProto =
		new Dictionary<string, UpdateReq.Types.ConsumerStrategy> {
			[SystemConsumerStrategies.DispatchToSingle] = UpdateReq.Types.ConsumerStrategy.DispatchToSingle,
			[SystemConsumerStrategies.RoundRobin]       = UpdateReq.Types.ConsumerStrategy.RoundRobin,
			[SystemConsumerStrategies.Pinned]           = UpdateReq.Types.ConsumerStrategy.Pinned,
		};

	static UpdateReq.Types.StreamOptions StreamOptionsForUpdateProto(string streamName, LogPosition position) {
		if (position == StreamPosition.Start) {
			return new UpdateReq.Types.StreamOptions {
				StreamIdentifier = streamName,
				Start            = new Empty()
			};
		}

		if (position == StreamPosition.End) {
			return new UpdateReq.Types.StreamOptions {
				StreamIdentifier = streamName,
				End              = new Empty()
			};
		}

		return new UpdateReq.Types.StreamOptions {
			StreamIdentifier = streamName,
			Revision         = position
		};
	}

	static UpdateReq.Types.AllOptions AllOptionsForUpdateProto(LogPosition position) {
		if (position == LogPosition.Earliest) {
			return new UpdateReq.Types.AllOptions {
				Start = new Empty()
			};
		}

		if (position == LogPosition.Latest) {
			return new UpdateReq.Types.AllOptions {
				End = new Empty()
			};
		}

		return new UpdateReq.Types.AllOptions {
			Position = new UpdateReq.Types.Position {
				CommitPosition  = position,
				PreparePosition = position
			}
		};
	}

	/// <summary>
	/// Updates a persistent subscription.
	/// </summary>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task UpdateToStreamAsync(
		string streamName, string groupName, PersistentSubscriptionSettings settings,
		TimeSpan? deadline = null, UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default
	) {
		if (streamName is null) {
			throw new ArgumentNullException(nameof(streamName));
		}

		if (groupName is null) {
			throw new ArgumentNullException(nameof(groupName));
		}

		if (settings is null) {
			throw new ArgumentNullException(nameof(settings));
		}

		if (settings.ConsumerStrategyName is null) {
			throw new ArgumentNullException(nameof(settings.ConsumerStrategyName));
		}

		var channelInfo = await LegacyClient.GetChannelInfo(cancellationToken).ConfigureAwait(false);

		if (streamName == SystemStreams.AllStream &&
		    !channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsToAll) {
			throw new InvalidOperationException("The server does not support persistent subscriptions to $all.");
		}

		using var call = new PersistentSubscriptions.PersistentSubscriptionsClient(channelInfo.CallInvoker)
			.UpdateAsync(
				new UpdateReq {
					Options = new UpdateReq.Types.Options {
						GroupName = groupName,
						Stream = streamName != SystemStreams.AllStream
							? StreamOptionsForUpdateProto(streamName, settings.StartFrom)
							: null,
						All = streamName == SystemStreams.AllStream
							? AllOptionsForUpdateProto(settings.StartFrom)
							: null,
#pragma warning disable 612
						StreamIdentifier =
							streamName != SystemStreams.AllStream
								? streamName
								: string.Empty, /*for backwards compatibility*/
#pragma warning restore 612
						Settings = new UpdateReq.Types.Settings {
#pragma warning disable 612
							Revision = streamName != SystemStreams.AllStream
								? settings.StartFrom
								: default, /*for backwards compatibility*/
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
							NamedConsumerStrategy = NamedConsumerStrategyToUpdateProto[settings.ConsumerStrategyName],
							ReadBatchSize = settings.ReadBatchSize
						}
					}
				},
				KurrentDBCallOptions.CreateNonStreaming(
					LegacySettings, deadline, userCredentials,
					cancellationToken
				)
			);

		await call.ResponseAsync.ConfigureAwait(false);
	}

	/// <summary>
	/// Updates a persistent subscription to $all.
	/// </summary>
	public async Task UpdateToAllAsync(
		string groupName, PersistentSubscriptionSettings settings,
		TimeSpan? deadline = null, UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default
	) =>
		await UpdateToStreamAsync(
				SystemStreams.AllStream, groupName, settings,
				deadline, userCredentials,
				cancellationToken
			)
			.ConfigureAwait(false);
}
