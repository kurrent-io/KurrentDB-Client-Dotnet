#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System.Diagnostics;
using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry;

namespace Kurrent.Client.SchemaRegistry.Serialization;

public record SchemaSerializerOptions {
	/// <summary>
	/// Configuration options for schema registration, validation, and type mapping behaviors.
	/// </summary>
	public SchemaRegistrationOptions SchemaRegistration { get; init; } = new SchemaRegistrationOptions();

	/// <summary>
	/// Specifies whether only schemas explicitly mapped to a .NET type should be consumed during
	/// deserialization.
	/// When <c>true</c>, only schemas with mapped .NET types will be processed during deserialization.
	/// </summary>
	public bool ConsumeMappedOnly { get; set; } = true;
}

public abstract class SchemaSerializerBase(SchemaSerializerOptions options, SchemaManager schemaManager) : ISchemaSerializer {
	SchemaSerializerOptions Options       { get; } = options;
	SchemaManager           SchemaManager { get; } = schemaManager;

	public abstract SchemaDataFormat DataFormat { get; }

	async ValueTask<ReadOnlyMemory<byte>> ISchemaSerializer.Serialize(object? value, SchemaSerializationContext context) {
		// -------------------------------------------------------------------------------------------------
		// these debug asserts ensure the required info is set during the development process
		// -------------------------------------------------------------------------------------------------
		Debug.Assert(!string.IsNullOrWhiteSpace(context.Stream) || context.Metadata.GetOrDefault<string>(SystemMetadataKeys.Stream) is not null, "Stream name is missing in the metadata");
		Debug.Assert(context.Metadata.Get(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Unspecified) == DataFormat, "Schema data format does not match the serializer data format");
		// -------------------------------------------------------------------------------------------------

		if (value is null)
			return ReadOnlyMemory<byte>.Empty;

		var policy = context.SchemaRegistryPolicy
			.Resolve(Options.SchemaRegistration, context.Stream)
			.EnsureDataFormatCompliance(DataFormat);

		var messageType = value.GetType();

		try {
			var result = await SchemaManager
				.RegisterOrValidateSchema(messageType, policy, context.CancellationToken)
				.ConfigureAwait(false);

			context.Metadata
				.With(SystemMetadataKeys.SchemaName, result.SchemaName)
				.With(SystemMetadataKeys.SchemaDataFormat, DataFormat);

			if (result.SchemaVersionId != SchemaVersionId.None)
				context.Metadata.With(SystemMetadataKeys.SchemaVersionId, result.SchemaVersionId);

			return Serialize(value);
		}
		catch (Exception ex) when (ex is not SerializationException) {
			throw new SerializationFailedException(DataFormat, messageType,  ex);
		}
	}

	async ValueTask<object?> ISchemaSerializer.Deserialize(ReadOnlyMemory<byte> data, SchemaSerializationContext context) {
		var schemaInfo = context.Metadata.GetSchemaInfo();

		// -------------------------------------------------------------------------------------------------
		// these debug asserts ensure the required info is set during the development process
		// -------------------------------------------------------------------------------------------------
		Debug.Assert(schemaInfo.HasSchemaName, "Schema name is missing in the metadata");
		Debug.Assert(schemaInfo.HasDataFormat, "Schema data format is missing in the metadata");
		Debug.Assert(schemaInfo.DataFormat == DataFormat, "Schema data format does not match the serializer data format");
		// -------------------------------------------------------------------------------------------------

		if (data.IsEmpty)
			return null;

		try {
			var policy = context.SchemaRegistryPolicy
				.Resolve(Options.SchemaRegistration, context.Stream)
				.EnsureDataFormatCompliance(DataFormat);

			var result = await SchemaManager
				.ValidateAndEnsureSchemaCompatibility(schemaInfo, policy, context.CancellationToken)
				.ConfigureAwait(false);

			// set the schema version id in the metadata
			if(result.SchemaVersionId != SchemaVersionId.None)
				context.Metadata.With(SystemMetadataKeys.SchemaVersionId, result.SchemaVersionId);

			return Deserialize(data, result.MessageType);
		}
		catch (Exception ex) when (ex is not SerializationException) {
			throw new DeserializationFailedException(DataFormat, schemaInfo.SchemaName,  ex);
		}
	}

	/// <summary>
	/// Serializes the given object to a byte array representation.
	/// </summary>
	/// <param name="value">The object to be serialized. Can be null.</param>
	/// <returns>A read-only memory containing the serialized byte array of the object.</returns>
	protected abstract ReadOnlyMemory<byte> Serialize(object? value);

	/// <summary>
	/// Deserializes the given byte array representation to an object of the specified type.
	/// </summary>
	/// <param name="data">The memory segment containing the serialized byte array of the object.</param>
	/// <param name="resolvedType">The target type to which the byte array should be deserialized.</param>
	/// <returns>The deserialized object of the specified type.</returns>
	protected abstract object Deserialize(ReadOnlyMemory<byte> data, Type resolvedType);
}
