using KurrentDB.Client.Model;

namespace KurrentDB.Client.SchemaRegistry.Serialization;

public class SerializationException(string message, Exception? innerException = null) : Exception(message, innerException);

public class SerializationFailedException(SchemaDataFormat expectedSchemaType, string schemaName, Exception? innerException = null)
	: SerializationException($"{expectedSchemaType} failed to serialize {schemaName}", innerException);

public class DeserializationFailedException(SchemaDataFormat expectedSchemaType, string schemaName, Exception? innerException = null)
	: SerializationException($"{expectedSchemaType} failed to deserialize {schemaName}", innerException);

public class UnsupportedSchemaException(SchemaDataFormat expectedSchemaType, SchemaDataFormat schemaType)
	: SerializationException($"Unsupported schema {schemaType} expected {expectedSchemaType}");

public class SerializerNotFoundException(SchemaDataFormat schemaType, params SchemaDataFormat[] supportedSchemaTypes)
	: SerializationException($"Unsupported schema {schemaType} expected one of {string.Join(", ", supportedSchemaTypes)}");
