using Kurrent.Client.SchemaRegistry;

namespace Kurrent.Client.Model;

[PublicAPI]
public readonly record struct Record() {
	public static readonly Record None = new();

	public Guid Id { get; init; } = Guid.Empty;

	/// <summary>
	/// The position of the record in the stream.
	/// </summary>
	public LogPosition Position { get; init; } = long.MaxValue;

	/// <summary>
	/// Represents the stream associated with the record.
	/// </summary>
	public string Stream { get; init; } = "";

	/// <summary>
	/// Represents the revision of a stream at a specific point in time.
	/// </summary>
	public StreamRevision StreamRevision { get; init; }

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
		Metadata.Get(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Unspecified),
		Metadata.Get(SystemMetadataKeys.SchemaVersionId, Guid.Empty)
	);

	public bool IsDecoded => Value is not null
	                      && !Data.IsEmpty
	                      && ValueType != typeof(byte[])
	                      && ValueType != typeof(ReadOnlyMemory<byte>)
	                      && ValueType != typeof(Memory<byte>);

	public override string ToString() => $"{ValueType.Name} {Position}@{Stream}";

	/// <summary>
	/// Creates a debug view.
	/// </summary>
	public string ToDebugString() =>
		$"Record: Id={Id}, Position={Position}, Stream={Stream}, Value={Value}, ValueType={ValueType.Name}";
}
