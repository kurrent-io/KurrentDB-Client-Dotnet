using Kurrent.Client.Registry;

namespace Kurrent.Client.Schema;

/// <summary>
/// Base class for exceptions related to message type mapping operations.
/// This class provides a common structure for exceptions that occur during the mapping of message types
/// to schema names, including specific exceptions for conflicts, not found mappings, and resolution failures.
/// It inherits from <see cref="KurrentException"/> to maintain consistency in error handling across the Kurrent client.
/// </summary>
[PublicAPI]
public abstract class MessageTypeMappingException(string message) : KurrentException(message);

[PublicAPI]
public class MessageTypeAlreadyMappedException(string schemaName, Type attemptedMessageType, Type registeredMessageType)
	: MessageTypeMappingException(ErrorMessage(schemaName, attemptedMessageType, registeredMessageType)) {
	public string SchemaName            { get; } = schemaName;
	public Type   AttemptedMessageType  { get; } = attemptedMessageType;
	public Type   RegisteredMessageType { get; } = registeredMessageType;

	static string ErrorMessage(string schemaName, Type attemptedMessageType, Type registeredMessageType) =>
		$"Message '{attemptedMessageType.Name}' is already mapped with the name '{schemaName}' as '{registeredMessageType.FullName}'";
}

[PublicAPI]
public class SchemaNameMapNotFoundException(string schemaName) : MessageTypeMappingException(ErrorMessage(schemaName)) {
	public string SchemaName { get; } = schemaName;

	static string ErrorMessage(string schemaName) =>
		$"Schema '{schemaName}' not mapped";
}

[PublicAPI]
public class MessageTypeMapNotFoundException(Type messageType) : MessageTypeMappingException(ErrorMessage(messageType)) {
	public Type MessageType { get; } = messageType;

	static string ErrorMessage(Type messageType) =>
		$"Message '{messageType.Name}' not mapped";
}

/// <summary>
/// Exception thrown when a schema type registration conflict occurs.
/// </summary>
public class MessageTypeConflictException(SchemaName schemaName, Type mappedType, Type attemptedType)
	: MessageTypeMappingException(ErrorMessage(schemaName, mappedType, attemptedType)) {
	public Type AttemptedType { get; } = attemptedType;
	public Type MappedType    { get; } = mappedType;

	static string ErrorMessage(SchemaName schemaName, Type registeredType, Type attemptedType) =>
		$"Schema '{schemaName}' is already mapped with type '{registeredType.FullName}' but attempted to use with incompatible type '{attemptedType.FullName}'";
}

/// <summary>
/// Exception thrown when a schema is not mapped and cannot be resolved to any known type.
/// </summary>
[PublicAPI]
public class MessageTypeResolutionException(SchemaName schemaName) : MessageTypeMappingException(ErrorMessage(schemaName)) {
	public SchemaName SchemaName { get; } = schemaName;

	static string ErrorMessage(SchemaName schemaName) =>
		$"Schema '{schemaName}' not mapped and does not match any known type";
}
