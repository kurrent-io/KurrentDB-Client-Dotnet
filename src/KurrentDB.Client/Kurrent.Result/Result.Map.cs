namespace Kurrent.Client;

public partial class Result<TSuccess, TError> {
    /// <summary>
    /// Transforms the success value of this result using the specified mapping function.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <param name="mapper">A function to transform the success value.</param>
    /// <returns>A new <see cref="Result{TOut, TError}"/> containing the transformed success value or the original error.</returns>
    public Result<TOut, TError> Map<TOut>(Func<TSuccess, TOut> mapper) where TOut : notnull =>
        IsSuccess ? Result<TOut, TError>.Success(mapper(AsSuccess)) : Result<TOut, TError>.Error(AsError);

    /// <summary>
    /// Transforms the success value of this result using the specified mapping function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the mapper.</typeparam>
    /// <param name="mapper">A function to transform the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the mapper.</param>
    /// <returns>A new <see cref="Result{TOut, TError}"/> containing the transformed success value or the original error.</returns>
    public Result<TOut, TError> Map<TOut, TState>(Func<TSuccess, TState, TOut> mapper, TState state) where TOut : notnull =>
        IsSuccess ? Result<TOut, TError>.Success(mapper(AsSuccess, state)) : Result<TOut, TError>.Error(AsError);

    /// <summary>
    /// Asynchronously transforms the success value of this result using the specified mapping function.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <param name="mapper">An asynchronous function to transform the success value.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the transformed success value or the original error.</returns>
    public async ValueTask<Result<TOut, TError>> MapAsync<TOut>(Func<TSuccess, ValueTask<TOut>> mapper) where TOut : notnull =>
        IsSuccess ? Result<TOut, TError>.Success(await mapper(AsSuccess).ConfigureAwait(false)) : Result<TOut, TError>.Error(AsError);

    /// <summary>
    /// Asynchronously transforms the success value of this result using the specified mapping function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the mapper.</typeparam>
    /// <param name="mapper">An asynchronous function to transform the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the mapper.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the transformed success value or the original error.</returns>
    public async ValueTask<Result<TOut, TError>> MapAsync<TOut, TState>(Func<TSuccess, TState, ValueTask<TOut>> mapper, TState state) where TOut : notnull =>
        IsSuccess ? Result<TOut, TError>.Success(await mapper(AsSuccess, state).ConfigureAwait(false)) : Result<TOut, TError>.Error(AsError);

    /// <summary>
    /// Transforms the error value of this result using the specified mapping function.
    /// If this result is a success, the success value is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new error value.</typeparam>
    /// <param name="mapper">A function to transform the error value.</param>
    /// <returns>A new <see cref="Result{TSuccess, TOut}"/> containing the original success value or the transformed error.</returns>
    public Result<TSuccess, TOut> MapError<TOut>(Func<TError, TOut> mapper) where TOut : notnull =>
        IsSuccess ? Result<TSuccess, TOut>.Success(AsSuccess) : Result<TSuccess, TOut>.Error(mapper(AsError));
}
