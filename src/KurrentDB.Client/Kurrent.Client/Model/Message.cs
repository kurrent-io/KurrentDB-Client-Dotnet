namespace Kurrent.Client.Model;

[PublicAPI]
public record Message {
	public static readonly Message None = new() {
		RecordId   = Guid.Empty,
		Value      = null!,
		DataFormat = SchemaDataFormat.Unspecified,
		Metadata   = null!
	};

	public static MessageBuilder New => new();

	/// <summary>
	/// The assigned record id.
	/// </summary>
	public Guid RecordId { get; init; } = Guids.CreateVersion7();

	/// <summary>
	/// The message payload.
	/// </summary>
	public required object Value { get; init; } = null!;

	/// <summary>
	/// Specifies the format of the schema associated with the message.
	/// </summary>
	public SchemaDataFormat DataFormat { get; init; } = SchemaDataFormat.Json;

	/// <summary>
	/// The message metadata.
	/// </summary>
	public Metadata Metadata { get; init; } = new();
}

[PublicAPI]
public class MessageBuilder {
	Message _message = new() {
		RecordId   = Guids.CreateVersion7(),
		Value      = null!,
		DataFormat = SchemaDataFormat.Json,
		Metadata   = new Metadata().With(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Json)
	};

	public MessageBuilder WithRecordId(Guid recordId) {
		_message = _message with { RecordId = recordId };
		return this;
	}

	public MessageBuilder WithValue(object value) {
        if (value is null)
            throw new ArgumentNullException(nameof(value), "Message value cannot be null");
		_message = _message with { Value = value };
		return this;
	}

	public MessageBuilder WithDataFormat(SchemaDataFormat dataFormat) {
		_message = _message with { DataFormat = dataFormat };
		return this;
	}

	public MessageBuilder WithMetadata(Metadata metadata) {
		_message = _message with { Metadata = metadata };
		return this;
	}

	public MessageBuilder WithMetadata<T>(string key, T value) {
		_message = _message with { Metadata = new Metadata(_message.Metadata).With(key, value) };
		return this;
	}

	public MessageBuilder WithMetadata(IDictionary<string, object?> entries) {
		_message = _message with { Metadata = new Metadata(_message.Metadata).WithMany(entries) };
		return this;
	}

    public MessageBuilder WithMetadata( Func<Metadata, Metadata> transformMetadata) {
        _message = _message with { Metadata = transformMetadata(new Metadata(_message.Metadata)) };
        return this;
    }

	public Message Build() {
		if (_message.Value is null)
			throw new InvalidOperationException("Message value cannot be null");

		// Create a copy of metadata to avoid modifying the original
		var metadata = new Metadata(_message.Metadata)
			.With(SystemMetadataKeys.SchemaDataFormat, _message.DataFormat);

		return _message with { Metadata = metadata };
	}
}
