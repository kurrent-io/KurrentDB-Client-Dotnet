namespace Kurrent;

public readonly partial record struct Result<TValue, TError> {
    #region . sync .

    /// <summary>
    /// Ensures that the success value meets a specified condition.
    /// If the result is a success and the condition is not met, the result is transformed into an error.
    /// </summary>
    /// <param name="predicate">The condition to check on the success value.</param>
    /// <param name="errorFactory">A function that creates an error value if the condition is not met.</param>
    /// <returns>
    /// The original <see cref="Result{TValue, TError}"/> if it's an error or if the condition is met;
    /// otherwise, a new error result.
    /// </returns>
    public Result<TValue, TError> Ensure(Func<TValue, bool> predicate, Func<TValue, TError> errorFactory) {
        if (IsFailure) return this;
        return predicate(Value) ? this : new(errorFactory(Value));
    }

    /// <summary>
    /// Ensures that the success value meets a specified condition, using additional state.
    /// If the result is a success and the condition is not met, the result is transformed into an error.
    /// </summary>
    /// <param name="predicate">The condition to check on the success value.</param>
    /// <param name="errorFactory">A function that creates an error value if the condition is not met.</param>
    /// <param name="state">The state to pass to the predicate and error factory.</param>
    /// <returns>
    /// The original <see cref="Result{TValue, TError}"/> if it's an error or if the condition is met;
    /// otherwise, a new error result.
    /// </returns>
    public Result<TValue, TError> Ensure<TState>(Func<TValue, TState, bool> predicate, Func<TValue, TState, TError> errorFactory, TState state) {
        if (IsFailure) return this;
        return predicate(Value, state) ? this : new(errorFactory(Value, state));
    }

    #endregion

    #region . async .

    /// <summary>
    /// Asynchronously ensures that the success value meets a specified condition.
    /// If the result is a success and the condition is not met, the result is transformed into an error.
    /// </summary>
    /// <param name="predicate">The asynchronous condition to check on the success value.</param>
    /// <param name="errorFactory">A function that creates an error value if the condition is not met.</param>
    /// <returns>
    /// A <see cref="ValueTask{Result}"/> representing the asynchronous operation, containing
    /// the original <see cref="Result{TValue, TError}"/> if it's an error or if the condition is met;
    /// otherwise, a new error result.
    /// </returns>
    public async ValueTask<Result<TValue, TError>> EnsureAsync(Func<TValue, ValueTask<bool>> predicate, Func<TValue, TError> errorFactory) {
        if (IsFailure) return this;
        return await predicate(Value).ConfigureAwait(false) ? this : new(errorFactory(Value));
    }

    /// <summary>
    /// Asynchronously ensures that the success value meets a specified condition, using additional state.
    /// If the result is a success and the condition is not met, the result is transformed into an error.
    /// </summary>
    /// <param name="predicate">The asynchronous condition to check on the success value.</param>
    /// <param name="errorFactory">A function that creates an error value if the condition is not met.</param>
    /// <param name="state">The state to pass to the predicate and error factory.</param>
    /// <returns>
    /// A <see cref="ValueTask{Result}"/> representing the asynchronous operation, containing
    /// the original <see cref="Result{TValue, TError}"/> if it's an error or if the condition is met;
    /// otherwise, a new error result.
    /// </returns>
    public async ValueTask<Result<TValue, TError>> EnsureAsync<TState>(Func<TValue, TState, ValueTask<bool>> predicate, Func<TValue, TState, TError> errorFactory, TState state) {
        if (IsFailure) return this;
        return await predicate(Value, state).ConfigureAwait(false) ? this : new(errorFactory(Value, state));
    }

    #endregion
}
