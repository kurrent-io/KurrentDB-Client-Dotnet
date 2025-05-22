using KurrentDB.Client.SchemaRegistry;

namespace KurrentDB.Client.Model;

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

[PublicAPI]
public readonly record struct Record() {
	public static readonly Record None = new();

	public Guid Id { get; init; } = Guid.Empty;

	/// <summary>
	/// The position of the record in the stream.
	/// </summary>
	public ulong Position { get; init; } = ulong.MaxValue;

	/// <summary>
	/// Represents the stream associated with the record.
	/// </summary>
	public string Stream { get; init; } = "";

	/// <summary>
	/// Represents the revision of a stream at a specific point in time.
	/// </summary>
	public long StreamRevision { get; init; }

	/// <summary>
	/// When the record was created in the database.
	/// </summary>
	public DateTime Timestamp { get; init; } = default;

	/// <summary>
	/// The metadata associated with the record, represented as a collection of key-value pairs.
	/// </summary>
	public Metadata Metadata { get; init; } = new();

	/// <summary>
	/// The deserialized message.
	/// </summary>
	public object Value { get; init; } = null!;

	/// <summary>
	/// The type of the deserialized message.
	/// </summary>
	public Type ValueType { get; init; } = null!;

	/// <summary>
	/// Binary data representation of the record's value.
	/// Used to store the raw serialized content of the record.
	/// </summary>
	public ReadOnlyMemory<byte> Data { get; init; } = ReadOnlyMemory<byte>.Empty;

	/// <summary>
	/// The schema information associated with the record.
	/// </summary>
	public RecordSchemaInfo Schema => new RecordSchemaInfo(
		Metadata.TryGet<string>(SystemMetadataKeys.SchemaName, out var schemaName) ? SchemaName.From(schemaName!) : SchemaName.None,
		Metadata.Get<SchemaDataFormat>(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Unspecified),
		Metadata.Get<Guid>(SystemMetadataKeys.SchemaVersionId, Guid.Empty)
	);

	public bool IsDecoded => Value is not null
	                      && !Data.IsEmpty
	                      && ValueType != typeof(byte[])
	                      && ValueType != typeof(ReadOnlyMemory<byte>)
	                      && ValueType != typeof(Memory<byte>);


	public override string ToString() => $"{Id} {Position} {Schema.DataFormat}";
}
