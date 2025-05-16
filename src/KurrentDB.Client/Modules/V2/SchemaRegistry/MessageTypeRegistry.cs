using JetBrains.Annotations;

namespace KurrentDB.Client.SchemaRegistry;

[PublicAPI]
public class MessageTypeRegistry {
	public static readonly Type Missing = Type.Missing.GetType();

	ConcurrentBidirectionalDictionary<string, Type> TypeMap { get; } = new();

	public Type GetOrRegister(SchemaName schemaName, Type messageType) {
		if (TypeMap.TryAdd(schemaName, messageType))
			return messageType;

		var registeredType = TypeMap[schemaName];
		if (registeredType != messageType)
			throw new InvalidOperationException($"The message '{messageType.Name}' is already registered with the name '{schemaName}' as '{registeredType.FullName}'.");

		return registeredType;
	}

	public bool TryRegister(SchemaName schemaName, Type messageType) =>
		TypeMap.TryAdd(schemaName, messageType);

	public bool TryGetMessageType(SchemaName schemaName, out Type messageType) {
		if (TypeMap.TryGetValue(schemaName, out var registeredMessageType)) {
			messageType = registeredMessageType;
			return true;
		}

		messageType = Missing;
		return false;
	}

	public bool TryGetSchemaName(Type messageType, out SchemaName schemaName) {
		if (TypeMap.TryGetKey(messageType, out var registeredSchemaName)) {
			schemaName = registeredSchemaName;
			return true;
		}

		schemaName = SchemaName.None;
		return false;
	}

	public Type GetOrRegister<T>(string schemaName) =>
		GetOrRegister(schemaName, typeof(T));

	public Type GetMessageType(string schemaName, bool throwWhenMissing = true) {
		return TypeMap.TryGetValue(schemaName, out var messageType)
			? messageType
			: throwWhenMissing
				? throw new UnregisteredMessageTypeException(schemaName)
				: Missing;
	}

	public SchemaName GetSchemaName(Type messageType, bool throwWhenMissing = true) {
		return TypeMap.TryGetKey(messageType, out var schemaName)
			? schemaName
			: throwWhenMissing
				? throw new UnregisteredMessageTypeException(messageType)
				: SchemaName.None;
	}

	public bool IsMessageTypeRegistered(Type messageType) =>
		TypeMap.ContainsValue(messageType);

	public bool IsSchemaNameRegistered(SchemaName schemaName) =>
		TypeMap.ContainsKey(schemaName);

	public bool ContainsMessageType(Type messageType) =>
		TypeMap.ContainsValue(messageType);

	public bool ContainsSchemaName(SchemaName schemaName) =>
		TypeMap.ContainsKey(schemaName);

	public void Register(Dictionary<string, Type> typeMap) {
		foreach (var map in typeMap) TryRegister(map.Key, map.Value);
	}
}

public class UnregisteredMessageTypeException : Exception {
	public UnregisteredMessageTypeException(Type type) : base($"Message {type.Name} registration not found") { }

	public UnregisteredMessageTypeException(string schemaName) : base($"Schema {schemaName} registration not found") { }
}
