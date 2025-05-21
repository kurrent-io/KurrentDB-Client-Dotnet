using JetBrains.Annotations;

namespace KurrentDB.Client.SchemaRegistry;

/// <summary>
/// Responsible for mapping message types to schema names and managing the bidirectional mapping
/// between schemas and message types. Provides methods for registering, retrieving, and verifying
/// mappings between schema names and message types.
/// </summary>
public class MessageTypeMapper {
	public static readonly Type Missing = Type.Missing.GetType();

	ConcurrentBidirectionalDictionary<string, Type> TypeMap { get; } = new();

	/// <summary>
	/// Retrieves the message type associated with the specified schema name, or maps the given schema name
	/// to the provided message type if no mapping exists. If a mapping already exists for the schema name
	/// and it conflicts with the given message type, a <see cref="MessageTypeConflictException"/> is thrown.
	/// </summary>
	/// <param name="schemaName">The schema name to retrieve or map to the specified message type.</param>
	/// <param name="messageType">The message type to map to the given schema name, if no mapping exists.</param>
	/// <returns>The message type associated with the specified schema name.</returns>
	/// <exception cref="MessageTypeConflictException">
	/// Thrown when the schema name is already mapped to a different message type than the one provided.
	/// </exception>
	public Type GetOrMap(SchemaName schemaName, Type messageType) {
		if (TypeMap.TryAdd(schemaName, messageType))
			return messageType;

		var registeredType = TypeMap[schemaName];
		if (registeredType != messageType)
			throw new MessageTypeConflictException(schemaName, registeredType, messageType);

		return registeredType;
	}

	public bool TryMap(SchemaName schemaName, Type messageType) =>
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

	public Type GetOrMap<T>(string schemaName) =>
		GetOrMap(schemaName, typeof(T));

	public Type GetMessageType(string schemaName, bool throwWhenMissing = true) {
		return TypeMap.TryGetValue(schemaName, out var messageType)
			? messageType
			: throwWhenMissing
				? throw new SchemaRegistrationNotFoundException(schemaName)
				: Missing;
	}

	public SchemaName GetSchemaName(Type messageType, bool throwWhenMissing = true) {
		return TypeMap.TryGetKey(messageType, out var schemaName)
			? schemaName
			: throwWhenMissing
				? throw new MessageTypeRegistrationNotFoundException(messageType)
				: SchemaName.None;
	}

	public SchemaName GetSchemaNameOrDefault(Type messageType, SchemaName defaultSchemaName) =>
		TypeMap.TryGetKey(messageType, out var schemaName) ? schemaName : defaultSchemaName;

	public bool IsMessageTypeMapped(Type messageType) =>
		TypeMap.ContainsValue(messageType);

	public bool IsMessageTypeMapped(SchemaName schemaName) =>
		TypeMap.ContainsKey(schemaName);
}

/// <summary>
/// Base class for all MessageTypeRegistry exceptions.
/// </summary>
[PublicAPI]
public abstract class MessageTypeRegistryException(string message) : InvalidOperationException(message);

[PublicAPI]
public class MessageTypeAlreadyRegisteredException(string schemaName, Type attemptedMessageType, Type registeredMessageType)
	: MessageTypeRegistryException(FormatMessage(schemaName, attemptedMessageType, registeredMessageType)) {
	public string SchemaName            { get; } = schemaName;
	public Type   AttemptedMessageType  { get; } = attemptedMessageType;
	public Type   RegisteredMessageType { get; } = registeredMessageType;

	static string FormatMessage(string schemaName, Type attemptedMessageType, Type registeredMessageType) =>
		$"The message '{attemptedMessageType.Name}' is already registered with the name '{schemaName}' as '{registeredMessageType.FullName}'.";
}

[PublicAPI]
public class SchemaRegistrationNotFoundException(string schemaName) : MessageTypeRegistryException(FormatMessage(schemaName)) {
	public string SchemaName { get; } = schemaName;

	static string FormatMessage(string schemaName) =>
		$"Schema {schemaName} registration not found";
}

[PublicAPI]
public class MessageTypeRegistrationNotFoundException(Type messageType) : MessageTypeRegistryException(FormatMessage(messageType)) {
	public Type MessageType { get; } = messageType;

	static string FormatMessage(Type messageType) =>
		$"Message {messageType.Name} registration not found";
}

/// <summary>
/// Exception thrown when a schema type registration conflict occurs.
/// </summary>
public class MessageTypeConflictException(SchemaName schemaName, Type registeredType, Type attemptedType)
	: MessageTypeRegistryException(FormatMessage(schemaName, registeredType, attemptedType)) {
	public Type AttemptedType  { get; } = attemptedType;
	public Type RegisteredType { get; } = registeredType;

	static string FormatMessage(SchemaName schemaName, Type registeredType, Type attemptedType) =>
		$"Schema '{schemaName}' is already registered with type '{registeredType.FullName}' but attempted to use with incompatible type '{attemptedType.FullName}'.";
}
