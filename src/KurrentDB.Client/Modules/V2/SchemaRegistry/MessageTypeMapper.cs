using KurrentDB.Client;

namespace Kurrent.Client.SchemaRegistry;

/// <summary>
/// Responsible for mapping message types to schema names and managing the bidirectional mapping
/// between schemas and message types. Provides methods for mapping, retrieving, and verifying
/// mappings between schema names and message types.
/// </summary>
public class MessageTypeMapper {
	public static readonly Type Missing = Type.Missing.GetType();

	ConcurrentBidirectionalDictionary<string, Type> TypeMap { get; } = new();

	/// <summary>
	/// Attempts to add a mapping between a schema name and a message type.
	/// </summary>
	/// <param name="schemaName">The schema name to map.</param>
	/// <param name="messageType">The message type to map to the schema name.</param>
	/// <returns>
	/// <c>true</c> if the mapping was added successfully; <c>false</c> if the
	/// schema name already has a mapping.
	/// </returns>
	public bool TryMap(SchemaName schemaName, Type messageType) {
		if (TypeMap.TryAdd(schemaName, messageType)) return true;

		var mappedType = TypeMap[schemaName];
		if (mappedType != messageType)
			throw new MessageTypeConflictException(schemaName, mappedType, messageType);

		return false;
	}

	/// <summary>
	/// Attempts to add a mapping between a schema name and a message type.
	/// </summary>
	/// <typeparam name="T">The type to map to the schema name if no mapping exists.</typeparam>
	/// <param name="schemaName">The schema name to map.</param>
	/// <returns>
	/// <c>true</c> if the mapping was added successfully; <c>false</c> if the
	/// schema name already has a mapping.
	/// </returns>
	public bool TryMap<T>(SchemaName schemaName) =>
		TryMap(schemaName, typeof(T));

	/// <summary>
	/// Attempts to retrieve the message type associated with the specified schema name.
	/// </summary>
	/// <param name="schemaName">The schema name to look up.</param>
	/// <param name="messageType">
	/// When this method returns, contains the message type associated with the specified schema name,
	/// or <see cref="Missing"/> if the schema name is not found.
	/// </param>
	/// <returns>
	/// <c>true</c> if the schema name was found in the type map; otherwise, <c>false</c>.
	/// </returns>
	public bool TryGetMessageType(SchemaName schemaName, out Type messageType) {
		if (TypeMap.TryGetValue(schemaName, out var mappedType)) {
			messageType = mappedType;
			return true;
		}

		messageType = Missing;
		return false;
	}

	/// <summary>
	/// Attempts to retrieve the schema name associated with the specified message type.
	/// </summary>
	/// <param name="messageType">The message type to look up.</param>
	/// <param name="schemaName">
	/// When this method returns, contains the schema name associated with the specified message type,
	/// or <see cref="SchemaName.None"/> if the message type is not found.
	/// </param>
	/// <returns>
	/// <c>true</c> if the message type was found in the type map; otherwise, <c>false</c>.
	/// </returns>
	public bool TryGetSchemaName(Type messageType, out SchemaName schemaName) {
		if (TypeMap.TryGetKey(messageType, out var mappedName)) {
			schemaName = mappedName;
			return true;
		}

		schemaName = SchemaName.None;
		return false;
	}

	/// <summary>
	/// Retrieves the message type associated with the specified schema name.
	/// </summary>
	/// <param name="schemaName">The schema name to look up.</param>
	/// <param name="throwWhenMissing">
	/// Determines whether to throw an exception when the schema name is not found.
	/// If <c>false</c>, returns <see cref="Missing"/> instead.
	/// </param>
	/// <returns>
	/// The message type associated with the specified schema name, or <see cref="Missing"/> if
	/// the schema name is not found and <paramref name="throwWhenMissing"/> is <c>false</c>.
	/// </returns>
	/// <exception cref="SchemaNameMapNotFoundException">
	/// Thrown when the schema name is not found and <paramref name="throwWhenMissing"/> is <c>true</c>.
	/// </exception>
	public Type GetMessageType(string schemaName, bool throwWhenMissing = true) {
		return TypeMap.TryGetValue(schemaName, out var messageType)
			? messageType
			: throwWhenMissing
				? throw new SchemaNameMapNotFoundException(schemaName)
				: Missing;
	}

	/// <summary>
	/// Retrieves the schema name associated with the specified message type.
	/// </summary>
	/// <param name="messageType">The message type to look up.</param>
	/// <param name="throwWhenMissing">
	/// Determines whether to throw an exception when the message type is not found.
	/// If <c>false</c>, returns <see cref="SchemaName.None"/> instead.
	/// </param>
	/// <returns>
	/// The schema name associated with the specified message type, or <see cref="SchemaName.None"/> if
	/// the message type is not found and <paramref name="throwWhenMissing"/> is <c>false</c>.
	/// </returns>
	/// <exception cref="MessageTypeMapNotFoundException">
	/// Thrown when the message type is not found and <paramref name="throwWhenMissing"/> is <c>true</c>.
	/// </exception>
	public SchemaName GetSchemaName(Type messageType, bool throwWhenMissing = true) {
		return TypeMap.TryGetKey(messageType, out var schemaName)
			? schemaName
			: throwWhenMissing
				? throw new MessageTypeMapNotFoundException(messageType)
				: SchemaName.None;
	}

