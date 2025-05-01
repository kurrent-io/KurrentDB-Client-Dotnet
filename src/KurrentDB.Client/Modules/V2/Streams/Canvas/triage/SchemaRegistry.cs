// namespace KurrentDB.Client.Schema.triage;
//
// using static Constants.Metadata.ContentTypes;
//
// public enum ContentType {
// 	Json = 1,
//
// 	// Protobuf  = 2,
// 	// Avro      = 3,
// 	Bytes = 4
// }
//
// static class ContentTypeExtensions {
// 	public static ContentType FromMessageContentType(string contentType) =>
// 		contentType == ApplicationJson
// 			? ContentType.Json
// 			: ContentType.Bytes;
//
// 	public static string ToMessageContentType(this ContentType contentType) =>
// 		contentType switch {
// 			ContentType.Json  => ApplicationJson,
// 			ContentType.Bytes => ApplicationOctetStream,
// 			_                 => throw new ArgumentOutOfRangeException(nameof(contentType), contentType, null)
// 		};
// }
//
// class SchemaRegistry(
// 	IDictionary<ContentType, ISerializer> serializers,
// 	IMessageTypeNamingStrategy messageTypeNamingStrategy,
// 	MessageTypeRegistry messageTypeRegistry,
// 	AutomaticTypeMappingRegistration automaticTypeMappingRegistration
// ) {
// 	public ISerializer GetSerializer(ContentType schemaType) =>
// 		serializers[schemaType];
//
// 	public string ResolveTypeName(Type messageType, MessageTypeNamingResolutionContext resolutionContext) =>
// 		messageTypeRegistry.GetTypeName(messageType) ??
// 		messageTypeNamingStrategy.ResolveTypeName(messageType, resolutionContext);
//
// 	public bool TryResolveClrType(EventRecord record, out Type? type) {
//
// 		// consume - zero code
// 		// 1. record received from the server
// 		// 2. check type mapper for clr type matching the event type name
// 		// 3. if exists then return it
// 		// 4. if not found, try to resolve the clr type from the event type name directly
// 		// 5. if resolved then register the type in the type mapper
// 		// 6. if not resolved, boom!!
//
//
// 		type = messageTypeRegistry.GetClrType(record.EventType);
//
// 		if (type is not null)
// 			return true;
//
// 		if (automaticTypeMappingRegistration == AutomaticTypeMappingRegistration.Disabled)
// 			return false;
//
// 		// 4.
// 		if (!messageTypeNamingStrategy.TryResolveClrTypeName(record, out var clrTypeName) || clrTypeName == null)
// 			return false;
//
// 		type = TypeProvider.GetTypeByFullName(clrTypeName);
//
// 		if (type is null)
// 			return false; // boom
//
// 		messageTypeRegistry.Register(record.EventType, type);
//
// 		return true;
// 	}
//
// 	public bool TryResolveClrMetadataType(EventRecord record, out Type? type) {
// 		type = messageTypeRegistry.GetClrType($"{record.EventType}-metadata");
//
// 		if (type != null)
// 			return true;
//
// 		if (automaticTypeMappingRegistration == AutomaticTypeMappingRegistration.Disabled)
// 			return false;
//
// 		if (!messageTypeNamingStrategy.TryResolveClrMetadataTypeName(record, out var clrTypeName) || clrTypeName == null)
// 			return false;
//
// 		type = TypeProvider.GetTypeByFullName(clrTypeName);
//
// 		if (type == null)
// 			return false;
//
// 		messageTypeRegistry.Register($"{record.EventType}-metadata", type);
//
// 		return true;
// 	}
//
// 	public static SchemaRegistry From(KurrentDBClientSerializationSettings settings) {
// 		var messageTypeNamingStrategy =
// 			settings.MessageTypeNamingStrategy
// 		 ?? new DefaultMessageTypeNamingStrategy(settings.MessageTypeMapping.DefaultMetadataType);
//
// 		var categoriesTypeMap = ResolveMessageTypeUsingNamingStrategy(
// 			settings.MessageTypeMapping,
// 			messageTypeNamingStrategy
// 		);
//
// 		var automaticTypeMappingRegistration = settings.MessageTypeMapping.AutomaticTypeMappingRegistration
// 		                                    ?? AutomaticTypeMappingRegistration.Enabled;
//
// 		var messageTypeRegistry = new MessageTypeRegistry();
// 		messageTypeRegistry.Register(settings.MessageTypeMapping.TypeMap);
// 		messageTypeRegistry.Register(categoriesTypeMap);
//
// 		var serializers = new Dictionary<ContentType, ISerializer> {
// 			{
// 				ContentType.Json,
// 				settings.JsonSerializer ?? new SystemTextJsonSerializer()
// 			}, {
// 				ContentType.Bytes,
// 				settings.BytesSerializer ?? new SystemTextJsonSerializer()
// 			}
// 		};
//
// 		return new SchemaRegistry(
// 			serializers,
// 			messageTypeNamingStrategy,
// 			messageTypeRegistry,
// 			automaticTypeMappingRegistration
// 		);
// 	}
//
// 	static Dictionary<string, Type> ResolveMessageTypeUsingNamingStrategy(
// 		MessageTypeMappingSettings messageTypeMappingSettings,
// 		IMessageTypeNamingStrategy messageTypeNamingStrategy
// 	) =>
// 		messageTypeMappingSettings.CategoryTypesMap
// 			.SelectMany(
// 				categoryTypes => categoryTypes.Value.Select(
// 					type =>
// 					(
// 						Type: type,
// 						TypeName: messageTypeNamingStrategy.ResolveTypeName(
// 							type,
// 							new MessageTypeNamingResolutionContext(categoryTypes.Key)
// 						)
// 					)
// 				)
// 			)
// 			.ToDictionary(
// 				ks => ks.TypeName,
// 				vs => vs.Type
// 			);
// }