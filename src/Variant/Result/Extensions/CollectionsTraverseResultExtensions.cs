namespace Kurrent;

/// <summary>
/// Provides extension methods for traversing collections and transforming elements to Results,
/// combining mapping and sequencing operations for functional composition.
/// </summary>
[PublicAPI]
public static class CollectionsTraverseResultExtensions {
    /// <summary>
    /// Projects each element of a sequence to a <see cref="Result{TValue,TError}"/> and collects them into a single <see cref="Result{TValue,TError}"/>.
    /// </summary>
    /// <remarks>
    /// This is a combination of `Select` and `Sequence`. It maps each element to a result and then sequences the results.
    /// If any mapping operation results in an error, the entire operation short-circuits and returns the first error.
    /// </remarks>
    /// <typeparam name="T">The type of the elements in the source collection.</typeparam>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="source">The source collection to traverse.</param>
    /// <param name="map">A function to transform each element into a <see cref="Result{TValue,TError}"/>.</param>
    /// <returns>A single <see cref="Result{TValue,TError}"/> containing either a collection of all success values or the first error.</returns>
    public static Result<IEnumerable<TValue>, TError> Traverse<T, TValue, TError>(this IEnumerable<T> source, Func<T, Result<TValue, TError>> map) where TError : notnull =>
        source.Select(map).Sequence();

    /// <summary>
    /// Asynchronously projects each element of a sequence to a <see cref="Result{TValue,TError}"/> and collects them into a single <see cref="Result{TValue,TError}"/>.
    /// </summary>
    /// <remarks>
    /// This method initiates all mapping operations in parallel and then awaits their completion.
    /// If any mapping operation results in an error, the entire operation will short-circuit and return the first error.
    /// </remarks>
    /// <typeparam name="T">The type of the elements in the source collection.</typeparam>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="source">The source collection to traverse.</param>
    /// <param name="map">An asynchronous function to transform each element into a <see cref="Result{TValue,TError}"/>.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, with a result containing either a collection of all success values or the first error.</returns>
    public static async ValueTask<Result<IEnumerable<TValue>, TError>> TraverseAsync<T, TValue, TError>(this IEnumerable<T> source, Func<T, ValueTask<Result<TValue, TError>>> map) where TError : notnull =>
        await source.Select(map).SequenceAsync().ConfigureAwait(false);

    /// <summary>
    /// Asynchronously projects each element of an <see cref="IAsyncEnumerable{TSource}"/> sequence to a <see cref="Result{TNext,TError}"/>
    /// using a synchronous selector function, and collects the success values into a read-only list.
    /// If any projection results in an error, the operation short-circuits and returns that error.
    /// This method processes the <see cref="IAsyncEnumerable{TSource}"/> sequentially.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TNext">The type of the success value of the <see cref="Result{TNext,TError}"/> returned by <paramref name="selector"/>.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="source">An asynchronous sequence of values to project.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>
    /// A <see cref="ValueTask{Result}"/> which, upon completion, yields:
    /// - A <see cref="Result{TValue,TError}"/> containing an <see cref="IReadOnlyList{TNext}"/> of the transformed success values, if all transformations were successful.
    /// - The first <see cref="Result{TValue,TError}"/> that was an error from the selector, if any transformation failed.
    /// </returns>
    public static async ValueTask<Result<IReadOnlyList<TNext>, TError>> TraverseAsync<TSource, TNext, TError>(
        this IAsyncEnumerable<TSource> source, Func<TSource, Result<TNext, TError>> selector)
        where TNext : notnull where TError : notnull {
        var successValues = new List<TNext>();
        await foreach (var item in source.ConfigureAwait(false)) {
            var result = selector(item);
            if (result.IsFailure)
                return Result.Failure<IReadOnlyList<TNext>, TError>(result.Error);
            successValues.Add(result.Value);
        }
        return Result.Success<IReadOnlyList<TNext>, TError>(successValues);
    }

