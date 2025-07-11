// ReSharper disable InvertIf

using Google.Protobuf.Collections;
using EventStore.Client.PersistentSubscriptions;
using Kurrent.Client.Model;
using KurrentDB.Client;

namespace Kurrent.Client;

/// <summary>
/// Provides the details for a persistent subscription.
/// </summary>
public record PersistentSubscriptionInfo {
	public string                                            EventSource { get; init; }
	public string                                            GroupName   { get; init; }
	public string                                            Status      { get; init; }
	public IEnumerable<PersistentSubscriptionConnectionInfo> Connections { get; init; }
	public PersistentSubscriptionSettings?                   Settings    { get; init; }
	public PersistentSubscriptionStats                       Stats       { get; init; }

	internal static PersistentSubscriptionInfo From(SubscriptionInfo info) {
		var startFrom                     = LogPosition.Unset;
		var lastCheckpointedEventPosition = LogPosition.Unset;
		var lastKnownEventPosition        = LogPosition.Unset;

		if (info.EventSource is SystemStreams.AllStream) {
			startFrom                     = LogPosition.From(info.StartFrom);
			lastCheckpointedEventPosition = LogPosition.From(info.LastCheckpointedEventPosition);
			lastKnownEventPosition        = LogPosition.From(info.LastKnownEventPosition);
		} else {
			if (long.TryParse(info.StartFrom, out var streamPosition))
				startFrom = LogPosition.From(streamPosition);

			if (ulong.TryParse(info.LastCheckpointedEventPosition, out var position))
				lastCheckpointedEventPosition = LogPosition.From((long)position);

			if (ulong.TryParse(info.LastKnownEventPosition, out position))
				lastKnownEventPosition = LogPosition.From((long)position);
		}

		return new PersistentSubscriptionInfo {
			EventSource = info.EventSource,
			GroupName   = info.GroupName,
			Status      = info.Status,
			Connections = From(info.Connections),
			Settings = new PersistentSubscriptionSettings {
				StartFrom            = startFrom,
				ResolveLinkTos       = info.ResolveLinkTos,
				ExtraStatistics      = info.ExtraStatistics,
				MaxRetryCount        = info.MaxRetryCount,
				LiveBufferSize       = info.LiveBufferSize,
				ReadBatchSize        = info.ReadBatchSize,
				HistoryBufferSize    = info.BufferSize,
				CheckPointLowerBound = info.MinCheckPointCount,
				CheckPointUpperBound = info.MaxCheckPointCount,
				MaxSubscriberCount   = info.MaxSubscriberCount,
				ConsumerStrategyName = info.NamedConsumerStrategy,
				MessageTimeout       = TimeSpan.FromMilliseconds(info.MessageTimeoutMilliseconds),
				CheckPointAfter      = TimeSpan.FromMilliseconds(info.CheckPointAfterMilliseconds)
			},
			Stats = new PersistentSubscriptionStats {
				AveragePerSecond              = info.AveragePerSecond,
				TotalItems                    = info.TotalItems,
				CountSinceLastMeasurement     = info.CountSinceLastMeasurement,
				ReadBufferCount               = info.ReadBufferCount,
				LiveBufferCount               = info.LiveBufferCount,
				RetryBufferCount              = info.RetryBufferCount,
				TotalInFlightMessages         = info.TotalInFlightMessages,
				OutstandingMessagesCount      = info.OutstandingMessagesCount,
				ParkedMessageCount            = info.ParkedMessageCount,
				LastCheckpointedEventPosition = lastCheckpointedEventPosition,
				LastKnownEventPosition        = lastKnownEventPosition
			}
		};
	}

