namespace Kurrent;

public readonly partial record struct Result<TValue, TError> {

    #region . sync .

    /// <summary>
    /// Executes one of the two provided functions depending on whether this result is a success or an error, returning a new value.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the matching functions.</typeparam>
    /// <param name="onSuccess">The function to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The function to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>The value returned by the executed function (<paramref name="onSuccess"/> or <paramref name="onError"/>).</returns>
    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<TError, TResult> onError) =>
        IsSuccess ? onSuccess(Value) : onError(Error);

    /// <summary>
    /// Executes one of the two provided functions depending on whether this result is a success or an error, returning a new value and passing additional state.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the matching functions.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
    /// <param name="onSuccess">The function to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The function to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the functions.</param>
    /// <returns>The value returned by the executed function (<paramref name="onSuccess"/> or <paramref name="onError"/>).</returns>
    public TResult Match<TResult, TState>(Func<TValue, TState, TResult> onSuccess, Func<TError, TState, TResult> onError, TState state) =>
        IsSuccess ? onSuccess(Value, state) : onError(Error, state);

    #endregion

    #region . async .

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether this result is a success or an error, returning a new value.
    /// Both functions are asynchronous and return <see cref="ValueTask{TOut}"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the matching functions.</typeparam>
    /// <param name="onSuccess">The asynchronous function to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The asynchronous function to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public ValueTask<TResult> MatchAsync<TResult>(Func<TValue, ValueTask<TResult>> onSuccess, Func<TError, ValueTask<TResult>> onError) =>
        IsSuccess ? onSuccess(Value) : onError(Error);

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether this result is a success or an error, returning a new value.
    /// The success function is synchronous while the error function returns <see cref="ValueTask{TOut}"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the matching functions.</typeparam>
    /// <param name="onSuccess">The synchronous function to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The asynchronous function to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public ValueTask<TResult> MatchAsync<TResult>(Func<TValue, TResult> onSuccess, Func<TError, ValueTask<TResult>> onError) =>
        IsSuccess ? ValueTask.FromResult(onSuccess(Value)) : onError(Error);

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether this result is a success or an error, returning a new value.
    /// The success function returns <see cref="ValueTask{TOut}"/> while the error function is synchronous.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the matching functions.</typeparam>
    /// <param name="onSuccess">The asynchronous function to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The synchronous function to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public ValueTask<TResult> MatchAsync<TResult>(Func<TValue, ValueTask<TResult>> onSuccess, Func<TError, TResult> onError) =>
        IsSuccess ? onSuccess(Value) : ValueTask.FromResult(onError(Error));

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether this result is a success or an error, returning a new value and passing additional state.
    /// Both functions are asynchronous and return <see cref="ValueTask{TOut}"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the matching functions.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
    /// <param name="onSuccess">The asynchronous function to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The asynchronous function to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the functions.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public ValueTask<TResult> MatchAsync<TResult, TState>(Func<TValue, TState, ValueTask<TResult>> onSuccess, Func<TError, TState, ValueTask<TResult>> onError, TState state) =>
        IsSuccess ? onSuccess(Value, state) : onError(Error, state);

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether this result is a success or an error, returning a new value and passing additional state.
    /// The success function is synchronous while the error function returns <see cref="ValueTask{TOut}"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the matching functions.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
    /// <param name="onSuccess">The synchronous function to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The asynchronous function to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the functions.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public ValueTask<TResult> MatchAsync<TResult, TState>(Func<TValue, TState, TResult> onSuccess, Func<TError, TState, ValueTask<TResult>> onError, TState state) =>
        IsSuccess ? ValueTask.FromResult(onSuccess(Value, state)) : onError(Error, state);

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether this result is a success or an error, returning a new value and passing additional state.
    /// The success function returns <see cref="ValueTask{TOut}"/> while the error function is synchronous.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the matching functions.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
    /// <param name="onSuccess">The asynchronous function to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The synchronous function to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the functions.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public ValueTask<TResult> MatchAsync<TResult, TState>(Func<TValue, TState, ValueTask<TResult>> onSuccess, Func<TError, TState, TResult> onError, TState state) =>
        IsSuccess ? onSuccess(Value, state) : ValueTask.FromResult(onError(Error, state));

    #endregion
}
