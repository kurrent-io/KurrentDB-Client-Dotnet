using System.Runtime.ExceptionServices;

namespace Kurrent;

public static partial class Result {
    /// <summary>
    /// Attempts to execute the specified function and returns a result indicating success or failure.
    /// If the function throws an exception, it is caught and transformed into an error using the provided error handler.
    /// </summary>
    /// <typeparam name="TValue">The type of the successful result.</typeparam>
    /// <typeparam name="TError">The type of the error result.</typeparam>
    /// <param name="operation">The function to execute, returning a result of type <typeparamref name="TValue"/>.</param>
    /// <param name="onError">A function to handle any exception thrown by <paramref name="operation"/>, returning an error of type <typeparamref name="TError"/>.</param>
    /// <returns>A result object representing either the success or the error of the executed function.</returns>
    public static Result<TValue, TError> Try<TValue, TError>(Func<TValue> operation, Func<Exception, TError> onError) where TValue : notnull where TError : notnull {
        try {
            return Success<TValue, TError>(operation());
        }
        catch (Exception ex) {
            var exInfo = ExceptionDispatchInfo.Capture(ex);
            return Failure<TValue, TError>(onError(exInfo.SourceException));
        }
    }

    public static Result<TValue, Exception> Try<TValue>(Func<TValue> operation) where TValue : notnull {
        try {
            return Success<TValue, Exception>(operation());
        }
        catch (Exception ex) {
            var exInfo = ExceptionDispatchInfo.Capture(ex);
            return Failure<TValue, Exception>(exInfo.SourceException);
        }
    }

    /// <summary>
    /// Attempts to execute the provided action and returns a result indicating success or failure.
    /// </summary>
    /// <typeparam name="TError">The type of the error result.</typeparam>
    /// <param name="operation">The action to execute.</param>
    /// <param name="onError">A function to handle any exception thrown by <paramref name="operation"/>, returning an error of type <typeparamref name="TError"/>.</param>
    /// <returns>A result object representing either the success or the error of the executed action.</returns>
    public static Result<Void, TError> Try<TError>(Action operation, Func<Exception, TError> onError) where TError : notnull {
        try {
            operation();
            return Success<Void, TError>(Void.Value);
        }
        catch (Exception ex) {
            var exInfo = ExceptionDispatchInfo.Capture(ex);
            return Failure<Void, TError>(onError(exInfo.SourceException));
        }
    }

    public static Result<Void, Exception> Try(Action operation) {
        try {
            operation();
            return Success<Void, Exception>(Void.Value);
        }
        catch (Exception ex) {
            var exInfo = ExceptionDispatchInfo.Capture(ex);
            return Failure<Void, Exception>(exInfo.SourceException);
        }
    }

    /// <summary>
    /// Attempts to execute the provided asynchronous function and returns a <see cref="Result{TSuccess, TError}"/>
    /// indicating whether the function executed successfully or encountered an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value returned on successful execution.</typeparam>
    /// <typeparam name="TError">The type of the error returned if execution fails.</typeparam>
    /// <param name="operation">The asynchronous function to execute whose result will be captured if successful.</param>
    /// <param name="onError">A function that accepts an exception and transforms it into an error object of type <typeparamref name="TError"/>.</param>
    /// <returns>
    /// A <see cref="Result{TSuccess, TError}"/> instance representing the outcome of the function execution.
    /// If the operation succeeds, the result contains the success value.
    /// If the operation fails, the result contains the mapped error.
    /// </returns>
    public static async ValueTask<Result<TValue, TError>> TryAsync<TValue, TError>(Func<ValueTask<TValue>> operation, Func<Exception, TError> onError) where TValue : notnull where TError : notnull {
        try {
            var result = await operation().ConfigureAwait(false);
            return Success<TValue, TError>(result);
        }
        catch (Exception ex) {
            var exInfo = ExceptionDispatchInfo.Capture(ex);
            return Failure<TValue, TError>(onError(exInfo.SourceException));
        }
    }

    public static async ValueTask<Result<TValue, Exception>> TryAsync<TValue>(Func<ValueTask<TValue>> operation) where TValue : notnull {
        try {
            var result = await operation().ConfigureAwait(false);
            return Success<TValue, Exception>(result);
        }
        catch (Exception ex) {
            var exInfo = ExceptionDispatchInfo.Capture(ex);
            return Failure<TValue, Exception>(exInfo.SourceException);
        }
    }

    /// <summary>
    /// Attempts to execute the provided asynchronous action and returns a result indicating success or failure.
    /// </summary>
    /// <typeparam name="TError">The type of the error result.</typeparam>
    /// <param name="operation">The asynchronous action to execute.</param>
    /// <param name="onError">A function to handle any exception thrown by <paramref name="operation"/>, returning an error of type <typeparamref name="TError"/>.</param>
    /// <returns>A result object representing either the success or the error of the executed action.</returns>
    public static async ValueTask<Result<Void, TError>> TryAsync<TError>(Func<ValueTask> operation, Func<Exception, TError> onError) where TError : notnull {
        try {
            await operation().ConfigureAwait(false);
            return Success<Void, TError>(Void.Value);
        }
        catch (Exception ex) {
            var exInfo = ExceptionDispatchInfo.Capture(ex);
            return Failure<Void, TError>(onError(exInfo.SourceException));
        }
    }

    public static async ValueTask<Result<Void, Exception>> TryAsync(Func<ValueTask> operation) {
        try {
            await operation().ConfigureAwait(false);
            return Success<Void, Exception>(Void.Value);
        }
        catch (Exception ex) {
            var exInfo = ExceptionDispatchInfo.Capture(ex);
            return Failure<Void, Exception>(exInfo.SourceException);
        }
    }
}
