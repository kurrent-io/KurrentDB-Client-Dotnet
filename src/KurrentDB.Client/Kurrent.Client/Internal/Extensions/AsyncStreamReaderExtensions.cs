using System.Runtime.CompilerServices;
using Grpc.Core;

namespace KurrentDB.Client;

static class AsyncStreamReaderExtensions {
	public static async IAsyncEnumerable<T> ReadAllAsync<T>(this IAsyncStreamReader<T> reader, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
		while (await reader.MoveNext(cancellationToken).ConfigureAwait(false))
			yield return reader.Current;
	}
}
