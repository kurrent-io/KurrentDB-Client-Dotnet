namespace Kurrent;

public readonly partial record struct Result<TValue, TError> {
    #region . sync .

    /// <summary>
    /// Executes one of the two provided actions depending on whether this result is a success or an error.
    /// This method is designed for side effects and does not return a value.
    /// </summary>
    /// <param name="onSuccess">The action to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The action to execute if the result is an error. It takes the error value as input.</param>
    public void Switch(Action<TValue> onSuccess, Action<TError> onError) {
        if (IsSuccess) {
            onSuccess(Value);
        } else {
            onError(AsError);
        }
    }

    /// <summary>
    /// Executes one of the two provided actions depending on whether this result is a success or an error, passing additional state.
    /// This method is designed for side effects and does not return a value.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the actions.</typeparam>
    /// <param name="onSuccess">The action to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The action to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the actions.</param>
    public void Switch<TState>(Action<TValue, TState> onSuccess, Action<TError, TState> onError, TState state) {
        if (IsSuccess) {
            onSuccess(Value, state);
        } else {
            onError(AsError, state);
        }
    }

    #endregion

    #region . async .

    /// <summary>
    /// Asynchronously executes one of the two provided actions depending on whether this result is a success or an error.
    /// Both actions are asynchronous and return <see cref="ValueTask"/>.
    /// This method is designed for side effects and does not return a value.
    /// </summary>
    /// <param name="onSuccess">The asynchronous action to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The asynchronous action to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public ValueTask SwitchAsync(Func<TValue, ValueTask> onSuccess, Func<TError, ValueTask> onError) =>
        IsSuccess ? onSuccess(Value) : onError(AsError);

    /// <summary>
    /// Asynchronously executes one of the two provided actions depending on whether this result is a success or an error.
    /// The success action is synchronous while the error action returns <see cref="ValueTask"/>.
    /// This method is designed for side effects and does not return a value.
    /// </summary>
    /// <param name="onSuccess">The synchronous action to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The asynchronous action to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public ValueTask SwitchAsync(Action<TValue> onSuccess, Func<TError, ValueTask> onError) {
        if (IsSuccess) {
            onSuccess(Value);
            return ValueTask.CompletedTask;
        }
        return onError(AsError);
    }

    /// <summary>
    /// Asynchronously executes one of the two provided actions depending on whether this result is a success or an error.
    /// The success action returns <see cref="ValueTask"/> while the error action is synchronous.
    /// This method is designed for side effects and does not return a value.
    /// </summary>
    /// <param name="onSuccess">The asynchronous action to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The synchronous action to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public ValueTask SwitchAsync(Func<TValue, ValueTask> onSuccess, Action<TError> onError) {
        if (IsSuccess) {
            return onSuccess(Value);
        }
        onError(AsError);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Asynchronously executes one of the two provided actions depending on whether this result is a success or an error, passing additional state.
    /// Both actions are asynchronous and return <see cref="ValueTask"/>.
    /// This method is designed for side effects and does not return a value.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the actions.</typeparam>
    /// <param name="onSuccess">The asynchronous action to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The asynchronous action to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the actions.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public ValueTask SwitchAsync<TState>(Func<TValue, TState, ValueTask> onSuccess, Func<TError, TState, ValueTask> onError, TState state) =>
        IsSuccess ? onSuccess(Value, state) : onError(AsError, state);

    /// <summary>
    /// Asynchronously executes one of the two provided actions depending on whether this result is a success or an error, passing additional state.
    /// The success action is synchronous while the error action returns <see cref="ValueTask"/>.
    /// This method is designed for side effects and does not return a value.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the actions.</typeparam>
    /// <param name="onSuccess">The synchronous action to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The asynchronous action to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the actions.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public ValueTask SwitchAsync<TState>(Action<TValue, TState> onSuccess, Func<TError, TState, ValueTask> onError, TState state) {
        if (IsSuccess) {
            onSuccess(Value, state);
            return ValueTask.CompletedTask;
        }
        return onError(AsError, state);
    }

    /// <summary>
    /// Asynchronously executes one of the two provided actions depending on whether this result is a success or an error, passing additional state.
    /// The success action returns <see cref="ValueTask"/> while the error action is synchronous.
    /// This method is designed for side effects and does not return a value.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the actions.</typeparam>
    /// <param name="onSuccess">The asynchronous action to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The synchronous action to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the actions.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public ValueTask SwitchAsync<TState>(Func<TValue, TState, ValueTask> onSuccess, Action<TError, TState> onError, TState state) {
        if (IsSuccess) {
            return onSuccess(Value, state);
        }
        onError(AsError, state);
        return ValueTask.CompletedTask;
    }

    #endregion
}
