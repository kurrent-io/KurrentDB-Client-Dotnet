namespace Kurrent;

public partial class Result<TSuccess, TError> {
    /// <summary>
    /// Ensures that the success value meets a specified condition.
    /// If the result is a success and the condition is not met, the result is transformed into an error.
    /// </summary>
    /// <param name="predicate">The condition to check on the success value.</param>
    /// <param name="errorFactory">A function that creates an error value if the condition is not met.</param>
    /// <returns>
    /// The original <see cref="Result{TSuccess, TError}"/> if it's an error or if the condition is met;
    /// otherwise, a new error result.
    /// </returns>
    public Result<TSuccess, TError> Ensure(Func<TSuccess, bool> predicate, Func<TSuccess, TError> errorFactory) {
        if (IsError) return this;
        return predicate(AsSuccess) ? this : Error(errorFactory(AsSuccess));
    }

    /// <summary>
    /// Asynchronously ensures that the success value meets a specified condition.
    /// If the result is a success and the condition is not met, the result is transformed into an error.
    /// </summary>
    /// <param name="predicate">The asynchronous condition to check on the success value.</param>
    /// <param name="errorFactory">A function that creates an error value if the condition is not met.</param>
    /// <returns>
    /// A <see cref="ValueTask{Result}"/> representing the asynchronous operation, containing
    /// the original <see cref="Result{TSuccess, TError}"/> if it's an error or if the condition is met;
    /// otherwise, a new error result.
    /// </returns>
    public async ValueTask<Result<TSuccess, TError>> EnsureAsync(Func<TSuccess, ValueTask<bool>> predicate, Func<TSuccess, TError> errorFactory) {
        if (IsError) return this;
        return await predicate(AsSuccess).ConfigureAwait(false) ? this : Error(errorFactory(AsSuccess));
    }

    /// <summary>
    /// Ensures that the success value meets a specified condition, using additional state.
    /// If the result is a success and the condition is not met, the result is transformed into an error.
    /// </summary>
    /// <param name="predicate">The condition to check on the success value.</param>
    /// <param name="errorFactory">A function that creates an error value if the condition is not met.</param>
    /// <param name="state">The state to pass to the predicate and error factory.</param>
    /// <returns>
    /// The original <see cref="Result{TSuccess, TError}"/> if it's an error or if the condition is met;
    /// otherwise, a new error result.
    /// </returns>
    public Result<TSuccess, TError> Ensure<TState>(Func<TSuccess, TState, bool> predicate, Func<TSuccess, TState, TError> errorFactory, TState state) {
        if (IsError) return this;
        return predicate(AsSuccess, state) ? this : Error(errorFactory(AsSuccess, state));
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
    /// the original <see cref="Result{TSuccess, TError}"/> if it's an error or if the condition is met;
    /// otherwise, a new error result.
    /// </returns>
    public async ValueTask<Result<TSuccess, TError>> EnsureAsync<TState>(Func<TSuccess, TState, ValueTask<bool>> predicate, Func<TSuccess, TState, TError> errorFactory, TState state) {
        if (IsError) return this;
        return await predicate(AsSuccess, state).ConfigureAwait(false) ? this : Error(errorFactory(AsSuccess, state));
    }
}
