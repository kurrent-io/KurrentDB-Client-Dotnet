using System.Diagnostics.CodeAnalysis;
using Kurrent.Whatever;
using Kurrent;

namespace Kurrent.Client.Model;


// public interface IKurrentClientError : IWhatever {
//     public KurrentClientException Throw() => ((KurrentClientErrorDetails)Value).Throw();
// }
//
// public interface IKurrentClientError<T0> : IKurrentClientError, IWhatever<T0>;
// public interface IKurrentClientError<T0, T1> : IKurrentClientError, IWhatever<T0, T1>;
// public interface IKurrentClientError<T0, T1, T2> : IKurrentClientError, IWhatever<T0, T1, T2>;
// public interface IKurrentClientError<T0, T1, T2, T3> : IKurrentClientError, IWhatever<T0, T1, T2, T3>;
// public interface IKurrentClientError<T0, T1, T2, T3, T4> : IKurrentClientError, IWhatever<T0, T1, T2, T3, T4>;
// public interface IKurrentClientError<T0, T1, T2, T3, T4, T5> : IKurrentClientError, IWhatever<T0, T1, T2, T3, T4, T5>;
// public interface IKurrentClientError<T0, T1, T2, T3, T4, T5, T6> : IKurrentClientError, IWhatever<T0, T1, T2, T3, T4, T5, T6>;
// public interface IKurrentClientError<T0, T1, T2, T3, T4, T5, T6, T7> : IKurrentClientError, IWhatever<T0, T1, T2, T3, T4, T5, T6, T7>;
// public interface IKurrentClientError<T0, T1, T2, T3, T4, T5, T6, T7, T8> : IKurrentClientError, IWhatever<T0, T1, T2, T3, T4, T5, T6, T7, T8>;
// public interface IKurrentClientError<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : IKurrentClientError, IWhatever<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>;


[PublicAPI]
[method: SetsRequiredMembers]
public record AppendStreamSuccess(string Stream, LogPosition Position, StreamRevision StreamRevision) {
    public required string         Stream         { get; init; } = Stream;
    public required LogPosition    Position       { get; init; } = Position;
    public required StreamRevision StreamRevision { get; init; } = StreamRevision;
}

[PublicAPI]
public partial class AppendStreamFailure : IWhatever<
    ErrorDetails.StreamNotFound,
    ErrorDetails.StreamDeleted,
    ErrorDetails.AccessDenied,
    ErrorDetails.TransactionMaxSizeExceeded,
    ErrorDetails.StreamRevisionConflict> {
    public KurrentClientException Throw() => ((KurrentClientErrorDetails)Value).Throw();
};

[PublicAPI]
[method: SetsRequiredMembers]
public record AppendStreamRequest(string Stream, ExpectedStreamState ExpectedState, IEnumerable<Message> Messages) {
    public required string                     Stream        { get; init; } = Stream;
    public required IEnumerable<Message>       Messages      { get; init; } = Messages;
    public required ExpectedStreamState        ExpectedState { get; init; } = ExpectedState;
    public static   AppendStreamRequestBuilder New()         => new();
}

[PublicAPI]
public partial class AppendStreamResult : Result<AppendStreamSuccess, AppendStreamFailure>;

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
public partial class MultiStreamAppendResult : Result<AppendStreamSuccesses, AppendStreamFailures>;

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
public partial class ReadMessage : IWhatever<Record, Heartbeat>;

[PublicAPI]
public partial class SubscribeMessage : IWhatever<Record, Heartbeat>;

/// <summary>
/// Represents the result of a delete operation on a stream in KurrentDB.
/// </summary>
[PublicAPI]
public partial class DeleteError : IWhatever<ErrorDetails.StreamNotFound, ErrorDetails.StreamDeleted, ErrorDetails.AccessDenied, ErrorDetails.StreamRevisionConflict> {
    public KurrentClientException Throw() => ((KurrentClientErrorDetails)Value).Throw();
}

/// <summary>
/// Represents the result of a tombstone operation on a stream in KurrentDB.
/// </summary>
[PublicAPI]
public partial class TombstoneError : IWhatever<ErrorDetails.StreamNotFound, ErrorDetails.StreamDeleted, ErrorDetails.AccessDenied, ErrorDetails.StreamRevisionConflict> {
    public KurrentClientException Throw() => ((KurrentClientErrorDetails)Value).Throw();
}

[PublicAPI]
public partial class GetStreamInfoResult : Result<StreamInfo, GetStreamInfoError>;

[PublicAPI]
public partial class GetStreamInfoError : IWhatever<ErrorDetails.StreamNotFound, ErrorDetails.AccessDenied> {
    public KurrentClientException Throw() => ((KurrentClientErrorDetails)Value).Throw();
}

[PublicAPI]
public partial class SetStreamMetadataResult : Result<StreamRevision, SetStreamMetadataError>;

[PublicAPI]
public partial class SetStreamMetadataError : IWhatever<ErrorDetails.StreamNotFound, ErrorDetails.StreamDeleted, ErrorDetails.AccessDenied, ErrorDetails.StreamRevisionConflict> {
    public KurrentClientException Throw() => ((KurrentClientErrorDetails)Value).Throw();
}
