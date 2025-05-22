using System.Runtime.CompilerServices;
using System.Text.Json;
using KurrentDB.Client.SchemaRegistry;
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

[PublicAPI]
public readonly record struct AppendRecord() {
	/// <summary>
	/// The assigned record id.
	/// </summary>
	public Guid RecordId { get; init; } = Guid.NewGuid();

	/// <summary>
	/// The serialized data associated with the message, represented as a read-only byte memory.
	/// </summary>
	public ReadOnlyMemory<byte> Data { get; init; } = ReadOnlyMemory<byte>.Empty;

	/// <summary>
	/// The message metadata.
	/// </summary>
	public Metadata Metadata { get; init; } = new();
}

/// <summary>
/// Base class for all operation builders in the KurrentDB client.
/// Provides core functionality for building requests and executing operations.
/// </summary>
/// <typeparam name="TClient">The client type that will execute the operation</typeparam>
/// <typeparam name="TBuilder">The concrete builder type (for fluent method chaining)</typeparam>
/// <typeparam name="TRequest">The type of request to build</typeparam>
/// <typeparam name="TResult">The type of result expected from the operation</typeparam>
[PublicAPI]
public abstract class OperationBase<TClient, TBuilder, TRequest, TResult>
	where TBuilder : OperationBase<TClient, TBuilder, TRequest, TResult>
	where TClient : class
	where TRequest : class {
	/// <summary>
	/// The client instance that will execute the operation
	/// </summary>
	protected readonly TClient Client;

	/// <summary>
	/// Initializes a new instance of the <see cref="OperationBase{TClient, TBuilder, TRequest, TResult}"/> class.
	/// </summary>
	/// <param name="client">The client to use for executing operations</param>
	protected OperationBase(TClient client) =>
		Client = client;

	/// <summary>
	/// Builds the request object that will be sent to the client.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation</param>
	/// <returns>A task that resolves to the constructed request object</returns>
	protected abstract ValueTask<TRequest> BuildAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes the operation using the built request.
	/// </summary>
	/// <param name="request">The request to send to the client</param>
	/// <param name="cancellationToken">A token to cancel the operation</param>
	/// <returns>A task that resolves to the operation result</returns>
	protected abstract ValueTask<TResult> ExecuteRequestAsync(TRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	/// Builds and executes the operation, returning the result.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation</param>
	/// <returns>A task that resolves to the operation result</returns>
	public async ValueTask<TResult> ExecuteAsync(CancellationToken cancellationToken = default) {
		var request = await BuildAsync(cancellationToken).ConfigureAwait(false);
		return await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Returns this instance cast to the concrete builder type.
	/// Used by fluent method implementations.
	/// </summary>
	protected TBuilder Self => (TBuilder)this;
}

/// <summary>
/// Base class for operations that provide asynchronous iteration over values of a specified type.
/// </summary>
/// <typeparam name="TClient">The client type that will execute the operation</typeparam>
/// <typeparam name="TBuilder">The concrete builder type (for fluent method chaining)</typeparam>
/// <typeparam name="TRequest">The type of request to build</typeparam>
/// <typeparam name="TItem">The type of values to enumerate.</typeparam>
[PublicAPI]
public abstract class StreamingOperationBase<TClient, TBuilder, TRequest, TItem>
	where TBuilder : StreamingOperationBase<TClient, TBuilder, TRequest, TItem>
	where TClient : class
	where TRequest : class {
	/// <summary>
	/// The client instance that will execute the operation
	/// </summary>
	protected readonly TClient Client;

	/// <summary>
	/// Initializes a new instance of the <see cref="StreamingOperationBase{TClient, TBuilder, TRequest, TItem}"/> class.
	/// </summary>
	/// <param name="client">The client to use for executing operations</param>
	/// <exception cref="ArgumentNullException">Thrown if client is null</exception>
	protected StreamingOperationBase(TClient client) =>
		Client = client;

	/// <summary>
	/// Builds the request object that will be sent to the client.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation</param>
	/// <returns>A task that resolves to the constructed request object</returns>
	protected abstract ValueTask<TRequest> BuildAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes the streaming operation using the built request.
	/// </summary>
	/// <param name="request">The request to send to the client</param>
	/// <param name="cancellationToken">A token to cancel the operation</param>
	/// <returns>An asynchronous enumerable of result items</returns>
	protected abstract IAsyncEnumerable<TItem> ExecuteRequestAsync(TRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	/// Builds and executes the streaming operation, returning the result stream.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation</param>
	/// <returns>An asynchronous enumerable of result items</returns>
	public async IAsyncEnumerable<TItem> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken = default) {
		var request = await BuildAsync(cancellationToken).ConfigureAwait(false);
		await foreach (var item in ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false))
			yield return item;
	}

	/// <summary>
	/// Returns this instance cast to the concrete builder type.
	/// Used by fluent method implementations.
	/// </summary>
	protected TBuilder Self => (TBuilder)this;
}



/// <summary>
/// Exception thrown when an operation fails.
/// </summary>
/// <typeparam name="TFailure">The type of failure that occurred</typeparam>
[PublicAPI]
public class OperationException<TFailure> : Exception
	where TFailure : class {
	/// <summary>
	/// Initializes a new instance of the <see cref="OperationException{TFailure}"/> class.
	/// </summary>
	/// <param name="failure">The failure details</param>
	public OperationException(TFailure failure)
		: base($"Operation failed: {failure}") =>
		Failure = failure;

	/// <summary>
	/// Gets the failure details.
	/// </summary>
	public TFailure Failure { get; }
}

[PublicAPI]
public class MessageBuilder(ISchemaSerializerProvider serializerProvider) {
	ReadOnlyMemory<byte> _data       = ReadOnlyMemory<byte>.Empty;
	SchemaDataFormat     _dataFormat = SchemaDataFormat.Json;
	Metadata             _metadata   = new();
	Guid                 _recordId   = Guid.NewGuid();
	object?              _value;

	public MessageBuilder RecordId(Guid recordId) {
		_recordId = recordId;
		return this;
	}

	public MessageBuilder Value(object value) {
		_value = value;
		return this;
	}

	public MessageBuilder DataFormat(SchemaDataFormat dataFormat) {
		_dataFormat = dataFormat;
		return this;
	}

	public MessageBuilder Metadata(Metadata metadata) {
		_metadata = metadata;
		return this;
	}

	public async ValueTask<Message> BuildAsync(string stream, CancellationToken ct) {
		if (_value is null)
			throw new InvalidOperationException("Message value cannot be null");

		var serializer = serializerProvider.GetSerializer(_dataFormat);

		_metadata.Set(SystemMetadataKeys.SchemaDataFormat, _dataFormat);

		// metadata is enriched with schema name, data format
		// and schema version id if autoregistration is enabled.
		var data = await serializer
			.Serialize(_value, new SchemaSerializationContext(stream, _metadata, new SchemaRegistryPolicy(), ct))
			.ConfigureAwait(false);

		return new Message {
			RecordId   = _recordId,
			Value      = _value,
			Data       = data,
			DataFormat = _dataFormat,
			Metadata   = _metadata
		};
	}

	public async ValueTask<EventData> BuildEventDataAsync(string stream, CancellationToken ct = default) {
		if (_value is null)
			throw new InvalidOperationException("Message value cannot be null");

		var serializer = serializerProvider.GetSerializer(_dataFormat);

		var message = new Message {
			RecordId   = _recordId,
			Value      = _value,
			DataFormat = _dataFormat,
			Metadata   = _metadata
		};

		_metadata.Set(SystemMetadataKeys.SchemaDataFormat, _dataFormat);

		var data = await serializer
			.Serialize(_value, new(stream, _metadata, new SchemaRegistryPolicy(), ct))
			.ConfigureAwait(false);

		var id          = Uuid.FromGuid(message.RecordId); // BROKEN
		var schemaName  = message.Metadata.Get<string>(SystemMetadataKeys.SchemaName)!;
		var contentType = message.DataFormat.GetContentType();

		return new EventData(
			id,
			schemaName,
			data,
			JsonSerializer.SerializeToUtf8Bytes(message.Metadata),
			contentType
		);
	}
}

[PublicAPI]
public class AppendStreamRequestBuilder {
	StreamState          _expectedState   = StreamState.Any;
	List<MessageBuilder> _messageBuilders = [];

	string _stream = "";

	public AppendStreamRequestBuilder(KurrentDBClient client) => Client = client;

	KurrentDBClient Client { get; }

	public AppendStreamRequestBuilder Stream(string stream) {
		_stream = stream;
		return this;
	}

	public AppendStreamRequestBuilder ExpectedState(StreamState expectedState) {
		_expectedState = expectedState;
		return this;
	}

	public AppendStreamRequestBuilder AddMessage(Action<MessageBuilder> configureBuilder) {
		var messageBuilder = new MessageBuilder(Client.SerializerProvider);
		configureBuilder(messageBuilder);
		_messageBuilders.Add(messageBuilder);
		return this;
	}

	public AppendStreamRequestBuilder AddMessage(MessageBuilder messageBuilder) {
		_messageBuilders.Add(messageBuilder);
		return this;
	}

	public ValueTask<AppendStreamRequest> BuildAsync(CancellationToken cancellationToken = default) {
		var messages = _messageBuilders.ToAsyncEnumerable().SelectAwaitWithCancellation((x, idx, ct) => x.BuildAsync(_stream, ct));
		var request  = new AppendStreamRequest(_stream, _expectedState, messages);
		return new ValueTask<AppendStreamRequest>(request);
	}

	// /// <summary>
	// /// Builds and executes the append operation
	// /// </summary>
	// public async Task<AppendStreamResult> ExecuteAsync(CancellationToken cancellationToken = default) {
	// 	var request = await BuildAsync(cancellationToken).ConfigureAwait(false);
	// 	return await Client.AppendStream(request, cancellationToken);
	// }
}

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
// 	}
// }
