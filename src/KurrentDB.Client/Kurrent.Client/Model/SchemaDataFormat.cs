using System.Diagnostics;

namespace Kurrent.Client.Model;

/// <summary>
/// Specifies the data format for schema content.
/// </summary>
public enum SchemaDataFormat {
    /// <summary>
    /// The data format is not specified.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// The data is in JSON format.
    /// </summary>
    Json = 1,

    /// <summary>
    /// The data is in Protocol Buffers format.
    /// </summary>
    Protobuf = 2,

    /// <summary>
    /// The data is in Avro format.
    /// </summary>
    Avro = 3,

    /// <summary>
    /// The data is in raw bytes format.
    /// </summary>
    Bytes = 4
}

/// <summary>
/// Provides extension methods for the <see cref="SchemaDataFormat"/> enum.
/// </summary>
static class SchemaDataFormatExtensions {
	const string JsonContentType     = "application/json";
	const string ProtobufContentType = "application/vnd.google.protobuf";
	const string AvroContentType     = "application/vnd.apache.avro+json";
	const string BytesContentType    = "application/octet-stream";

    /// <summary>
    /// Gets the corresponding content type string for a <see cref="SchemaDataFormat"/>.
    /// </summary>
    /// <param name="dataFormat">The schema data format.</param>
    /// <returns>The content type string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the <paramref name="dataFormat"/> is not a defined value in the <see cref="SchemaDataFormat"/> enum.</exception>
	public static string GetContentType(this SchemaDataFormat dataFormat) =>
		dataFormat switch {
			SchemaDataFormat.Json        => JsonContentType,
			SchemaDataFormat.Protobuf    => ProtobufContentType,
			SchemaDataFormat.Avro        => AvroContentType,
			SchemaDataFormat.Bytes       => BytesContentType,
			SchemaDataFormat.Unspecified => BytesContentType,
			_                            => throw new ArgumentOutOfRangeException(nameof(dataFormat), dataFormat, null)
		};

    /// <summary>
    /// Gets the <see cref="SchemaDataFormat"/> enum value corresponding to a content type string.
    /// </summary>
    /// <param name="contentType">The content type string. The comparison is case-insensitive.</param>
    /// <returns>The corresponding <see cref="SchemaDataFormat"/> and <see cref="SchemaDataFormat.Unspecified"/> if the content type is not recognized.</returns>
	public static SchemaDataFormat GetSchemaDataFormat(this string contentType) {
		Debug.Assert(!string.IsNullOrWhiteSpace(contentType), "Content type should not be empty");

		return contentType.ToLower() switch {
            JsonContentType     => SchemaDataFormat.Json,
            ProtobufContentType => SchemaDataFormat.Protobuf,
            AvroContentType     => SchemaDataFormat.Avro,
            BytesContentType    => SchemaDataFormat.Bytes,
            _                   => SchemaDataFormat.Unspecified
        };
    }
}
