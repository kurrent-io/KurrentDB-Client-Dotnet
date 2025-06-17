namespace Kurrent;

public readonly partial record struct Result<TValue, TError> {
    #region . sync .

    /// <summary>
    /// Chains a new operation based on the success value of this result using the specified binding function.
    /// If this result is an error, the error is propagated.
    /// This is also known as 'flatMap' or 'SelectMany'.
    /// </summary>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <param name="binder">A function that takes the success value and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <returns>The result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    public Result<TOut, TError> Then<TOut>(Func<TValue, Result<TOut, TError>> binder) where TOut : notnull =>
        IsSuccess ? binder(Value) : Kurrent.Result.Failure<TOut, TError>(Error);

    /// <summary>
    /// Chains a new operation based on the success value of this result using the specified binding function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the binder.</typeparam>
    /// <param name="binder">A function that takes the success value and state, and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <param name="state">The state to pass to the binder.</param>
    /// <returns>The result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    public Result<TOut, TError> Then<TOut, TState>(Func<TValue, TState, Result<TOut, TError>> binder, TState state) where TOut : notnull =>
        IsSuccess ? binder(Value, state) : Kurrent.Result.Failure<TOut, TError>(Error);

    #endregion

    #region . async .

    /// <summary>
    /// Asynchronously chains a new operation based on the success value of this result using the specified binding function.
    /// If this result is an error, the error is propagated.
    /// This is also known as 'flatMap' or 'SelectMany'.
    /// </summary>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <param name="binder">An asynchronous function that takes the success value and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    public async ValueTask<Result<TOut, TError>> ThenAsync<TOut>(Func<TValue, ValueTask<Result<TOut, TError>>> binder) where TOut : notnull =>
        IsSuccess ? await binder(Value).ConfigureAwait(false) : Kurrent.Result.Failure<TOut, TError>(Error);

    /// <summary>
    /// Asynchronously chains a new operation based on the success value of this result using the specified binding function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the binder.</typeparam>
    /// <param name="binder">An asynchronous function that takes the success value and state, and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <param name="state">The state to pass to the binder.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    public async ValueTask<Result<TOut, TError>> ThenAsync<TOut, TState>(Func<TValue, TState, ValueTask<Result<TOut, TError>>> binder, TState state) where TOut : notnull =>
        IsSuccess ? await binder(Value, state).ConfigureAwait(false) : Kurrent.Result.Failure<TOut, TError>(Error);

    #endregion
}
