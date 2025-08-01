using Kurrent.Client.Model;

namespace Kurrent.Client.Streams;

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
	public Guid RecordId { get; internal init; } = Guid.NewGuid();

	/// <summary>
	/// The message payload.
	/// </summary>
	public object Value { get; internal init; } = null!;

	/// <summary>
	/// Specifies the format of the schema associated with the message.
	/// </summary>
	public SchemaDataFormat DataFormat { get; internal init; } = SchemaDataFormat.Json;

	/// <summary>
	/// The message metadata.
	/// </summary>
	public Metadata Metadata { get; internal init; } = new();

	/// <summary>
	/// Creates a new <see cref="Message"/> instance with the specified value and data format.
	/// </summary>
	/// <param name="value">
	/// The content value to include in the message. This cannot be null and must align with the specified
	/// <paramref name="dataFormat"/> (e.g., when passing byte arrays, the format must be <see cref="SchemaDataFormat.Bytes"/>).
	/// </param>
	/// <param name="dataFormat">
	/// The format of the data represented by <paramref name="value"/>. Defaults to <see cref="SchemaDataFormat.Json"/>.
	/// Cannot be <see cref="SchemaDataFormat.Unspecified"/>.
	/// </param>
	public static Message Create(object value, SchemaDataFormat dataFormat = SchemaDataFormat.Json) =>
		New.WithValue(value).WithDataFormat(dataFormat).Build();
}

[PublicAPI]
public class MessageBuilder {
	Message _message = new() {
		RecordId   = Guid.NewGuid(),
		Value      = null!,
		DataFormat = SchemaDataFormat.Json,
		Metadata   = new Metadata().With(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Json)
	};

	public MessageBuilder WithRecordId(Guid recordId) {
		if (_message.RecordId == Guid.Empty)
			throw new ArgumentNullException(nameof(recordId), "RecordId cannot be empty");

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
		if (dataFormat == SchemaDataFormat.Unspecified)
			throw new ArgumentNullException(nameof(dataFormat), "Data format cannot be unspecified");

		if (_message.Value.IsBytes() && dataFormat != SchemaDataFormat.Bytes)
			throw new ArgumentException($"Data format must be {SchemaDataFormat.Bytes} because the value is a byte array");

		_message = _message with {
			DataFormat = dataFormat,
			Metadata   = _message.Metadata.With(SystemMetadataKeys.SchemaDataFormat, dataFormat)
		};

		return this;
	}

	public MessageBuilder WithMetadata(Metadata metadata) {
		var dataFormat = metadata.GetSchemaDataFormat();
		if (dataFormat == SchemaDataFormat.Unspecified)
			dataFormat = _message.DataFormat;

		_message = _message with {
			DataFormat = dataFormat,
			Metadata   = metadata.CreateUnlockedCopy()
				.With(SystemMetadataKeys.SchemaDataFormat, dataFormat)
		};

		return this;
	}

	public MessageBuilder WithMetadata<T>(string key, T value) =>
		WithMetadata(_message.Metadata.With(key, value));

	public MessageBuilder WithMetadata(IDictionary<string, object?> entries) =>
		WithMetadata(_message.Metadata.WithMany(entries));

	public Message Build() {
		if (_message.Value is null)
			throw new InvalidOperationException("Message value cannot be null");

		// Create a copy of metadata to avoid modifying the original
		var metadata = _message.Metadata.CreateUnlockedCopy()
			.With(SystemMetadataKeys.SchemaDataFormat, _message.DataFormat);

		return _message with { Metadata = metadata };
	}
}
