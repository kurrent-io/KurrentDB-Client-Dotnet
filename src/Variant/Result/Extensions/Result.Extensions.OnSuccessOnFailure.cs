namespace Kurrent;

/// <summary>
/// Extension methods for side-effect operations on IResult types.
/// These methods provide fluent chaining capabilities for performing actions based on success or failure states.
/// </summary>
[PublicAPI]
public static class ResultOnSuccessOnFailureExtensions {
    #region . sync .

    /// <summary>
    /// Performs the specified action on the success value if the result is a success.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to operate on.</param>
    /// <param name="action">The action to perform on the success value.</param>
    /// <returns>The current <see cref="IResult{TValue,TError}"/> instance.</returns>
    public static TResult OnSuccess<TValue, TError, TResult>(this TResult result, Action<TValue> action) 
        where TResult : IResult<TValue, TError> where TValue : notnull where TError : notnull {
        if (result.IsSuccess) action(result.Value);
        return result;
    }

    /// <summary>
    /// Performs the specified action on the success value if the result is a success, passing additional state.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TResult">The type of the result implementing IResult.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="result">The result to operate on.</param>
    /// <param name="action">The action to perform on the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>The current <see cref="IResult{TValue,TError}"/> instance.</returns>
    public static TResult OnSuccess<TValue, TError, TResult, TState>(this TResult result, Action<TValue, TState> action, TState state) 
        where TResult : IResult<TValue, TError> where TValue : notnull where TError : notnull {
        if (result.IsSuccess) action(result.Value, state);
        return result;
    }

    /// <summary>
    /// Performs the specified action on the error value if the result is an error.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TResult">The type of the result implementing IResult.</typeparam>
    /// <param name="result">The result to operate on.</param>
    /// <param name="action">The action to perform on the error value.</param>
    /// <returns>The current <see cref="IResult{TValue,TError}"/> instance.</returns>
    public static TResult OnFailure<TValue, TError, TResult>(this TResult result, Action<TError> action) 
        where TResult : IResult<TValue, TError> where TValue : notnull where TError : notnull {
        if (result.IsFailure) action(result.Error);
        return result;
    }

    /// <summary>
    /// Performs the specified action on the error value if the result is an error, passing additional state.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TResult">The type of the result implementing IResult.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="result">The result to operate on.</param>
    /// <param name="action">The action to perform on the error value, taking additional state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>The current <see cref="IResult{TValue,TError}"/> instance.</returns>
    public static TResult OnFailure<TValue, TError, TResult, TState>(this TResult result, Action<TError, TState> action, TState state) 
        where TResult : IResult<TValue, TError> where TValue : notnull where TError : notnull {
        if (result.IsFailure) action(result.Error, state);
        return result;
    }

    #endregion

    #region . async .

    /// <summary>
    /// Asynchronously performs the specified action on the success value if the result is a success.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TResult">The type of the result implementing IResult.</typeparam>
    /// <param name="result">The result to operate on.</param>
    /// <param name="action">The asynchronous action to perform on the success value.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the current <see cref="IResult{TValue,TError}"/> instance.</returns>
    public static async ValueTask<TResult> OnSuccessAsync<TValue, TError, TResult>(this TResult result, Func<TValue, ValueTask> action) 
        where TResult : IResult<TValue, TError> where TValue : notnull where TError : notnull {
        if (result.IsSuccess) await action(result.Value).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Asynchronously performs the specified action on the success value if the result is a success, passing additional state.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TResult">The type of the result implementing IResult.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="result">The result to operate on.</param>
    /// <param name="action">The asynchronous action to perform on the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the current <see cref="IResult{TValue,TError}"/> instance.</returns>
    public static async ValueTask<TResult> OnSuccessAsync<TValue, TError, TResult, TState>(this TResult result, Func<TValue, TState, ValueTask> action, TState state) 
        where TResult : IResult<TValue, TError> where TValue : notnull where TError : notnull {
        if (result.IsSuccess) await action(result.Value, state).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Asynchronously performs the specified action on the error value if the result is an error.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TResult">The type of the result implementing IResult.</typeparam>
    /// <param name="result">The result to operate on.</param>
    /// <param name="action">The asynchronous action to perform on the error value.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the current <see cref="IResult{TValue,TError}"/> instance.</returns>
    public static async ValueTask<TResult> OnFailureAsync<TValue, TError, TResult>(this TResult result, Func<TError, ValueTask> action) 
        where TResult : IResult<TValue, TError> where TValue : notnull where TError : notnull {
        if (result.IsFailure) await action(result.Error).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Asynchronously performs the specified action on the error value if the result is an error, passing additional state.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TResult">The type of the result implementing IResult.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="result">The result to operate on.</param>
    /// <param name="action">The asynchronous action to perform on the error value, taking additional state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the current <see cref="IResult{TValue,TError}"/> instance.</returns>
    public static async ValueTask<TResult> OnFailureAsync<TValue, TError, TResult, TState>(this TResult result, Func<TError, TState, ValueTask> action, TState state) 
        where TResult : IResult<TValue, TError> where TValue : notnull where TError : notnull {
        if (result.IsFailure) await action(result.Error, state).ConfigureAwait(false);
        return result;
    }

    #endregion
}