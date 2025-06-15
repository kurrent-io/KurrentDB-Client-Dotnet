namespace Kurrent;

public readonly partial record struct Result<TValue, TError> {
    #region . sync .

    /// <summary>
    /// Gets the success value if the result is a success; otherwise, returns a fallback value computed from the error.
    /// </summary>
    /// <param name="fallback">A function that takes the error value and returns a fallback success value.</param>
    /// <returns>The success value if <see cref="IsSuccess"/> is <c>true</c>; otherwise, the result of the <paramref name="fallback"/> function.</returns>
    public TValue GetValueOrDefault(Func<TError, TValue> fallback) =>
        TryGetValue(out var value) ? value : fallback(AsError);

    /// <summary>
    /// Gets the success value if the result is a success; otherwise, returns a fallback value computed from the error, passing additional state.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the fallback function.</typeparam>
    /// <param name="fallback">A function that takes the error value and state, and returns a fallback success value.</param>
    /// <param name="state">The state to pass to the fallback function.</param>
    /// <returns>The success value if <see cref="IsSuccess"/> is <c>true</c>; otherwise, the result of the <paramref name="fallback"/> function.</returns>
    public TValue GetValueOrDefault<TState>(Func<TError, TState, TValue> fallback, TState state) =>
        TryGetValue(out var value) ? value : fallback(AsError, state);

    #endregion

    #region . async .

    /// <summary>
    /// Asynchronously gets the success value if the result is a success; otherwise, returns a fallback value computed from the error.
    /// </summary>
    /// <param name="fallback">An asynchronous function that takes the error value and returns a fallback success value.</param>
    /// <returns>A <see cref="ValueTask{TValue}"/> representing the asynchronous operation with the success value or the result of the <paramref name="fallback"/> function.</returns>
    public ValueTask<TValue> GetValueOrDefaultAsync(Func<TError, ValueTask<TValue>> fallback) =>
        TryGetValue(out var value) ? ValueTask.FromResult(value) : fallback(AsError);

    /// <summary>
    /// Asynchronously gets the success value if the result is a success; otherwise, returns a fallback value computed from the error, passing additional state.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the fallback function.</typeparam>
    /// <param name="fallback">An asynchronous function that takes the error value and state, and returns a fallback success value.</param>
    /// <param name="state">The state to pass to the fallback function.</param>
    /// <returns>A <see cref="ValueTask{TValue}"/> representing the asynchronous operation with the success value or the result of the <paramref name="fallback"/> function.</returns>
    public ValueTask<TValue> GetValueOrDefaultAsync<TState>(Func<TError, TState, ValueTask<TValue>> fallback, TState state) =>
        TryGetValue(out var value) ? ValueTask.FromResult(value) : fallback(AsError, state);

    #endregion
}
