namespace Kurrent;

/// <summary>
/// Provides extension methods for Result types where the error type implements IResultError,
/// enabling automatic exception creation and throwing capabilities.
/// </summary>
[PublicAPI]
public static class IResultErrorExtensions {
    #region . sync .

    /// <summary>
    /// Throws an exception if the result represents an error; otherwise, returns the success value.
    /// Uses the error's CreateException() method to generate the appropriate exception type.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value that implements IResultError.</typeparam>
    /// <param name="result">The result instance to check for success or error.</param>
    /// <returns>The success value if the result represents success.</returns>
    /// <exception cref="Exception">Thrown when the result represents an error, using the error's CreateException() method.</exception>
    public static TValue ThrowOnError<TValue, TError>(this Result<TValue, TError> result)
        where TError : IResultError where TValue : notnull =>
        result.IsFailure ? throw result.Error.CreateException() : result.Value;

    /// <summary>
    /// Executes the specified action on the success value if the result is a success, or throws an exception if it's an error.
    /// This method is designed for side effects when you only care about the success case.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value that implements IResultError.</typeparam>
    /// <param name="result">The result instance to handle.</param>
    /// <param name="onSuccess">The action to perform on the success value.</param>
    /// <exception cref="Exception">Thrown when the result represents an error, using the error's CreateException() method.</exception>
    public static void SwitchOrThrow<TValue, TError>(this Result<TValue, TError> result, Action<TValue> onSuccess)
        where TError : IResultError where TValue : notnull {
        if (result.IsSuccess) {
            onSuccess(result.Value);
        } else {
            throw result.Error.CreateException();
        }
    }

    /// <summary>
    /// Executes the specified action on the success value with additional state if the result is a success, or throws an exception if it's an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value that implements IResultError.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="result">The result instance to handle.</param>
    /// <param name="onSuccess">The action to perform on the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <exception cref="Exception">Thrown when the result represents an error, using the error's CreateException() method.</exception>
    public static void SwitchOrThrow<TValue, TError, TState>(this Result<TValue, TError> result, Action<TValue, TState> onSuccess, TState state)
        where TError : IResultError where TValue : notnull {
        if (result.IsSuccess) {
            onSuccess(result.Value, state);
        } else {
            throw result.Error.CreateException();
        }
    }

    /// <summary>
    /// Transforms the success value using the specified function if the result is a success, or throws an exception if it's an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value that implements IResultError.</typeparam>
    /// <typeparam name="TOut">The type of the transformed value.</typeparam>
    /// <param name="result">The result instance to handle.</param>
    /// <param name="onSuccess">The function to transform the success value.</param>
    /// <returns>The transformed success value.</returns>
    /// <exception cref="Exception">Thrown when the result represents an error, using the error's CreateException() method.</exception>
    public static TOut MatchOrThrow<TValue, TError, TOut>(this Result<TValue, TError> result, Func<TValue, TOut> onSuccess)
        where TError : IResultError where TValue : notnull =>
        result.IsSuccess ? onSuccess(result.Value) : throw result.Error.CreateException();

    /// <summary>
    /// Transforms the success value using the specified function with additional state if the result is a success, or throws an exception if it's an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value that implements IResultError.</typeparam>
    /// <typeparam name="TOut">The type of the transformed value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the function.</typeparam>
    /// <param name="result">The result instance to handle.</param>
    /// <param name="onSuccess">The function to transform the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the function.</param>
    /// <returns>The transformed success value.</returns>
    /// <exception cref="Exception">Thrown when the result represents an error, using the error's CreateException() method.</exception>
    public static TOut MatchOrThrow<TValue, TError, TOut, TState>(this Result<TValue, TError> result, Func<TValue, TState, TOut> onSuccess, TState state)
        where TError : IResultError where TValue : notnull =>
        result.IsSuccess ? onSuccess(result.Value, state) : throw result.Error.CreateException();

    #endregion

    #region . async .

