using JetBrains.Annotations;

namespace KurrentDB.Client.Model;

[PublicAPI]
public readonly record struct Record() {
	public static readonly Record None = new();

	public Guid Id { get; init; } = Guid.Empty;

	/// <summary>
	/// The position of the record in the stream.
	/// </summary>
	public Position Position { get; init; } = Position.End;

	/// <summary>
	/// Represents the stream identifier associated with the record.
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
	/// Represents metadata about the schema associated with a record, including schema name and data format.
	/// </summary>
	public SchemaInfo SchemaInfo { get; init; } = SchemaInfo.None;

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

	public bool HasValue => Value is not null;

	public bool IsDecoded => Value is not null
	                      && !Data.IsEmpty
	                      && ValueType != typeof(byte[])
	                      && ValueType != typeof(ReadOnlyMemory<byte>)
	                      && ValueType != typeof(Memory<byte>);


	public override string ToString() => $"{Id} {Position} {SchemaInfo.SchemaName}";
}
