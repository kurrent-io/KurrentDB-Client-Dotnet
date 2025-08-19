using System.Runtime.CompilerServices;
using Grpc.Core;

namespace Kurrent.Grpc;

public static class AsyncStreamReaderExtensions {
	public static async IAsyncEnumerable<T> ReadAllAsync<T>(this IAsyncStreamReader<T> reader, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
		while (await reader.MoveNext(cancellationToken).ConfigureAwait(false))
			yield return reader.Current;
	}

    public static ValueTask<T?> FirstOrDefaultAsync<T>(this IAsyncStreamReader<T> reader, CancellationToken cancellationToken = default) =>
        reader.ReadAllAsync(cancellationToken).FirstOrDefaultAsync(cancellationToken);

    public static ValueTask<List<T>> ToListAsync<T>(this IAsyncStreamReader<T> reader, CancellationToken cancellationToken = default) =>
        reader.ReadAllAsync(cancellationToken).ToListAsync(cancellationToken);

    public static ValueTask<int> CountAsync<T>(this IAsyncStreamReader<T> reader, CancellationToken cancellationToken = default) =>
        reader.ReadAllAsync(cancellationToken).CountAsync(cancellationToken);

    public static ValueTask<bool> AnyAsync<T>(this IAsyncStreamReader<T> reader, CancellationToken cancellationToken = default) =>
        reader.ReadAllAsync(cancellationToken).AnyAsync(cancellationToken);
}
