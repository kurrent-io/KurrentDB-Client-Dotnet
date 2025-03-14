namespace KurrentDB.Client.Core.Serialization;

using System.Diagnostics.CodeAnalysis;
using static ContentTypeExtensions;

interface IMessageSerializer {
	public MessageData Serialize(Message value, MessageSerializationContext context);

#if NET48
	public bool TryDeserialize(EventRecord record, out Message? deserialized);
#else
	public bool TryDeserialize(EventRecord record, [NotNullWhen(true)] out Message? deserialized);
#endif

	public IMessageSerializer With(OperationSerializationSettings? operationSettings);
}

record MessageSerializationContext(MessageTypeNamingResolutionContext NamingResolution);

static class MessageSerializerExtensions {
	public static MessageData[] Serialize(
		this IMessageSerializer serializer,
		IEnumerable<Message> messages,
		MessageSerializationContext serializationContext
	) =>
		messages.Select(m => serializer.Serialize(m, serializationContext)).ToArray();
}

class MessageSerializer(SchemaRegistry schemaRegistry, KurrentDBClientSerializationSettings serializationSettings)
	: IMessageSerializer {
	readonly SystemTextJsonSerializer _metadataSerializer =
		new SystemTextJsonSerializer(
			new SystemTextJsonSerializationSettings { Options = KurrentDBClient.StreamMetadataJsonSerializerOptions }
		);

	readonly string _contentType = serializationSettings.DefaultContentType.ToMessageContentType();

	public MessageData Serialize(Message message, MessageSerializationContext serializationContext) {
		var (data, metadata, messageId) = message;

		var messageType = schemaRegistry
			.ResolveTypeName(
				message.Data.GetType(),
				serializationContext.NamingResolution
			);

		var serializedData = schemaRegistry
			.GetSerializer(serializationSettings.DefaultContentType)
			.Serialize(data);

		var serializedMetadata = metadata != null
			? _metadataSerializer.Serialize(metadata)
			: ReadOnlyMemory<byte>.Empty;

		return new MessageData(
			messageType,
			serializedData,
			serializedMetadata,
			messageId,
			_contentType
		);
	}

#if NET48
	public bool TryDeserialize(EventRecord record, out Message? deserialized) {
#else
	public bool TryDeserialize(EventRecord record, [NotNullWhen(true)] out Message? deserialized) {
#endif
		if (!schemaRegistry.TryResolveClrType(record.EventType, out var clrType)) {
			deserialized = null;
			return false;
		}

		var data = schemaRegistry
			.GetSerializer(FromMessageContentType(record.ContentType))
			.Deserialize(record.Data, clrType!);

		if (data == null) {
			deserialized = null;
			return false;
		}

		object? metadata = record.Metadata.Length > 0
		                && schemaRegistry.TryResolveClrMetadataType(record.EventType, out var clrMetadataType)
			? _metadataSerializer.Deserialize(record.Metadata, clrMetadataType!)
			: null;

		deserialized = Message.From(data, metadata, record.EventId);
		return true;
	}

	public IMessageSerializer With(OperationSerializationSettings? operationSettings) {
		if (operationSettings == null)
			return this;

		if (operationSettings.AutomaticDeserialization == AutomaticDeserialization.Disabled)
			return NullMessageSerializer.Instance;

		if (operationSettings.ConfigureSettings == null)
			return this;

		var settings = serializationSettings.Clone();
		operationSettings.ConfigureSettings.Invoke(settings);

		return new MessageSerializer(SchemaRegistry.From(settings), settings);
	}

	public static MessageSerializer From(KurrentDBClientSerializationSettings? settings = null) {
		settings ??= KurrentDBClientSerializationSettings.Get();

		return new MessageSerializer(SchemaRegistry.From(settings), settings);
	}
}

class NullMessageSerializer : IMessageSerializer {
	public static readonly NullMessageSerializer Instance = new NullMessageSerializer();

	public MessageData Serialize(Message value, MessageSerializationContext context) {
		throw new InvalidOperationException("Cannot serialize, automatic deserialization is disabled");
	}

#if NET48
	public bool TryDeserialize(EventRecord record, out Message? deserialized) {
#else
	public bool TryDeserialize(EventRecord eventRecord, [NotNullWhen(true)] out Message? deserialized) {
#endif
		deserialized = null;
		return false;
	}

	public IMessageSerializer With(OperationSerializationSettings? operationSettings) {
		return this;
	}
}
