using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
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
public readonly partial record struct AppendStreamFailure : IVariantResultError<
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
public readonly record struct ReadAllOptions(
    LogPosition StartPosition,
    ReadFilter Filter,
    HeartbeatOptions Heartbeat,
    long Limit = long.MaxValue,
    ReadDirection Direction = ReadDirection.Forwards,
    CancellationToken CancellationToken = default
) {
    public static readonly ReadAllOptions Default = new(
        LogPosition.Earliest,
        ReadFilter.None,
        HeartbeatOptions.Disabled,
        long.MaxValue,
        ReadDirection.Forwards,
        CancellationToken.None
    );

    public static ReadAllOptions FirstRecord(CancellationToken cancellationToken = default) =>
        Default with {
            Limit             = 1,
            CancellationToken = cancellationToken
        };

    public static ReadAllOptions LastRecord(CancellationToken cancellationToken = default) =>
        Default with {
            Direction         = ReadDirection.Backwards,
            StartPosition     = LogPosition.Latest,
            Limit             = 1,
            CancellationToken = cancellationToken
        };

    public void EnsureValid() {
        ArgumentOutOfRangeException.ThrowIfLessThan(StartPosition, LogPosition.Earliest);
        ArgumentOutOfRangeException.ThrowIfLessThan(Limit, 1);
    }
}

[PublicAPI]
public readonly partial record struct ReadAllError : IVariantResultError<
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly record struct ReadStreamOptions(
    StreamRevision StartRevision,
    long Limit = long.MaxValue,
    ReadDirection Direction = ReadDirection.Forwards,
    CancellationToken CancellationToken = default
) {
    public static readonly ReadStreamOptions Default = new(StreamRevision.Min, long.MaxValue, ReadDirection.Forwards, CancellationToken.None);

    public static ReadStreamOptions FirstRecord(CancellationToken cancellationToken = default) =>
        new(StreamRevision.Min, 1, ReadDirection.Forwards, cancellationToken);

    public static ReadStreamOptions LastRecord(CancellationToken cancellationToken = default) =>
        new(StreamRevision.Max, 1, ReadDirection.Backwards, cancellationToken);

    public void EnsureValid() {
        ArgumentOutOfRangeException.ThrowIfLessThan(StartRevision, StreamRevision.Min);
        ArgumentOutOfRangeException.ThrowIfLessThan(Limit, 1);
    }
}

[PublicAPI]
public readonly partial record struct ReadError : IVariantResultError<
    ErrorDetails.StreamNotFound,
    ErrorDetails.StreamDeleted,
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct ReadMessage : IVariant<Record, Heartbeat>;

[PublicAPI]
public readonly record struct Messages(IAsyncEnumerable<ReadMessage> Source) : IAsyncEnumerable<ReadMessage> {
    public IAsyncEnumerator<ReadMessage> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken()) =>
        Source.GetAsyncEnumerator(cancellationToken);
}

[PublicAPI]
public readonly partial record struct StreamSubscriptionError : IVariantResultError<
    ErrorDetails.StreamNotFound,
    ErrorDetails.StreamDeleted,
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct SubscriptionError : IVariantResultError<
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct SubscriptionMessage : IVariant<Record, Heartbeat>;

// [PublicAPI]
// public readonly record struct SubscriptionEnhanced : IAsyncEnumerable<SubscriptionMessage>, IAsyncDisposable {
//     internal SubscriptionEnhanced(string subscriptionId, ChannelReader<SubscriptionMessage> reader, CancellationTokenSource cancellator) {
//         SubscriptionId = subscriptionId;
//         Reader         = reader;
//         Cancellator    = cancellator;
//         Messages       = reader.ReadAllAsync();
//     }
//
//     ChannelReader<SubscriptionMessage> Reader      { get; }
//     CancellationTokenSource            Cancellator { get; }
//
//     public string                                SubscriptionId { get; }
//     public IAsyncEnumerable<SubscriptionMessage> Messages       { get; }
//
//     public IAsyncEnumerator<SubscriptionMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
//         Messages.GetAsyncEnumerator(cancellationToken);
//
//     public async ValueTask DisposeAsync() {
//         if (Cancellator is not null) {
//             await Cancellator.CancelAsync().ConfigureAwait(false);
//             Cancellator.Dispose();
//         }
//
//         await Reader.Completion.ConfigureAwait(false);
//     }
// }

