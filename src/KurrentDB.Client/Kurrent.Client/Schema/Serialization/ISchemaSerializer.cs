using Kurrent.Client.Streams;

namespace Kurrent.Client.Schema.Serialization;

/// <summary>
/// Defines the contract for schema-based serialization and deserialization operations.
/// </summary>
public interface ISchemaSerializer {
	/// <summary>
	/// Gets the data format used by this serializer.
	/// </summary>
	SchemaDataFormat DataFormat { get; }

	/// <summary>
	/// Serializes an object to a byte array according to a schema.
	/// </summary>
	/// <param name="value">The object to serialize, which may be null.</param>
	/// <param name="context">Context information for the serialization operation.</param>
	/// <returns>A value task containing the serialized data as bytes.</returns>
	ValueTask<ReadOnlyMemory<byte>> Serialize(object? value, SchemaSerializationContext context);

	/// <summary>
	/// Deserializes a byte array to an object according to a schema.
	/// </summary>
	/// <param name="data">The bytes to deserialize.</param>
	/// <param name="context">Context information for the deserialization operation.</param>
	/// <returns>A value task containing the deserialized object, which may be null.</returns>
	ValueTask<object?> Deserialize(ReadOnlyMemory<byte> data, SchemaSerializationContext context);
}

/// <summary>
/// Provides context information for schema serialization and deserialization operations.
/// </summary>
/// <param name="Stream">The name of the data stream being processed.</param>
/// <param name="Metadata">Additional metadata associated with the operation.</param>
/// <param name="SchemaRegistryPolicy">Policy settings for schema registry interactions.</param>
/// <param name="CancellationToken">Token for cancellation support during async operations.</param>
public record struct SchemaSerializationContext(string Stream, Metadata Metadata, SchemaRegistryPolicy SchemaRegistryPolicy, CancellationToken CancellationToken);
