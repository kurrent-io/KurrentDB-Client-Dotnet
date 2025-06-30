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
}
