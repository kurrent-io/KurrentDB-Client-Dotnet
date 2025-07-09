// ReSharper disable InconsistentNaming

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
#pragma warning disable CS0612 // Type or member is obsolete

using EventStore.Client;
using Kurrent.Client.SchemaRegistry;
using Kurrent.Client.SchemaRegistry.Serialization;
using KurrentDB.Client;
using static EventStore.Client.PersistentSubscriptions.CreateReq.Types;
using static EventStore.Client.PersistentSubscriptions.CreateReq.Types.AllOptions.Types;
using Contracts = EventStore.Client.PersistentSubscriptions;

namespace Kurrent.Client.Model.PersistentSubscription;

public record ConsumerStrategyName(string Value) {
	public static implicit operator string(ConsumerStrategyName strategyName) => strategyName.Value;
	public static implicit operator ConsumerStrategyName(string value)        => new(value);

	public bool IsSupported() => Value is SystemConsumerStrategies.DispatchToSingle or SystemConsumerStrategies.RoundRobin or SystemConsumerStrategies.Pinned;
}

static class PersistentSubscriptionV1Mapper {
	static readonly Empty DefaultEmpty = new();

	static readonly Dictionary<string, ConsumerStrategy> SupportedConsumerStrategies
		= new Dictionary<string, ConsumerStrategy> {
			[SystemConsumerStrategies.DispatchToSingle] = ConsumerStrategy.DispatchToSingle,
			[SystemConsumerStrategies.RoundRobin]       = ConsumerStrategy.RoundRobin,
			[SystemConsumerStrategies.Pinned]           = ConsumerStrategy.Pinned
		};

	public static class Requests {
		public static Contracts.CreateReq CreateSubscriptionRequest(
			StreamName streamName, string groupName, PersistentSubscriptionSettings settings, HeartbeatOptions heartbeat,
			ReadFilter filter
		) {
			var options = new Options {
				GroupName = groupName,
				Settings  = ConvertToSettings(settings, streamName.IsAllStream)
			};

			ConfigureStreamOptions(options, streamName, settings, filter, heartbeat);

			return new Contracts.CreateReq { Options = options };
		}
	}

	static void ConfigureStreamOptions(
		Options options, StreamName streamName, PersistentSubscriptionSettings settings, ReadFilter filter, HeartbeatOptions heartbeat
	) {
		if (streamName.IsAllStream) {
			options.All = ConvertToAllOptions(settings.StartFrom, filter, heartbeat);
		} else {
			options.Stream           = ConvertToStreamOptions(streamName, settings.StartFrom);
			options.StreamIdentifier = streamName.Value; // backward compatibility
		}
	}

	static ConsumerStrategy GetConsumerStrategy(string strategyName) {
		var strategy = new ConsumerStrategyName(strategyName);

		if (!strategy.IsSupported())
			throw new ArgumentException(
				$"Consumer strategy '{strategyName}' is not supported. Supported strategies are: {string.Join(", ", SupportedConsumerStrategies.Keys)}",
				nameof(strategyName)
			);

		return SupportedConsumerStrategies[strategyName];
	}

	static Settings ConvertToSettings(PersistentSubscriptionSettings settings, bool isAllStream) =>
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

			// backward compatibility
			NamedConsumerStrategy = GetConsumerStrategy(settings.ConsumerStrategyName),
			Revision              = isAllStream ? settings.StartFrom : 0
		};

	static AllOptions ConvertToAllOptions(LogPosition start, ReadFilter filter, HeartbeatOptions heartbeat) {
		var allFilter = ConvertToFilterOptions(filter, heartbeat);

		return start switch {
			_ when start == LogPosition.Latest => new() { End = DefaultEmpty, Filter = allFilter },
			_ when start == LogPosition.Earliest => new() { Start = DefaultEmpty, Filter = allFilter },
			_ => new() { Position = new() { CommitPosition = (ulong)start.Value, PreparePosition = (ulong)start.Value }, Filter = allFilter }
		};
	}

	static StreamOptions ConvertToStreamOptions(string streamName, LogPosition start) =>
		start switch {
			_ when start == LogPosition.Latest   => new() { StreamIdentifier = streamName, End      = DefaultEmpty, },
			_ when start == LogPosition.Earliest => new() { StreamIdentifier = streamName, Start    = DefaultEmpty },
			_                                    => new() { StreamIdentifier = streamName, Revision = (ulong)start.Value }
		};

	static FilterOptions ConvertToFilterOptions(ReadFilter filter, HeartbeatOptions heartbeat) {
		FilterOptions options = filter.Scope switch {
			ReadFilterScope.Stream => new() { StreamIdentifier = new() { Regex = filter.Expression } },
			ReadFilterScope.Record => new() { EventType        = new() { Regex = filter.Expression } },
		};

		options.Max = (uint)heartbeat.RecordsThreshold;

		options.CheckpointIntervalMultiplier = 1;

		return options;
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
