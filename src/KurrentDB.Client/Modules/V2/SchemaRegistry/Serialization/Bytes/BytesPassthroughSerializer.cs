#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

using System.Diagnostics;
using KurrentDB.Client.Model;

namespace KurrentDB.Client.SchemaRegistry.Serialization.Bytes;

/// <summary>
/// Provides a passthrough implementation of the <see cref="ISchemaSerializer"/> interface
/// for the <see cref="SchemaDataFormat.Bytes"/> format. This serializer does not transform
/// the raw data and assumes the input value is an array or memory structure of bytes.
/// <para />
///  It will enforce the schema data format to be <see cref="SchemaDataFormat.Bytes"/>.
/// </summary>
public class BytesPassthroughSerializer : ISchemaSerializer {
	public SchemaDataFormat DataFormat => SchemaDataFormat.Bytes;

	public ValueTask<ReadOnlyMemory<byte>> Serialize(object? value, SchemaSerializationContext context) {
		Debug.Assert(value is null or byte[] or ReadOnlyMemory<byte> or Memory<byte>, "value must be byte[] or ReadOnlyMemory<byte> or Memory<byte>");

		// enforce the schema data format
		context.Metadata
			.With(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Bytes);

		var result = value switch {
			null                       => ReadOnlyMemory<byte>.Empty,
			byte[] bytes               => bytes,
			ReadOnlyMemory<byte> bytes => bytes,
			Memory<byte> bytes         => bytes
		};

		return new ValueTask<ReadOnlyMemory<byte>>(result);
	}

	public ValueTask<object?> Deserialize(ReadOnlyMemory<byte> data, SchemaSerializationContext context) =>
		new ValueTask<object?>(data);
}
