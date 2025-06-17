using System.Diagnostics.CodeAnalysis;
using Kurrent.Variant;

namespace Kurrent.Client.Model;

[PublicAPI]
[method: SetsRequiredMembers]
public record AppendStreamSuccess(string Stream, LogPosition Position, StreamRevision StreamRevision) {
    public required string         Stream         { get; init; } = Stream;
    public required LogPosition    Position       { get; init; } = Position;
    public required StreamRevision StreamRevision { get; init; } = StreamRevision;
}

[PublicAPI]
public readonly partial record struct AppendStreamFailure : IVariant<
    ErrorDetails.StreamNotFound,
    ErrorDetails.StreamDeleted,
    ErrorDetails.AccessDenied,
    ErrorDetails.TransactionMaxSizeExceeded,
    ErrorDetails.StreamRevisionConflict>;

[PublicAPI]
[method: SetsRequiredMembers]
public record AppendStreamRequest(string Stream, ExpectedStreamState ExpectedState, IEnumerable<Message> Messages) {
    public required string               Stream        { get; init; } = Stream;
    public required IEnumerable<Message> Messages      { get; init; } = Messages;
    public required ExpectedStreamState  ExpectedState { get; init; } = ExpectedState;

    public static AppendStreamRequestBuilder New() => new();
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
public readonly partial record struct ReadMessage : IVariant<Record, Heartbeat>;

[PublicAPI]
public readonly partial record struct SubscribeMessage : IVariant<Record, Heartbeat>;

/// <summary>
/// Represents the result of a delete operation on a stream in KurrentDB.
/// </summary>
[PublicAPI]
public readonly partial record struct DeleteStreamError : IVariant<
    ErrorDetails.StreamNotFound,
    ErrorDetails.StreamDeleted,
    ErrorDetails.AccessDenied,
    ErrorDetails.StreamRevisionConflict>;

/// <summary>
/// Represents the result of a tombstone operation on a stream in KurrentDB.
/// </summary>
[PublicAPI]
public readonly partial record struct TombstoneStreamError : IVariant<
    ErrorDetails.StreamNotFound,
    ErrorDetails.StreamDeleted,
    ErrorDetails.AccessDenied,
    ErrorDetails.StreamRevisionConflict>;

[PublicAPI]
public readonly partial record struct GetStreamInfoError : IVariant<
    ErrorDetails.StreamNotFound,
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct SetStreamMetadataError : IVariant<
    ErrorDetails.StreamNotFound,
    ErrorDetails.StreamDeleted,
    ErrorDetails.AccessDenied,
    ErrorDetails.StreamRevisionConflict>;

[PublicAPI]
public readonly partial record struct TruncateStreamError : IVariant<
    ErrorDetails.StreamNotFound,
    ErrorDetails.StreamDeleted,
    ErrorDetails.AccessDenied,
    ErrorDetails.StreamRevisionConflict>;
