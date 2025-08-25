using Kurrent.Client.Registry;
using Kurrent.Client.Streams;

namespace Kurrent.Client.Schema.Serialization;

/// <summary>
/// Base class for serialization exceptions.
/// Provides a common structure for exceptions related to serialization processes,
/// including metadata about the schema data format used.
/// Inherits from <see cref="KurrentException"/> to maintain consistency in error handling across the Kurrent client.
/// </summary>
public abstract class SchemaSerializationException(string message, SchemaDataFormat dataFormat, Exception? innerException = null)
    : KurrentException(message, new Metadata().With(nameof(SchemaDataFormat), dataFormat), innerException) {
	public SchemaDataFormat DataFormat { get; } = dataFormat;
}

/// <summary>
/// Exception thrown when serialization of an object fails.
/// </summary>
public class SchemaSerializationFailedException(SchemaDataFormat dataFormat, Type messageType, Exception? innerException = null)
	: SchemaSerializationException($"Failed to serialize '{messageType}' using {dataFormat} format.", dataFormat, innerException) {
	public Type MessageType { get; } = messageType;
}

/// <summary>
/// Exception thrown when deserialization of data fails.
/// </summary>
public class SchemaDeserializationFailedException(SchemaDataFormat dataFormat, SchemaName schemaName, Exception? innerException = null)
	: SchemaSerializationException($"Failed to deserialize data with schema '{schemaName}' using {dataFormat} format.", dataFormat, innerException) {
	public SchemaName SchemaName { get; } = schemaName;
}

/// <summary>
/// Exception thrown when a schema format is not supported by the serializer.
/// </summary>
public class UnsupportedSchemaDataFormatException(SchemaDataFormat expectedFormat, SchemaDataFormat actualFormat)
	: SchemaSerializationException($"Unsupported schema format. Expected {expectedFormat}, but got {actualFormat}.", actualFormat) {
	public SchemaDataFormat ExpectedFormat { get; } = expectedFormat;
	public SchemaDataFormat ActualFormat   { get; } = actualFormat;
}

/// <summary>
/// Exception thrown when schema auto-registration is disabled but required.
/// </summary>
public class AutoRegistrationDisabledException(SchemaDataFormat dataFormat, Type messageType)
	: SchemaSerializationException($"The message '{messageType.FullName}' is not mapped and auto-registration is disabled.", dataFormat) {
	public Type MessageType { get; } = messageType;
}
