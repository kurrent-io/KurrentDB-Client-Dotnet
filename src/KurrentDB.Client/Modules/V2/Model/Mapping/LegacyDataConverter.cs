using KurrentDB.Client.SchemaRegistry;
using KurrentDB.Client.SchemaRegistry.Serialization;

namespace KurrentDB.Client.Model;

public class LegacyDataConverter(ISchemaSerializerProvider serializerProvider, IMetadataDecoder metadataDecoder, SchemaRegistryPolicy registryPolicy) {
	ISchemaSerializerProvider SerializerProvider { get; } = serializerProvider;
	IMetadataDecoder          MetadataDecoder    { get; } = metadataDecoder;
	SchemaRegistryPolicy      RegistryPolicy     { get; } = registryPolicy;

	public ValueTask<EventData> ConvertToEventData(Message message, string stream, CancellationToken ct) =>
		message.ConvertToEventData(stream, SerializerProvider, RegistryPolicy, ct);

	public ValueTask<Record> ConvertToRecord(ResolvedEvent resolvedEvent, CancellationToken ct) =>
		resolvedEvent.ConvertToRecord(SerializerProvider, MetadataDecoder, RegistryPolicy, ct);

	public IAsyncEnumerable<EventData> ConvertAllToEventData(IEnumerable<Message> messages, string stream, CancellationToken ct) =>
		messages.ConvertAllToEventData(stream, SerializerProvider, RegistryPolicy, ct);

	public IAsyncEnumerable<Record> ConvertAllToRecord(IEnumerable<ResolvedEvent> resolvedEvents, CancellationToken ct) =>
		resolvedEvents.ConvertAllToRecord(SerializerProvider, MetadataDecoder, RegistryPolicy, ct);
}
