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
	IMessageTypeNamingStrategy messageTypeNamingStrategy
) {
	public ISerializer GetSerializer(ContentType schemaType) =>
		serializers[schemaType];

	public string ResolveTypeName(Type messageType, MessageTypeNamingResolutionContext resolutionContext) =>
		messageTypeNamingStrategy.ResolveTypeName(messageType, resolutionContext);

#if NET48
	public bool TryResolveClrType(string messageTypeName, out Type? type) =>
#else
	public bool TryResolveClrType(string messageTypeName, [NotNullWhen(true)] out Type? type) =>
#endif
		messageTypeNamingStrategy.TryResolveClrType(messageTypeName, out type);

#if NET48
	public bool TryResolveClrMetadataType(string messageTypeName, out Type? type) =>
#else
	public bool TryResolveClrMetadataType(string messageTypeName, [NotNullWhen(true)] out Type? type) =>
#endif
		messageTypeNamingStrategy.TryResolveClrMetadataType(messageTypeName, out type);

	public static SchemaRegistry From(KurrentDBClientSerializationSettings settings) {
		var messageTypeNamingStrategy =
			settings.MessageTypeNamingStrategy ?? new DefaultMessageTypeNamingStrategy(settings.DefaultMetadataType);

		var categoriesTypeMap = ResolveMessageTypeUsingNamingStrategy(
			settings.CategoryMessageTypesMap,
			messageTypeNamingStrategy
		);

		var messageTypeRegistry = new MessageTypeRegistry();
		messageTypeRegistry.Register(settings.MessageTypeMap);
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
			new MessageTypeNamingStrategyWrapper(
				messageTypeRegistry,
				settings.MessageTypeNamingStrategy ?? new DefaultMessageTypeNamingStrategy(settings.DefaultMetadataType)
			)
		);
	}

	static Dictionary<Type, string> ResolveMessageTypeUsingNamingStrategy(
		IDictionary<string, Type[]> categoryMessageTypesMap,
		IMessageTypeNamingStrategy messageTypeNamingStrategy
	) =>
		categoryMessageTypesMap
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
				ks => ks.Type,
				vs => vs.TypeName
			);
}
