namespace Kurrent.Client;

/// <summary>
/// Provides extension methods for working with collections of <see cref="Result{TSuccess,TError}"/>.
/// </summary>
public static class ResultCollectionsExtensions {
    /// <summary>
    /// Transforms a collection of <see cref="Result{TSuccess,TError}"/> into a single <see cref="Result{TSuccess,TError}"/> containing a collection of success values.
    /// </summary>
    /// <remarks>
    /// This method follows a fail-fast approach. If any result in the input collection is an <see cref="Result{TSuccess,TError}.IsError"/>,
    /// the entire operation will short-circuit and return the first encountered error.
    /// If all results are successful, it returns a single success result containing an enumeration of all the success values.
    /// </remarks>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="results">The collection of results to sequence.</param>
    /// <returns>
    /// A <see cref="Result{TSuccess,TError}"/> containing either a collection of all success values or the first error encountered.
    /// </returns>
    public static Result<IEnumerable<TSuccess>, TError> Sequence<TSuccess, TError>(this IEnumerable<Result<TSuccess, TError>> results) {
        var successes = new List<TSuccess>();
        foreach (var result in results) {
            if (result.IsError) return result.AsError;
            successes.Add(result.AsSuccess);
        }

        return Result<IEnumerable<TSuccess>, TError>.Success(successes);
    }

    /// <summary>
    /// Transforms a collection of <see cref="Result{TSuccess,TError}"/> into a single <see cref="Result{TSuccess,TError}"/> containing an array of success values.
    /// </summary>
    /// <remarks>
    /// This method follows a fail-fast approach. If any result in the input collection is an <see cref="Result{TSuccess,TError}.IsError"/>,
    /// the entire operation will short-circuit and return the first encountered error.
    /// If all results are successful, it returns a single success result containing an array of all the success values.
    /// </remarks>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="results">The collection of results to sequence.</param>
    /// <returns>
    /// A <see cref="Result{TSuccess,TError}"/> containing either an array of all success values or the first error encountered.
    /// </returns>
    public static Result<TSuccess[], TError> SequenceToArray<TSuccess, TError>(this IEnumerable<Result<TSuccess, TError>> results) {
        var successes = new List<TSuccess>();
        foreach (var result in results) {
            if (result.IsError) return result.AsError;
            successes.Add(result.AsSuccess);
        }

        return Result<TSuccess[], TError>.Success(successes.ToArray());
    }

    /// <summary>
    /// Projects each element of a sequence to a <see cref="Result{TSuccess,TError}"/> and collects them into a single <see cref="Result{TSuccess,TError}"/>.
    /// </summary>
    /// <remarks>
    /// This is a combination of `Select` and `Sequence`. It maps each element to a result and then sequences the results.
    /// If any mapping operation results in an error, the entire operation short-circuits and returns the first error.
    /// </remarks>
    /// <typeparam name="T">The type of the elements in the source collection.</typeparam>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="source">The source collection to traverse.</param>
    /// <param name="map">A function to transform each element into a <see cref="Result{TSuccess,TError}"/>.</param>
    /// <returns>A single <see cref="Result{TSuccess,TError}"/> containing either a collection of all success values or the first error.</returns>
    public static Result<IEnumerable<TSuccess>, TError> Traverse<T, TSuccess, TError>(this IEnumerable<T> source, Func<T, Result<TSuccess, TError>> map) {
        return source.Select(map).Sequence();
    }

    /// <summary>
    /// Asynchronously transforms a collection of <see cref="ValueTask{Result{TSuccess,TError}}"/> into a single <see cref="Result{TSuccess,TError}"/> containing a collection of success values.
    /// </summary>
    /// <remarks>
    /// This method awaits all tasks in parallel and then sequences the results. If any task results in an error,
    /// the final result will be the first error encountered among the completed tasks.
    /// </remarks>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="tasks">The collection of result-producing tasks to sequence.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, with a result containing either a collection of all success values or the first error.</returns>
    public static async ValueTask<Result<IEnumerable<TSuccess>, TError>> SequenceAsync<TSuccess, TError>(this IEnumerable<ValueTask<Result<TSuccess, TError>>> tasks) {
        var results = await Task.WhenAll(tasks.Select(t => t.AsTask())).ConfigureAwait(false);
        return results.Sequence();
    }

    /// <summary>
    /// Asynchronously projects each element of a sequence to a <see cref="Result{TSuccess,TError}"/> and collects them into a single <see cref="Result{TSuccess,TError}"/>.
    /// </summary>
    /// <remarks>
    /// This method initiates all mapping operations in parallel and then awaits their completion.
    /// If any mapping operation results in an error, the entire operation will short-circuit and return the first error.
    /// </remarks>
    /// <typeparam name="T">The type of the elements in the source collection.</typeparam>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="source">The source collection to traverse.</param>
    /// <param name="map">An asynchronous function to transform each element into a <see cref="Result{TSuccess,TError}"/>.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, with a result containing either a collection of all success values or the first error.</returns>
    public static async ValueTask<Result<IEnumerable<TSuccess>, TError>> TraverseAsync<T, TSuccess, TError>(this IEnumerable<T> source, Func<T, ValueTask<Result<TSuccess, TError>>> map) {
        var tasks = source.Select(map);
        return await tasks.SequenceAsync().ConfigureAwait(false);
    }
}
