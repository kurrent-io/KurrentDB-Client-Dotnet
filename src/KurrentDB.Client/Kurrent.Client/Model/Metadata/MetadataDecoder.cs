// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using System.Text.Json;
using System.Text.Json.Nodes;
using Kurrent.Client.SchemaRegistry;

namespace Kurrent.Client.Model;

/// <summary>
/// Provides a base implementation for decoding metadata from serialized byte representations.
/// Serves as an abstraction for specific metadata decoding implementations.
/// </summary>
public abstract class MetadataDecoder : IMetadataDecoder {
    static readonly SchemaName LinkSchemaName = "$>";

    /// <inheritdoc />
    public Metadata Decode(ReadOnlyMemory<byte> bytes, MetadataDecoderContext context) {
        if (bytes.IsEmpty)
            return new Metadata()
                .With(SystemMetadataKeys.SchemaName, context.SchemaName)
                .With(SystemMetadataKeys.SchemaDataFormat, context.SchemaDataFormat);

        try {
            if (context.SchemaName == LinkSchemaName)
                return DeserializeLinkMetadata(bytes);

            return DecodeCore(bytes, context)
                .AddSchemaNameIfMissing(context.SchemaName)
                .AddSchemaDataFormatIfMissing(context.SchemaDataFormat);
        }
        catch (Exception ex) when (ex is not MetadataDecodingException) {
            throw new MetadataDecodingException("Failed to decode metadata", ex);
        }
    }

    static Metadata DeserializeLinkMetadata(ReadOnlyMemory<byte> bytes) {
        var deserialized = JsonSerializer.Deserialize<Dictionary<string, JsonValue>>(bytes.Span)!;
        return new Metadata()
            .With(SystemMetadataKeys.SchemaName, LinkSchemaName)
            .With(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Json)
            .With("$v", deserialized["$v"].ToString())
            .With("$c", deserialized["$c"].GetValue<long>())
            .With("$p", deserialized["$p"].GetValue<long>())
            .With("$o", StreamName.From(deserialized["$o"].ToString()))
            .With("$causedBy", deserialized["$causedBy"].GetValue<Guid>());
    }

    protected abstract Metadata DecodeCore(ReadOnlyMemory<byte> bytes, MetadataDecoderContext context);
}

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
    /// <param name="context">
    /// The context for decoding metadata, providing additional information such as the originating stream's name,
    /// </param>
    /// <returns>The decoded <see cref="Metadata"/> object.</returns>
    Metadata Decode(ReadOnlyMemory<byte> bytes,  MetadataDecoderContext context);
}

/// <summary>
/// Encapsulates metadata decoding context, providing necessary information to decode metadata.
/// It includes the originating stream's name, the schema associated with the data,
/// and the format of the schema content.
/// </summary>
[PublicAPI]
public readonly record struct MetadataDecoderContext(
    StreamName Stream,
    SchemaName SchemaName,
    SchemaDataFormat SchemaDataFormat
);

public class MetadataDecodingException(string message, Exception? innerException = null)
    : KurrentClientException(errorCode: "MetadataDecodingError", message, null, innerException);