    /// <summary>
    /// Asynchronously projects each element of an <see cref="IAsyncEnumerable{TSource}"/> sequence to a <see cref="Result{TNext,TError}"/>
    /// using an asynchronous selector function, and collects the success values into a read-only list.
    /// If any projection results in an error, the operation short-circuits and returns that error.
    /// This method processes the <see cref="IAsyncEnumerable{TSource}"/> sequentially.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TNext">The type of the success value of the <see cref="Result{TNext,TError}"/> returned by <paramref name="selector"/>.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="source">An asynchronous sequence of values to project.</param>
    /// <param name="selector">An asynchronous transform function to apply to each element.</param>
    /// <returns>
    /// A <see cref="ValueTask{Result}"/> which, upon completion, yields:
    /// - A <see cref="Result{TValue,TError}"/> containing an <see cref="IReadOnlyList{TNext}"/> of the transformed success values, if all transformations were successful.
    /// - The first <see cref="Result{TValue,TError}"/> that was an error from the selector, if any transformation failed.
    /// </returns>
    public static async ValueTask<Result<IReadOnlyList<TNext>, TError>> TraverseAsync<TSource, TNext, TError>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, ValueTask<Result<TNext, TError>>> selector)
        where TNext : notnull where TError : notnull {
        var successValues = new List<TNext>();
        await foreach (var item in source.ConfigureAwait(false)) {
            var result = await selector(item).ConfigureAwait(false);
            if (result.IsFailure)
                return Result.Failure<IReadOnlyList<TNext>, TError>(result.Error);
            successValues.Add(result.Value);
        }
        return Result.Success<IReadOnlyList<TNext>, TError>(successValues);
    }

    /// <summary>
    /// Asynchronously projects each element of an <see cref="IAsyncEnumerable{TSource}"/> sequence to a <see cref="Result{TNext,TError}"/>
    /// using a synchronous selector function that also accepts a state parameter, and collects the success values into a read-only list.
    /// If any projection results in an error, the operation short-circuits and returns that error.
    /// This method processes the <see cref="IAsyncEnumerable{TSource}"/> sequentially.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TNext">The type of the success value of the <see cref="Result{TNext,TError}"/> returned by <paramref name="selector"/>.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the selector.</typeparam>
    /// <param name="source">An asynchronous sequence of values to project.</param>
    /// <param name="selector">A transform function to apply to each element, accepting state.</param>
    /// <param name="state">The state to pass to the selector function.</param>
    /// <returns>
    /// A <see cref="ValueTask{Result}"/> which, upon completion, yields:
    /// - A <see cref="Result{TValue,TError}"/> containing an <see cref="IReadOnlyList{TNext}"/> of the transformed success values, if all transformations were successful.
    /// - The first <see cref="Result{TValue,TError}"/> that was an error from the selector, if any transformation failed.
    /// </returns>
    public static async ValueTask<Result<IReadOnlyList<TNext>, TError>> TraverseAsync<TSource, TNext, TError, TState>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, TState, Result<TNext, TError>> selector,
        TState state)
        where TNext : notnull where TError : notnull {
        var successValues = new List<TNext>();
        await foreach (var item in source.ConfigureAwait(false)) {
            var result = selector(item, state);
            if (result.IsFailure)
                return Result.Failure<IReadOnlyList<TNext>, TError>(result.Error);
            successValues.Add(result.Value);
        }
        return Result.Success<IReadOnlyList<TNext>, TError>(successValues);
    }

    /// <summary>
    /// Asynchronously projects each element of an <see cref="IAsyncEnumerable{TSource}"/> sequence to a <see cref="Result{TNext,TError}"/>
    /// using an asynchronous selector function that also accepts a state parameter, and collects the success values into a read-only list.
    /// If any projection results in an error, the operation short-circuits and returns that error.
    /// This method processes the <see cref="IAsyncEnumerable{TSource}"/> sequentially.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TNext">The type of the success value of the <see cref="Result{TNext,TError}"/> returned by <paramref name="selector"/>.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the selector.</typeparam>
    /// <param name="source">An asynchronous sequence of values to project.</param>
    /// <param name="selector">An asynchronous transform function to apply to each element, accepting state.</param>
    /// <param name="state">The state to pass to the selector function.</param>
    /// <returns>
    /// A <see cref="ValueTask{Result}"/> which, upon completion, yields:
    /// - A <see cref="Result{TValue,TError}"/> containing an <see cref="IReadOnlyList{TNext}"/> of the transformed success values, if all transformations were successful.
    /// - The first <see cref="Result{TValue,TError}"/> that was an error from the selector, if any transformation failed.
    /// </returns>
    public static async ValueTask<Result<IReadOnlyList<TNext>, TError>> TraverseAsync<TSource, TNext, TError, TState>(
        this IAsyncEnumerable<TSource> source, Func<TSource, TState, ValueTask<Result<TNext, TError>>> selector, TState state)
        where TNext : notnull where TError : notnull {
        var successValues = new List<TNext>();
        await foreach (var item in source.ConfigureAwait(false)) {
            var result = await selector(item, state).ConfigureAwait(false);
            if (result.IsFailure)
                return Result.Failure<IReadOnlyList<TNext>, TError>(result.Error);
            successValues.Add(result.Value);
        }
        return Result.Success<IReadOnlyList<TNext>, TError>(successValues);
    }
}
