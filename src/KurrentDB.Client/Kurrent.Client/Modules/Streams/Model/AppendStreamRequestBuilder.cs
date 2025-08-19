namespace Kurrent.Client.Streams;

[PublicAPI]
public class AppendStreamRequestBuilder {
    ExpectedStreamState  _expectedState   = ExpectedStreamState.Any;
    List<AppendRecordBuilder> _messageBuilders = [];

    string _stream = "";

    public AppendStreamRequestBuilder ForStream(string stream) {
        _stream = stream;
        return this;
    }

    public AppendStreamRequestBuilder ExpectingState(ExpectedStreamState expectedState) {
        _expectedState = expectedState;
        return this;
    }

    public AppendStreamRequestBuilder WithMessage(Action<AppendRecordBuilder> configureBuilder) {
        var messageBuilder = new AppendRecordBuilder();
        configureBuilder(messageBuilder);
        _messageBuilders.Add(messageBuilder);
        return this;
    }

    public AppendStreamRequestBuilder WithMessage(AppendRecordBuilder appendRecordBuilder) {
        _messageBuilders.Add(appendRecordBuilder);
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