	internal static PersistentSubscriptionInfo From(PersistentSubscriptionDto info) {
		PersistentSubscriptionSettings? settings = null;
		if (info.Config is not null) {
			settings = new PersistentSubscriptionSettings {
				ResolveLinkTos = info.Config.ResolveLinktos,
				// we only need to support StreamPosition as $all was never implemented in http api.
				StartFrom            = LogPosition.From((long)info.Config.StartFrom),
				ExtraStatistics      = info.Config.ExtraStatistics,
				MessageTimeout       = TimeSpan.FromMilliseconds(info.Config.MessageTimeoutMilliseconds),
				MaxRetryCount        = info.Config.MaxRetryCount,
				LiveBufferSize       = info.Config.LiveBufferSize,
				ReadBatchSize        = info.Config.ReadBatchSize,
				HistoryBufferSize    = info.Config.BufferSize,
				CheckPointAfter      = TimeSpan.FromMilliseconds(info.Config.CheckPointAfterMilliseconds),
				CheckPointLowerBound = info.Config.MinCheckPointCount,
				CheckPointUpperBound = info.Config.MaxCheckPointCount,
				MaxSubscriberCount   = info.Config.MaxSubscriberCount,
				ConsumerStrategyName = info.Config.NamedConsumerStrategy
			};
		}

		return new PersistentSubscriptionInfo {
			EventSource = info.EventStreamId,
			GroupName = info.GroupName,
			Status = info.Status,
			Connections = PersistentSubscriptionConnectionInfo.CreateFrom(info.Connections),
			Settings = settings,
			Stats = new PersistentSubscriptionStats {
				AveragePerSecond              = (int)info.AverageItemsPerSecond,
				TotalItems                    = info.TotalItemsProcessed,
				CountSinceLastMeasurement     = info.CountSinceLastMeasurement,
				ReadBufferCount               = info.ReadBufferCount,
				LiveBufferCount               = info.LiveBufferCount,
				RetryBufferCount              = info.RetryBufferCount,
				TotalInFlightMessages         = info.TotalInFlightMessages,
				OutstandingMessagesCount      = info.OutstandingMessagesCount,
				ParkedMessageCount            = info.ParkedMessageCount,
				LastCheckpointedEventPosition = LogPosition.From(info.LastProcessedEventNumber),
				LastKnownEventPosition        = LogPosition.From(info.LastKnownEventNumber)
			}
		};
	}

	static IEnumerable<PersistentSubscriptionConnectionInfo> From(
		RepeatedField<SubscriptionInfo.Types.ConnectionInfo> connections
	) {
		return connections.Select(conn =>
			new PersistentSubscriptionConnectionInfo {
				From                      = conn.From,
				Username                  = conn.Username,
				AverageItemsPerSecond     = conn.AverageItemsPerSecond,
				TotalItems                = conn.TotalItems,
				CountSinceLastMeasurement = conn.CountSinceLastMeasurement,
				AvailableSlots            = conn.AvailableSlots,
				InFlightMessages          = conn.InFlightMessages,
				ConnectionName            = conn.ConnectionName,
				ExtraStatistics           = From(conn.ObservedMeasurements)
			}
		);
	}

	static Dictionary<string, long> From(IEnumerable<SubscriptionInfo.Types.Measurement> measurements) =>
		measurements.ToDictionary(k => k.Key, v => v.Value);
}

record PersistentSubscriptionDto(
	string EventStreamId,
	string GroupName,
	string Status,
	float AverageItemsPerSecond,
	long TotalItemsProcessed,
	long CountSinceLastMeasurement,
	long LastProcessedEventNumber,
	long LastKnownEventNumber,
	int ReadBufferCount,
	long LiveBufferCount,
	int RetryBufferCount,
	int TotalInFlightMessages,
	int OutstandingMessagesCount,
	int ParkedMessageCount,
	PersistentSubscriptionConfig? Config,
	IEnumerable<PersistentSubscriptionConnectionInfoDto> Connections
);

record PersistentSubscriptionConfig(
	bool ResolveLinktos,
	ulong StartFrom,
	string StartPosition,
	int MessageTimeoutMilliseconds,
	bool ExtraStatistics,
	int MaxRetryCount,
	int LiveBufferSize,
	int BufferSize,
	int ReadBatchSize,
	int CheckPointAfterMilliseconds,
	int MinCheckPointCount,
	int MaxCheckPointCount,
	int MaxSubscriberCount,
	string NamedConsumerStrategy
);

record PersistentSubscriptionConnectionInfoDto(
	string From,
	string Username,
	float AverageItemsPerSecond,
	long TotalItems,
	long CountSinceLastMeasurement,
	int AvailableSlots,
	int InFlightMessages,
	string ConnectionName,
	IEnumerable<PersistentSubscriptionMeasurementInfoDto> ExtraStatistics
);

record PersistentSubscriptionMeasurementInfoDto(string Key, long Value);
