// ReSharper disable InconsistentNaming

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
#pragma warning disable CS0612 // Type or member is obsolete

using EventStore.Client;
using Kurrent.Client.Registry;
using Kurrent.Client.Schema;
using Kurrent.Client.Schema.Serialization;
using Kurrent.Client.Streams;
using KurrentDB.Client;
using Contracts = EventStore.Client.PersistentSubscriptions;

namespace Kurrent.Client.Model.PersistentSubscription;

/// Utility class for mapping persistent subscription events to application-level records.
static class PersistentSubscriptionV1Mapper {
	static readonly Empty DefaultEmpty = new();

	static readonly Dictionary<string, Contracts.CreateReq.Types.ConsumerStrategy> CreateConsumerStrategies
		= new() {
			[SystemConsumerStrategies.DispatchToSingle] = Contracts.CreateReq.Types.ConsumerStrategy.DispatchToSingle,
			[SystemConsumerStrategies.RoundRobin]       = Contracts.CreateReq.Types.ConsumerStrategy.RoundRobin,
			[SystemConsumerStrategies.Pinned]           = Contracts.CreateReq.Types.ConsumerStrategy.Pinned
		};

	static readonly Dictionary<string, Contracts.UpdateReq.Types.ConsumerStrategy> UpdateConsumerStrategies
		= new() {
			[SystemConsumerStrategies.DispatchToSingle] = Contracts.UpdateReq.Types.ConsumerStrategy.DispatchToSingle,
			[SystemConsumerStrategies.RoundRobin]       = Contracts.UpdateReq.Types.ConsumerStrategy.RoundRobin,
			[SystemConsumerStrategies.Pinned]           = Contracts.UpdateReq.Types.ConsumerStrategy.Pinned
		};

	public static class Requests {
		public static Contracts.CreateReq CreateSubscriptionRequest(
			StreamName streamName, string groupName, PersistentSubscriptionSettings settings, HeartbeatOptions heartbeat, ReadFilter filter
		) {
			var options = new Contracts.CreateReq.Types.Options {
				GroupName = groupName,
				Settings  = SettingsConverter.ToCreateSettings(settings)
			};

			StreamOptionsMapper.MapToCreateOptions(options, streamName, settings.StartFrom, filter, heartbeat);
			LegacyBackwardCompatibility.ApplyToCreateOptions(options, streamName, settings.StartFrom, settings.ConsumerStrategyName);

			return new Contracts.CreateReq { Options = options };
		}

		public static Contracts.UpdateReq UpdateSubscriptionRequest(
			StreamName streamName, string groupName, PersistentSubscriptionSettings settings
		) {
			var options = new Contracts.UpdateReq.Types.Options {
				GroupName = groupName,
				Settings  = SettingsConverter.ToUpdateSettings(settings)
			};

			StreamOptionsMapper.MapToUpdateOptions(options, streamName, settings.StartFrom);
			LegacyBackwardCompatibility.ApplyToUpdateOptions(options, streamName, settings.StartFrom, settings.ConsumerStrategyName);

			return new Contracts.UpdateReq { Options = options };
		}
	}

	static class SettingsConverter {
		internal static Contracts.CreateReq.Types.Settings ToCreateSettings(PersistentSubscriptionSettings settings) =>
			new() {
				ExtraStatistics    = settings.ExtraStatistics,
				ResolveLinks       = settings.ResolveLinkTos,
				HistoryBufferSize  = settings.HistoryBufferSize,
				LiveBufferSize     = settings.LiveBufferSize,
				MaxCheckpointCount = settings.CheckPointUpperBound,
				MaxRetryCount      = settings.MaxRetryCount,
				MaxSubscriberCount = settings.MaxSubscriberCount,
				MinCheckpointCount = settings.CheckPointLowerBound,
				ReadBatchSize      = settings.ReadBatchSize,
				CheckpointAfterMs  = (int)settings.CheckPointAfter.TotalMilliseconds,
				MessageTimeoutMs   = (int)settings.MessageTimeout.TotalMilliseconds
			};

