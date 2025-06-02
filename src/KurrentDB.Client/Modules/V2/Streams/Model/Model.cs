using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Kurrent.Client;
using OneOf;

namespace Kurrent.Client.Model;

[PublicAPI]
[method: SetsRequiredMembers]
public record AppendStreamSuccess(string Stream, long Position, long StreamRevision) {
	public required string Stream         { get; init; } = Stream;
	public required long   Position       { get; init; } = Position;
	public required long   StreamRevision { get; init; } = StreamRevision;
}

// [PublicAPI]
// [method: SetsRequiredMembers]
// public record AppendStreamFailure(string Stream, Exception Error) {
// 	public required string    Stream { get; init; } = Stream;
// 	public required Exception Error  { get; init; } = Error;
// }

/// <summary>
/// Provides a set of error detail types for representing specific append operation failures in a stream.
/// </summary>
[PublicAPI]
public static class AppendErrorDetails {
	/// <summary>
	/// Represents an error indicating that the specified stream could not be found.
	/// </summary>
	/// <param name="Stream">The name of the stream that could not be located.</param>
	public readonly record struct StreamNotFound(string Stream);

	/// <summary>
	/// Represents an error indicating that the specified stream has been deleted.
	/// </summary>
	/// <param name="Stream">The name of the stream that has been deleted.</param>
	public readonly record struct StreamDeleted(string Stream);

	/// <summary>
	/// Represents an error indicating that access to the requested resource has been denied.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public readonly struct AccessDenied {
		public static readonly AccessDenied Value = new();
	}

	/// <summary>
	/// Indicates an error where the maximum allowable size of a transaction has been exceeded.
	/// </summary>
	/// <param name="MaxSize">The maximum allowed size of the transaction.</param>
	public readonly record struct TransactionMaxSizeExceeded(uint MaxSize);

	/// <summary>
	/// Represents a failure due to an unexpected revision conflict during an append operation.
	/// </summary>
	/// <param name="StreamRevision">The actual revision of the stream.</param>
	public readonly record struct WrongExpectedRevision(string Stream, long StreamRevision);
}

[PublicAPI]
[GenerateOneOf]
public partial class AppendStreamFailure : OneOfBase<AppendErrorDetails.StreamNotFound, AppendErrorDetails.StreamDeleted, AppendErrorDetails.AccessDenied, AppendErrorDetails.TransactionMaxSizeExceeded,
	AppendErrorDetails.WrongExpectedRevision> {
	public bool IsStreamNotFound             => IsT0;
	public bool IsStreamDeleted              => IsT1;
	public bool IsAccessDenied               => IsT2;
	public bool IsTransactionMaxSizeExceeded => IsT3;
	public bool IsWrongExpectedRevision      => IsT4;

	public AppendErrorDetails.StreamNotFound             StreamNotFound             => AsT0;
	public AppendErrorDetails.StreamDeleted              StreamDeleted              => AsT1;
	public AppendErrorDetails.AccessDenied               AccessDenied               => AsT2;
	public AppendErrorDetails.TransactionMaxSizeExceeded TransactionMaxSizeExceeded => AsT3;
	public AppendErrorDetails.WrongExpectedRevision      WrongExpectedRevision      => AsT4;
}

[PublicAPI]
public class AppendStreamSuccesses : List<AppendStreamSuccess> {
	public AppendStreamSuccesses() { }
	public AppendStreamSuccesses(IEnumerable<AppendStreamSuccess> input) : base(input) { }
}

[PublicAPI]
public class AppendStreamFailures : List<AppendStreamFailure> {
	public AppendStreamFailures() { }
	public AppendStreamFailures(IEnumerable<AppendStreamFailure> input) : base(input) { }
}

[PublicAPI]
[method: SetsRequiredMembers]
public record AppendStreamRequest(string Stream, ExpectedStreamState ExpectedState, IEnumerable<Message> Messages) {
	public static AppendStreamRequestBuilder New() => new();

