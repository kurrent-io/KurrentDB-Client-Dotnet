namespace Kurrent;

public partial class Result<TSuccess, TError> {
    /// <summary>
    /// Chains a new operation based on the success value of this result using the specified binding function.
    /// If this result is an error, the error is propagated.
    /// This is also known as 'flatMap' or 'SelectMany'.
    /// </summary>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <param name="binder">A function that takes the success value and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <returns>The result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    public Result<TOut, TError> Then<TOut>(Func<TSuccess, Result<TOut, TError>> binder) where TOut : notnull =>
        IsSuccess ? binder(AsSuccess) : Result<TOut, TError>.Error(AsError);

    /// <summary>
    /// Chains a new operation based on the success value of this result using the specified binding function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the binder.</typeparam>
    /// <param name="binder">A function that takes the success value and state, and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <param name="state">The state to pass to the binder.</param>
    /// <returns>The result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    public Result<TOut, TError> Then<TOut, TState>(Func<TSuccess, TState, Result<TOut, TError>> binder, TState state) where TOut : notnull =>
        IsSuccess ? binder(AsSuccess, state) : Result<TOut, TError>.Error(AsError);

    /// <summary>
    /// Asynchronously chains a new operation based on the success value of this result using the specified binding function.
    /// If this result is an error, the error is propagated.
    /// This is also known as 'flatMap' or 'SelectMany'.
    /// </summary>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <param name="binder">An asynchronous function that takes the success value and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    public async ValueTask<Result<TOut, TError>> ThenAsync<TOut>(Func<TSuccess, ValueTask<Result<TOut, TError>>> binder) where TOut : notnull =>
        IsSuccess ? await binder(AsSuccess).ConfigureAwait(false) : Result<TOut, TError>.Error(AsError);

    /// <summary>
    /// Asynchronously chains a new operation based on the success value of this result using the specified binding function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the binder.</typeparam>
    /// <param name="binder">An asynchronous function that takes the success value and state, and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <param name="state">The state to pass to the binder.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    public async ValueTask<Result<TOut, TError>> ThenAsync<TOut, TState>(Func<TSuccess, TState, ValueTask<Result<TOut, TError>>> binder, TState state) where TOut : notnull =>
        IsSuccess ? await binder(AsSuccess, state).ConfigureAwait(false) : Result<TOut, TError>.Error(AsError);
}
