namespace Kurrent;

public readonly partial record struct Result<TValue, TError> {
    #region . sync .

    /// <summary>
    /// Transforms the success value of this result using the specified mapping function.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <param name="mapper">A function to transform the success value.</param>
    /// <returns>A new <see cref="Result{TOut, TError}"/> containing the transformed success value or the original error.</returns>
    public Result<TOut, TError> Map<TOut>(Func<TValue, TOut> mapper) where TOut : notnull =>
        IsSuccess ? Kurrent.Result.Success<TOut, TError>(mapper(Value)) : Kurrent.Result.Failure<TOut, TError>(Error);

    /// <summary>
    /// Transforms the success value of this result using the specified mapping function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the mapper.</typeparam>
    /// <param name="mapper">A function to transform the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the mapper.</param>
    /// <returns>A new <see cref="Result{TOut, TError}"/> containing the transformed success value or the original error.</returns>
    public Result<TOut, TError> Map<TOut, TState>(Func<TValue, TState, TOut> mapper, TState state) where TOut : notnull =>
        IsSuccess ? Kurrent.Result.Success<TOut, TError>(mapper(Value, state)) : Kurrent.Result.Failure<TOut, TError>(Error);

    /// <summary>
    /// Transforms the error value of this result using the specified mapping function.
    /// If this result is a success, the success value is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new error value.</typeparam>
    /// <param name="mapper">A function to transform the error value.</param>
    /// <returns>A new <see cref="Result{TValue, TOut}"/> containing the original success value or the transformed error.</returns>
    public Result<TValue, TOut> MapError<TOut>(Func<TError, TOut> mapper) where TOut : notnull =>
        IsSuccess ? Kurrent.Result.Success<TValue, TOut>(Value) : Kurrent.Result.Failure<TValue, TOut>(mapper(Error));

    #endregion

    #region . async .

    /// <summary>
    /// Asynchronously transforms the success value of this result using the specified mapping function.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <param name="mapper">An asynchronous function to transform the success value.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the transformed success value or the original error.</returns>
    public async ValueTask<Result<TOut, TError>> MapAsync<TOut>(Func<TValue, ValueTask<TOut>> mapper) where TOut : notnull =>
        IsSuccess ? Kurrent.Result.Success<TOut, TError>(await mapper(Value).ConfigureAwait(false)) : Kurrent.Result.Failure<TOut, TError>(Error);

    /// <summary>
    /// Asynchronously transforms the success value of this result using the specified mapping function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the mapper.</typeparam>
    /// <param name="mapper">An asynchronous function to transform the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the mapper.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the transformed success value or the original error.</returns>
    public async ValueTask<Result<TOut, TError>> MapAsync<TOut, TState>(Func<TValue, TState, ValueTask<TOut>> mapper, TState state) where TOut : notnull =>
        IsSuccess ? Kurrent.Result.Success<TOut, TError>(await mapper(Value, state).ConfigureAwait(false)) : Kurrent.Result.Failure<TOut, TError>(Error);

    #endregion
}