	/// <summary>
	/// Retrieves the schema name associated with the specified message type,
	/// or returns the provided default schema name if the message type is not found.
	/// </summary>
	/// <param name="messageType">The message type to look up.</param>
	/// <param name="defaultSchemaName">The default schema name to return if the message type is not found.</param>
	/// <returns>
	/// The schema name associated with the specified message type, or the specified default schema name
	/// if the message type is not found.
	/// </returns>
	public SchemaName GetSchemaNameOrDefault(Type messageType, SchemaName defaultSchemaName) =>
		TypeMap.TryGetKey(messageType, out var schemaName) ? schemaName : defaultSchemaName;

	/// <summary>
	/// Determines whether the specified message type is mapped to any schema name.
	/// </summary>
	/// <param name="messageType">The message type to check.</param>
	/// <returns>
	/// <c>true</c> if the message type is mapped to a schema name; otherwise, <c>false</c>.
	/// </returns>
	public bool ContainsMapTo(Type messageType) =>
		TypeMap.ContainsValue(messageType);

	/// <summary>
	/// Determines whether the specified schema name is mapped to any message type.
	/// </summary>
	/// <param name="schemaName">The schema name to check.</param>
	/// <returns>
	/// <c>true</c> if the schema name is mapped to a message type; otherwise, <c>false</c>.
	/// </returns>
	public bool ContainsMapTo(SchemaName schemaName) =>
		TypeMap.ContainsKey(schemaName);

	/// <summary>
	/// Resolves the message type associated with the specified schema name. Attempts to retrieve
	/// the message type from the existing mapping or resolve it using known system types. If the schema
	/// name is not mapped and cannot be resolved, an exception is thrown.
	/// </summary>
	/// <param name="schemaName">The schema name for which the message type should be resolved.</param>
	/// <returns>The resolved message type associated with the specified schema name.</returns>
	/// <exception cref="MessageTypeResolutionException">
	/// Thrown if the schema name is not mapped and cannot be resolved to any known type.
	/// </exception>
	public Type GetOrResolveMessageType(SchemaName schemaName) =>
		TryGetMessageType(schemaName, out var messageType) || SystemTypes.TryResolveType(schemaName, out messageType)
			? messageType : throw new MessageTypeResolutionException(schemaName);
}

/// <summary>
/// Base class for all MessageTypeRegistry exceptions.
/// </summary>
[PublicAPI]
public abstract class MessageTypeMapperException(string message) : InvalidOperationException(message);

[PublicAPI]
public class MessageTypeAlreadyMappedException(string schemaName, Type attemptedMessageType, Type registeredMessageType)
	: MessageTypeMapperException(FormatMessage(schemaName, attemptedMessageType, registeredMessageType)) {
	public string SchemaName            { get; } = schemaName;
	public Type   AttemptedMessageType  { get; } = attemptedMessageType;
	public Type   RegisteredMessageType { get; } = registeredMessageType;

	static string FormatMessage(string schemaName, Type attemptedMessageType, Type registeredMessageType) =>
		$"Message '{attemptedMessageType.Name}' is already mapped with the name '{schemaName}' as '{registeredMessageType.FullName}'";
}

[PublicAPI]
public class SchemaNameMapNotFoundException(string schemaName) : MessageTypeMapperException(FormatMessage(schemaName)) {
	public string SchemaName { get; } = schemaName;

	static string FormatMessage(string schemaName) =>
		$"Schema '{schemaName}' not mapped";
}

[PublicAPI]
public class MessageTypeMapNotFoundException(Type messageType) : MessageTypeMapperException(FormatMessage(messageType)) {
	public Type MessageType { get; } = messageType;

	static string FormatMessage(Type messageType) =>
		$"Message '{messageType.Name}' not mapped";
}

/// <summary>
/// Exception thrown when a schema type registration conflict occurs.
/// </summary>
public class MessageTypeConflictException(SchemaName schemaName, Type mappedType, Type attemptedType)
	: MessageTypeMapperException(FormatMessage(schemaName, mappedType, attemptedType)) {
	public Type AttemptedType { get; } = attemptedType;
	public Type MappedType    { get; } = mappedType;

	static string FormatMessage(SchemaName schemaName, Type registeredType, Type attemptedType) =>
		$"Schema '{schemaName}' is already mapped with type '{registeredType.FullName}' but attempted to use with incompatible type '{attemptedType.FullName}'";
}

/// <summary>
/// Exception thrown when a schema is not mapped and cannot be resolved to any known type.
/// </summary>
[PublicAPI]
public class MessageTypeResolutionException(SchemaName schemaName) : MessageTypeMapperException(FormatMessage(schemaName)) {
	public SchemaName SchemaName { get; } = schemaName;

	static string FormatMessage(SchemaName schemaName) =>
		$"Schema '{schemaName}' not mapped and does not match any known type";
}
