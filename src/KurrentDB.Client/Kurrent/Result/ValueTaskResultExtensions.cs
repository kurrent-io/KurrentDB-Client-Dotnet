namespace Kurrent;

[PublicAPI]
public static class ValueTaskResultExtensions {
    /// <summary>
    /// Converts a <see cref="ValueTask{TSuccess}"/> into a <see cref="ValueTask{Result}"/>, handling exceptions.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="task">The value task to convert.</param>
    /// <param name="errorHandler">The function to execute if the task faults.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> representing the outcome of the task.</returns>
    public static async ValueTask<Result<TSuccess, TError>> AsResultAsync<TSuccess, TError>(this ValueTask<TSuccess> task, Func<Exception, TError> errorHandler) where TSuccess : notnull where TError : notnull {
        try {
            var result = await task.ConfigureAwait(false);
            return Result<TSuccess, TError>.Success(result);
        }
        catch (Exception ex) {
            return Result<TSuccess, TError>.Error(errorHandler(ex));
        }
    }

    /// <summary>
    /// Converts a <see cref="ValueTask"/> into a <see cref="ValueTask{Result}"/>, handling exceptions.
    /// </summary>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="task">The value task to convert.</param>
    /// <param name="errorHandler">The function to execute if the task faults.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> representing the outcome of the task.</returns>
    public static async ValueTask<Result<Unit, TError>> AsResultAsync<TError>(this ValueTask task, Func<Exception, TError> errorHandler) where TError : notnull {
        try {
            await task.ConfigureAwait(false);
            return Result<Unit, TError>.Success(Unit.Value);
        }
        catch (Exception ex) {
            return Result<Unit, TError>.Error(errorHandler(ex));
        }
    }

    /// <summary>
    /// Converts a <see cref="Task{TSuccess}"/> into a <see cref="ValueTask{Result}"/>, handling exceptions.
    /// </summary>
    public static async ValueTask<Result<TSuccess, TError>> AsResultAsync<TSuccess, TError>(this Task<TSuccess> task, Func<Exception, TError> errorHandler) where TSuccess : notnull where TError : notnull {
        try {
            var result = await task.ConfigureAwait(false);
            return Result<TSuccess, TError>.Success(result);
        }
        catch (Exception ex) {
            return Result<TSuccess, TError>.Error(errorHandler(ex));
        }
    }

    /// <summary>
    /// Converts a <see cref="Task"/> into a <see cref="ValueTask{Result}"/>, handling exceptions.
    /// </summary>
    public static async ValueTask<Result<Unit, TError>> AsResultAsync<TError>(this Task task, Func<Exception, TError> errorHandler) where TError : notnull {
        try {
            await task.ConfigureAwait(false);
            return Result<Unit, TError>.Success(Unit.Value);
        }
        catch (Exception ex) {
            return Result<Unit, TError>.Error(errorHandler(ex));
        }
    }

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
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="action">The action to perform on the success value.</param>
    /// <returns>The original <see cref="ValueTask{Result}"/>, allowing for fluent chaining.</returns>
    public static async ValueTask<Result<TSuccess, TError>> OnSuccessAsync<TSuccess, TError>(
        this ValueTask<Result<TSuccess, TError>> resultTask,
        Action<TSuccess> action) where TSuccess : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.OnSuccess(action);
    }

    /// <summary>
    /// Asynchronously performs an asynchronous action on the success value of the result in the <see cref="ValueTask"/>.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="asyncAction">The asynchronous action to perform on the success value.</param>
    /// <returns>The original <see cref="ValueTask{Result}"/>, allowing for fluent chaining.</returns>
    public static async ValueTask<Result<TSuccess, TError>> OnSuccessAsync<TSuccess, TError>(
        this ValueTask<Result<TSuccess, TError>> resultTask,
        Func<TSuccess, ValueTask> asyncAction) where TSuccess : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return await result.OnSuccessAsync(asyncAction);
    }

    /// <summary>
    /// Asynchronously performs an action on the error value of the result in the <see cref="ValueTask"/>.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="action">The action to perform on the error value.</param>
    /// <returns>The original <see cref="ValueTask{Result}"/>, allowing for fluent chaining.</returns>
    public static async ValueTask<Result<TSuccess, TError>> OnErrorAsync<TSuccess, TError>(
        this ValueTask<Result<TSuccess, TError>> resultTask,
        Action<TError> action) where TSuccess : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return result.OnError(action);
    }

    /// <summary>
    /// Asynchronously performs an asynchronous action on the error value of the result in the <see cref="ValueTask"/>.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The value task containing the result.</param>
    /// <param name="asyncAction">The asynchronous action to perform on the error value.</param>
    /// <returns>The original <see cref="ValueTask{Result}"/>, allowing for fluent chaining.</returns>
    public static async ValueTask<Result<TSuccess, TError>> OnErrorAsync<TSuccess, TError>(
        this ValueTask<Result<TSuccess, TError>> resultTask,
        Func<TError, ValueTask> asyncAction) where TSuccess : notnull where TError : notnull {
        var result = await resultTask.ConfigureAwait(false);
        return await result.OnErrorAsync(asyncAction);
    }
}
