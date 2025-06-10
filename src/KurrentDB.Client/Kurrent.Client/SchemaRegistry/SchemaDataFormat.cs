using System.Diagnostics;

namespace Kurrent.Client.Model;

public enum SchemaDataFormat {
    Unspecified = 0,
    Json        = 1,
    Protobuf    = 2,
    Avro        = 3,
    Bytes       = 4
}

static class SchemaDataFormatExtensions {
	const string JsonContentType     = "application/json";
	const string ProtobufContentType = "application/vnd.google.protobuf";
	const string AvroContentType     = "application/vnd.apache.avro+json";
	const string BytesContentType    = "application/octet-stream";

	public static string GetContentType(this SchemaDataFormat dataFormat) =>
		dataFormat switch {
			SchemaDataFormat.Json        => JsonContentType,
			SchemaDataFormat.Protobuf    => ProtobufContentType,
			SchemaDataFormat.Avro        => AvroContentType,
			SchemaDataFormat.Bytes       => BytesContentType,
			SchemaDataFormat.Unspecified => BytesContentType,
			_                            => throw new ArgumentOutOfRangeException(nameof(dataFormat), dataFormat, null)
		};

	public static SchemaDataFormat GetSchemaDataFormat(this string contentType) {
		Debug.Assert(!string.IsNullOrWhiteSpace(contentType), "Content type should not be empty");

		return contentType.ToLower() switch {
			JsonContentType        => SchemaDataFormat.Json,
			ProtobufContentType    => SchemaDataFormat.Protobuf,
			AvroContentType        => SchemaDataFormat.Avro,
			BytesContentType       => SchemaDataFormat.Bytes,
			_                      => SchemaDataFormat.Unspecified
		};
	}
}