		internal static Contracts.UpdateReq.Types.Settings ToUpdateSettings(PersistentSubscriptionSettings settings) =>
			new() {
				ExtraStatistics    = settings.ExtraStatistics,
				ResolveLinks       = settings.ResolveLinkTos,
				HistoryBufferSize  = settings.HistoryBufferSize,
				LiveBufferSize     = settings.LiveBufferSize,
				MaxCheckpointCount = settings.CheckPointUpperBound,
				MaxRetryCount      = settings.MaxRetryCount,
				MaxSubscriberCount = settings.MaxSubscriberCount,
				MinCheckpointCount = settings.CheckPointLowerBound,
				ReadBatchSize      = settings.ReadBatchSize,
				CheckpointAfterMs  = (int)settings.CheckPointAfter.TotalMilliseconds,
				MessageTimeoutMs   = (int)settings.MessageTimeout.TotalMilliseconds,
			};
	}

	static class StreamOptionsMapper {
		internal static void MapToCreateOptions(
			Contracts.CreateReq.Types.Options options,
			StreamName streamName,
			LogPosition startFrom,
			ReadFilter filter,
			HeartbeatOptions heartbeat
		) {
			if (streamName.IsAllStream)
				options.All = CreateAllOptions(startFrom, filter, heartbeat);
			else
				options.Stream = CreateStreamOptions(streamName, startFrom);
		}

		internal static void MapToUpdateOptions(Contracts.UpdateReq.Types.Options options, StreamName streamName, LogPosition startFrom) {
			if (streamName.IsAllStream)
				options.All = UpdateAllOptions(startFrom);
			else
				options.Stream = UpdateStreamOptions(streamName, startFrom);
		}

		static Contracts.CreateReq.Types.AllOptions CreateAllOptions(LogPosition start, ReadFilter filter, HeartbeatOptions heartbeat) {
			var allFilter = FilterOptionsConverter.ToCreateFilterOptions(filter, heartbeat);

			return start switch {
				_ when start == LogPosition.Latest => new() { End = DefaultEmpty, Filter = allFilter },
				_ when start == LogPosition.Earliest => new() { Start = DefaultEmpty, Filter = allFilter },
				_ => new() { Position = new() { CommitPosition = (ulong)start.Value, PreparePosition = (ulong)start.Value }, Filter = allFilter }
			};
		}

		static Contracts.UpdateReq.Types.AllOptions UpdateAllOptions(LogPosition start) {
			return start switch {
				_ when start == LogPosition.Latest   => new() { End   = DefaultEmpty },
				_ when start == LogPosition.Earliest => new() { Start = DefaultEmpty },
				_ => new() {
					Position = new() {
						CommitPosition  = (ulong)start.Value,
						PreparePosition = (ulong)start.Value
					}
				}
			};
		}

		static Contracts.CreateReq.Types.StreamOptions CreateStreamOptions(StreamName streamName, LogPosition start) =>
			start switch {
				_ when start == LogPosition.Latest   => new() { StreamIdentifier = streamName.Value, End      = DefaultEmpty },
				_ when start == LogPosition.Earliest => new() { StreamIdentifier = streamName.Value, Start    = DefaultEmpty },
				_                                    => new() { StreamIdentifier = streamName.Value, Revision = (ulong)start.Value }
			};

		static Contracts.UpdateReq.Types.StreamOptions UpdateStreamOptions(StreamName streamName, LogPosition start) =>
			start switch {
				_ when start == LogPosition.Latest   => new() { StreamIdentifier = streamName.Value, End      = DefaultEmpty },
				_ when start == LogPosition.Earliest => new() { StreamIdentifier = streamName.Value, Start    = DefaultEmpty },
				_                                    => new() { StreamIdentifier = streamName.Value, Revision = (ulong)start.Value }
			};
	}

	static class FilterOptionsConverter {
		internal static Contracts.CreateReq.Types.AllOptions.Types.FilterOptions? ToCreateFilterOptions(ReadFilter filter, HeartbeatOptions heartbeat) {
			if (filter == ReadFilter.None)
				return null;

			var options = filter.Scope switch {
				ReadFilterScope.Stream => new Contracts.CreateReq.Types.AllOptions.Types.FilterOptions { StreamIdentifier = new() { Regex = filter.Expression } },
				ReadFilterScope.Record => new Contracts.CreateReq.Types.AllOptions.Types.FilterOptions { EventType = new() { Regex = filter.Expression } }
			};

			options.Max = (uint)heartbeat.RecordsThreshold;
			options.CheckpointIntervalMultiplier = 1;

			return options;
		}
	}

