using System.Diagnostics.CodeAnalysis;

namespace KurrentDB.Client.Core.Serialization;

using static Constants.Metadata.ContentTypes;

public enum ContentType {
	Json = 1,

	// Protobuf  = 2,
	// Avro      = 3,
	Bytes = 4
}

static class ContentTypeExtensions {
	public static ContentType FromMessageContentType(string contentType) =>
		contentType == ApplicationJson
			? ContentType.Json
			: ContentType.Bytes;

	public static string ToMessageContentType(this ContentType contentType) =>
		contentType switch {
			ContentType.Json  => ApplicationJson,
			ContentType.Bytes => ApplicationOctetStream,
			_                 => throw new ArgumentOutOfRangeException(nameof(contentType), contentType, null)
		};
}

class SchemaRegistry(
	IDictionary<ContentType, ISerializer> serializers,
	IMessageTypeNamingStrategy messageTypeNamingStrategy,
	IMessageTypeRegistry messageTypeRegistry,
	AutomaticTypeMappingRegistration automaticTypeMappingRegistration
) {
	public ISerializer GetSerializer(ContentType schemaType) =>
		serializers[schemaType];

	public string ResolveTypeName(Type messageType, MessageTypeNamingResolutionContext resolutionContext) =>
		messageTypeRegistry.GetTypeName(messageType) ??
		messageTypeNamingStrategy.ResolveTypeName(messageType, resolutionContext);

#if NET48
	public bool TryResolveClrType(EventRecord record, out Type? type) {
#else
	public bool TryResolveClrType(EventRecord record, [NotNullWhen(true)] out Type? type) {
#endif
		type = messageTypeRegistry.GetClrType(record.EventType);

		if (type != null)
			return true;

		if (automaticTypeMappingRegistration == AutomaticTypeMappingRegistration.Disabled)
			return false;

		if (!messageTypeNamingStrategy.TryResolveClrTypeName(record, out var clrTypeName) || clrTypeName == null)
			return false;

		type = TypeProvider.GetTypeByFullName(clrTypeName);

		if (type == null)
			return false;

		messageTypeRegistry.Register(record.EventType, type);

		return true;
	}

#if NET48
	public bool TryResolveClrMetadataType(EventRecord record, out Type? type) {
#else
	public bool TryResolveClrMetadataType(EventRecord record, [NotNullWhen(true)] out Type? type) {
#endif
		type = messageTypeRegistry.GetClrType($"{record.EventType}-metadata");

		if (type != null)
			return true;

		if (automaticTypeMappingRegistration == AutomaticTypeMappingRegistration.Disabled)
			return false;

		if (!messageTypeNamingStrategy.TryResolveClrMetadataTypeName(record, out var clrTypeName) || clrTypeName == null)
			return false;

		type = TypeProvider.GetTypeByFullName(clrTypeName);

		if (type == null)
			return false;

		messageTypeRegistry.Register($"{record.EventType}-metadata", type);

		return true;
	}

	public static SchemaRegistry From(KurrentDBClientSerializationSettings settings) {
		var messageTypeNamingStrategy =
			settings.MessageTypeNamingStrategy
		 ?? new DefaultMessageTypeNamingStrategy(settings.MessageTypeMapping.DefaultMetadataType);

		var categoriesTypeMap = ResolveMessageTypeUsingNamingStrategy(
			settings.MessageTypeMapping,
			messageTypeNamingStrategy
		);

		var automaticTypeMappingRegistration = settings.MessageTypeMapping.AutomaticTypeMappingRegistration
		                                    ?? AutomaticTypeMappingRegistration.Enabled;

		var messageTypeRegistry = new MessageTypeRegistry();
		messageTypeRegistry.Register(settings.MessageTypeMapping.TypeMap);
		messageTypeRegistry.Register(categoriesTypeMap);

		var serializers = new Dictionary<ContentType, ISerializer> {
			{
				ContentType.Json,
				settings.JsonSerializer ?? new SystemTextJsonSerializer()
			}, {
				ContentType.Bytes,
				settings.BytesSerializer ?? new SystemTextJsonSerializer()
			}
		};

		return new SchemaRegistry(
			serializers,
			messageTypeNamingStrategy,
			messageTypeRegistry,
			automaticTypeMappingRegistration
		);
	}

	static Dictionary<string, Type> ResolveMessageTypeUsingNamingStrategy(
		MessageTypeMappingSettings messageTypeMappingSettings,
		IMessageTypeNamingStrategy messageTypeNamingStrategy
	) =>
		messageTypeMappingSettings.CategoryTypesMap
			.SelectMany(
				categoryTypes => categoryTypes.Value.Select(
					type =>
					(
						Type: type,
						TypeName: messageTypeNamingStrategy.ResolveTypeName(
							type,
							new MessageTypeNamingResolutionContext(categoryTypes.Key)
						)
					)
				)
			)
			.ToDictionary(
				ks => ks.TypeName,
				vs => vs.Type
			);
}
