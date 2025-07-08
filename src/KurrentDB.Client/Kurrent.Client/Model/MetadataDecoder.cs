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
            return new Metadata();

        try {
            if (context.SchemaName == LinkSchemaName)
                return DeserializeLinkMetadata(bytes);

            var metadata = DecodeCore(bytes, context)
                .AddSchemaNameIfMissing(context.SchemaName)
                .AddSchemaDataFormatIfMissing(context.SchemaDataFormat);

            return metadata;
        }
        catch (Exception ex) when (ex is not MetadataDecodingException) {
            throw new MetadataDecodingException("Failed to decode metadata", ex);
        }
    }

    static Metadata DeserializeLinkMetadata(ReadOnlyMemory<byte> bytes) {
        var deserialized = JsonSerializer.Deserialize<Dictionary<string, JsonValue>>(bytes.Span)!;
        return new Metadata()
            .With(SystemMetadataKeys.SchemaName, LinkSchemaName)
            .With(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Json) // change to json
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

/// <summary>
/// The default metadata decoder that deserializes metadata from JSON-encoded byte arrays.
/// This allows for backward compatibility with previous metadata formats.
/// </summary>
[PublicAPI]
public sealed class JsonMetadataDecoder : MetadataDecoder {
    static readonly SchemaRegistry.Serialization.Json.JsonSerializer Serializer = new();

    protected override Metadata DecodeCore(ReadOnlyMemory<byte> bytes, MetadataDecoderContext context) {
        return Serializer.Deserialize<Dictionary<string, string?>>(bytes) is { Count: > 0 } deserialized
            ? new(deserialized.ToDictionary(x => x.Key, static kvp => EvolveValue(kvp)))
            : new();

        static object? EvolveValue(KeyValuePair<string, string?> kvp) => kvp switch {
            { Key: SystemMetadataKeys.SchemaDataFormat, Value: not null } => Enum.Parse<SchemaDataFormat>(kvp.Value, ignoreCase: true),
            { Key: SystemMetadataKeys.SchemaName,       Value: not null } => SchemaName.From(kvp.Value),
            _                                                             => kvp.Value
        };
    }
}

public class MetadataDecodingException(string message, Exception? innerException = null)
    : KurrentClientException(errorCode: "MetadataDecodingError", message, null, innerException);