	static class LegacyBackwardCompatibility {
		internal static void ApplyToCreateOptions(Contracts.CreateReq.Types.Options options, StreamName streamName, LogPosition startFrom, string consumerStrategyName) {
			if (!CreateConsumerStrategies.TryGetValue(consumerStrategyName, out var strategy))
				throw new ArgumentException($"Unknown consumer strategy: {consumerStrategyName}");

			options.Settings.NamedConsumerStrategy = strategy;
			options.Settings.Revision              = streamName.IsAllStream ? startFrom : 0;
			options.StreamIdentifier               = streamName.IsAllStream ? string.Empty : streamName.Value;
		}

		internal static void ApplyToUpdateOptions(Contracts.UpdateReq.Types.Options options, StreamName streamName, LogPosition startFrom, string consumerStrategyName) {
			if (!UpdateConsumerStrategies.TryGetValue(consumerStrategyName, out var strategy))
				throw new ArgumentException($"Unknown consumer strategy: {consumerStrategyName}");

			options.Settings.NamedConsumerStrategy = strategy;
			options.Settings.Revision              = streamName.IsAllStream ? startFrom : 0;
			options.StreamIdentifier               = streamName.IsAllStream ? string.Empty : streamName.Value;
		}
	}

	public static async ValueTask<Record> MapToRecord(
		this Contracts.ReadResp.Types.ReadEvent readEvent,
		ISchemaSerializerProvider serializerProvider,
		IMetadataDecoder metadataDecoder,
		SchemaRegistryPolicy registryPolicy,
		bool skipDecoding = false,
		CancellationToken ct = default
	) {
		var recordedEvent = readEvent.Event;

		// first parse all info
		var stream           = StreamName.From(recordedEvent.StreamIdentifier!);
		var recordId         = Uuid.FromDto(recordedEvent.Id).ToGuid();
		var revision         = StreamRevision.From((long)recordedEvent.StreamRevision);
		var position         = LogPosition.From((long)recordedEvent.CommitPosition);
		var schemaName       = SchemaName.From(recordedEvent.Metadata[Constants.Metadata.Type]);
		var schemaDataFormat = recordedEvent.Metadata[Constants.Metadata.ContentType].GetSchemaDataFormat(); // it will always be json or octet-stream
		var data             = recordedEvent.Data.Memory;
		var rawMetadata      = recordedEvent.CustomMetadata.Memory;
		var timestamp        = Convert.ToInt64(recordedEvent.Metadata[Constants.Metadata.Created]).FromTicksSinceEpoch();

		var metadata = metadataDecoder.Decode(rawMetadata, new(stream, schemaName, schemaDataFormat));

		var link = readEvent.Link is { } linkEvent
			? new Link {
				Name     = linkEvent.Metadata[Constants.Metadata.Type],
				Position = (long)linkEvent.CommitPosition,
				Revision = (long)linkEvent.StreamRevision,
				Stream   = StreamName.From(linkEvent.StreamIdentifier!),
			}
			: Link.None;

		var indexRevision = readEvent.HasCommitPosition
			? StreamRevision.From((long)readEvent.CommitPosition)
			: link.Revision == StreamRevision.Unset
				? revision
				: link.Revision;

		// create a decoder
		IRecordDecoder decoder = new RecordDecoder(serializerProvider, registryPolicy);

		// and we are done
		var record = new Record(decoder) {
			Id             = recordId,
			Stream         = stream,
			StreamRevision = revision,
			Position       = position,
			Timestamp      = timestamp,
			Metadata       = metadata,
			Data           = data,
			Link           = link,
			IndexRevision  = indexRevision
		};

		// now decode the record if required
		if (!skipDecoding)
			await record.TryDecode(ct);

		return record;
	}

	public static ValueTask<Record> MapToRecord(
		this Contracts.ReadResp.Types.ReadEvent readEvent,
		ISchemaSerializerProvider serializerProvider,
		IMetadataDecoder metadataDecoder,
		bool skipDecoding = false,
		CancellationToken ct = default
	) =>
		MapToRecord(
			readEvent, serializerProvider, metadataDecoder,
			SchemaRegistryPolicy.NoRequirements, skipDecoding, ct
		);
}
