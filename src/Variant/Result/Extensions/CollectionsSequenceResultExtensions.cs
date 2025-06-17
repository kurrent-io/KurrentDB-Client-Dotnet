namespace Kurrent;

/// <summary>
/// Provides extension methods for working with collections of <see cref="Result{TValue,TError}"/>.
/// </summary>
[PublicAPI]
public static class CollectionsSequenceResultExtensions {
    /// <summary>
    /// Transforms a collection of <see cref="Result{TValue,TError}"/> into a single <see cref="Result{TValue,TError}"/> containing a collection of success values.
    /// </summary>
    /// <remarks>
    /// This method follows a fail-fast approach. If any result in the input collection is an <see cref="Result{TValue,TError}.IsFailure"/>,
    /// the entire operation will short-circuit and return the first encountered error.
    /// If all results are successful, it returns a single success result containing an enumeration of all the success values.
    /// </remarks>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="results">The collection of results to sequence.</param>
    /// <returns>
    /// A <see cref="Result{TValue,TError}"/> containing either a collection of all success values or the first error encountered.
    /// </returns>
    public static Result<IEnumerable<TValue>, TError> Sequence<TValue, TError>(this IEnumerable<Result<TValue, TError>> results) where TError : notnull {
        var successes = new List<TValue>();
        foreach (var result in results) {
            if (result.IsFailure) return result.Error;
            successes.Add(result.Value);
        }
        return Kurrent.Result.Success<IEnumerable<TValue>, TError>(successes);
    }

    /// <summary>
    /// Transforms a collection of <see cref="Result{TValue,TError}"/> into a single <see cref="Result{TValue,TError}"/> containing an array of success values.
    /// </summary>
    /// <remarks>
    /// This method follows a fail-fast approach. If any result in the input collection is an <see cref="Result{TValue,TError}.IsFailure"/>,
    /// the entire operation will short-circuit and return the first encountered error.
    /// If all results are successful, it returns a single success result containing an array of all the success values.
    /// </remarks>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="results">The collection of results to sequence.</param>
    /// <returns>
    /// A <see cref="Result{TValue,TError}"/> containing either an array of all success values or the first error encountered.
    /// </returns>
    public static Result<TValue[], TError> SequenceToArray<TValue, TError>(this IEnumerable<Result<TValue, TError>> results) where TError : notnull {
        var successes = new List<TValue>();
        foreach (var result in results) {
            if (result.IsFailure) return result.Error;
            successes.Add(result.Value);
        }
        return Kurrent.Result.Success<TValue[], TError>(successes.ToArray());
    }

    /// <summary>
    /// Asynchronously transforms a collection of <see cref="ValueTask{Result{TValue,TError}}"/> into a single <see cref="Result{TValue,TError}"/> containing a collection of success values.
    /// </summary>
    /// <remarks>
    /// This method awaits all tasks in parallel and then sequences the results. If any task results in an error,
    /// the final result will be the first error encountered among the completed tasks.
    /// </remarks>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="tasks">The collection of result-producing tasks to sequence.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, with a result containing either a collection of all success values or the first error.</returns>
    public static async ValueTask<Result<IEnumerable<TValue>, TError>> SequenceAsync<TValue, TError>(this IEnumerable<ValueTask<Result<TValue, TError>>> tasks) where TError : notnull {
        var results = await Task.WhenAll(tasks.Select(t => t.AsTask())).ConfigureAwait(false);
        return results.Sequence();
    }

    /// <summary>
    /// Evaluates an asynchronous sequence of <see cref="Result{TValue,TError}"/> instances.
    /// If all results in the sequence are successes, returns a <see cref="Result{TValue,TError}"/> containing a read-only list of all success values.
    /// If any result in the sequence is an error, returns the first encountered error.
    /// This method processes the <see cref="IAsyncEnumerable{T}"/> sequentially.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="source">The asynchronous sequence of results to evaluate.</param>
    /// <returns>
    /// A <see cref="ValueTask{Result}"/> which, upon completion, yields:
    /// - A <see cref="Result{TValue,TError}"/> containing an <see cref="IReadOnlyList{TValue}"/> of all success values, if all results in the sequence were successes.
    /// - The first <see cref="Result{TValue,TError}"/> that was an error, if any error was encountered.
    /// </returns>
    public static async ValueTask<Result<IReadOnlyList<TValue>, TError>> SequenceAsync<TValue, TError>(this IAsyncEnumerable<Result<TValue, TError>> source)
        where TValue : notnull where TError : notnull {
        var successValues = new List<TValue>();
        await foreach (var result in source.ConfigureAwait(false)) {
            if (result.IsFailure)
                return Kurrent.Result.Failure<IReadOnlyList<TValue>, TError>(result.Error);
            successValues.Add(result.Value);
        }
        return Kurrent.Result.Success<IReadOnlyList<TValue>, TError>(successValues);
    }
}
