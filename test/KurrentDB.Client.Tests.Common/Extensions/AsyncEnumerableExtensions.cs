// ReSharper disable once CheckNamespace
namespace KurrentDB.Client.Tests;

public static class AsyncEnumerableExtensions {
	public static async Task ForEachAwaitAsync<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Task> action, CancellationToken cancellationToken = default) {
		await foreach (var element in source.WithCancellation(cancellationToken)) {
			await action(element);
		}
	}
}
