using System.Diagnostics.CodeAnalysis;
using OneOf;

namespace Kurrent.Client.Model;

/// <summary>
/// Base exception class for all KurrentDB client exceptions.
/// </summary>
public class KurrentClientException(string errorCode, string message, Exception? innerException = null) : Exception(message, innerException) {
    public static void Throw<T>(T error, Exception? innerException = null) => throw new KurrentClientException(typeof(T).Name, error!.ToString()!, innerException);
}

/// <summary>
/// Provides a set of error detail types for representing specific append operation failures in a stream.
/// </summary>
[PublicAPI]
public static class ErrorDetails {
    /// <summary>
    /// Represents an error indicating that the specified stream could not be found.
    /// </summary>
    /// <param name="Stream">The name of the stream that could not be located.</param>
    public readonly record struct StreamNotFound(string Stream) {
        public override string ToString() => $"Stream '{Stream}' not found.";
    }

    /// <summary>
    /// Represents an error indicating that the specified stream has been deleted.
    /// </summary>
    /// <param name="Stream">The name of the stream that has been deleted.</param>
    public readonly record struct StreamDeleted(string Stream) {
        public override string ToString() => $"Stream '{Stream}' has been deleted.";
    }

    /// <summary>
    /// Represents an error indicating that access to the requested resource has been denied.
    /// </summary>
    public readonly struct AccessDenied(string Stream) {
        public override string ToString() => $"Stream '{Stream}' access denied.";
    }

    /// <summary>
    /// Indicates an error where the maximum allowable size of a transaction has been exceeded.
    /// </summary>
    /// <param name="MaxSize">The maximum allowed size of the transaction.</param>
    public readonly record struct TransactionMaxSizeExceeded(uint MaxSize) {
        public override string ToString() => $"Transaction size exceeded. Maximum allowed size: {MaxSize}.";
    }

    /// <summary>
    /// Represents a failure due to an unexpected revision conflict during an append operation.
    /// </summary>
    /// <param name="StreamRevision">The actual revision of the stream.</param>
    public readonly record struct StreamRevisionConflict(string Stream, StreamRevision StreamRevision) {
        public override string ToString() => $"Stream '{Stream}' operation failed due to revision conflict. Actual revision: {StreamRevision}.";
    }

    // public readonly record struct WrongExpectedRevision(string Stream, ExpectedStreamState ExpectedStreamState, StreamRevision StreamRevision) {
    //     public override string ToString() => $"Stream '{Stream}' operation failed due to revision conflict. Expected revision: {ExpectedStreamState}, actual revision: {StreamRevision}.";
    // }
}

[PublicAPI]
[method: SetsRequiredMembers]
public record AppendStreamSuccess(string Stream, LogPosition Position, StreamRevision StreamRevision) {
    public required string         Stream         { get; init; } = Stream;
    public required LogPosition    Position       { get; init; } = Position;
    public required StreamRevision StreamRevision { get; init; } = StreamRevision;
}

[PublicAPI]
[GenerateOneOf]
public partial class AppendStreamFailure : OneOfBase<ErrorDetails.StreamNotFound, ErrorDetails.StreamDeleted, ErrorDetails.AccessDenied, ErrorDetails.TransactionMaxSizeExceeded, ErrorDetails.StreamRevisionConflict> {
    public bool IsStreamNotFound             => IsT0;
    public bool IsStreamDeleted              => IsT1;
    public bool IsAccessDenied               => IsT2;
    public bool IsTransactionMaxSizeExceeded => IsT3;
    public bool IsStreamRevisionConflict     => IsT4;

    public ErrorDetails.StreamNotFound             AsStreamNotFound             => AsT0;
    public ErrorDetails.StreamDeleted              AsStreamDeleted              => AsT1;
    public ErrorDetails.AccessDenied               AsAccessDenied               => AsT2;
    public ErrorDetails.TransactionMaxSizeExceeded AsTransactionMaxSizeExceeded => AsT3;
    public ErrorDetails.StreamRevisionConflict     AsStreamRevisionConflict     => AsT4;

    public void Throw() =>
        Switch(
            notFound => KurrentClientException.Throw(notFound),
            deleted => KurrentClientException.Throw(deleted),
            accessDenied => KurrentClientException.Throw(accessDenied),
            maxSizeExceeded => KurrentClientException.Throw(maxSizeExceeded),
            revisionConflict => KurrentClientException.Throw(revisionConflict)
        );
}

[PublicAPI]
[method: SetsRequiredMembers]
public record AppendStreamRequest(string Stream, ExpectedStreamState ExpectedState, IEnumerable<Message> Messages) {
    public required string                     Stream        { get; init; } = Stream;
    public required IEnumerable<Message>       Messages      { get; init; } = Messages;
    public required ExpectedStreamState        ExpectedState { get; init; } = ExpectedState;
    public static   AppendStreamRequestBuilder New()         => new();
}

[PublicAPI]
public partial class AppendStreamResult : Result<AppendStreamSuccess, AppendStreamFailure> {
    // Constructor removed - will be generated
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
public record MultiStreamAppendRequest {
    public IEnumerable<AppendStreamRequest> Requests { get; init; } = [];
}

[PublicAPI]
public partial class MultiStreamAppendResult : Result<AppendStreamSuccesses, AppendStreamFailures> {
    // Constructor removed - will be generated
}

[PublicAPI]
public class AppendStreamRequestBuilder {
    ExpectedStreamState  _expectedState   = ExpectedStreamState.Any;
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
[method: SetsRequiredMembers]
public readonly record struct HeartbeatOptions(bool Enable, int RecordsThreshold) {
    public static readonly HeartbeatOptions Default  = new(true, 1000);
    public static readonly HeartbeatOptions Disabled = new(false, 0);

    public required bool Enable           { get; init; } = Enable;
    public required int  RecordsThreshold { get; init; } = RecordsThreshold;
}

[PublicAPI]
[GenerateOneOf]
public partial class ReadMessage : OneOfBase<Record, Heartbeat> {
    public bool IsRecord    => IsT0;
    public bool IsHeartbeat => IsT1;

