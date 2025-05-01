using KurrentDB.Client.Model;

namespace KurrentDB.Client.Schema.Serialization;

public class SerializationException(string message, Exception? innerException = null) : Exception(message, innerException);

public class SerializationFailedException(SchemaDataFormat expectedSchemaType, SchemaInfo schemaInfo, Exception? innerException = null)
	: SerializationException($"{expectedSchemaType} failed to serialize {schemaInfo.SchemaName}", innerException);

public class DeserializationFailedException(SchemaDataFormat expectedSchemaType, SchemaInfo schemaInfo, Exception? innerException = null)
	: SerializationException($"{expectedSchemaType} failed to deserialize {schemaInfo.SchemaName}", innerException);

public class UnsupportedSchemaException(SchemaDataFormat expectedSchemaType, SchemaDataFormat schemaType)
	: SerializationException($"Unsupported schema {schemaType} expected {expectedSchemaType}");

public class SerializerNotFoundException(SchemaDataFormat schemaType, params SchemaDataFormat[] supportedSchemaTypes)
	: SerializationException($"Unsupported schema {schemaType} expected one of {string.Join(", ", supportedSchemaTypes)}");