namespace Kurrent.Client;

public partial class Result<TSuccess, TError> {
    /// <summary>
    /// Performs the specified action on the success value if the result is a success.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <param name="action">The action to perform on the success value.</param>
    /// <returns>The current <see cref="Result{TSuccess,TError}"/> instance.</returns>
    public Result<TSuccess, TError> OnSuccess(Action<TSuccess> action) {
        if (IsSuccess) action(AsSuccess);
        return this;
    }

    /// <summary>
    /// Performs the specified action on the success value if the result is a success, passing additional state.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="action">The action to perform on the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>The current <see cref="Result{TSuccess,TError}"/> instance.</returns>
    public Result<TSuccess, TError> OnSuccess<TState>(Action<TSuccess, TState> action, TState state) {
        if (IsSuccess) action(AsSuccess, state);
        return this;
    }

    /// <summary>
    /// Performs the specified action on the error value if the result is an error.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <param name="action">The action to perform on the error value.</param>
    /// <returns>The current <see cref="Result{TSuccess,TError}"/> instance.</returns>
    public Result<TSuccess, TError> OnError(Action<TError> action) {
        if (IsError) action(AsError);
        return this;
    }

    /// <summary>
    /// Performs the specified action on the error value if the result is an error, passing additional state.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="action">The action to perform on the error value, taking additional state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>The current <see cref="Result{TSuccess,TError}"/> instance.</returns>
    public Result<TSuccess, TError> OnError<TState>(Action<TError, TState> action, TState state) {
        if (IsError) action(AsError, state);
        return this;
    }

    /// <summary>
    /// Asynchronously performs the specified action on the success value if the result is a success.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <param name="action">The asynchronous action to perform on the success value.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the current <see cref="Result{TSuccess,TError}"/> instance.</returns>
    public async ValueTask<Result<TSuccess, TError>> OnSuccessAsync(Func<TSuccess, ValueTask> action) {
        if (IsSuccess) await action(AsSuccess).ConfigureAwait(false);
        return this;
    }

    /// <summary>
    /// Asynchronously performs the specified action on the success value if the result is a success, passing additional state.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="action">The asynchronous action to perform on the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the current <see cref="Result{TSuccess,TError}"/> instance.</returns>
    public async ValueTask<Result<TSuccess, TError>> OnSuccessAsync<TState>(Func<TSuccess, TState, ValueTask> action, TState state) {
        if (IsSuccess) await action(AsSuccess, state).ConfigureAwait(false);
        return this;
    }

    /// <summary>
    /// Asynchronously performs the specified action on the error value if the result is an error.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <param name="action">The asynchronous action to perform on the error value.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the current <see cref="Result{TSuccess,TError}"/> instance.</returns>
    public async ValueTask<Result<TSuccess, TError>> OnErrorAsync(Func<TError, ValueTask> action) {
        if (IsError) await action(AsError).ConfigureAwait(false);
        return this;
    }

    /// <summary>
    /// Asynchronously performs the specified action on the error value if the result is an error, passing additional state.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="action">The asynchronous action to perform on the error value, taking additional state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the current <see cref="Result{TSuccess,TError}"/> instance.</returns>
    public async ValueTask<Result<TSuccess, TError>> OnErrorAsync<TState>(Func<TError, TState, ValueTask> action, TState state) {
        if (IsError) await action(AsError, state).ConfigureAwait(false);
        return this;
    }
}
