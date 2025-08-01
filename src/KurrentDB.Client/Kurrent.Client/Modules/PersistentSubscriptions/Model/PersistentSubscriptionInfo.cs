// ReSharper disable InvertIf

using Google.Protobuf.Collections;
using Humanizer;
using Kurrent.Client.Streams;
using KurrentDB.Client;
using KurrentDB.Protocol.PersistentSubscriptions.V1;

namespace Kurrent.Client.PersistentSubscriptions;

/// <summary>
/// Provides the details for a persistent subscription.
/// </summary>
public record PersistentSubscriptionInfo {
	public string                                            EventSource { get; init; } = null!;
	public string                                            GroupName   { get; init; } = null!;
	public string                                            Status      { get; init; } = null!;
	public IEnumerable<PersistentSubscriptionConnectionInfo> Connections { get; init; } = null!;
	public PersistentSubscriptionStats                       Stats       { get; init; } = null!;
	public PersistentSubscriptionSettings?                   Settings    { get; init; }

	internal static PersistentSubscriptionInfo From(SubscriptionInfo info) {
		var startFrom = info.EventSource is SystemStreams.AllStream
			? LogPosition.From(info.StartFrom)
			: long.TryParse(info.StartFrom, out var streamPosition)
				? LogPosition.From(streamPosition)
				: LogPosition.Unset;

		var lastCheckpointedEventPosition = info.EventSource is SystemStreams.AllStream
			? LogPosition.From(info.LastCheckpointedEventPosition)
			: ulong.TryParse(info.LastCheckpointedEventPosition, out var checkpointPosition)
				? LogPosition.From((long)checkpointPosition)
				: LogPosition.Unset;

		var lastKnownEventPosition = info.EventSource is SystemStreams.AllStream
			? LogPosition.From(info.LastKnownEventPosition)
			: ulong.TryParse(info.LastKnownEventPosition, out var knownPosition)
				? LogPosition.From((long)knownPosition)
				: LogPosition.Unset;

		return new PersistentSubscriptionInfo {
			EventSource = info.EventSource,
			GroupName   = info.GroupName,
			Status      = info.Status,
			Connections = From(info.Connections),
			Settings = new() {
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
				MessageTimeout       = info.MessageTimeoutMilliseconds.Milliseconds(),
				CheckPointAfter      = info.CheckPointAfterMilliseconds.Milliseconds()
			},
			Stats = new() {
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

		if (info.Config is not null)
			settings = new PersistentSubscriptionSettings {
				ResolveLinkTos       = info.Config.ResolveLinktos,
				StartFrom            = LogPosition.From((long)info.Config.StartFrom),
				ExtraStatistics      = info.Config.ExtraStatistics,
				MaxRetryCount        = info.Config.MaxRetryCount,
				LiveBufferSize       = info.Config.LiveBufferSize,
				ReadBatchSize        = info.Config.ReadBatchSize,
				HistoryBufferSize    = info.Config.BufferSize,
				MessageTimeout       = info.Config.MessageTimeoutMilliseconds.Milliseconds(),
				CheckPointAfter      = info.Config.CheckPointAfterMilliseconds.Milliseconds(),
				CheckPointLowerBound = info.Config.MinCheckPointCount,
				CheckPointUpperBound = info.Config.MaxCheckPointCount,
				MaxSubscriberCount   = info.Config.MaxSubscriberCount,
				ConsumerStrategyName = info.Config.NamedConsumerStrategy
			};

		return new() {
			EventSource = info.EventStreamId,
			GroupName = info.GroupName,
			Status = info.Status,
			Connections = PersistentSubscriptionConnectionInfo.CreateFrom(info.Connections),
			Settings = settings,
			Stats = new() {
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

	static IEnumerable<PersistentSubscriptionConnectionInfo> From(RepeatedField<SubscriptionInfo.Types.ConnectionInfo> connections) =>
		connections.Select(conn =>
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

	static Dictionary<string, long> From(IEnumerable<SubscriptionInfo.Types.Measurement> measurements) =>
		measurements.ToDictionary(k => k.Key, v => v.Value);
}

#region dtos

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

#endregion
