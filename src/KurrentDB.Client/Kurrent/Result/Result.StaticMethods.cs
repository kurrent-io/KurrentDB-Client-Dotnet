namespace Kurrent;

[PublicAPI]
public static partial class Result {
    #region . Success and Failure Methods .

    /// <summary>
    /// Creates a new <see cref="Result{TSuccess,TError}"/> representing a successful operation with the specified value.
    /// </summary>
    public static Result<TSuccess, TError> Success<TSuccess, TError>(TSuccess value) where TSuccess : notnull =>
        Result<TSuccess, TError>.Success(value);

    /// <summary>
    /// Creates a new <see cref="Result{TSuccess,TError}"/> representing a failed operation with the specified error.
    /// </summary>
    public static Result<TSuccess, TError> Failure<TSuccess, TError>(TError error) where TError : notnull =>
        Result<TSuccess, TError>.Error(error);

    #endregion

    #region . Try Methods .

    /// <summary>
    /// Attempts to execute the specified function and returns a result indicating success or failure.
    /// If the function throws an exception, it is caught and transformed into an error using the provided error handler.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the successful result.</typeparam>
    /// <typeparam name="TError">The type of the error result.</typeparam>
    /// <param name="func">The function to execute, returning a result of type <typeparamref name="TSuccess"/>.</param>
    /// <param name="errorHandler">A function to handle any exception thrown by <paramref name="func"/>, returning an error of type <typeparamref name="TError"/>.</param>
    /// <returns>A result object representing either the success or the error of the executed function.</returns>
    public static Result<TSuccess, TError> Try<TSuccess, TError>(Func<TSuccess> func, Func<Exception, TError> errorHandler) where TSuccess : notnull where TError : notnull {
        try {
            return Success<TSuccess, TError>(func());
        }
        catch (Exception ex) {
            return Failure<TSuccess, TError>(errorHandler(ex));
        }
    }

    public static Result<TSuccess, Exception> Try<TSuccess>(Func<TSuccess> func) where TSuccess : notnull {
        try {
            return Success<TSuccess, Exception>(func());
        }
        catch (Exception ex) {
            return Failure<TSuccess, Exception>(ex);
        }
    }

    /// <summary>
    /// Attempts to execute the provided action and returns a result indicating success or failure.
    /// </summary>
    /// <typeparam name="TError">The type of the error result.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <param name="errorHandler">A function to handle any exception thrown by <paramref name="action"/>, returning an error of type <typeparamref name="TError"/>.</param>
    /// <returns>A result object representing either the success or the error of the executed action.</returns>
    public static Result<Unit, TError> Try<TError>(Action action, Func<Exception, TError> errorHandler) where TError : notnull {
        try {
            action();
            return Success<Unit, TError>(Unit.Value);
        }
        catch (Exception ex) {
            return Failure<Unit, TError>(errorHandler(ex));
        }
    }

    /// <summary>
    /// Attempts to execute the provided asynchronous function and returns a <see cref="Result{TSuccess, TError}"/>
    /// indicating whether the function executed successfully or encountered an error.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value returned on successful execution.</typeparam>
    /// <typeparam name="TError">The type of the error returned if execution fails.</typeparam>
    /// <param name="func">The asynchronous function to execute whose result will be captured if successful.</param>
    /// <param name="errorHandler">A function that accepts an exception and transforms it into an error object of type <typeparamref name="TError"/>.</param>
    /// <returns>
    /// A <see cref="Result{TSuccess, TError}"/> instance representing the outcome of the function execution.
    /// If the operation succeeds, the result contains the success value.
    /// If the operation fails, the result contains the mapped error.
    /// </returns>
    public static async Task<Result<TSuccess, TError>> Try<TSuccess, TError>(Func<Task<TSuccess>> func, Func<Exception, TError> errorHandler) where TSuccess : notnull where TError : notnull {
        try {
            var result = await func().ConfigureAwait(false);
            return Success<TSuccess, TError>(result);
        }
        catch (Exception ex) {
            return Failure<TSuccess, TError>(errorHandler(ex));
        }
    }

    /// <summary>
    /// Attempts to execute the provided asynchronous function and returns a <see cref="Result{TSuccess, TError}"/>
    /// indicating whether the function executed successfully or encountered an error.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value returned on successful execution.</typeparam>
    /// <typeparam name="TError">The type of the error returned if execution fails.</typeparam>
    /// <param name="func">The asynchronous function to execute whose result will be captured if successful.</param>
    /// <param name="errorHandler">A function that accepts an exception and transforms it into an error object of type <typeparamref name="TError"/>.</param>
    /// <returns>
    /// A <see cref="Result{TSuccess, TError}"/> instance representing the outcome of the function execution.
    /// If the operation succeeds, the result contains the success value.
    /// If the operation fails, the result contains the mapped error.
    /// </returns>
    public static async ValueTask<Result<TSuccess, TError>> Try<TSuccess, TError>(Func<ValueTask<TSuccess>> func, Func<Exception, TError> errorHandler) where TSuccess : notnull where TError : notnull {
        try {
            var result = await func().ConfigureAwait(false);
            return Success<TSuccess, TError>(result);
        }
        catch (Exception ex) {
            return Failure<TSuccess, TError>(errorHandler(ex));
        }
    }

    /// <summary>
    /// Attempts to execute the provided asynchronous action and returns a result indicating success or failure.
    /// </summary>
    /// <typeparam name="TError">The type of the error result.</typeparam>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <param name="errorHandler">A function to handle any exception thrown by <paramref name="action"/>, returning an error of type <typeparamref name="TError"/>.</param>
    /// <returns>A result object representing either the success or the error of the executed action.</returns>
    public static async ValueTask<Result<Unit, TError>> Try<TError>(Func<ValueTask> action, Func<Exception, TError> errorHandler) where TError : notnull {
        try {
            await action().ConfigureAwait(false);
            return Success<Unit, TError>(Unit.Value);
        }
        catch (Exception ex) {
            return Failure<Unit, TError>(errorHandler(ex));
        }
    }

    #endregion
}