	public required string               Stream        { get; init; } = Stream;
	public required IEnumerable<Message> Messages      { get; init; } = Messages;
	public required ExpectedStreamState  ExpectedState { get; init; } = ExpectedState;
}

[PublicAPI]
[GenerateOneOf]
public partial class AppendStreamResult : OneOfBase<AppendStreamSuccess, AppendStreamFailure>;

[PublicAPI]
public record MultiStreamAppendRequest {
	public IEnumerable<AppendStreamRequest> Requests { get; init; } = [];
}

[PublicAPI]
[GenerateOneOf]
public partial class MultiStreamAppendResult : OneOfBase<AppendStreamSuccesses, AppendStreamFailures> {
	public bool IsSuccess => IsT0;
	public bool IsFailure => IsT1;

	public AppendStreamSuccesses Successes => AsT0;
	public AppendStreamFailures  Failures  => AsT1;
}

[PublicAPI]
[method: SetsRequiredMembers]
public readonly record struct HeartbeatOptions(bool Enable, int RecordsThreshold) {
	public static readonly HeartbeatOptions Disabled = new(false, 0);
	public static readonly HeartbeatOptions Default  = new(true, 1000);

	public required bool Enable           { get; init; } = Enable;
	public required int  RecordsThreshold { get; init; } = RecordsThreshold;
}

[PublicAPI]
public record Message {
	public static readonly Message Empty = new() {
		RecordId   = Guid.Empty,
		Value      = null!,
		DataFormat = SchemaDataFormat.Unspecified,
		Metadata   = null!
	};

	public static MessageBuilder New() => new();

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

	public MessageBuilder WithMetadata(IDictionary<string, object> entries) {
		var newMetadata = new Metadata(_message.Metadata);
		foreach (var (key, value) in entries)
			newMetadata[key] = value;

		_message = _message with { Metadata = newMetadata };
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

[PublicAPI]
public class AppendStreamRequestBuilder {
	ExpectedStreamState  _expectedState   = ExpectedStreamState.Any.Instance;
	List<MessageBuilder> _messageBuilders = [];

	string _stream = "";

	public AppendStreamRequestBuilder ForStream(string stream) {
		_stream = stream;
		return this;
	}

	public AppendStreamRequestBuilder ExpectingState(ExpectedStreamState expectedState) {
		_expectedState = expectedState;
		return this;
	}

	public AppendStreamRequestBuilder WithMessage(Action<MessageBuilder> configureBuilder) {
		var messageBuilder = new MessageBuilder();
		configureBuilder(messageBuilder);
		_messageBuilders.Add(messageBuilder);
		return this;
	}

	public AppendStreamRequestBuilder WithMessage(MessageBuilder messageBuilder) {
		_messageBuilders.Add(messageBuilder);
		return this;
	}

	public AppendStreamRequestBuilder WithMessage(object value, SchemaDataFormat dataFormat = SchemaDataFormat.Json, Metadata? metadata = null) =>
		WithMessage(builder => builder.WithValue(value).WithDataFormat(dataFormat).WithMetadata(metadata ?? new Metadata()));

	public AppendStreamRequest Build() {
		var messages = _messageBuilders.Select(x => x.Build());
		var request  = new AppendStreamRequest(_stream, _expectedState, messages);
		return request;
	}
}

[PublicAPI]
[GenerateOneOf]
public partial class ReadResult : OneOfBase<Record, Heartbeat> {
	public bool IsRecord    => IsT0;
	public bool IsHeartbeat => IsT1;

	public Record    AsRecord()    => AsT0;
	public Heartbeat AsHeartbeat() => AsT1;
}

[PublicAPI]
[GenerateOneOf]
public partial class SubscribeResult : OneOfBase<Record, Heartbeat> {
	public bool IsRecord    => IsT0;
	public bool IsHeartbeat => IsT1;

	public Record    AsRecord()    => AsT0;
	public Heartbeat AsHeartbeat() => AsT1;
}
