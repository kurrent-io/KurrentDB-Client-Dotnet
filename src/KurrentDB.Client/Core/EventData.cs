using System.Net.Http.Headers;

namespace KurrentDB.Client;

/// <summary>
/// Represents an event to be written.
/// </summary>
[Obsolete("Use MessageData instead.", false)]
public sealed class EventData {
	/// <summary>
	/// The raw bytes of the event data.
	/// </summary>
	public readonly ReadOnlyMemory<byte> Data;

	/// <summary>
	/// The <see cref="Uuid"/> of the event, used as part of the idempotent write check.
	/// </summary>
	public readonly Uuid EventId;

	/// <summary>
	/// The raw bytes of the event metadata.
	/// </summary>
	public readonly ReadOnlyMemory<byte> Metadata;

	/// <summary>
	/// The name of the event type. It is strongly recommended that these
	/// use lowerCamelCase if projections are to be used.
	/// </summary>
	public readonly string Type;

	/// <summary>
	/// The Content-Type of the <see cref="Data"/>. Valid values are 'application/json' and 'application/octet-stream'.
	/// </summary>
	public readonly string ContentType;

	/// <summary>
	/// Constructs a new <see cref="EventData"/>.
	/// </summary>
	/// <param name="eventId">The <see cref="Uuid"/> of the event, used as part of the idempotent write check.</param>
	/// <param name="type">The name of the event type. It is strongly recommended that these use lowerCamelCase if projections are to be used.</param>
	/// <param name="data">The raw bytes of the event data.</param>
	/// <param name="metadata">The raw bytes of the event metadata.</param>
	/// <param name="contentType">The Content-Type of the <see cref="Data"/>. Valid values are 'application/json' and 'application/octet-stream'.</param>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public EventData(
		Uuid eventId, string type, ReadOnlyMemory<byte> data, ReadOnlyMemory<byte>? metadata = null,
		string contentType = Constants.Metadata.ContentTypes.ApplicationJson
	) {
		if (eventId == Uuid.Empty) {
			throw new ArgumentOutOfRangeException(nameof(eventId));
		}

		MediaTypeHeaderValue.Parse(contentType);

		if (contentType != Constants.Metadata.ContentTypes.ApplicationJson &&
		    contentType != Constants.Metadata.ContentTypes.ApplicationOctetStream) {
			throw new ArgumentOutOfRangeException(
				nameof(contentType),
				contentType,
				$"Only {Constants.Metadata.ContentTypes.ApplicationJson} or {Constants.Metadata.ContentTypes.ApplicationOctetStream} are acceptable values."
			);
		}

		EventId     = eventId;
		Type        = type;
		Data        = data;
		Metadata    = metadata ?? Array.Empty<byte>();
		ContentType = contentType;
	}

	/// <summary>
	/// Creates a new <see cref="EventData"/> with the specified event type, id, binary data and metadata
	/// </summary>
	/// <param name="type">The name of the event type. It is strongly recommended that these use lowerCamelCase if projections are to be used.</param>
	/// <param name="data">The raw bytes of the event data.</param>
	/// <param name="metadata">Optional metadata providing additional context about the event, such as correlation IDs, timestamps, or user information.</param>
	/// <param name="eventId">Unique identifier for this specific event instance. </param>
	/// <param name="contentType">The Content-Type of the <see cref="Data"/>. Valid values are 'application/json' and 'application/octet-stream'.</param>
	/// <returns>A new immutable EventData instance with the specified properties.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when eventId is explicitly set to Uuid.Empty, which is an invalid identifier.</exception>
	/// <example>
	/// <code>
	/// // Create an event with data and metadata
	/// var orderPlaced = new OrderPlaced { OrderId = "ORD-123", Amount = 99.99m };
	/// 
	/// var metadata = new EventMetadata { 
	///     UserId = "user-456", 
	///     Timestamp = DateTimeOffset.UtcNow,
	///     CorrelationId = correlationId
	/// };
	/// 
	/// var type = typeof(OrderPlaced).FullName!;
	/// var dataBytes = JsonSerializer.SerializeToUtf8Bytes(orderPlaced);
	/// var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata);
	/// 
	/// // Let the system assign an ID automatically
	/// var event = EventData.From(type, dataBytes, metadataBytes);
	/// 
	/// // Or specify a custom ID
	/// var messageWithId = EventData.From(type, dataBytes, metadataBytes, Uuid.NewUuid());
	/// </code>
	/// </example>
	public static EventData From(
		string type,
		ReadOnlyMemory<byte> data,
		ReadOnlyMemory<byte>? metadata = null,
		Uuid? eventId = null,
		string contentType = Constants.Metadata.ContentTypes.ApplicationJson
	) =>
		new(eventId ?? Uuid.NewUuid(), type, data, metadata, contentType);

	/// <summary>
	/// Implicitly converts an <see cref="EventData"/> instance to a <see cref="MessageData"/> instance.
	/// The EventId becomes the MessageId, and all other properties are directly mapped.
	/// </summary>
	/// <param name="eventData">The event data to convert to message data.</param>
	/// <returns>A new <see cref="MessageData"/> instance with properties copied from the event data.</returns>
	public static implicit operator MessageData(EventData eventData) =>
		new MessageData(
			eventData.Type,
			eventData.Data,
			eventData.Metadata,
			eventData.EventId,
			eventData.ContentType
		);
}
