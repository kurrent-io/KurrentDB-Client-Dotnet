// ReSharper disable InconsistentNaming

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using Kurrent.Variant;
using KurrentDB.Diagnostics;
using KurrentDB.Diagnostics.Tracing;
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
    readonly ActivityTagsCollection?              Tags       = new();

    Lazy<Channel<ReadMessage>> _lazyChannel;

    internal Messages(Func<Channel<ReadMessage>> channelFactory, ActivityTagsCollection? tags = null) {
	    Tags         = tags;
	    _lazyChannel = new Lazy<Channel<ReadMessage>>(channelFactory);
    }

    Channel<ReadMessage> Channel => _lazyChannel.Value;

    /// <summary>
    /// The number of messages currently queued for processing
    /// </summary>
    public int QueuedMessages => _lazyChannel.IsValueCreated ? Channel.Reader.Count : 0;

    public async ValueTask DisposeAsync() {
        if (!_lazyChannel.IsValueCreated || CompareExchange(ref _disposed, 1, 0) != 0)
            return;

        Channel.Writer.TryComplete();

        foreach (var activity in Activities.Values)
            activity.Dispose();

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

                if (message.IsRecord) {
	                var record = message.AsRecord;

	                var tags = new ActivityTagsCollection(Tags ?? [])
		                .WithRequiredTag(TraceConstants.Tags.DatabaseRecordId, record.Id.ToString())
		                .WithRequiredTag(TraceConstants.Tags.DatabaseSchemaFormat, record.Schema.DataFormat.ToString())
		                .WithRequiredTag(TraceConstants.Tags.DatabaseSchemaName, record.Schema.SchemaName.ToString());

	                var activity = KurrentActivitySource.StartSubscriptionActivity(message, tags);

	                if (activity is not null)
		                Activities.TryAdd(record.Id, activity);
                }
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

	internal void CompleteActivity(Record record) {
		Activities.TryRemove(record.Id, out var activity);
		activity?.SetStatus(ActivityStatusCode.Ok);
		activity?.Dispose();
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
	internal Subscription(string subscriptionId, Func<Channel<ReadMessage>> channelFactory, ActivityTagsCollection tags) : base(channelFactory, tags) =>
		SubscriptionId = subscriptionId;

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
