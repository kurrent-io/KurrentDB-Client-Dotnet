#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

using System.Diagnostics;
using KurrentDB.Client.Model;

namespace KurrentDB.Client.SchemaRegistry.Serialization.Bytes;

public class BytesSerializer : ISchemaSerializer {
	public SchemaDataFormat DataFormat => SchemaDataFormat.Bytes;

	public ValueTask<ReadOnlyMemory<byte>> Serialize(object? value, SchemaSerializationContext context, CancellationToken cancellationToken) {
		Debug.Assert(value is null or byte[] or ReadOnlyMemory<byte> or Memory<byte>, "value must be byte[] or ReadOnlyMemory<byte> or Memory<byte>");

		// enforce the schema data format
		context.Metadata
			.Set(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Bytes);

		var result = value switch {
			null                       => ReadOnlyMemory<byte>.Empty,
			byte[] bytes               => bytes,
			ReadOnlyMemory<byte> bytes => bytes,
			Memory<byte> bytes         => bytes
		};

		return new ValueTask<ReadOnlyMemory<byte>>(result);
	}

	public ValueTask<object?> Deserialize(ReadOnlyMemory<byte> data, SchemaSerializationContext context, CancellationToken cancellationToken) =>
		new ValueTask<object?>(data);
}
