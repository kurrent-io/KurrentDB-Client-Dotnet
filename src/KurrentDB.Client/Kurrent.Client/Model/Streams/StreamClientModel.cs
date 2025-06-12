using System.Diagnostics.CodeAnalysis;
using Google.Protobuf.WellKnownTypes;
using Kurrent.Whatever;
using OneOf;

namespace Kurrent.Client.Model;

[PublicAPI]
[method: SetsRequiredMembers]
public record AppendStreamSuccess(string Stream, LogPosition Position, StreamRevision StreamRevision) {
    public required string         Stream         { get; init; } = Stream;
    public required LogPosition    Position       { get; init; } = Position;
    public required StreamRevision StreamRevision { get; init; } = StreamRevision;
}

[PublicAPI]
public partial class AppendStreamFailure : IWhatever<ErrorDetails.StreamNotFound, ErrorDetails.StreamDeleted, ErrorDetails.AccessDenied, ErrorDetails.TransactionMaxSizeExceeded, ErrorDetails.StreamRevisionConflict> {
    public KurrentClientException Throw() => ((ErrorDetailsBase)Value).Throw();
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

    public void Throw() => ((ErrorDetailsBase)Value).Throw();
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

    public void Throw() => ((ErrorDetailsBase)Value).Throw();
}


public partial class GetStreamInfoResult : Result<StreamInfo, GetStreamInfoError>;

[PublicAPI]
[GenerateOneOf]
public partial class GetStreamInfoError : OneOfBase<ErrorDetails.StreamNotFound, ErrorDetails.AccessDenied> {
    public bool IsStreamNotFound => IsT0;
    public bool IsAccessDenied   => IsT1;

    public ErrorDetails.StreamNotFound AsStreamNotFound => AsT0;
    public ErrorDetails.AccessDenied   AsAccessDenied   => AsT1;

    public void Throw() => ((ErrorDetailsBase)Value).Throw();
}

public partial class SetStreamMetadataResult : Result<StreamRevision, SetStreamMetadataError>;

[GenerateOneOf]
public partial class SetStreamMetadataError : OneOfBase<ErrorDetails.StreamNotFound, ErrorDetails.StreamDeleted, ErrorDetails.AccessDenied, ErrorDetails.StreamRevisionConflict> {
    public bool IsStreamNotFound         => IsT0;
    public bool IsStreamDeleted          => IsT1;
    public bool IsAccessDenied           => IsT2;
    public bool IsStreamRevisionConflict => IsT3;

    public ErrorDetails.StreamNotFound         AsStreamNotFound         => AsT0;
    public ErrorDetails.StreamDeleted          AsStreamDeleted          => AsT1;
    public ErrorDetails.AccessDenied           AsAccessDenied           => AsT2;
    public ErrorDetails.StreamRevisionConflict AsStreamRevisionConflict => AsT3;

    public void Throw() => ((ErrorDetailsBase)Value).Throw();
}