    /// <summary>
    /// Asynchronously throws an exception if the result represents an error; otherwise, returns the success value.
    /// Uses the error's CreateException() method to generate the appropriate exception type.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value that implements IResultError.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <returns>A ValueTask representing the asynchronous operation with the success value.</returns>
    /// <exception cref="Exception">Thrown when the result represents an error, using the error's CreateException() method.</exception>
    public static async ValueTask<TValue> ThrowOnErrorAsync<TValue, TError>(this ValueTask<Result<TValue, TError>> resultTask)
        where TError : IResultError where TValue : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.ThrowOnError();
    }

    /// <summary>
    /// Asynchronously executes the specified action on the success value if the result is a success, or throws an exception if it's an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value that implements IResultError.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The action to perform on the success value.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    /// <exception cref="Exception">Thrown when the result represents an error, using the error's CreateException() method.</exception>
    public static async ValueTask SwitchOrThrowAsync<TValue, TError>(this ValueTask<Result<TValue, TError>> resultTask, Action<TValue> onSuccess)
        where TError : IResultError where TValue : notnull {
        var result = await resultTask.ConfigureAwait(false);
        result.SwitchOrThrow(onSuccess);
    }

    /// <summary>
    /// Asynchronously executes the specified asynchronous action on the success value if the result is a success, or throws an exception if it's an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value that implements IResultError.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The asynchronous action to perform on the success value.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    /// <exception cref="Exception">Thrown when the result represents an error, using the error's CreateException() method.</exception>
    public static async ValueTask SwitchOrThrowAsync<TValue, TError>(this ValueTask<Result<TValue, TError>> resultTask, Func<TValue, ValueTask> onSuccess)
        where TError : IResultError where TValue : notnull {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess) {
            await onSuccess(result.Value).ConfigureAwait(false);
        } else {
            throw result.Error.CreateException();
        }
    }

    /// <summary>
    /// Asynchronously executes the specified action on the success value with additional state if the result is a success, or throws an exception if it's an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value that implements IResultError.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The action to perform on the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    /// <exception cref="Exception">Thrown when the result represents an error, using the error's CreateException() method.</exception>
    public static async ValueTask SwitchOrThrowAsync<TValue, TError, TState>(this ValueTask<Result<TValue, TError>> resultTask, Action<TValue, TState> onSuccess, TState state)
        where TError : IResultError where TValue : notnull {
        var result = await resultTask.ConfigureAwait(false);
        result.SwitchOrThrow(onSuccess, state);
    }

    /// <summary>
    /// Asynchronously executes the specified asynchronous action on the success value with additional state if the result is a success, or throws an exception if it's an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value that implements IResultError.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The asynchronous action to perform on the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    /// <exception cref="Exception">Thrown when the result represents an error, using the error's CreateException() method.</exception>
    public static async ValueTask SwitchOrThrowAsync<TValue, TError, TState>(this ValueTask<Result<TValue, TError>> resultTask, Func<TValue, TState, ValueTask> onSuccess, TState state)
        where TError : IResultError where TValue : notnull {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess) {
            await onSuccess(result.Value, state).ConfigureAwait(false);
        } else {
            throw result.Error.CreateException();
        }
    }

    /// <summary>
    /// Asynchronously transforms the success value using the specified function if the result is a success, or throws an exception if it's an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value that implements IResultError.</typeparam>
    /// <typeparam name="TOut">The type of the transformed value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The function to transform the success value.</param>
    /// <returns>A ValueTask representing the asynchronous operation with the transformed success value.</returns>
    /// <exception cref="Exception">Thrown when the result represents an error, using the error's CreateException() method.</exception>
    public static async ValueTask<TOut> MatchOrThrowAsync<TValue, TError, TOut>(this ValueTask<Result<TValue, TError>> resultTask, Func<TValue, TOut> onSuccess)
        where TError : IResultError where TValue : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.MatchOrThrow(onSuccess);
    }

    /// <summary>
    /// Asynchronously transforms the success value using the specified asynchronous function if the result is a success, or throws an exception if it's an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value that implements IResultError.</typeparam>
    /// <typeparam name="TOut">The type of the transformed value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The asynchronous function to transform the success value.</param>
    /// <returns>A ValueTask representing the asynchronous operation with the transformed success value.</returns>
    /// <exception cref="Exception">Thrown when the result represents an error, using the error's CreateException() method.</exception>
    public static async ValueTask<TOut> MatchOrThrowAsync<TValue, TError, TOut>(this ValueTask<Result<TValue, TError>> resultTask, Func<TValue, ValueTask<TOut>> onSuccess)
        where TError : IResultError where TValue : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? await onSuccess(result.Value).ConfigureAwait(false) : throw result.Error.CreateException();
    }

    /// <summary>
    /// Asynchronously transforms the success value using the specified function with additional state if the result is a success, or throws an exception if it's an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value that implements IResultError.</typeparam>
    /// <typeparam name="TOut">The type of the transformed value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the function.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The function to transform the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the function.</param>
    /// <returns>A ValueTask representing the asynchronous operation with the transformed success value.</returns>
    /// <exception cref="Exception">Thrown when the result represents an error, using the error's CreateException() method.</exception>
    public static async ValueTask<TOut> MatchOrThrowAsync<TValue, TError, TOut, TState>(this ValueTask<Result<TValue, TError>> resultTask, Func<TValue, TState, TOut> onSuccess, TState state)
        where TError : IResultError where TValue : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.MatchOrThrow(onSuccess, state);
    }

    /// <summary>
    /// Asynchronously transforms the success value using the specified asynchronous function with additional state if the result is a success, or throws an exception if it's an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value that implements IResultError.</typeparam>
    /// <typeparam name="TOut">The type of the transformed value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the function.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The asynchronous function to transform the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the function.</param>
    /// <returns>A ValueTask representing the asynchronous operation with the transformed success value.</returns>
    /// <exception cref="Exception">Thrown when the result represents an error, using the error's CreateException() method.</exception>
    public static async ValueTask<TOut> MatchOrThrowAsync<TValue, TError, TOut, TState>(this ValueTask<Result<TValue, TError>> resultTask, Func<TValue, TState, ValueTask<TOut>> onSuccess, TState state)
        where TError : IResultError where TValue : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? await onSuccess(result.Value, state).ConfigureAwait(false) : throw result.Error.CreateException();
    }

    #endregion
}