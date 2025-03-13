using System.Net.Http.Headers;

namespace KurrentDB.Client {
	/// <summary>
	/// Represents a message to be written.
	/// </summary>
	public sealed class MessageData {
		/// <summary>
		/// The raw bytes of the message data.
		/// </summary>
		public readonly ReadOnlyMemory<byte> Data;

		/// <summary>
		/// The <see cref="Uuid"/> of the message, used as part of the idempotent write check.
		/// </summary>
		public readonly Uuid MessageId;

		/// <summary>
		/// The raw bytes of the message metadata.
		/// </summary>
		public readonly ReadOnlyMemory<byte> Metadata;

		/// <summary>
		/// The name of the message type. It is strongly recommended that these
		/// use lowerCamelCase if projections are to be used.
		/// </summary>
		public readonly string Type;

		/// <summary>
		/// The Content-Type of the <see cref="Data"/>. Valid values are 'application/json' and 'application/octet-stream'.
		/// </summary>
		public readonly string ContentType;

		/// <summary>
		/// Constructs a new <see cref="MessageData"/>.
		/// </summary>
		/// <param name="type">The name of the message type. It is strongly recommended that these use lowerCamelCase if projections are to be used.</param>
		/// <param name="data">The raw bytes of the message data.</param>
		/// <param name="metadata">The raw bytes of the message metadata.</param>
		/// <param name="messageId">The <see cref="Uuid"/> of the message, used as part of the idempotent write check.</param>
		/// <param name="contentType">The Content-Type of the <see cref="Data"/>. Valid values are 'application/json' and 'application/octet-stream'.</param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <example>
		/// <code>
		/// // Create an message with data and metadata
		/// var orderPlaced = new OrderPlaced { OrderId = "ORD-123", Amount = 99.99m };
		/// 
		/// var metadata = new MessageMetadata { 
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
		/// var message = new MessageData(type, dataBytes, metadataBytes);
		/// 
		/// // Or specify a custom ID
		/// var messageWithId =  new MessageData(type, dataBytes, metadataBytes, Uuid.NewUuid());
		/// </code>
		/// </example>
		public MessageData(
			string type,
			ReadOnlyMemory<byte> data,
			ReadOnlyMemory<byte>? metadata = null,
			Uuid? messageId = null,
			string contentType = Constants.Metadata.ContentTypes.ApplicationJson
		) {
			messageId ??= Uuid.NewUuid();

			if (messageId == Uuid.Empty) {
				throw new ArgumentOutOfRangeException(nameof(messageId));
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

			MessageId   = messageId.Value;
			Type        = type;
			Data        = data;
			Metadata    = metadata ?? Array.Empty<byte>();
			ContentType = contentType;
		}

		/// <summary>
		/// Creates a new <see cref="MessageData"/> with the specified message type, id, binary data and metadata
		/// </summary>
		/// <param name="type">The name of the message type. It is strongly recommended that these use lowerCamelCase if projections are to be used.</param>
		/// <param name="data">The raw bytes of the message data.</param>
		/// <param name="metadata">Optional metadata providing additional context about the message, such as correlation IDs, timestamps, or user information.</param>
		/// <param name="messageId">Unique identifier for this specific message instance. </param>
		/// <param name="contentType">The Content-Type of the <see cref="Data"/>. Valid values are 'application/json' and 'application/octet-stream'.</param>
		/// <returns>A new immutable <see cref="MessageData"/> instance with the specified properties.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when messageId is explicitly set to Uuid.Empty, which is an invalid identifier.</exception>
		/// <example>
		/// <code>
		/// // Create an message with data and metadata
		/// var orderPlaced = new OrderPlaced { OrderId = "ORD-123", Amount = 99.99m };
		/// 
		/// var metadata = new MessageMetadata { 
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
		/// var message = MessageData.From(type, dataBytes, metadataBytes);
		/// 
		/// // Or specify a custom ID
		/// var messageWithId = MessageData.From(type, dataBytes, metadataBytes, Uuid.NewUuid());
		/// </code>
		/// </example>
		public static MessageData From(
			string type,
			ReadOnlyMemory<byte> data,
			ReadOnlyMemory<byte>? metadata = null,
			Uuid? messageId = null,
			string contentType = Constants.Metadata.ContentTypes.ApplicationJson
		) =>
			new(type, data, metadata, messageId, contentType);
	}
}
