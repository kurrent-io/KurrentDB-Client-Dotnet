using Kurrent.Client.SchemaRegistry.Serialization.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Kurrent.Client.Model;

/// <summary>
/// Defines a contract for decoding metadata from serialized byte representations.
/// Implementors of this interface should provide functionality to convert serialized binary data
/// into <see cref="Metadata"/> objects.
/// </summary>
public interface IMetadataDecoder {
	/// <summary>
	/// Decodes metadata from a byte array.
	/// </summary>
	/// <param name="bytes">The serialized metadata bytes.</param>
	/// <returns>The decoded <see cref="Metadata"/> object.</returns>
    Metadata Decode(ReadOnlyMemory<byte> bytes);
}

/// <summary>
/// Provides a base implementation for decoding metadata from serialized byte representations.
/// Serves as an abstraction for specific metadata decoding implementations.
/// </summary>
public abstract class MetadataDecoderBase : IMetadataDecoder {
	/// <inheritdoc />
	public Metadata Decode(ReadOnlyMemory<byte> bytes) {
		if (bytes.IsEmpty)
			throw new MetadataDecodingException("Cannot decode empty metadata bytes");

		try {
			return DecodeCore(bytes);
		} catch (Exception ex) when (ex is not MetadataDecodingException) {
			throw new MetadataDecodingException("Failed to decode metadata", ex);
		}
	}

	 protected abstract Metadata DecodeCore(ReadOnlyMemory<byte> bytes);
}

[PublicAPI]
public sealed class MetadataDecoder : MetadataDecoderBase {
    protected override Metadata DecodeCore(ReadOnlyMemory<byte> bytes) =>
	    JsonSerializer.Deserialize<Metadata>(bytes.Span, JsonSchemaSerializerOptions.DefaultJsonSerializerOptions)
	 ?? throw new MetadataDecodingException("Decoded metadata cannot be null");
}

public class MetadataDecodingException(string message, Exception? innerException = null) : Exception(message, innerException);
