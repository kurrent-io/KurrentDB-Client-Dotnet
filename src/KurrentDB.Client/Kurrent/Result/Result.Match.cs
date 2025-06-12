namespace Kurrent;

public partial class Result<TSuccess, TError> {
    /// <summary>
    /// Executes one of the two provided functions depending on whether this result is a success or an error, returning a new value.
    /// </summary>
    /// <typeparam name="TOut">The type of the value returned by the matching functions.</typeparam>
    /// <param name="onSuccess">The function to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The function to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>The value returned by the executed function (<paramref name="onSuccess"/> or <paramref name="onError"/>).</returns>
    public TOut Match<TOut>(Func<TSuccess, TOut> onSuccess, Func<TError, TOut> onError) =>
        IsSuccess ? onSuccess(AsSuccess) : onError(AsError);

    /// <summary>
    /// Executes one of the two provided functions depending on whether this result is a success or an error, returning a new value and passing additional state.
    /// </summary>
    /// <typeparam name="TOut">The type of the value returned by the matching functions.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
    /// <param name="onSuccess">The function to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The function to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the functions.</param>
    /// <returns>The value returned by the executed function (<paramref name="onSuccess"/> or <paramref name="onError"/>).</returns>
    public TOut Match<TOut, TState>(Func<TSuccess, TState, TOut> onSuccess, Func<TError, TState, TOut> onError, TState state) =>
        IsSuccess ? onSuccess(AsSuccess, state) : onError(AsError, state);

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether this result is a success or an error, returning a new value.
    /// Both functions are asynchronous and return <see cref="ValueTask{TOut}"/>.
    /// </summary>
    /// <typeparam name="TOut">The type of the value returned by the matching functions.</typeparam>
    /// <param name="onSuccess">The asynchronous function to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The asynchronous function to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public ValueTask<TOut> MatchAsync<TOut>(Func<TSuccess, ValueTask<TOut>> onSuccess, Func<TError, ValueTask<TOut>> onError) =>
        IsSuccess ? onSuccess(AsSuccess) : onError(AsError);

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether this result is a success or an error, returning a new value.
    /// The success function is synchronous while the error function returns <see cref="ValueTask{TOut}"/>.
    /// </summary>
    /// <typeparam name="TOut">The type of the value returned by the matching functions.</typeparam>
    /// <param name="onSuccess">The synchronous function to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The asynchronous function to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public ValueTask<TOut> MatchAsync<TOut>(Func<TSuccess, TOut> onSuccess, Func<TError, ValueTask<TOut>> onError) =>
        IsSuccess ? ValueTask.FromResult(onSuccess(AsSuccess)) : onError(AsError);

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether this result is a success or an error, returning a new value.
    /// The success function returns <see cref="ValueTask{TOut}"/> while the error function is synchronous.
    /// </summary>
    /// <typeparam name="TOut">The type of the value returned by the matching functions.</typeparam>
    /// <param name="onSuccess">The asynchronous function to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The synchronous function to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public ValueTask<TOut> MatchAsync<TOut>(Func<TSuccess, ValueTask<TOut>> onSuccess, Func<TError, TOut> onError) =>
        IsSuccess ? onSuccess(AsSuccess) : ValueTask.FromResult(onError(AsError));

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether this result is a success or an error, returning a new value and passing additional state.
    /// Both functions are asynchronous and return <see cref="ValueTask{TOut}"/>.
    /// </summary>
    /// <typeparam name="TOut">The type of the value returned by the matching functions.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
    /// <param name="onSuccess">The asynchronous function to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The asynchronous function to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the functions.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public ValueTask<TOut> MatchAsync<TOut, TState>(Func<TSuccess, TState, ValueTask<TOut>> onSuccess, Func<TError, TState, ValueTask<TOut>> onError, TState state) =>
        IsSuccess ? onSuccess(AsSuccess, state) : onError(AsError, state);

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether this result is a success or an error, returning a new value and passing additional state.
    /// The success function is synchronous while the error function returns <see cref="ValueTask{TOut}"/>.
    /// </summary>
    /// <typeparam name="TOut">The type of the value returned by the matching functions.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
    /// <param name="onSuccess">The synchronous function to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The asynchronous function to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the functions.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public ValueTask<TOut> MatchAsync<TOut, TState>(Func<TSuccess, TState, TOut> onSuccess, Func<TError, TState, ValueTask<TOut>> onError, TState state) =>
        IsSuccess ? ValueTask.FromResult(onSuccess(AsSuccess, state)) : onError(AsError, state);

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether this result is a success or an error, returning a new value and passing additional state.
    /// The success function returns <see cref="ValueTask{TOut}"/> while the error function is synchronous.
    /// </summary>
    /// <typeparam name="TOut">The type of the value returned by the matching functions.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
    /// <param name="onSuccess">The asynchronous function to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The synchronous function to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the functions.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public ValueTask<TOut> MatchAsync<TOut, TState>(Func<TSuccess, TState, ValueTask<TOut>> onSuccess, Func<TError, TState, TOut> onError, TState state) =>
        IsSuccess ? onSuccess(AsSuccess, state) : ValueTask.FromResult(onError(AsError, state));
}
