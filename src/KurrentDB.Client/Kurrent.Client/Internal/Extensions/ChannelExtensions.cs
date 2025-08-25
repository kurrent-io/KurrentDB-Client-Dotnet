using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Kurrent.Client;

static class ChannelExtensions {
    /// <summary>
    /// Reads data from the channel in batches of a specified size up to a given timeout period.
    /// </summary>
    /// <typeparam name="T">The type of data in the channel.</typeparam>
    /// <param name="reader">The reader to read data from the channel.</param>
    /// <param name="batchSize">The maximum number of items in a single batch.</param>
    /// <param name="timeout">The maximum duration to wait while gathering a batch.</param>
    /// <param name="cancellationToken">A token to observe for cancellation of the batch processing.</param>
    public static async IAsyncEnumerable<List<T>> ReadBatches<T>(
        this ChannelReader<T> reader, int batchSize, TimeSpan timeout,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        var buffer = new List<T>(batchSize);

        while (!cancellationToken.IsCancellationRequested) {
            var timerTask = Task.Delay(timeout, cancellationToken);

            while (buffer.Count < batchSize) {
                var readSignal    = reader.WaitToReadAsync(cancellationToken).AsTask();
                var completedTask = await Task.WhenAny(readSignal, timerTask);

                if (completedTask == readSignal && !readSignal.IsCanceled && await readSignal) {
                    while (buffer.Count < batchSize && reader.TryRead(out var item))
                        buffer.Add(item);
                }
                else break;
            }

            if (buffer.Count > 0) {
                yield return [..buffer];
                buffer.Clear();
            }

            if (reader.Completion.IsCompleted) yield break;
        }
    }

    // just having fun
    static async IAsyncEnumerable<T[]> ReadBatchesOptimized<T>(this ChannelReader<T> reader, int batchSize, TimeSpan timeout, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        var pool   = ArrayPool<T>.Shared;
        var buffer = pool.Rent(batchSize);

        try {
            while (!cancellationToken.IsCancellationRequested) {
                var currentIndex = 0;

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                cts.CancelAfter(timeout);

                while (currentIndex < batchSize && !cts.Token.IsCancellationRequested) {
                    try {
                        if (await reader.WaitToReadAsync(cts.Token))
                            while (currentIndex < batchSize && reader.TryRead(out var item))
                                buffer[currentIndex++] = item;
                    }
                    catch (OperationCanceledException) when (cts.Token.IsCancellationRequested) {
                        break; // Timeout occurred
                    }
                }

                if (currentIndex > 0) {
                    var result = new T[currentIndex];
                    Array.Copy(buffer, 0, result, 0, currentIndex);
                    yield return result;
                }

                if (reader.Completion.IsCompleted)
                    yield break;
            }
        }
        finally {
            pool.Return(buffer, clearArray: true);
        }
    }
}
