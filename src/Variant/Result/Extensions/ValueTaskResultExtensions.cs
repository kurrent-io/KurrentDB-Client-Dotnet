namespace Kurrent;

[PublicAPI]
public static partial class ValueTaskResultExtensions {
    #region . valuetask-based methods .

    /// <summary>
    /// Converts a <see cref="ValueTask{TSuccess}"/> into a <see cref="ValueTask{Result}"/>, handling exceptions.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="task">The value task to convert.</param>
    /// <param name="onError">The function to execute if the task faults.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> representing the outcome of the task.</returns>
    public static ValueTask<Result<TValue, TError>> ToResultAsync<TValue, TError>(this ValueTask<TValue> task, Func<Exception, TError> onError) where TValue : notnull where TError : notnull =>
        Result.TryAsync(() => task, onError);

    public static ValueTask<Result<TValue, Exception>> ToResultAsync<TValue>(this ValueTask<TValue> task) where TValue : notnull =>
        Result.TryAsync(() => task);

    /// <summary>
    /// Converts a <see cref="ValueTask"/> into a <see cref="ValueTask{Result}"/>, handling exceptions.
    /// </summary>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="task">The value task to convert.</param>
    /// <param name="onError">The function to execute if the task faults.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> representing the outcome of the task.</returns>
    public static ValueTask<Result<Void, TError>> ToResultAsync<TError>(this ValueTask task, Func<Exception, TError> onError) where TError : notnull =>
        Result.TryAsync(() => task, onError);

    #endregion

    #region . task-based methods .

    /// <summary>
    /// Converts a <see cref="Task{TSuccess}"/> into a <see cref="ValueTask{Result}"/>, handling exceptions.
    /// </summary>
    public static ValueTask<Result<TValue, TError>> ToResultAsync<TValue, TError>(this Task<TValue> task, Func<Exception, TError> onError) where TValue : notnull where TError : notnull {
        return Result.TryAsync(async () => await task.ConfigureAwait(false), onError);
    }

    /// <summary>
    /// Converts a <see cref="Task{TSuccess}"/> into a <see cref="ValueTask{Result}"/>, handling exceptions.
    /// </summary>
    public static ValueTask<Result<TValue, Exception>> ToResultAsync<TValue>(this Task<TValue> task) where TValue : notnull {
        return Result.TryAsync(async () => await task.ConfigureAwait(false));
    }

    /// <summary>
    /// Converts a <see cref="Task"/> into a <see cref="ValueTask{Result}"/>, handling exceptions.
    /// </summary>
    public static ValueTask<Result<Void, TError>> ToResultAsync<TError>(this Task task, Func<Exception, TError> onError) where TError : notnull {
        return Result.TryAsync(async () => await task.ConfigureAwait(false), onError);
    }

    #endregion
}

public static partial class ValueTaskResultExtensions {
    #region . result operation extensions .

