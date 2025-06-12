namespace Kurrent.Client;

public partial class Result<TSuccess, TError> {
    /// <summary>
    /// Gets the success value if the result is a success; otherwise, returns a fallback value computed from the error.
    /// </summary>
    /// <param name="fallback">A function that takes the error value and returns a fallback success value.</param>
    /// <returns>The success value if <see cref="IsSuccess"/> is <c>true</c>; otherwise, the result of the <paramref name="fallback"/> function.</returns>
    public TSuccess GetValueOrElse(Func<TError, TSuccess> fallback) =>
        TryGetValue(out var value) ? value : fallback(AsError);

    /// <summary>
    /// Gets the success value if the result is a success; otherwise, returns a fallback value computed from the error, passing additional state.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the fallback function.</typeparam>
    /// <param name="fallback">A function that takes the error value and state, and returns a fallback success value.</param>
    /// <param name="state">The state to pass to the fallback function.</param>
    /// <returns>The success value if <see cref="IsSuccess"/> is <c>true</c>; otherwise, the result of the <paramref name="fallback"/> function.</returns>
    public TSuccess GetValueOrElse<TState>(Func<TError, TState, TSuccess> fallback, TState state) =>
        TryGetValue(out var value) ? value : fallback(AsError, state);

    /// <summary>
    /// Asynchronously gets the success value if the result is a success; otherwise, returns a fallback value computed from the error.
    /// </summary>
    /// <param name="fallback">An asynchronous function that takes the error value and returns a fallback success value.</param>
    /// <returns>A <see cref="ValueTask{TSuccess}"/> representing the asynchronous operation with the success value or the result of the <paramref name="fallback"/> function.</returns>
    public ValueTask<TSuccess> GetValueOrElseAsync(Func<TError, ValueTask<TSuccess>> fallback) =>
        TryGetValue(out var value) ? ValueTask.FromResult(value) : fallback(AsError);

    /// <summary>
    /// Asynchronously gets the success value if the result is a success; otherwise, returns a fallback value computed from the error, passing additional state.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the fallback function.</typeparam>
    /// <param name="fallback">An asynchronous function that takes the error value and state, and returns a fallback success value.</param>
    /// <param name="state">The state to pass to the fallback function.</param>
    /// <returns>A <see cref="ValueTask{TSuccess}"/> representing the asynchronous operation with the success value or the result of the <paramref name="fallback"/> function.</returns>
    public ValueTask<TSuccess> GetValueOrElseAsync<TState>(Func<TError, TState, ValueTask<TSuccess>> fallback, TState state) =>
        TryGetValue(out var value) ? ValueTask.FromResult(value) : fallback(AsError, state);
}
