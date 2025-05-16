// using JetBrains.Annotations;
//
// namespace KurrentDB.Client.SchemaRegistry;
//
// // public class MessageTypeRegistry {
// // 	readonly ConcurrentDictionary<string, Type?> _typeMap     = new();
// // 	readonly ConcurrentDictionary<Type, string>  _typeNameMap = new();
// //
// // 	public void Register(string messageTypeName, Type messageType) {
// // 		_typeNameMap.AddOrUpdate(messageType, messageTypeName, (_, _) => messageTypeName);
// // 		_typeMap.AddOrUpdate(messageTypeName, messageType, (_, type) => type);
// // 	}
// //
// // 	public string? GetTypeName(Type messageType) {
// // #if NET48
// // 		return _typeNameMap.TryGetValue(messageType, out var value) ? value : null;
// // #else
// // 		return _typeNameMap.GetValueOrDefault(messageType);
// // #endif
// // 	}
// //
// // 	public string GetOrAddTypeName(Type clrType, Func<Type, string> getTypeName) =>
// // 		_typeNameMap.GetOrAdd(
// // 			clrType,
// // 			_ => {
// // 				var typeName = getTypeName(clrType);
// // 				_typeMap.TryAdd(typeName, clrType);
// // 				return typeName;
// // 			}
// // 		);
// //
// // 	public Type? GetClrType(string messageTypeName) {
// // #if NET48
// // 		return _typeMap.TryGetValue(messageTypeName, out var value) ? value : null;
// // #else
// // 		return _typeMap.GetValueOrDefault(messageTypeName);
// // #endif
// // 	}
// //
// // 	public Type? GetOrAddClrType(string messageTypeName, Func<string, Type?> getClrType) =>
// // 		_typeMap.GetOrAdd(
// // 			messageTypeName,
// // 			_ => {
// // 				var clrType = getClrType(messageTypeName);
// //
// // 				if (clrType is null)
// // 					return null;
// //
// // 				_typeNameMap.TryAdd(clrType, messageTypeName);
// //
// // 				return clrType;
// // 			}
// // 		);
// // }
// //
// // static class MessageTypeRegistryExtensions {
// // 	public static void Register<T>(this MessageTypeRegistry messageTypeRegistry, string messageTypeName) =>
// // 		messageTypeRegistry.Register(messageTypeName, typeof(T));
// //
// // 	public static void Register(this MessageTypeRegistry messageTypeRegistry, IDictionary<string, Type> typeMap) {
// // 		foreach (var map in typeMap) messageTypeRegistry.Register(map.Key, map.Value);
// // 	}
// //
// // 	public static string? GetTypeName<TMessageType>(this MessageTypeRegistry messageTypeRegistry) =>
// // 		messageTypeRegistry.GetTypeName(typeof(TMessageType));
// // }
//
// [PublicAPI]
// public class MessageTypeRegistry {
// 	public static readonly Type Missing = Type.Missing.GetType();
//
// 	ConcurrentBidirectionalDictionary<string, Type> TypeMap { get; } = new();
//
// 	public Type GetOrRegister(SchemaName schemaName, Type messageType) {
// 		if (TypeMap.TryAdd(schemaName, messageType))
// 			return messageType;
//
// 		var registeredType = TypeMap[schemaName];
// 		if (registeredType != messageType)
// 			throw new InvalidOperationException($"The message '{messageType.Name}' is already registered with the name '{schemaName}' as '{registeredType.FullName}'.");
//
// 		return registeredType;
// 	}
//
// 	public bool TryRegister(SchemaName schemaName, Type messageType) =>
// 		TypeMap.TryAdd(schemaName, messageType);
//
// 	public bool TryGetMessageType(SchemaName schemaName, out Type messageType) {
// 		if (TypeMap.TryGetValue(schemaName, out var registeredMessageType)) {
// 			messageType = registeredMessageType;
// 			return true;
// 		}
//
// 		messageType = Missing;
// 		return false;
// 	}
//
// 	public bool TryGetSchemaName(Type messageType, out SchemaName schemaName) {
// 		if (TypeMap.TryGetKey(messageType, out var registeredSchemaName)) {
// 			schemaName = registeredSchemaName;
// 			return true;
// 		}
//
// 		schemaName = SchemaName.None;
// 		return false;
// 	}
//
// 	public Type GetOrRegister<T>(string schemaName) =>
// 		GetOrRegister(schemaName, typeof(T));
//
// 	public Type GetMessageType(string schemaName, bool throwWhenMissing = true) {
// 		return TypeMap.TryGetValue(schemaName, out var messageType)
// 			? messageType
// 			: throwWhenMissing
// 				? throw new UnregisteredMessageTypeException(schemaName)
// 				: Missing;
// 	}
//
// 	public SchemaName GetSchemaName(Type messageType, bool throwWhenMissing = true) {
// 		return TypeMap.TryGetKey(messageType, out var schemaName)
// 			? schemaName
// 			: throwWhenMissing
// 				? throw new UnregisteredMessageTypeException(messageType)
// 				: SchemaName.None;
// 	}
//
// 	public bool IsMessageTypeRegistered(Type messageType) =>
// 		TypeMap.ContainsValue(messageType);
//
// 	public bool IsSchemaNameRegistered(SchemaName schemaName) =>
// 		TypeMap.ContainsKey(schemaName);
//
// 	public bool ContainsMessageType(Type messageType) =>
// 		TypeMap.ContainsValue(messageType);
//
// 	public bool ContainsSchemaName(SchemaName schemaName) =>
// 		TypeMap.ContainsKey(schemaName);
//
// 	public void Register(Dictionary<string, Type> typeMap) {
// 		foreach (var map in typeMap) TryRegister(map.Key, map.Value);
// 	}
// }
//
// public class UnregisteredMessageTypeException : Exception {
// 	public UnregisteredMessageTypeException(Type type) : base($"Message {type.Name} registration not found") { }
//
// 	public UnregisteredMessageTypeException(string schemaName) : base($"Schema {schemaName} registration not found") { }
// }