[PublicAPI]
public readonly record struct Subscription : IAsyncDisposable {
    internal Subscription(string subscriptionId, Channel<SubscriptionMessage> channel) {
        SubscriptionId = subscriptionId;
        Channel        = channel;
        Messages       = channel.Reader.ReadAllAsync();
    }

    Channel<SubscriptionMessage> Channel { get; }

    public string                                SubscriptionId { get; }
    public IAsyncEnumerable<SubscriptionMessage> Messages       { get; }

    public int BufferedMessages => Channel.Reader.Count;

    // public async ValueTask Stop() {
    //     Channel.Writer.TryComplete();
    //     await Channel.Reader.Completion.ConfigureAwait(false);
    // }

    public ValueTask DisposeAsync() {
        Channel.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}

[PublicAPI]
public record StreamSubscriptionOptions {
    /// <summary>
    /// Represents the name of the stream associated with a subscription or append operation.
    /// This property is used to identify the specific stream from which records are read.
    /// </summary>
    public StreamName Stream { get; init; } = StreamName.None;

    /// <summary>
    /// Denotes the starting revision for a stream subscription.
    /// This property determines from which revision the stream will begin to be read
    /// when a subscription is initiated. By default, it is set to the maximum
    /// stream revision, indicating the subscription will start at the most recent revision.
    /// </summary>
    public StreamRevision Start { get; init; } = StreamRevision.Max;

    /// <summary>
    /// Specifies the filtering criteria for a subscription operation.
    /// This determines which records will be included
    /// based on the specified filter settings.
    /// By default, no filter is applied.
    /// </summary>
    public ReadFilter Filter { get; init; } = ReadFilter.None;

    /// <summary>
    /// Configures heartbeat settings to monitor the connection's health and responsiveness.
    /// The heartbeat settings are used to ensure that the server and client maintain an active connection
    /// and to detect any potential issues or delays in data delivery.
    /// Default: Enabled, with a record threshold of 1000.
    /// </summary>
    public HeartbeatOptions Heartbeat { get; init; } = HeartbeatOptions.Default;

    /// <summary>
    /// Maximum time to wait for message delivery before terminating the subscription.
    /// This timeout only applies when the application is not reading messages fast enough,
    /// causing the internal buffer to fill up. Normal waiting for new messages is not affected.
    /// Default: 30 seconds
    /// </summary>
    public TimeSpan SubscriptionTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Number of messages to buffer internally for the subscription.
    /// Higher values improve throughput but increase memory usage.
    /// Default: 1000
    /// </summary>
    public int BufferSize { get; init; } = 1000;

    /// <summary>
    /// Represents a <c>CancellationToken</c> used to propagate notification that operations in the subscription
    /// should be cancelled. This allows cooperative cancellation of pending asynchronous tasks.
    /// Default: <c>CancellationToken.None</c>.
    /// </summary>
    public CancellationToken StoppingToken { get; init; } = CancellationToken.None;

    public void EnsureValid() {
        ArgumentNullException.ThrowIfNullOrEmpty(Stream);

        ArgumentOutOfRangeException.ThrowIfLessThan(Start, StreamRevision.Min);

        ArgumentOutOfRangeException.ThrowIfLessThan(Heartbeat.RecordsThreshold, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Heartbeat.RecordsThreshold, 10000);

        ArgumentOutOfRangeException.ThrowIfLessThan(SubscriptionTimeout, TimeSpan.FromSeconds(1));

        ArgumentOutOfRangeException.ThrowIfLessThan(BufferSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(BufferSize, 10000);
    }
}


[PublicAPI]
public record SubscriptionOptions {
    /// <summary>
    /// Specifies the starting position in the log stream for the subscription.
    /// This determines where the subscription begins reading records from the log.
    /// Possible values include predefined positions such as <c>LogPosition.Earliest</c>
    /// to start from the beginning or <c>LogPosition.Latest</c> for the most recent records.
    /// Default: <c>LogPosition.Latest</c>.
    /// </summary>
    public LogPosition Start { get; init; } = LogPosition.Latest;

    /// <summary>
    /// Specifies the filtering criteria for a subscription operation.
    /// This determines which records will be included
    /// based on the specified filter settings.
    /// By default, no filter is applied.
    /// </summary>
    public ReadFilter Filter { get; init; } = ReadFilter.None;

    /// <summary>
    /// Configures heartbeat settings to monitor the connection's health and responsiveness.
    /// The heartbeat settings are used to ensure that the server and client maintain an active connection
    /// and to detect any potential issues or delays in data delivery.
    /// Default: Enabled, with a record threshold of 1000.
    /// </summary>
    public HeartbeatOptions Heartbeat { get; init; } = HeartbeatOptions.Default;

    /// <summary>
    /// Maximum time to wait for message delivery before terminating the subscription.
    /// This timeout only applies when the application is not reading messages fast enough,
    /// causing the internal buffer to fill up. Normal waiting for new messages is not affected.
    /// Default: 30 seconds
    /// </summary>
    public TimeSpan SubscriptionTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Number of messages to buffer internally for the subscription.
    /// Higher values improve throughput but increase memory usage.
    /// Default: 1000
    /// </summary>
    public int BufferSize { get; init; } = 1000;

    /// <summary>
    /// Represents a <c>CancellationToken</c> used to propagate notification that operations in the subscription
    /// should be cancelled. This allows cooperative cancellation of pending asynchronous tasks.
    /// Default: <c>CancellationToken.None</c>.
    /// </summary>
    public CancellationToken StoppingToken { get; init; } = CancellationToken.None;

    public void EnsureValid() {
        ArgumentOutOfRangeException.ThrowIfLessThan(Start, LogPosition.Earliest);

        ArgumentOutOfRangeException.ThrowIfLessThan(Heartbeat.RecordsThreshold, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Heartbeat.RecordsThreshold, 10000);

        ArgumentOutOfRangeException.ThrowIfLessThan(SubscriptionTimeout, TimeSpan.FromSeconds(1));

        ArgumentOutOfRangeException.ThrowIfLessThan(BufferSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(BufferSize, 10000);
    }
}


/// <summary>
/// Represents the result of a delete operation on a stream in KurrentDB.
/// </summary>
[PublicAPI]
public readonly partial record struct DeleteStreamError : IVariantResultError<
    ErrorDetails.StreamNotFound,
    ErrorDetails.StreamDeleted,
    ErrorDetails.AccessDenied,
    ErrorDetails.StreamRevisionConflict>;

/// <summary>
/// Represents the result of a tombstone operation on a stream in KurrentDB.
/// </summary>
[PublicAPI]
public readonly partial record struct TombstoneError : IVariantResultError<
    ErrorDetails.StreamNotFound,
    ErrorDetails.StreamDeleted,
    ErrorDetails.AccessDenied,
    ErrorDetails.StreamRevisionConflict>;

[PublicAPI]
public readonly partial record struct GetStreamInfoError : IVariantResultError<
    ErrorDetails.StreamNotFound,
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct SetStreamMetadataError : IVariantResultError<
    ErrorDetails.StreamNotFound,
    ErrorDetails.StreamDeleted,
    ErrorDetails.AccessDenied,
    ErrorDetails.StreamRevisionConflict>;

[PublicAPI]
public readonly partial record struct TruncateStreamError : IVariantResultError<
    ErrorDetails.StreamNotFound,
    ErrorDetails.StreamDeleted,
    ErrorDetails.AccessDenied,
    ErrorDetails.StreamRevisionConflict>;
