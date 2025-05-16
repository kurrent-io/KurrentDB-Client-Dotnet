using System.Text.Json;
using JetBrains.Annotations;
using KurrentDB.Client.SchemaRegistry.Serialization;

namespace KurrentDB.Client.Model;

[PublicAPI]
public readonly record struct Message() {
	public static readonly Message Empty = new();

	/// <summary>
	/// The assigned record id.
	/// </summary>
	public Guid RecordId { get; init; } = Guid.NewGuid();

	/// <summary>
	/// The message payload.
	/// </summary>
	public object Value { get; init; } = null!;

	/// <summary>
	/// The serialized data associated with the message, represented as a read-only byte memory.
	/// </summary>
	public ReadOnlyMemory<byte> Data { get; init; } = ReadOnlyMemory<byte>.Empty;

	/// <summary>
	/// Specifies the format of the schema associated with the message.
	/// </summary>
	public SchemaDataFormat DataFormat { get; init; } = SchemaDataFormat.Json;

	/// <summary>
	/// The message metadata.
	/// </summary>
	public Metadata Metadata { get; init; } = new();
}

// [PublicAPI]
// public readonly record struct AppendRecord() {
// 	/// <summary>
// 	/// The assigned record id.
// 	/// </summary>
// 	public Guid RecordId { get; init; } = Guid.NewGuid();
//
// 	/// <summary>
// 	/// The serialized data associated with the message, represented as a read-only byte memory.
// 	/// </summary>
// 	public ReadOnlyMemory<byte> Data { get; init; } = ReadOnlyMemory<byte>.Empty;
//
// 	/// <summary>
// 	/// The message metadata.
// 	/// </summary>
// 	public Metadata Metadata { get; init; } = new();
// }
//
// [PublicAPI]
// public class MessageBuilder(KurrentDBClient client) {
// 	SchemaDataFormat _dataFormat = SchemaDataFormat.Json;
// 	Metadata         _metadata   = new();
// 	Guid             _recordId   = Guid.NewGuid();
// 	string           _stream     = string.Empty;
// 	object?          _value;
//
// 	ReadOnlyMemory<byte> _data = ReadOnlyMemory<byte>.Empty;
//
// 	public MessageBuilder WithRecordId(Guid recordId) {
// 		_recordId = recordId;
// 		return this;
// 	}
//
// 	public MessageBuilder WithValue(object value) {
// 		_value = value;
// 		return this;
// 	}
//
// 	public MessageBuilder WithDataFormat(SchemaDataFormat dataFormat) {
// 		_dataFormat = dataFormat;
// 		return this;
// 	}
//
// 	public MessageBuilder WithMetadata(Metadata metadata) {
// 		_metadata = metadata;
// 		return this;
// 	}
//
// 	public MessageBuilder WithStream(string stream) {
// 		_stream = stream;
// 		return this;
// 	}
//
// 	public Message Create() {
// 		if (_value is null)
// 			throw new InvalidOperationException("Message value cannot be null");
//
// 		return new Message {
// 			RecordId   = _recordId,
// 			Value      = _value,
// 			DataFormat = _dataFormat,
// 			Metadata   = _metadata
// 		};
// 	}
//
// 	public async ValueTask<Message> Create(CancellationToken ct) {
// 		if (_value is null)
// 			throw new InvalidOperationException("Message value cannot be null");
//
// 		var serializer = client.SerializerProvider.GetSerializer(_dataFormat);
//
// 		// metadata is enriched with schema name, data format
// 		// and schema version id if autoregistration is enabled.
// 		var data = await serializer
// 			.Serialize(_value, new SchemaSerializationContext(_stream, _metadata, _dataFormat), ct)
// 			.ConfigureAwait(false);
//
// 		return new Message {
// 			RecordId   = _recordId,
// 			Value      = _value,
// 			Data       = data,
// 			DataFormat = _dataFormat,
// 			Metadata   = _metadata
// 		};
// 	}
//
// 	public async ValueTask<EventData> BuildAsEventData(CancellationToken ct = default) {
// 		if (_value is null)
// 			throw new InvalidOperationException("Message value cannot be null");
//
// 		var serializer = client.SerializerProvider.GetSerializer(_dataFormat);
//
// 		var message = new Message {
// 			RecordId   = _recordId,
// 			Value      = _value ?? throw new InvalidOperationException("Message value cannot be null"),
// 			DataFormat = _dataFormat,
// 			Metadata   = _metadata
// 		};
//
// 		var data = await serializer
// 			.Serialize(_value, new SchemaSerializationContext(_stream, _metadata, _dataFormat), ct)
// 			.ConfigureAwait(false);
//
// 		var id          = Uuid.FromGuid(message.RecordId); // BROKEN
// 		var schemaName  = message.Metadata.Get<string>(SystemMetadataKeys.SchemaName)!;
// 		var contentType = message.DataFormat.GetContentType();
//
// 		return new EventData(
// 			eventId: id,
// 			type: schemaName,
// 			data: data,
// 			metadata: JsonSerializer.SerializeToUtf8Bytes(message.Metadata),
// 			contentType: contentType
// 		);
// 	}
// }
//
// class MyClass {
// 	public async ValueTask DoIt(CancellationToken ct) {
// 		var blah = new MessageBuilder(new KurrentDBClient());
//
// 		var message = await blah
// 			.WithValue(new { Name = "Test" })
// 			.WithDataFormat(SchemaDataFormat.Json)
// 			.WithMetadata(new Metadata())
// 			.WithRecordId(Guid.NewGuid())
// 			.WithStream("test-stream")
// 			.Create(ct);
//
//
// 	}
//
// 	public async ValueTask AppendStream(string stream, StreamState expectedState, IEnumerable<MessageBuilder> messages, CancellationToken cancellationToken) {
// 		var omg = messages.Select(x => x.Create(cancellationToken));
// 	}
//
// 	public async ValueTask AppendStream(string stream, StreamState expectedState, IAsyncEnumerable<MessageBuilder> messages, CancellationToken cancellationToken) {
// 		var eventData = await messages
// 			.SelectAwaitWithCancellation((builder, index, ct) => builder.BuildAsEventData(ct))
// 			.ToListAsync(cancellationToken);
//
//
//
//
// 	}
// }
