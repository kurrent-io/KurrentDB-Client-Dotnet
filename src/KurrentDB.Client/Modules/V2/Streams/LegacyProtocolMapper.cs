using System.Runtime.CompilerServices;
using KurrentDB.Client.Model;
using KurrentDB.Client.Schema.Serialization;

namespace KurrentDB.Client;

public class LegacyProtocolMapper(ISchemaSerializer dataSerializer, IMetadataDecoder metadataDecoder) {
	ISchemaSerializer DataSerializer  { get; } = dataSerializer;
	IMetadataDecoder  MetadataDecoder { get; } = metadataDecoder;

	public async IAsyncEnumerable<EventData> ConvertMessagesToEventDataAsync(string stream, IEnumerable<Message> messages, Action<Metadata> prepareMetadata, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
		foreach (var message in messages) {
			prepareMetadata(message.Metadata);
			yield return await ConvertMessage(message, stream, cancellationToken).ConfigureAwait(false);
		}
	}

	public IEnumerable<EventData> ConvertMessagesToEventData(string stream, IEnumerable<Message> messages, Action<Metadata> prepareMetadata, CancellationToken cancellationToken = default) {
		foreach (var message in messages) {
			prepareMetadata(message.Metadata);
			yield return ConvertMessage(message, stream, cancellationToken).AsTask().GetAwaiter().GetResult();
		}
	}

	async ValueTask<EventData> ConvertMessage(Message message, string stream, CancellationToken cancellationToken) {
		var data = await DataSerializer
			.Serialize(message.Value, new(message.Metadata, stream, cancellationToken))
			.ConfigureAwait(false);

		// must be readable json in the end. convert the proto map to json? or the dic to json? as a struct?
		//var metadata = MetadataDecoder.Serialize(message.Metadata);

		var id     = Uuid.FromGuid(message.RecordId); // BROKEN
		var schema = SchemaInfo.FromMetadata(message.Metadata);

		// db features must be checked to see if the schema is supported
		// if it is we can send the correct content type.
		var compatibleContentType = schema.DataFormat switch {
			SchemaDataFormat.Json => schema.ContentType,
			_                     => "application/octet-stream"
		};

		return new EventData(
			eventId: id,
			type: schema.SchemaName,
			data: data,
			//metadata: metadata,
			contentType: compatibleContentType // schema.ContentType
		);
	}

	public async ValueTask<Record> ConvertResolvedEventToRecordAsync(ResolvedEvent resolvedEvent, CancellationToken cancellationToken) {
		var metadata = MetadataDecoder.Decode(resolvedEvent.OriginalEvent.Metadata);

		// Handle backwards compatibility with old schema by injecting the legacy schema in the headers.
		// The legacy schema is generated using the event type and content type from the resolved event.

		var schemaInfo = metadata.ContainsKey(SystemMetadataKeys.SchemaDataFormat)
			? SchemaInfo.FromMetadata(metadata)
			: SchemaInfo
				.FromContentType(resolvedEvent.OriginalEvent.EventType, resolvedEvent.OriginalEvent.ContentType)
				.InjectIntoMetadata(metadata);

		var data = resolvedEvent.OriginalEvent.Data;

		var value = await DataSerializer
			.Deserialize(data, new(metadata, resolvedEvent.OriginalStreamId, cancellationToken))
			.ConfigureAwait(false);

		var record = new Record {
			Id             = resolvedEvent.OriginalEvent.EventId.ToGuid(),
			Position       = resolvedEvent.OriginalPosition ?? new Position(),   //TODO SS: what to do here, cause this is different now....
			Stream         = resolvedEvent.OriginalEvent.EventStreamId,
			StreamRevision = resolvedEvent.OriginalEvent.EventNumber.ToInt64(),
			Timestamp      = resolvedEvent.OriginalEvent.Created,
			Metadata       = metadata,
			SchemaInfo     = schemaInfo,
			Value          = value!,
			ValueType      = value is not null ? value.GetType() : Type.Missing.GetType(),
			Data           = data
		};

		return record;
	}
}
