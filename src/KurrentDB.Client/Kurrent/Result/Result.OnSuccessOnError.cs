namespace Kurrent;

public readonly partial record struct Result<TValue, TError> {
    #region . sync .

    /// <summary>
    /// Performs the specified action on the success value if the result is a success.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <param name="action">The action to perform on the success value.</param>
    /// <returns>The current <see cref="Result{TValue,TError}"/> instance.</returns>
    public Result<TValue, TError> OnSuccess(Action<TValue> action) {
        if (IsSuccess) action(Value);
        return this;
    }

    /// <summary>
    /// Performs the specified action on the success value if the result is a success, passing additional state.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="action">The action to perform on the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>The current <see cref="Result{TValue,TError}"/> instance.</returns>
    public Result<TValue, TError> OnSuccess<TState>(Action<TValue, TState> action, TState state) {
        if (IsSuccess) action(Value, state);
        return this;
    }

    /// <summary>
    /// Performs the specified action on the error value if the result is an error.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <param name="action">The action to perform on the error value.</param>
    /// <returns>The current <see cref="Result{TValue,TError}"/> instance.</returns>
    public Result<TValue, TError> OnError(Action<TError> action) {
        if (IsFailure) action(AsError);
        return this;
    }

    /// <summary>
    /// Performs the specified action on the error value if the result is an error, passing additional state.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="action">The action to perform on the error value, taking additional state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>The current <see cref="Result{TValue,TError}"/> instance.</returns>
    public Result<TValue, TError> OnError<TState>(Action<TError, TState> action, TState state) {
        if (IsFailure) action(AsError, state);
        return this;
    }

    #endregion

    #region . async .

    /// <summary>
    /// Asynchronously performs the specified action on the success value if the result is a success.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <param name="action">The asynchronous action to perform on the success value.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the current <see cref="Result{TValue,TError}"/> instance.</returns>
    public async ValueTask<Result<TValue, TError>> OnSuccessAsync(Func<TValue, ValueTask> action) {
        if (IsSuccess) await action(Value).ConfigureAwait(false);
        return this;
    }

    /// <summary>
    /// Asynchronously performs the specified action on the success value if the result is a success, passing additional state.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="action">The asynchronous action to perform on the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the current <see cref="Result{TValue,TError}"/> instance.</returns>
    public async ValueTask<Result<TValue, TError>> OnSuccessAsync<TState>(Func<TValue, TState, ValueTask> action, TState state) {
        if (IsSuccess) await action(Value, state).ConfigureAwait(false);
        return this;
    }

    /// <summary>
    /// Asynchronously performs the specified action on the error value if the result is an error.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <param name="action">The asynchronous action to perform on the error value.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the current <see cref="Result{TValue,TError}"/> instance.</returns>
    public async ValueTask<Result<TValue, TError>> OnErrorAsync(Func<TError, ValueTask> action) {
        if (IsFailure) await action(AsError).ConfigureAwait(false);
        return this;
    }

    /// <summary>
    /// Asynchronously performs the specified action on the error value if the result is an error, passing additional state.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="action">The asynchronous action to perform on the error value, taking additional state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the current <see cref="Result{TValue,TError}"/> instance.</returns>
    public async ValueTask<Result<TValue, TError>> OnErrorAsync<TState>(Func<TError, TState, ValueTask> action, TState state) {
        if (IsFailure) await action(AsError, state).ConfigureAwait(false);
        return this;
    }

    #endregion
}
