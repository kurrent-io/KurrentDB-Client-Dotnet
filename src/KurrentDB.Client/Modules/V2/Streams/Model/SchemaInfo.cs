#pragma warning disable CS0618 // Type or member is obsolete

using System.Net.Http.Headers;
using JetBrains.Annotations;

namespace KurrentDB.Client.Model;

[PublicAPI]
public record SchemaInfo(string SchemaName, SchemaDataFormat DataFormat) {
	internal static readonly MediaTypeHeaderValue JsonContentTypeHeader     = new("application/json");
    internal static readonly MediaTypeHeaderValue ProtobufContentTypeHeader = new("application/vnd.google.protobuf");
    internal static readonly MediaTypeHeaderValue AvroContentTypeHeader     = new("application/vnd.apache.avro+json");
    internal static readonly MediaTypeHeaderValue BytesContentTypeHeader    = new("application/octet-stream");

	public static readonly SchemaInfo None = new("", SchemaDataFormat.Unspecified);

	public MediaTypeHeaderValue ContentTypeHeader { get; } = DataFormat switch {
		SchemaDataFormat.Json     => JsonContentTypeHeader,
		SchemaDataFormat.Protobuf => ProtobufContentTypeHeader,
		_                         => BytesContentTypeHeader,
	};

	public string ContentType => ContentTypeHeader.MediaType!;

    public bool SchemaNameMissing => string.IsNullOrWhiteSpace(SchemaName);

	public SchemaInfo InjectIntoMetadata(Metadata headers) {
		headers.Set(SystemMetadataKeys.SchemaName, SchemaName);
		headers.Set(SystemMetadataKeys.SchemaDataFormat, DataFormat.ToString().ToLower());
		return this;
	}

    public SchemaInfo InjectSchemaNameIntoMetadata(Metadata headers) {
        headers.Set(SystemMetadataKeys.SchemaName, SchemaName);
        return this;
    }

	public static SchemaInfo FromMetadata(Metadata headers) {
		return new(ExtractSchemaName(headers), ExtractSchemaDataFormat(headers));

		static string ExtractSchemaName(Metadata headers) =>
            headers.Get<string>(SystemMetadataKeys.SchemaName) ?? "";

        static SchemaDataFormat ExtractSchemaDataFormat(Metadata headers) =>
            headers.GetEnum(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Unspecified);
    }

	/// <summary>
	/// For legacy purposes, we need to be able to create the schema info from the content type.
	/// </summary>
	public static SchemaInfo FromContentType(string schemaName, string contentType) {
		if (string.IsNullOrEmpty(schemaName))
			throw new ArgumentNullException(nameof(schemaName));

		if (string.IsNullOrEmpty(contentType))
			throw new ArgumentNullException(nameof(contentType));

		var schemaDataFormat = contentType == JsonContentTypeHeader.MediaType
			? SchemaDataFormat.Json
			: contentType == ProtobufContentTypeHeader.MediaType
				? SchemaDataFormat.Protobuf
				: contentType == BytesContentTypeHeader.MediaType
					? SchemaDataFormat.Bytes
					: SchemaDataFormat.Unspecified;

		return new(schemaName, schemaDataFormat);
	}
}