    /// <summary>
    /// Asynchronously chains a new operation from the success value of the result in the <see cref="ValueTask"/>.
    /// </summary>
    /// <typeparam name="TCurrent">The type of the current success value.</typeparam>
    /// <typeparam name="TNext">The type of the next success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="binder">A function that takes the success value and returns a new <see cref="Result{TNext, TError}"/>.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> representing the chained operation.</returns>
    public static async ValueTask<Result<TNext, TError>> ThenAsync<TCurrent, TNext, TError>(
        this ValueTask<Result<TCurrent, TError>> resultTask,
        Func<TCurrent, Result<TNext, TError>> binder) where TCurrent : notnull where TNext : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.Then(binder);
    }

    /// <summary>
    /// Asynchronously chains a new asynchronous operation from the success value of the result in the <see cref="ValueTask"/>.
    /// </summary>
    /// <typeparam name="TCurrent">The type of the current success value.</typeparam>
    /// <typeparam name="TNext">The type of the next success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="asyncBinder">An asynchronous function that takes the success value and returns a new <see cref="ValueTask{Result}"/>.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> representing the chained operation.</returns>
    public static async ValueTask<Result<TNext, TError>> ThenAsync<TCurrent, TNext, TError>(
        this ValueTask<Result<TCurrent, TError>> resultTask,
        Func<TCurrent, ValueTask<Result<TNext, TError>>> asyncBinder) where TCurrent : notnull where TNext : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return await result.ThenAsync(asyncBinder);
    }

    /// <summary>
    /// Asynchronously chains a new operation from the success value of the result in the <see cref="ValueTask"/>, passing additional state.
    /// </summary>
    /// <typeparam name="TCurrent">The type of the current success value.</typeparam>
    /// <typeparam name="TNext">The type of the next success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the binder.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="binder">A function that takes the success value and state, and returns a new <see cref="Result{TNext, TError}"/>.</param>
    /// <param name="state">The state to pass to the binder.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> representing the chained operation.</returns>
    public static async ValueTask<Result<TNext, TError>> ThenAsync<TCurrent, TNext, TError, TState>(
        this ValueTask<Result<TCurrent, TError>> resultTask,
        Func<TCurrent, TState, Result<TNext, TError>> binder, TState state) where TCurrent : notnull where TNext : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.Then(binder, state);
    }

    /// <summary>
    /// Asynchronously maps the success value of the result in the <see cref="ValueTask"/> to a new value.
    /// </summary>
    /// <typeparam name="TCurrent">The type of the current success value.</typeparam>
    /// <typeparam name="TNext">The type of the next success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="mapper">A function to map the success value to a new value.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> with the mapped success value or the original error.</returns>
    public static async ValueTask<Result<TNext, TError>> MapAsync<TCurrent, TNext, TError>(
        this ValueTask<Result<TCurrent, TError>> resultTask,
        Func<TCurrent, TNext> mapper) where TCurrent : notnull where TNext : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.Map(mapper);
    }

    /// <summary>
    /// Asynchronously maps the success value of the result in the <see cref="ValueTask"/> to a new value using an asynchronous mapper.
    /// </summary>
    /// <typeparam name="TCurrent">The type of the current success value.</typeparam>
    /// <typeparam name="TNext">The type of the next success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="asyncMapper">An asynchronous function to map the success value to a new value.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> with the mapped success value or the original error.</returns>
    public static async ValueTask<Result<TNext, TError>> MapAsync<TCurrent, TNext, TError>(
        this ValueTask<Result<TCurrent, TError>> resultTask,
        Func<TCurrent, ValueTask<TNext>> asyncMapper) where TCurrent : notnull where TNext : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return await result.MapAsync(asyncMapper);
    }

    /// <summary>
    /// Asynchronously maps the success value of the result in the <see cref="ValueTask"/> to a new value, passing additional state.
    /// </summary>
    /// <typeparam name="TCurrent">The type of the current success value.</typeparam>
    /// <typeparam name="TNext">The type of the next success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the mapper.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="mapper">A function to map the success value to a new value, taking additional state.</param>
    /// <param name="state">The state to pass to the mapper.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> with the mapped success value or the original error.</returns>
    public static async ValueTask<Result<TNext, TError>> MapAsync<TCurrent, TNext, TError, TState>(
        this ValueTask<Result<TCurrent, TError>> resultTask,
        Func<TCurrent, TState, TNext> mapper, TState state) where TCurrent : notnull where TNext : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.Map(mapper, state);
    }

    /// <summary>
    /// Asynchronously performs an action on the success value of the result in the <see cref="ValueTask"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="action">The action to perform on the success value.</param>
    /// <returns>The original <see cref="ValueTask{Result}"/>, allowing for fluent chaining.</returns>
    public static async ValueTask<Result<TValue, TError>> OnSuccessAsync<TValue, TError>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Action<TValue> action) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.OnSuccess(action);
    }

    /// <summary>
    /// Asynchronously performs an asynchronous action on the success value of the result in the <see cref="ValueTask"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="asyncAction">The asynchronous action to perform on the success value.</param>
    /// <returns>The original <see cref="ValueTask{Result}"/>, allowing for fluent chaining.</returns>
    public static async ValueTask<Result<TValue, TError>> OnSuccessAsync<TValue, TError>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TValue, ValueTask> asyncAction) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return await result.OnSuccessAsync(asyncAction);
    }

    /// <summary>
    /// Asynchronously performs an action on the error value of the result in the <see cref="ValueTask"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="action">The action to perform on the error value.</param>
    /// <returns>The original <see cref="ValueTask{Result}"/>, allowing for fluent chaining.</returns>
    public static async ValueTask<Result<TValue, TError>> OnErrorAsync<TValue, TError>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Action<TError> action) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.OnError(action);
    }

    /// <summary>
    /// Asynchronously performs an asynchronous action on the error value of the result in the <see cref="ValueTask"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="asyncAction">The asynchronous action to perform on the error value.</param>
    /// <returns>The original <see cref="ValueTask{Result}"/>, allowing for fluent chaining.</returns>
    public static async ValueTask<Result<TValue, TError>> OnErrorAsync<TValue, TError>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TError, ValueTask> asyncAction) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return await result.OnErrorAsync(asyncAction);
    }

    /// <summary>
    /// Asynchronously transforms the error value of the result in the <see cref="ValueTask"/> using the specified mapping function.
    /// If this result is a success, the success value is propagated.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TCurrent">The type of the current error value.</typeparam>
    /// <typeparam name="TNext">The type of the next error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="mapper">A function to transform the error value.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the original success value or the transformed error.</returns>
    public static async ValueTask<Result<TValue, TNext>> MapErrorAsync<TValue, TCurrent, TNext>(
        this ValueTask<Result<TValue, TCurrent>> resultTask,
        Func<TCurrent, TNext> mapper) where TValue : notnull where TCurrent : notnull where TNext : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.MapError(mapper);
    }

    /// <summary>
    /// Asynchronously transforms the error value of the result in the <see cref="ValueTask"/> using the specified asynchronous mapping function.
    /// If this result is a success, the success value is propagated.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TCurrent">The type of the current error value.</typeparam>
    /// <typeparam name="TNext">The type of the next error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="asyncMapper">An asynchronous function to transform the error value.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the original success value or the transformed error.</returns>
    public static async ValueTask<Result<TValue, TNext>> MapErrorAsync<TValue, TCurrent, TNext>(
        this ValueTask<Result<TValue, TCurrent>> resultTask,
        Func<TCurrent, ValueTask<TNext>> asyncMapper) where TValue : notnull where TCurrent : notnull where TNext : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? result.Value : Kurrent.Result.Failure<TValue, TNext>(await asyncMapper(result.Error).ConfigureAwait(false));
    }

    /// <summary>
    /// Asynchronously transforms the error value of the result in the <see cref="ValueTask"/> using the specified mapping function, passing additional state.
    /// If this result is a success, the success value is propagated.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TCurrent">The type of the current error value.</typeparam>
    /// <typeparam name="TNext">The type of the next error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the mapper.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="mapper">A function to transform the error value, taking additional state.</param>
    /// <param name="state">The state to pass to the mapper.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the original success value or the transformed error.</returns>
    public static async ValueTask<Result<TValue, TNext>> MapErrorAsync<TValue, TCurrent, TNext, TState>(
        this ValueTask<Result<TValue, TCurrent>> resultTask,
        Func<TCurrent, TState, TNext> mapper, TState state
    ) where TValue : notnull where TCurrent : notnull where TNext : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? result.Value : Kurrent.Result.Failure<TValue, TNext>(mapper(result.Error, state));
    }

    /// <summary>
    /// Asynchronously transforms the error value of the result in the <see cref="ValueTask"/> using the specified asynchronous mapping function, passing additional state.
    /// If this result is a success, the success value is propagated.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TCurrent">The type of the current error value.</typeparam>
    /// <typeparam name="TNext">The type of the next error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the mapper.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="asyncMapper">An asynchronous function to transform the error value, taking additional state.</param>
    /// <param name="state">The state to pass to the mapper.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the original success value or the transformed error.</returns>
    public static async ValueTask<Result<TValue, TNext>> MapErrorAsync<TValue, TCurrent, TNext, TState>(
        this ValueTask<Result<TValue, TCurrent>> resultTask,
        Func<TCurrent, TState, ValueTask<TNext>> asyncMapper, TState state
    ) where TValue : notnull where TCurrent : notnull where TNext : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? result.Value : Kurrent.Result.Failure<TValue, TNext>(await asyncMapper(result.Error, state).ConfigureAwait(false));
    }

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether the result in the <see cref="ValueTask"/> is a success or an error, returning a new value.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the value returned by the matching functions.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The function to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The function to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public static async ValueTask<TOut> MatchAsync<TValue, TError, TOut>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TValue, TOut> onSuccess,
        Func<TError, TOut> onError) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.Match(onSuccess, onError);
    }

    /// <summary>
    /// Asynchronously executes one of the two provided asynchronous functions depending on whether the result in the <see cref="ValueTask"/> is a success or an error, returning a new value.
    /// Both functions are asynchronous and return <see cref="ValueTask{TOut}"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the value returned by the matching functions.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The asynchronous function to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The asynchronous function to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public static async ValueTask<TOut> MatchAsync<TValue, TError, TOut>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TValue, ValueTask<TOut>> onSuccess,
        Func<TError, ValueTask<TOut>> onError) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return await result.MatchAsync(onSuccess, onError).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether the result in the <see cref="ValueTask"/> is a success or an error, returning a new value.
    /// The success function is synchronous while the error function is asynchronous.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the value returned by the matching functions.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The synchronous function to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The asynchronous function to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public static async ValueTask<TOut> MatchAsync<TValue, TError, TOut>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TValue, TOut> onSuccess,
        Func<TError, ValueTask<TOut>> onError) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return await result.MatchAsync(onSuccess, onError).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether the result in the <see cref="ValueTask"/> is a success or an error, returning a new value.
    /// The success function is asynchronous while the error function is synchronous.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the value returned by the matching functions.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The asynchronous function to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The synchronous function to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public static async ValueTask<TOut> MatchAsync<TValue, TError, TOut>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TValue, ValueTask<TOut>> onSuccess,
        Func<TError, TOut> onError) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return await result.MatchAsync(onSuccess, onError).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether the result in the <see cref="ValueTask"/> is a success or an error, returning a new value and passing additional state.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the value returned by the matching functions.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The function to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The function to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the functions.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public static async ValueTask<TOut> MatchAsync<TValue, TError, TOut, TState>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TValue, TState, TOut> onSuccess,
        Func<TError, TState, TOut> onError,
        TState state) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.Match(onSuccess, onError, state);
    }

    /// <summary>
    /// Asynchronously executes one of the two provided asynchronous functions depending on whether the result in the <see cref="ValueTask"/> is a success or an error, returning a new value and passing additional state.
    /// Both functions are asynchronous and return <see cref="ValueTask{TOut}"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the value returned by the matching functions.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The asynchronous function to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The asynchronous function to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the functions.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public static async ValueTask<TOut> MatchAsync<TValue, TError, TOut, TState>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TValue, TState, ValueTask<TOut>> onSuccess,
        Func<TError, TState, ValueTask<TOut>> onError,
        TState state) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return await result.MatchAsync(onSuccess, onError, state).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether the result in the <see cref="ValueTask"/> is a success or an error, returning a new value and passing additional state.
    /// The success function is synchronous while the error function is asynchronous.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the value returned by the matching functions.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The synchronous function to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The asynchronous function to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the functions.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public static async ValueTask<TOut> MatchAsync<TValue, TError, TOut, TState>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TValue, TState, TOut> onSuccess,
        Func<TError, TState, ValueTask<TOut>> onError,
        TState state) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return await result.MatchAsync(onSuccess, onError, state).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously executes one of the two provided functions depending on whether the result in the <see cref="ValueTask"/> is a success or an error, returning a new value and passing additional state.
    /// The success function is asynchronous while the error function is synchronous.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the value returned by the matching functions.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The asynchronous function to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The synchronous function to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the functions.</param>
    /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
    public static async ValueTask<TOut> MatchAsync<TValue, TError, TOut, TState>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TValue, TState, ValueTask<TOut>> onSuccess,
        Func<TError, TState, TOut> onError,
        TState state) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return await result.MatchAsync(onSuccess, onError, state).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously applies a synchronous action to the <see cref="Result{TValue, TError}"/> within the <see cref="ValueTask"/>
    /// and returns the original result, allowing for fluent chaining.
    /// This is useful for performing side effects, like logging, based on the result.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="action">The synchronous action to perform, which accepts the <see cref="Result{TValue, TError}"/>.</param>
    /// <returns>The original <see cref="ValueTask{Result}"/>, allowing for fluent chaining.</returns>
    public static async ValueTask<Result<TValue, TError>> ApplyAsync<TValue, TError>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Action<Result<TValue, TError>> action)
        where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        action(result);
        return result;
    }

    /// <summary>
    /// Asynchronously applies an asynchronous action to the <see cref="Result{TValue, TError}"/> within the <see cref="ValueTask"/>
    /// and returns the original result, allowing for fluent chaining.
    /// This is useful for performing asynchronous side effects, like logging, based on the result.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="asyncAction">The asynchronous action to perform, which accepts the <see cref="Result{TValue, TError}"/> and returns a <see cref="ValueTask"/>.</param>
    /// <returns>The original <see cref="ValueTask{Result}"/>, allowing for fluent chaining.</returns>
    public static async ValueTask<Result<TValue, TError>> ApplyAsync<TValue, TError>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<Result<TValue, TError>, ValueTask> asyncAction)
        where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        await asyncAction(result).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Asynchronously applies a synchronous action to the <see cref="Result{TValue, TError}"/> within the <see cref="ValueTask"/>,
    /// passing additional state, and returns the original result, allowing for fluent chaining.
    /// This is useful for performing side effects with state, like logging, based on the result.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="action">The synchronous action to perform, which accepts the <see cref="Result{TValue, TError}"/> and state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>The original <see cref="ValueTask{Result}"/>, allowing for fluent chaining.</returns>
    public static async ValueTask<Result<TValue, TError>> ApplyAsync<TValue, TError, TState>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Action<Result<TValue, TError>, TState> action,
        TState state)
        where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        action(result, state);
        return result;
    }

    /// <summary>
    /// Asynchronously applies an asynchronous action to the <see cref="Result{TValue, TError}"/> within the <see cref="ValueTask"/>,
    /// passing additional state, and returns the original result, allowing for fluent chaining.
    /// This is useful for performing asynchronous side effects with state, like logging, based on the result.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="asyncAction">The asynchronous action to perform, which accepts the <see cref="Result{TValue, TError}"/> and state, and returns a <see cref="ValueTask"/>.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>The original <see cref="ValueTask{Result}"/>, allowing for fluent chaining.</returns>
    public static async ValueTask<Result<TValue, TError>> ApplyAsync<TValue, TError, TState>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<Result<TValue, TError>, TState, ValueTask> asyncAction,
        TState state)
        where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        await asyncAction(result, state).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Asynchronously ensures that the success value of the result in the <see cref="ValueTask"/> meets a specified condition.
    /// If the result is a success and the condition is not met, the result is transformed into an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="predicate">The condition to check on the success value.</param>
    /// <param name="errorFactory">A function that creates an error value if the condition is not met.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the original result if it's an error or if the condition is met; otherwise, a new error result.</returns>
    public static async ValueTask<Result<TValue, TError>> EnsureAsync<TValue, TError>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TValue, bool> predicate,
        Func<TValue, TError> errorFactory) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.Ensure(predicate, errorFactory);
    }

    /// <summary>
    /// Asynchronously ensures that the success value of the result in the <see cref="ValueTask"/> meets a specified condition, using additional state.
    /// If the result is a success and the condition is not met, the result is transformed into an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the predicate and error factory.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="predicate">The condition to check on the success value.</param>
    /// <param name="errorFactory">A function that creates an error value if the condition is not met.</param>
    /// <param name="state">The state to pass to the predicate and error factory.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the original result if it's an error or if the condition is met; otherwise, a new error result.</returns>
    public static async ValueTask<Result<TValue, TError>> EnsureAsync<TValue, TError, TState>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TValue, TState, bool> predicate,
        Func<TValue, TState, TError> errorFactory,
        TState state) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.Ensure(predicate, errorFactory, state);
    }

    /// <summary>
    /// Asynchronously ensures that the success value of the result in the <see cref="ValueTask"/> meets a specified asynchronous condition.
    /// If the result is a success and the condition is not met, the result is transformed into an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="predicate">The asynchronous condition to check on the success value.</param>
    /// <param name="errorFactory">A function that creates an error value if the condition is not met.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the original result if it's an error or if the condition is met; otherwise, a new error result.</returns>
    public static async ValueTask<Result<TValue, TError>> EnsureAsync<TValue, TError>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TValue, ValueTask<bool>> predicate,
        Func<TValue, TError> errorFactory) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return await result.EnsureAsync(predicate, errorFactory).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously ensures that the success value of the result in the <see cref="ValueTask"/> meets a specified asynchronous condition, using additional state.
    /// If the result is a success and the condition is not met, the result is transformed into an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the predicate and error factory.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="predicate">The asynchronous condition to check on the success value.</param>
    /// <param name="errorFactory">A function that creates an error value if the condition is not met.</param>
    /// <param name="state">The state to pass to the predicate and error factory.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the original result if it's an error or if the condition is met; otherwise, a new error result.</returns>
    public static async ValueTask<Result<TValue, TError>> EnsureAsync<TValue, TError, TState>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TValue, TState, ValueTask<bool>> predicate,
        Func<TValue, TState, TError> errorFactory,
        TState state) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return await result.EnsureAsync(predicate, errorFactory, state).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously gets the success value from the result in the <see cref="ValueTask"/> if it's a success; otherwise, returns a fallback value computed from the error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="fallback">A function that takes the error value and returns a fallback success value.</param>
    /// <returns>A <see cref="ValueTask{TValue}"/> representing the asynchronous operation with the success value or the result of the <paramref name="fallback"/> function.</returns>
    public static async ValueTask<TValue> GetValueOrDefaultAsync<TValue, TError>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TError, TValue> fallback) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.GetValueOrDefault(fallback);
    }

    /// <summary>
    /// Asynchronously gets the success value from the result in the <see cref="ValueTask"/> if it's a success; otherwise, returns a fallback value computed from the error, passing additional state.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the fallback function.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="fallback">A function that takes the error value and state, and returns a fallback success value.</param>
    /// <param name="state">The state to pass to the fallback function.</param>
    /// <returns>A <see cref="ValueTask{TValue}"/> representing the asynchronous operation with the success value or the result of the <paramref name="fallback"/> function.</returns>
    public static async ValueTask<TValue> GetValueOrDefaultAsync<TValue, TError, TState>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TError, TState, TValue> fallback,
        TState state) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.GetValueOrDefault(fallback, state);
    }

    /// <summary>
    /// Asynchronously gets the success value from the result in the <see cref="ValueTask"/> if it's a success; otherwise, returns a fallback value computed asynchronously from the error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="fallback">An asynchronous function that takes the error value and returns a fallback success value.</param>
    /// <returns>A <see cref="ValueTask{TValue}"/> representing the asynchronous operation with the success value or the result of the <paramref name="fallback"/> function.</returns>
    public static async ValueTask<TValue> GetValueOrDefaultAsync<TValue, TError>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TError, ValueTask<TValue>> fallback) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return await result.GetValueOrDefaultAsync(fallback).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously gets the success value from the result in the <see cref="ValueTask"/> if it's a success; otherwise, returns a fallback value computed asynchronously from the error, passing additional state.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the fallback function.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="fallback">An asynchronous function that takes the error value and state, and returns a fallback success value.</param>
    /// <param name="state">The state to pass to the fallback function.</param>
    /// <returns>A <see cref="ValueTask{TValue}"/> representing the asynchronous operation with the success value or the result of the <paramref name="fallback"/> function.</returns>
    public static async ValueTask<TValue> GetValueOrDefaultAsync<TValue, TError, TState>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TError, TState, ValueTask<TValue>> fallback,
        TState state) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return await result.GetValueOrDefaultAsync(fallback, state).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously executes one of the two provided actions depending on whether the result in the <see cref="ValueTask"/> is a success or an error.
    /// Both actions are synchronous and this method is designed for side effects without return values.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The action to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The action to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public static async ValueTask SwitchAsync<TValue, TError>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Action<TValue> onSuccess,
        Action<TError> onError) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        result.Switch(onSuccess, onError);
    }

    /// <summary>
    /// Asynchronously executes one of the two provided asynchronous actions depending on whether the result in the <see cref="ValueTask"/> is a success or an error.
    /// Both actions are asynchronous and this method is designed for side effects without return values.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The asynchronous action to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The asynchronous action to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public static async ValueTask SwitchAsync<TValue, TError>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TValue, ValueTask> onSuccess,
        Func<TError, ValueTask> onError) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        await result.SwitchAsync(onSuccess, onError).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously executes one of the two provided actions depending on whether the result in the <see cref="ValueTask"/> is a success or an error.
    /// The success action is synchronous while the error action is asynchronous. This method is designed for side effects without return values.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The synchronous action to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The asynchronous action to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public static async ValueTask SwitchAsync<TValue, TError>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Action<TValue> onSuccess,
        Func<TError, ValueTask> onError) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        await result.SwitchAsync(onSuccess, onError).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously executes one of the two provided actions depending on whether the result in the <see cref="ValueTask"/> is a success or an error.
    /// The success action is asynchronous while the error action is synchronous. This method is designed for side effects without return values.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The asynchronous action to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The synchronous action to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public static async ValueTask SwitchAsync<TValue, TError>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TValue, ValueTask> onSuccess,
        Action<TError> onError) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        await result.SwitchAsync(onSuccess, onError).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously executes one of the two provided actions depending on whether the result in the <see cref="ValueTask"/> is a success or an error, passing additional state.
    /// Both actions are synchronous and this method is designed for side effects without return values.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the actions.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The action to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The action to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the actions.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public static async ValueTask SwitchAsync<TValue, TError, TState>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Action<TValue, TState> onSuccess,
        Action<TError, TState> onError,
        TState state) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        result.Switch(onSuccess, onError, state);
    }

    /// <summary>
    /// Asynchronously executes one of the two provided asynchronous actions depending on whether the result in the <see cref="ValueTask"/> is a success or an error, passing additional state.
    /// Both actions are asynchronous and this method is designed for side effects without return values.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the actions.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The asynchronous action to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The asynchronous action to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the actions.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public static async ValueTask SwitchAsync<TValue, TError, TState>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TValue, TState, ValueTask> onSuccess,
        Func<TError, TState, ValueTask> onError,
        TState state) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        await result.SwitchAsync(onSuccess, onError, state).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously executes one of the two provided actions depending on whether the result in the <see cref="ValueTask"/> is a success or an error, passing additional state.
    /// The success action is synchronous while the error action is asynchronous. This method is designed for side effects without return values.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the actions.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The synchronous action to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The asynchronous action to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the actions.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public static async ValueTask SwitchAsync<TValue, TError, TState>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Action<TValue, TState> onSuccess,
        Func<TError, TState, ValueTask> onError,
        TState state) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        await result.SwitchAsync(onSuccess, onError, state).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously executes one of the two provided actions depending on whether the result in the <see cref="ValueTask"/> is a success or an error, passing additional state.
    /// The success action is asynchronous while the error action is synchronous. This method is designed for side effects without return values.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the actions.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="onSuccess">The asynchronous action to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The synchronous action to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the actions.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public static async ValueTask SwitchAsync<TValue, TError, TState>(
        this ValueTask<Result<TValue, TError>> resultTask,
        Func<TValue, TState, ValueTask> onSuccess,
        Action<TError, TState> onError,
        TState state) where TValue : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        await result.SwitchAsync(onSuccess, onError, state).ConfigureAwait(false);
    }

    #endregion
}
