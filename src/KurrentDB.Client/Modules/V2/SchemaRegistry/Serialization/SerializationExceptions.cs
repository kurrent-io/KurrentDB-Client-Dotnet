using KurrentDB.Client.Model;

namespace KurrentDB.Client.SchemaRegistry.Serialization;

/// <summary>
/// Base exception for all schema serialization related errors.
/// </summary>
public abstract class SerializationException(string message, SchemaDataFormat dataFormat, Exception? innerException = null) : Exception(message, innerException) {
	public SchemaDataFormat DataFormat { get; } = dataFormat;
}

/// <summary>
/// Exception thrown when serialization of an object fails.
/// </summary>
public class SerializationFailedException(SchemaDataFormat dataFormat, Type messageType, Exception? innerException = null)
	: SerializationException($"Failed to serialize value'{messageType}' using {dataFormat} format.", dataFormat, innerException) {
	public Type MessageType { get; } = messageType;
}

/// <summary>
/// Exception thrown when deserialization of data fails.
/// </summary>
public class DeserializationFailedException(SchemaDataFormat dataFormat, SchemaName schemaName, Exception? innerException = null)
	: SerializationException($"Failed to deserialize data with schema '{schemaName}' using {dataFormat} format.", dataFormat, innerException) {
	public SchemaName SchemaName { get; } = schemaName;
}

/// <summary>
/// Exception thrown when a schema format is not supported by the serializer.
/// </summary>
public class UnsupportedSchemaDataFormatException(SchemaDataFormat expectedFormat, SchemaDataFormat actualFormat)
	: SerializationException($"Unsupported schema format. Expected {expectedFormat}, but got {actualFormat}.", actualFormat) {
	public SchemaDataFormat ExpectedFormat { get; } = expectedFormat;
	public SchemaDataFormat ActualFormat   { get; } = actualFormat;
}

/// <summary>
/// Exception thrown when schema auto-registration is disabled but required.
/// </summary>
public class AutoRegistrationDisabledException(SchemaDataFormat dataFormat, Type messageType)
	: SerializationException($"The message '{messageType.FullName}' is not mapped and auto-registration is disabled.", dataFormat) {
	public Type MessageType { get; } = messageType;
}