    public Record    AsRecord    => AsT0;
    public Heartbeat AsHeartbeat => AsT1;
}

[PublicAPI]
[GenerateOneOf]
public partial class SubscribeMessage : OneOfBase<Record, Heartbeat> {
    public bool IsRecord    => IsT0;
    public bool IsHeartbeat => IsT1;

    public Record    AsRecord    => AsT0;
    public Heartbeat AsHeartbeat => AsT1;
}

/// <summary>
/// Represents the result of a delete operation on a stream in KurrentDB.
/// </summary>
[PublicAPI]
[GenerateOneOf]
public partial class DeleteError : OneOfBase<ErrorDetails.StreamNotFound, ErrorDetails.StreamDeleted, ErrorDetails.AccessDenied, ErrorDetails.StreamRevisionConflict> {
    public bool IsStreamNotFound         => IsT0;
    public bool IsStreamDeleted          => IsT1;
    public bool IsAccessDenied           => IsT2;
    public bool IsStreamRevisionConflict => IsT3;

    public ErrorDetails.StreamNotFound         AsStreamNotFound         => AsT0;
    public ErrorDetails.StreamDeleted          AsStreamDeleted          => AsT1;
    public ErrorDetails.AccessDenied           AsAccessDenied           => AsT2;
    public ErrorDetails.StreamRevisionConflict AsStreamRevisionConflict => AsT3;

    public void Throw() =>
        Switch(
            notFound => KurrentClientException.Throw(notFound),
            deleted => KurrentClientException.Throw(deleted),
            accessDenied => KurrentClientException.Throw(accessDenied),
            revisionConflict => KurrentClientException.Throw(revisionConflict)
        );
}

/// <summary>
/// Represents the result of a tombstone operation on a stream in KurrentDB.
/// </summary>
[PublicAPI]
[GenerateOneOf]
public partial class TombstoneError : OneOfBase<ErrorDetails.StreamNotFound, ErrorDetails.StreamDeleted, ErrorDetails.AccessDenied, ErrorDetails.StreamRevisionConflict> {
    public bool IsStreamNotFound         => IsT0;
    public bool IsStreamDeleted          => IsT1;
    public bool IsAccessDenied           => IsT2;
    public bool IsStreamRevisionConflict => IsT3;

    public ErrorDetails.StreamNotFound         AsStreamNotFound         => AsT0;
    public ErrorDetails.StreamDeleted          AsStreamDeleted          => AsT1;
    public ErrorDetails.AccessDenied           AsAccessDenied           => AsT2;
    public ErrorDetails.StreamRevisionConflict AsStreamRevisionConflict => AsT3;

    public void Throw() =>
        Switch(
            notFound => KurrentClientException.Throw(notFound),
            deleted => KurrentClientException.Throw(deleted),
            accessDenied => KurrentClientException.Throw(accessDenied),
            revisionConflict => KurrentClientException.Throw(revisionConflict)
        );
}

[PublicAPI]
[GenerateOneOf]
public partial class GetStreamInfoError : OneOfBase<ErrorDetails.StreamNotFound, ErrorDetails.StreamDeleted, ErrorDetails.AccessDenied> {
    public bool IsStreamNotFound => IsT0;
    public bool IsStreamDeleted  => IsT1;
    public bool IsAccessDenied   => IsT2;

    public ErrorDetails.StreamNotFound AsStreamNotFound => AsT0;
    public ErrorDetails.StreamDeleted  AsStreamDeleted  => AsT1;
    public ErrorDetails.AccessDenied   AsAccessDenied   => AsT2;

    public void Throw() =>
        Switch(
            notFound => KurrentClientException.Throw(notFound),
            deleted => KurrentClientException.Throw(deleted),
            accessDenied => KurrentClientException.Throw(accessDenied)
        );
}

[PublicAPI]
[GenerateOneOf]
public partial class SetMetadataError : OneOfBase<ErrorDetails.StreamNotFound, ErrorDetails.StreamDeleted, ErrorDetails.AccessDenied, ErrorDetails.StreamRevisionConflict> {
    public bool IsStreamNotFound         => IsT0;
    public bool IsStreamDeleted          => IsT1;
    public bool IsAccessDenied           => IsT2;
    public bool IsStreamRevisionConflict => IsT3;

    public ErrorDetails.StreamNotFound         AsStreamNotFound         => AsT0;
    public ErrorDetails.StreamDeleted          AsStreamDeleted          => AsT1; 
    public ErrorDetails.AccessDenied           AsAccessDenied           => AsT2; 
    public ErrorDetails.StreamRevisionConflict AsStreamRevisionConflict => AsT3; 

    public void Throw() =>
        Switch(
            notFound => KurrentClientException.Throw(notFound),
            deleted => KurrentClientException.Throw(deleted),
            accessDenied => KurrentClientException.Throw(accessDenied),
            revisionConflict => KurrentClientException.Throw(revisionConflict)
        );
}
