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
public partial record PersistentSubscriptionDetails {
    internal static PersistentSubscriptionDetails From(SubscriptionInfo info) {
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

        return new PersistentSubscriptionDetails {
            Source = info.EventSource,
            Group       = info.GroupName,
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
                SubscriptionType     = info.NamedConsumerStrategy,
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

    static List<PersistentSubscriptionConnectionInfo> From(RepeatedField<SubscriptionInfo.Types.ConnectionInfo> connections) =>
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
        ).ToList();

    static Dictionary<string, long> From(RepeatedField<SubscriptionInfo.Types.Measurement> measurements) => measurements.ToDictionary(k => k.Key, v => v.Value);
}
