using Kurrent.Client.Registry;

namespace Kurrent.Client.Streams;

/// <summary>
/// Represents information about the schema of a record, including its name, format, and version identifier.
/// </summary>
/// <param name="SchemaName">
/// The name of the schema for the record.
/// </param>
/// <param name="DataFormat">
/// The data format of the schema, such as JSON, Protobuf, Avro, Bytes, or Unspecified.
/// </param>
/// <param name="SchemaVersionId">
/// The unique identifier for the version of the schema.
/// </param>
public record RecordSchemaInfo(SchemaName SchemaName, SchemaDataFormat DataFormat, SchemaVersionId SchemaVersionId) {
	public static readonly RecordSchemaInfo None = new(SchemaName.None, SchemaDataFormat.Unspecified, SchemaVersionId.None);

	public bool HasSchemaName      => SchemaName != SchemaName.None;
	public bool HasDataFormat      => DataFormat != SchemaDataFormat.Unspecified;
	public bool HasSchemaVersionId => SchemaVersionId != SchemaVersionId.None;

	public override string ToString() => $"{SchemaName} {DataFormat} {SchemaVersionId}";
}
