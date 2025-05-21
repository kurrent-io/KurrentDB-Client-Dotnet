using System.Runtime.CompilerServices;
using System.Text.Json;
using KurrentDB.Client.Model;
using KurrentDB.Client.SchemaRegistry.Serialization;

namespace KurrentDB.Client;

public class LegacyDataConverter(ISchemaSerializerProvider serializerProvider, IMetadataDecoder metadataDecoder) {

	public async IAsyncEnumerable<EventData> ConvertToEventData(
		string stream,
		IEnumerable<Message> messages,
		[EnumeratorCancellation] CancellationToken ct
	) {
		foreach (var message in messages)
			yield return await ConvertToEventData(message, serializerProvider.GetSerializer(message.DataFormat), ct).ConfigureAwait(false);
	}

	static async ValueTask<EventData> ConvertToEventData(Message message, ISchemaSerializer serializer, CancellationToken ct) {
		var data = await serializer
			.Serialize(message, new SchemaSerializationContext(), ct)
			.ConfigureAwait(false);

		// we need to remove the schema name from the
		// metadata as it is not required in the end.
		message.Metadata.Remove(SystemMetadataKeys.SchemaName);

		// must be readable json in the end. convert the proto map to json? or the dic to json? as a struct?
		//var metadata = MetadataDecoder.Serialize(message.Metadata);

		var id          = Uuid.FromGuid(message.RecordId); // BROKEN
		var schemaName  = message.Metadata.Get<string>(SystemMetadataKeys.SchemaName)!;
		var contentType = message.DataFormat.GetContentType();

		// // db features must be checked to see if the schema is supported
		// // if it is we can send the correct content type.
		// var compatibleContentType = schema.DataFormat switch {
		// 	SchemaDataFormat.Json => schema.ContentType,
		// 	_                     => "application/octet-stream"
		// };

		return new EventData(
			eventId: id,
			type: schemaName,
			data: data,
			metadata: JsonSerializer.SerializeToUtf8Bytes(message.Metadata),
			contentType: contentType
		);
	}

}

public static class LegacyMapping {
	public static async IAsyncEnumerable<EventData> ToEventDataAsync(
		this IEnumerable<Message> messages,
		Action<Metadata> updateMetadata,
		ISchemaSerializerProvider serializerProvider,
		[EnumeratorCancellation] CancellationToken ct
	) {
		foreach (var message in messages)
			yield return await message.ToEventDataAsync(updateMetadata, serializerProvider, ct).ConfigureAwait(false);
	}

	public static async ValueTask<EventData> ToEventDataAsync(this Message message, Action<Metadata> updateMetadata, ISchemaSerializerProvider serializerProvider, CancellationToken ct) {
		updateMetadata(message.Metadata);

		var serializer = serializerProvider.GetSerializer(message.DataFormat);

		var data = await serializer
			.Serialize(message, new SchemaSerializationContext(), ct)
			.ConfigureAwait(false);

		// we need to remove the schema name from the
		// metadata as it is not required in the end.
		message.Metadata.Remove(SystemMetadataKeys.SchemaName);

		// must be readable json in the end. convert the proto map to json? or the dic to json? as a struct?
		//var metadata = MetadataDecoder.Serialize(message.Metadata);

		var id          = Uuid.FromGuid(message.RecordId); // BROKEN
		var schemaName  = message.Metadata.Get<string>(SystemMetadataKeys.SchemaName)!;
		var contentType = message.DataFormat.GetContentType();

		// // db features must be checked to see if the schema is supported
		// // if it is we can send the correct content type.
		// var compatibleContentType = schema.DataFormat switch {
		// 	SchemaDataFormat.Json => schema.ContentType,
		// 	_                     => "application/octet-stream"
		// };

		return new EventData(
			eventId: id,
			type: schemaName,
			data: data,
			metadata: JsonSerializer.SerializeToUtf8Bytes(message.Metadata),
			contentType: contentType
		);
	}

	public static async ValueTask<Record> ToRecordAsync(this ResolvedEvent re, ISchemaSerializerProvider serializerProvider, IMetadataDecoder metadataDecoder, CancellationToken ct) {
		var metadata = metadataDecoder.Decode(re.OriginalEvent.Metadata);

		// Handle backwards compatibility with old data by injecting the legacy schema in the metadata.
		if (!metadata.TryGet<SchemaDataFormat>(SystemMetadataKeys.SchemaDataFormat, out var dataFormat)) {
			metadata.Set(SystemMetadataKeys.SchemaName, re.OriginalEvent.EventType);
			metadata.Set(SystemMetadataKeys.SchemaDataFormat, dataFormat = re.OriginalEvent.ContentType.GetSchemaDataFormat());
		}

		var value = await serializerProvider
			.GetSerializer(dataFormat)
			.Deserialize(re.OriginalEvent.Data, new SchemaSerializationContext("", metadata), ct)
			.ConfigureAwait(false);

		// if null we want to skip it

		var record = new Record {
			Id             = re.OriginalEvent.EventId.ToGuid(),
			Position       = re.OriginalPosition.GetValueOrDefault().CommitPosition,
			Stream         = re.OriginalEvent.EventStreamId,
			StreamRevision = re.OriginalEvent.EventNumber.ToInt64(),
			Timestamp      = re.OriginalEvent.Created,
			Metadata       = metadata,
			Value          = value!,
			ValueType      = value is not null ? value.GetType() : Type.Missing.GetType(),
			Data           = re.OriginalEvent.Data
		};

		return record;
	}
}
