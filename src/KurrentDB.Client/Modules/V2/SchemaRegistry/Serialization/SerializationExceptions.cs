using KurrentDB.Client.Model;

namespace KurrentDB.Client.SchemaRegistry.Serialization;

/// <summary>
/// Base exception for all schema serialization related errors.
/// </summary>
public abstract class SchemaSerializationException(string message, SchemaDataFormat dataFormat, SchemaName schemaName, Exception? innerException = null)
	: Exception(message, innerException) {
	public SchemaDataFormat DataFormat { get; } = dataFormat;
	public SchemaName       SchemaName { get; } = schemaName;
}

/// <summary>
/// Exception thrown when serialization of an object fails.
/// </summary>
public class SerializationFailedException(SchemaDataFormat dataFormat, SchemaName schemaName, Exception? innerException = null)
	: SchemaSerializationException($"Failed to serialize object with schema '{schemaName}' using {dataFormat} format.", dataFormat, schemaName, innerException);

/// <summary>
/// Exception thrown when deserialization of data fails.
/// </summary>
public class DeserializationFailedException(SchemaDataFormat dataFormat, SchemaName schemaName, Exception? innerException = null)
	: SchemaSerializationException($"Failed to deserialize data with schema '{schemaName}' using {dataFormat} format.", dataFormat, schemaName, innerException);

/// <summary>
/// Exception thrown when a schema format is not supported by the serializer.
/// </summary>
public class UnsupportedSchemaDataFormatException(SchemaDataFormat expectedFormat, SchemaDataFormat actualFormat)
	: SchemaSerializationException($"Unsupported schema format. Expected {expectedFormat}, but got {actualFormat}.", actualFormat, SchemaName.None) {
	public SchemaDataFormat ExpectedFormat { get; } = expectedFormat;
	public SchemaDataFormat ActualFormat   { get; } = actualFormat;
}

/// <summary>
/// Exception thrown when schema auto-registration is disabled but required.
/// </summary>
public class AutoRegistrationDisabledException(SchemaDataFormat dataFormat, SchemaName schemaName, Type messageType)
	: SchemaSerializationException($"The message '{messageType.FullName}' is not registered and auto-registration is disabled.", dataFormat, schemaName) {
	public Type MessageType { get; } = messageType;
}
