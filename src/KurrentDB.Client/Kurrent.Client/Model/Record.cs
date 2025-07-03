using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Kurrent.Client.SchemaRegistry;
using Kurrent.Client.SchemaRegistry.Serialization;

namespace Kurrent.Client.Model;

[PublicAPI]
[DebuggerDisplay("{ToDebugString()}")]
public record Record(IRecordDecoder? Decoder  = null) {
	public static readonly Record None = new();

	IRecordDecoder? Decoder { get; } = Decoder;

	/// <summary>
    /// The unique identifier of the record.
    /// </summary>
	public Guid Id { get; init; } = Guid.Empty;

	/// <summary>
	/// The position of the record in the stream.
	/// </summary>
	public LogPosition Position { get; init; } = LogPosition.Unset;

	/// <summary>
	/// Represents the stream associated with the record.
	/// </summary>
	public StreamName Stream { get; init; } = StreamName.None;

	/// <summary>
	/// Represents the revision of a stream at a specific point in time.
	/// </summary>
	public StreamRevision StreamRevision { get; init; } = StreamRevision.Unset;

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
	[MaybeNull]
	public object Value { get; internal set; } = null!;

	/// <summary>
	/// The type of the deserialized message.
	/// </summary>
	public Type ValueType => Value is not null ? Value.GetType() : SystemTypes.MissingType;

	/// <summary>
	/// Binary data representation of the record's value.
	/// Used to store the raw serialized content of the record.
	/// </summary>
	public ReadOnlyMemory<byte> Data { get; init; } = ReadOnlyMemory<byte>.Empty;

	/// <summary>
	/// The schema information associated with the record.
	/// </summary>
	public RecordSchemaInfo Schema => Metadata.GetSchemaInfo();

	/// <summary>
	/// Indicates whether the current <see cref="Record"/> instance has been successfully decoded.
	/// </summary>
	public bool IsDecoded => !Data.IsEmpty
	                       && Value is not null
	                       && ValueType != typeof(byte[])
	                       && ValueType != typeof(ReadOnlyMemory<byte>)
	                       && ValueType != typeof(Memory<byte>);

	/// <summary>
	/// Attempts to decode the record's data asynchronously.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>True if decoding was performed, false if already decoded or no decoder available</returns>
	public ValueTask<bool> TryDecode(CancellationToken cancellationToken = default) {
		if (IsDecoded || Decoder is null)
			return ValueTask.FromResult(false);

		return Decoder.Decode(this, cancellationToken);
	}

	public override string ToString() => $"{Schema.SchemaName} {StreamRevision}@{Stream} - {Position}";

	string ToDebugString() =>
		$"Record: Id={Id}, Position={Position}, Stream={Stream}, StreamRevision={StreamRevision}, " +
		$"Value={Value ?? "Not Decoded"}, ValueType={ValueType}";
}

// public delegate ValueTask<Result<object, Exception>> DecodeRecordData(Record record, CancellationToken cancellationToken = default);

public interface IRecordDecoder {
	ValueTask<bool> Decode(Record record, CancellationToken ct);
}

sealed class RecordDecoder(ISchemaSerializerProvider serializerProvider, SchemaRegistryPolicy? registryPolicy = null) : IRecordDecoder {
	SchemaRegistryPolicy RegistryPolicy { get; } = registryPolicy ?? SchemaRegistryPolicy.NoRequirements;

	public async ValueTask<bool> Decode(Record record, CancellationToken ct) {
		if (record.IsDecoded)
			return true;

		var context = new SchemaSerializationContext {
			Stream               = record.Stream,
			Metadata             = record.Metadata,
			SchemaRegistryPolicy = RegistryPolicy,
			CancellationToken    = ct
		};

		record.Value = await serializerProvider
			.GetSerializer(record.Schema.DataFormat)
			.Deserialize(record.Data, context)
			.ConfigureAwait(false)!;

		return true;
	}
}
