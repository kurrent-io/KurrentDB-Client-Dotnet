// ReSharper disable InconsistentNaming

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using Kurrent.Variant;
using KurrentDB.Diagnostics;
using static System.Threading.Interlocked;

namespace Kurrent.Client.Streams;

[PublicAPI]
public readonly partial record struct ReadMessage : IVariant<Record, Heartbeat> {
    public static readonly ReadMessage None;
}

[PublicAPI]
public partial record Messages : IAsyncEnumerable<ReadMessage>, IAsyncDisposable {
    int _disposed;
    int _enumeratorCreated;

    readonly ConcurrentDictionary<Guid, Activity> Activities = new();

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

        foreach (var activity in Activities.Values) {
            activity.Stop();
            activity.Dispose();
        }

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
                message = await reader.ReadAsync(cancellationToken);

                var activity = KurrentActivitySource.StartSubscriptionActivity(message);

                if (message.IsRecord && activity is not null)
	                Activities.TryAdd(message.AsRecord.Id, activity);
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
