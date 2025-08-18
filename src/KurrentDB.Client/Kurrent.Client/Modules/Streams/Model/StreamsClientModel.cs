using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Kurrent.Variant;
using static System.Threading.Interlocked;

namespace Kurrent.Client.Streams;

#region . append .

[PublicAPI]
[method: SetsRequiredMembers]
public record AppendStreamSuccess(string Stream, LogPosition Position, StreamRevision StreamRevision) {
    public required string         Stream         { get; init; } = Stream;
    public required LogPosition    Position       { get; init; } = Position;
    public required StreamRevision StreamRevision { get; init; } = StreamRevision;
}

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

#endregion

#region . read & subscriptions .

[PublicAPI]
[method: SetsRequiredMembers]
public readonly record struct HeartbeatOptions(bool Enable, int RecordsThreshold) {
    public static readonly HeartbeatOptions Default  = new(true, 1000);
    public static readonly HeartbeatOptions Disabled = new(false, 0);

    public required bool Enable           { get; init; } = Enable;
    public required int  RecordsThreshold { get; init; } = RecordsThreshold;
}

[PublicAPI]
public readonly partial record struct ReadMessage : IVariant<Record, Heartbeat> {
    public static readonly ReadMessage None;
}

[PublicAPI]
public record Messages : IAsyncEnumerable<ReadMessage>, IAsyncDisposable {
    int _disposed;
    int _enumeratorCreated;

    Lazy<Channel<ReadMessage>> _lazyChannel;

    internal Messages(Func<Channel<ReadMessage>> channelFactory) => _lazyChannel = new Lazy<Channel<ReadMessage>>(channelFactory);

    Channel<ReadMessage> Channel => _lazyChannel.Value;

    /// <summary>
    /// The number of messages currently queued for processing
    /// </summary>
    public int QueuedMessages => _lazyChannel.IsValueCreated ? Channel.Reader.Count : 0;

    public async ValueTask DisposeAsync() {
        if (!_lazyChannel.IsValueCreated || CompareExchange(ref _disposed, 1, 0) != 0)
            return;

        Channel.Writer.TryComplete();

        if (Channel.Reader.Completion.IsFaulted)
            await Channel.Reader.Completion;
    }

    public async IAsyncEnumerator<ReadMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(CompareExchange(ref _disposed, 0, 0) == 1, nameof(Subscription));

        if (CompareExchange(ref _enumeratorCreated, 1, 0) == 1)
            throw new InvalidOperationException("Only one enumerator can be active at a time");

        // return Channel.Reader
        //     .ReadAllAsync(cancellationToken)
        //     // .TakeWhile(_ => !cancellationToken.IsCancellationRequested)
        //     .GetAsyncEnumerator(CancellationToken.None);

        var reader = Channel.Reader;

        while (true) {
            ReadMessage message;
            try {
                message = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                yield break;
            }
            // catch (ObjectDisposedException) {
            //     yield break;
            // }
            catch (ChannelClosedException) {
                yield break;
            }

            if (message == ReadMessage.None)
                break;

            yield return message;
        }
    }
}

[PublicAPI]
public record AllSubscriptionOptions : ReadAllOptions {
    new ReadDirection Direction { get; init; }
    new long          Limit     { get; init; }
}

[PublicAPI]
public record StreamSubscriptionOptions : ReadStreamOptions {
    new ReadDirection Direction { get; init; }
    new long          Limit     { get; init; }
}

[PublicAPI]
public record Subscription : Messages {
    internal Subscription(string subscriptionId, Func<Channel<ReadMessage>> channelFactory) : base(channelFactory) => SubscriptionId = subscriptionId;

    /// <summary>
    /// Gets the unique identifier associated with the subscription.
    /// </summary>
    public string SubscriptionId { get; }
}

//
// [PublicAPI]
// public sealed record Subscription : IAsyncEnumerable<ReadMessage>, IAsyncDisposable {
//     internal Subscription(string subscriptionId, Func<Channel<ReadMessage>> channelFactory) {
//         SubscriptionId = subscriptionId;
//         _lazyChannel   = new Lazy<Channel<ReadMessage>>(channelFactory);
//     }
//
//     Lazy<Channel<ReadMessage>> _lazyChannel;
//
//     int _disposed;
//     int _enumeratorCreated;
//
//     Channel<ReadMessage> Channel => _lazyChannel.Value;
//
//     /// <summary>
//     /// Gets the unique identifier associated with the subscription.
//     /// </summary>
//     public string SubscriptionId { get; }
//
//     /// <summary>
//     /// The stream of subscription messages, which can include records or heartbeat notifications,
//     /// depending on the subscription type and the state of the subscribed stream.
//     /// </summary>
//     public IAsyncEnumerable<ReadMessage> Messages => this;
//
//     /// <summary>
//     /// The number of messages currently queued for processing
//     /// </summary>
//     public int QueuedMessages => _lazyChannel.IsValueCreated ? Channel.Reader.Count : 0;
//
//     /// <summary>
//     /// Flushes the buffer and waits until all messages are processed
//     /// </summary>
//     /// <param name="timeout">An optional timeout duration to wait. Defaults to 30 seconds if not specified.</param>
//     public async ValueTask<bool> Flush(TimeSpan? timeout = null) {
//         if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
//             return true;
//
//         if (!_lazyChannel.IsValueCreated)
//             return true;
//
//         Channel.Writer.TryComplete();
//
//         var completedTask = await Task
//             .WhenAny(Channel.Reader.Completion, Task.Delay(timeout ?? TimeSpan.FromSeconds(30)))
//             .ConfigureAwait(false);
//
//         return completedTask == Channel.Reader.Completion;
//     }
//
//     public async IAsyncEnumerator<ReadMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
//         if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
//             throw new ObjectDisposedException(nameof(Subscription));
//
//         if (Interlocked.CompareExchange(ref _enumeratorCreated, 1, 0) == 1)
//             throw new InvalidOperationException("Only one enumerator can be active at a time");
//
//         // return Channel.Reader.ReadAllAsync(CancellationToken.None).GetAsyncEnumerator(CancellationToken.None);
//
//         var reader = Channel.Reader;
//
//         while (true) {
//             bool dataAvailable;
//             try {
//                 dataAvailable = await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);
//             }
//             catch (OperationCanceledException) {
//                 yield break;
//             }
//             catch (ObjectDisposedException) {
//                 yield break;
//             }
//
//             if (!dataAvailable)
//                 break;
//
//             while (reader.TryRead(out var item))
//                 yield return item;
//         }
//     }
//
//     public ValueTask DisposeAsync() {
//         if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
//             return ValueTask.CompletedTask;
//
//         if (_lazyChannel.IsValueCreated)
//             Channel.Writer.TryComplete();
//
//         return ValueTask.CompletedTask;
//     }
// }

#endregion
