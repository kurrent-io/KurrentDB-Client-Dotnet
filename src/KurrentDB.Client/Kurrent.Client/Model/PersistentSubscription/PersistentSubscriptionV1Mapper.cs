// ReSharper disable InconsistentNaming

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

using EventStore.Client;
using Kurrent.Client.SchemaRegistry;
using Kurrent.Client.SchemaRegistry.Serialization;
using KurrentDB.Client;
using Contracts = EventStore.Client.PersistentSubscriptions;

#pragma warning disable CS0612 // Type or member is obsolete

namespace Kurrent.Client.Model.PersistentSubscription;

static class PersistentSubscriptionV1Mapper {
	static readonly Empty DefaultEmpty = new();

	static readonly Dictionary<string, Contracts.CreateReq.Types.ConsumerStrategy> NamedConsumerStrategyToCreateProto
		= new Dictionary<string, Contracts.CreateReq.Types.ConsumerStrategy> {
			[SystemConsumerStrategies.DispatchToSingle] = Contracts.CreateReq.Types.ConsumerStrategy.DispatchToSingle,
			[SystemConsumerStrategies.RoundRobin]       = Contracts.CreateReq.Types.ConsumerStrategy.RoundRobin,
			[SystemConsumerStrategies.Pinned]           = Contracts.CreateReq.Types.ConsumerStrategy.Pinned
		};

	public static class Requests {
		public static Contracts.CreateReq CreateSubscriptionRequest(
			StreamName streamName,
			string groupName,
			PersistentSubscriptionSettings settings,
			HeartbeatOptions heartbeat,
			ReadFilter filter
		) =>
			new() {
				Options = new() {
					GroupName        = groupName,
					Settings         = ConvertToSettings(settings),
					All              = streamName.IsAllStream ? ConvertToAllOptions(settings.StartFrom, filter, heartbeat) : null,
					Stream           = !streamName.IsAllStream ? ConvertToStreamOptions(streamName.Value, settings.StartFrom) : null,
					StreamIdentifier = streamName.Value
				}
			};
	}

	static Contracts.CreateReq.Types.Settings ConvertToSettings(PersistentSubscriptionSettings settings) {
		if (!NamedConsumerStrategyToCreateProto.TryGetValue(settings.ConsumerStrategyName, out var consumerStrategy))
			throw new ArgumentException("The specified consumer strategy is not supported, specify one of the SystemConsumerStrategies");

		return new() {
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
			ReadBatchSize      = settings.ReadBatchSize,

			// backward compatibility
			NamedConsumerStrategy = consumerStrategy,
			Revision              = settings.StartFrom
		};
	}

	static Contracts.CreateReq.Types.AllOptions ConvertToAllOptions(LogPosition start, ReadFilter filter, HeartbeatOptions heartbeat) {
		var allFilter = ConvertToFilterOptions(filter, heartbeat);

		return start switch {
			_ when start == LogPosition.Latest => new() { End = DefaultEmpty, Filter = allFilter },
			_ when start == LogPosition.Earliest => new() { Start = DefaultEmpty, Filter = allFilter },
			_ => new() { Position = new() { CommitPosition = (ulong)start.Value, PreparePosition = (ulong)start.Value }, Filter = allFilter }
		};
	}

	static Contracts.CreateReq.Types.StreamOptions ConvertToStreamOptions(string streamName, LogPosition start) =>
		start switch {
			_ when start == LogPosition.Latest   => new() { End              = DefaultEmpty },
			_ when start == LogPosition.Earliest => new() { Start            = DefaultEmpty },
			_                                    => new() { StreamIdentifier = streamName, Revision = (ulong)start.Value }
		};

	static Contracts.CreateReq.Types.AllOptions.Types.FilterOptions ConvertToFilterOptions(ReadFilter filter, HeartbeatOptions heartbeat) {
		var options = filter.Scope switch {
			ReadFilterScope.Stream => new Contracts.CreateReq.Types.AllOptions.Types.FilterOptions { StreamIdentifier = { Regex = filter.Expression } },
			ReadFilterScope.Record => new Contracts.CreateReq.Types.AllOptions.Types.FilterOptions { EventType        = { Regex = filter.Expression } }
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
            : link.Revision == StreamRevision.Unset ? revision : link.Revision;

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
    ) => MapToRecord(readEvent, serializerProvider, metadataDecoder, SchemaRegistryPolicy.NoRequirements, skipDecoding, ct);
}
