using System;
using System.Threading.Tasks;

namespace KurrentDB.Client.V2;

/// <summary>
/// Provides a set of asynchronous extension methods for the <see cref="Result{TError,TSuccess}"/> type.
/// These methods facilitate working with Results in an asynchronous pipeline,
/// allowing for chaining operations that involve Tasks or ValueTasks.
/// </summary>
public static partial class ResultAsyncExtensions
{
    #region . Task<Result<TError, TSuccess>> Source Extensions .

    // ThenAsync: Task<Result> source, Task<Result> binder

    /// <summary>
    /// Asynchronously binds the success value of a Task of Result to a new Task of Result.
    /// If the source Result is an error, the error is propagated, and the binder is not called.
    /// </summary>
    public static async Task<Result<TError, TSuccessOut>> ThenAsync<TError, TSuccess, TSuccessOut>(
        this Task<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, Task<Result<TError, TSuccessOut>>> binder)
    {
        if (resultTask == null) throw new ArgumentNullException(nameof(resultTask));
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        return await binder(result.SuccessValue()).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously binds the success value of a Task of Result to a new Task of Result, using a state value.
    /// If the source Result is an error, the error is propagated, and the binder is not called.
    /// </summary>
    public static async Task<Result<TError, TSuccessOut>> ThenAsync<TError, TSuccess, TSuccessOut, TState>(
        this Task<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, TState, Task<Result<TError, TSuccessOut>>> binder,
        TState state)
    {
        if (resultTask == null) throw new ArgumentNullException(nameof(resultTask));
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        return await binder(result.SuccessValue(), state).ConfigureAwait(false);
    }

    // ThenAsync: Task<Result> source, Result binder (synchronous)

    /// <summary>
    /// Asynchronously binds the success value of a Task of Result to a new synchronous Result.
    /// If the source Result is an error, the error is propagated, and the binder is not called.
    /// </summary>
    public static async Task<Result<TError, TSuccessOut>> ThenAsync<TError, TSuccess, TSuccessOut>(
        this Task<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, Result<TError, TSuccessOut>> binder)
    {
        if (resultTask == null) throw new ArgumentNullException(nameof(resultTask));
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        var result = await resultTask.ConfigureAwait(false);
        return result.Then(binder); // Uses the synchronous .Then()
    }

    /// <summary>
    /// Asynchronously binds the success value of a Task of Result to a new synchronous Result, using a state value.
    /// If the source Result is an error, the error is propagated, and the binder is not called.
    /// </summary>
    public static async Task<Result<TError, TSuccessOut>> ThenAsync<TError, TSuccess, TSuccessOut, TState>(
        this Task<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, TState, Result<TError, TSuccessOut>> binder,
        TState state)
    {
        if (resultTask == null) throw new ArgumentNullException(nameof(resultTask));
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        var result = await resultTask.ConfigureAwait(false);
        return result.Then(binder, state); // Uses the synchronous .Then() with state
    }
    
    // ThenAsync: Task<Result> source, ValueTask<Result> binder

    /// <summary>
    /// Asynchronously binds the success value of a Task of Result to a new ValueTask of Result.
    /// If the source Result is an error, the error is propagated, and the binder is not called.
    /// </summary>
    public static async Task<Result<TError, TSuccessOut>> ThenAsync<TError, TSuccess, TSuccessOut>(
        this Task<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, ValueTask<Result<TError, TSuccessOut>>> binder)
    {
        if (resultTask == null) throw new ArgumentNullException(nameof(resultTask));
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        return await binder(result.SuccessValue()).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously binds the success value of a Task of Result to a new ValueTask of Result, using a state value.
    /// If the source Result is an error, the error is propagated, and the binder is not called.
    /// </summary>
    public static async Task<Result<TError, TSuccessOut>> ThenAsync<TError, TSuccess, TSuccessOut, TState>(
        this Task<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, TState, ValueTask<Result<TError, TSuccessOut>>> binder,
        TState state)
    {
        if (resultTask == null) throw new ArgumentNullException(nameof(resultTask));
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        return await binder(result.SuccessValue(), state).ConfigureAwait(false);
    }

    // MapAsync: Task<Result> source, Task<TSuccessOut> mapper

    /// <summary>
    /// Asynchronously maps the success value of a Task of Result to a new success value using a Task-returning mapper.
    /// If the source Result is an error, the error is propagated, and the mapper is not called.
    /// </summary>
    public static async Task<Result<TError, TSuccessOut>> MapAsync<TError, TSuccess, TSuccessOut>(
        this Task<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, Task<TSuccessOut>> mapper)
    {
        if (resultTask == null) throw new ArgumentNullException(nameof(resultTask));
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        var mappedValue = await mapper(result.SuccessValue()).ConfigureAwait(false);
        return Result<TError, TSuccessOut>.Success(mappedValue);
    }

    /// <summary>
    /// Asynchronously maps the success value of a Task of Result to a new success value using a Task-returning mapper and a state value.
    /// If the source Result is an error, the error is propagated, and the mapper is not called.
    /// </summary>
    public static async Task<Result<TError, TSuccessOut>> MapAsync<TError, TSuccess, TSuccessOut, TState>(
        this Task<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, TState, Task<TSuccessOut>> mapper,
        TState state)
    {
        if (resultTask == null) throw new ArgumentNullException(nameof(resultTask));
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        var mappedValue = await mapper(result.SuccessValue(), state).ConfigureAwait(false);
        return Result<TError, TSuccessOut>.Success(mappedValue);
    }

    // OrElseAsync: Task<Result> source, Task<Result> binder

    /// <summary>
    /// Asynchronously binds the error value of a Task of Result to a new Task of Result.
    /// If the source Result is a success, the success is propagated, and the binder is not called.
    /// </summary>
    public static async Task<Result<TErrorOut, TSuccess>> OrElseAsync<TError, TSuccess, TErrorOut>(
        this Task<Result<TError, TSuccess>> resultTask,
        Func<TError, Task<Result<TErrorOut, TSuccess>>> binder)
    {
        if (resultTask == null) throw new ArgumentNullException(nameof(resultTask));
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());

        return await binder(result.ErrorValue()).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously binds the error value of a Task of Result to a new Task of Result, using a state value.
    /// If the source Result is a success, the success is propagated, and the binder is not called.
    /// </summary>
    public static async Task<Result<TErrorOut, TSuccess>> OrElseAsync<TError, TSuccess, TErrorOut, TState>(
        this Task<Result<TError, TSuccess>> resultTask,
        Func<TError, TState, Task<Result<TErrorOut, TSuccess>>> binder,
        TState state)
    {
        if (resultTask == null) throw new ArgumentNullException(nameof(resultTask));
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());

        return await binder(result.ErrorValue(), state).ConfigureAwait(false);
    }

    // MapErrorAsync: Task<Result> source, Task<TErrorOut> mapper

    /// <summary>
    /// Asynchronously maps the error value of a Task of Result to a new error value using a Task-returning mapper.
    /// If the source Result is a success, the success is propagated, and the mapper is not called.
    /// </summary>
    public static async Task<Result<TErrorOut, TSuccess>> MapErrorAsync<TError, TSuccess, TErrorOut>(
        this Task<Result<TError, TSuccess>> resultTask,
        Func<TError, Task<TErrorOut>> mapper)
    {
        if (resultTask == null) throw new ArgumentNullException(nameof(resultTask));
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());

        var mappedError = await mapper(result.ErrorValue()).ConfigureAwait(false);
        return Result<TErrorOut, TSuccess>.Error(mappedError);
    }

    /// <summary>
    /// Asynchronously maps the error value of a Task of Result to a new error value using a Task-returning mapper and a state value.
    /// If the source Result is a success, the success is propagated, and the mapper is not called.
    /// </summary>
    public static async Task<Result<TErrorOut, TSuccess>> MapErrorAsync<TError, TSuccess, TErrorOut, TState>(
        this Task<Result<TError, TSuccess>> resultTask,
        Func<TError, TState, Task<TErrorOut>> mapper,
        TState state)
    {
        if (resultTask == null) throw new ArgumentNullException(nameof(resultTask));
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());

        var mappedError = await mapper(result.ErrorValue(), state).ConfigureAwait(false);
        return Result<TErrorOut, TSuccess>.Error(mappedError);
    }

    // OnSuccessAsync: Task<Result> source, Task action

    /// <summary>
    /// Asynchronously performs an action on the success value of a Task of Result.
    /// If the source Result is an error, the action is not performed.
    /// Returns the original Task of Result.
    /// </summary>
    public static async Task<Result<TError, TSuccess>> OnSuccessAsync<TError, TSuccess>(
        this Task<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, Task> action)
    {
        if (resultTask == null) throw new ArgumentNullException(nameof(resultTask));
        if (action == null) throw new ArgumentNullException(nameof(action));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            await action(result.SuccessValue()).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Asynchronously performs an action on the success value of a Task of Result, using a state value.
    /// If the source Result is an error, the action is not performed.
    /// Returns the original Task of Result.
    /// </summary>
    public static async Task<Result<TError, TSuccess>> OnSuccessAsync<TError, TSuccess, TState>(
        this Task<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, TState, Task> action,
        TState state)
    {
        if (resultTask == null) throw new ArgumentNullException(nameof(resultTask));
        if (action == null) throw new ArgumentNullException(nameof(action));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            await action(result.SuccessValue(), state).ConfigureAwait(false);

        return result;
    }

    // OnErrorAsync: Task<Result> source, Task action

    /// <summary>
    /// Asynchronously performs an action on the error value of a Task of Result.
    /// If the source Result is a success, the action is not performed.
    /// Returns the original Task of Result.
    /// </summary>
    public static async Task<Result<TError, TSuccess>> OnErrorAsync<TError, TSuccess>(
        this Task<Result<TError, TSuccess>> resultTask,
        Func<TError, Task> action)
    {
        if (resultTask == null) throw new ArgumentNullException(nameof(resultTask));
        if (action == null) throw new ArgumentNullException(nameof(action));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            await action(result.ErrorValue()).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Asynchronously performs an action on the error value of a Task of Result, using a state value.
    /// If the source Result is a success, the action is not performed.
    /// Returns the original Task of Result.
    /// </summary>
    public static async Task<Result<TError, TSuccess>> OnErrorAsync<TError, TSuccess, TState>(
        this Task<Result<TError, TSuccess>> resultTask,
        Func<TError, TState, Task> action,
        TState state)
    {
        if (resultTask == null) throw new ArgumentNullException(nameof(resultTask));
        if (action == null) throw new ArgumentNullException(nameof(action));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            await action(result.ErrorValue(), state).ConfigureAwait(false);

        return result;
    }

    #endregion

    #region . ValueTask<Result<TError, TSuccess>> Source Extensions .

    // ThenAsync: ValueTask<Result> source, ValueTask<Result> binder

    /// <summary>
    /// Asynchronously binds the success value of a ValueTask of Result to a new ValueTask of Result.
    /// If the source Result is an error, the error is propagated, and the binder is not called.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccessOut>> ThenAsync<TError, TSuccess, TSuccessOut>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, ValueTask<Result<TError, TSuccessOut>>> binder)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        return await binder(result.SuccessValue()).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously binds the success value of a ValueTask of Result to a new ValueTask of Result, using a state value.
    /// If the source Result is an error, the error is propagated, and the binder is not called.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccessOut>> ThenAsync<TError, TSuccess, TSuccessOut, TState>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, TState, ValueTask<Result<TError, TSuccessOut>>> binder,
        TState state)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        return await binder(result.SuccessValue(), state).ConfigureAwait(false);
    }
    
    // ThenAsync: ValueTask<Result> source, Task<Result> binder
    
    /// <summary>
    /// Asynchronously binds the success value of a ValueTask of Result to a new Task of Result.
    /// If the source Result is an error, the error is propagated, and the binder is not called.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccessOut>> ThenAsync<TError, TSuccess, TSuccessOut>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, Task<Result<TError, TSuccessOut>>> binder)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));
    
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());
    
        return await binder(result.SuccessValue()).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Asynchronously binds the success value of a ValueTask of Result to a new Task of Result, using a state value.
    /// If the source Result is an error, the error is propagated, and the binder is not called.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccessOut>> ThenAsync<TError, TSuccess, TSuccessOut, TState>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, TState, Task<Result<TError, TSuccessOut>>> binder,
        TState state)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));
    
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());
    
        return await binder(result.SuccessValue(), state).ConfigureAwait(false);
    }

    // MapAsync: ValueTask<Result> source, ValueTask<TSuccessOut> mapper

    /// <summary>
    /// Asynchronously maps the success value of a ValueTask of Result to a new success value using a ValueTask-returning mapper.
    /// If the source Result is an error, the error is propagated, and the mapper is not called.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccessOut>> MapAsync<TError, TSuccess, TSuccessOut>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, ValueTask<TSuccessOut>> mapper)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        var mappedValue = await mapper(result.SuccessValue()).ConfigureAwait(false);
        return Result<TError, TSuccessOut>.Success(mappedValue);
    }

    /// <summary>
    /// Asynchronously maps the success value of a ValueTask of Result to a new success value using a ValueTask-returning mapper and a state value.
    /// If the source Result is an error, the error is propagated, and the mapper is not called.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccessOut>> MapAsync<TError, TSuccess, TSuccessOut, TState>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, TState, ValueTask<TSuccessOut>> mapper,
        TState state)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        var mappedValue = await mapper(result.SuccessValue(), state).ConfigureAwait(false);
        return Result<TError, TSuccessOut>.Success(mappedValue);
    }
    
    // MapAsync: ValueTask<Result> source, Task<TSuccessOut> mapper
    
    /// <summary>
    /// Asynchronously maps the success value of a ValueTask of Result to a new success value using a Task-returning mapper.
    /// If the source Result is an error, the error is propagated, and the mapper is not called.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccessOut>> MapAsync<TError, TSuccess, TSuccessOut>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, Task<TSuccessOut>> mapper)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));
    
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());
    
        var mappedValue = await mapper(result.SuccessValue()).ConfigureAwait(false);
        return Result<TError, TSuccessOut>.Success(mappedValue);
    }
    
    /// <summary>
    /// Asynchronously maps the success value of a ValueTask of Result to a new success value using a Task-returning mapper and a state value.
    /// If the source Result is an error, the error is propagated, and the mapper is not called.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccessOut>> MapAsync<TError, TSuccess, TSuccessOut, TState>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, TState, Task<TSuccessOut>> mapper,
        TState state)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));
    
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());
    
        var mappedValue = await mapper(result.SuccessValue(), state).ConfigureAwait(false);
        return Result<TError, TSuccessOut>.Success(mappedValue);
    }

    // OrElseAsync: ValueTask<Result> source, ValueTask<Result> binder

    /// <summary>
    /// Asynchronously binds the error value of a ValueTask of Result to a new ValueTask of Result.
    /// If the source Result is a success, the success is propagated, and the binder is not called.
    /// </summary>
    public static async ValueTask<Result<TErrorOut, TSuccess>> OrElseAsync<TError, TSuccess, TErrorOut>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TError, ValueTask<Result<TErrorOut, TSuccess>>> binder)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());

        return await binder(result.ErrorValue()).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously binds the error value of a ValueTask of Result to a new ValueTask of Result, using a state value.
    /// If the source Result is a success, the success is propagated, and the binder is not called.
    /// </summary>
    public static async ValueTask<Result<TErrorOut, TSuccess>> OrElseAsync<TError, TSuccess, TErrorOut, TState>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TError, TState, ValueTask<Result<TErrorOut, TSuccess>>> binder,
        TState state)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());

        return await binder(result.ErrorValue(), state).ConfigureAwait(false);
    }
    
    // OrElseAsync: ValueTask<Result> source, Task<Result> binder
    
    /// <summary>
    /// Asynchronously binds the error value of a ValueTask of Result to a new Task of Result.
    /// If the source Result is a success, the success is propagated, and the binder is not called.
    /// </summary>
    public static async ValueTask<Result<TErrorOut, TSuccess>> OrElseAsync<TError, TSuccess, TErrorOut>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TError, Task<Result<TErrorOut, TSuccess>>> binder)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));
    
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());
    
        return await binder(result.ErrorValue()).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Asynchronously binds the error value of a ValueTask of Result to a new Task of Result, using a state value.
    /// If the source Result is a success, the success is propagated, and the binder is not called.
    /// </summary>
    public static async ValueTask<Result<TErrorOut, TSuccess>> OrElseAsync<TError, TSuccess, TErrorOut, TState>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TError, TState, Task<Result<TErrorOut, TSuccess>>> binder,
        TState state)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));
    
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());
    
        return await binder(result.ErrorValue(), state).ConfigureAwait(false);
    }

    // MapErrorAsync: ValueTask<Result> source, ValueTask<TErrorOut> mapper

    /// <summary>
    /// Asynchronously maps the error value of a ValueTask of Result to a new error value using a ValueTask-returning mapper.
    /// If the source Result is a success, the success is propagated, and the mapper is not called.
    /// </summary>
    public static async ValueTask<Result<TErrorOut, TSuccess>> MapErrorAsync<TError, TSuccess, TErrorOut>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TError, ValueTask<TErrorOut>> mapper)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());

        var mappedError = await mapper(result.ErrorValue()).ConfigureAwait(false);
        return Result<TErrorOut, TSuccess>.Error(mappedError);
    }

    /// <summary>
    /// Asynchronously maps the error value of a ValueTask of Result to a new error value using a ValueTask-returning mapper and a state value.
    /// If the source Result is a success, the success is propagated, and the mapper is not called.
    /// </summary>
    public static async ValueTask<Result<TErrorOut, TSuccess>> MapErrorAsync<TError, TSuccess, TErrorOut, TState>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TError, TState, ValueTask<TErrorOut>> mapper,
        TState state)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());

        var mappedError = await mapper(result.ErrorValue(), state).ConfigureAwait(false);
        return Result<TErrorOut, TSuccess>.Error(mappedError);
    }
    
    // MapErrorAsync: ValueTask<Result> source, Task<TErrorOut> mapper
    
    /// <summary>
    /// Asynchronously maps the error value of a ValueTask of Result to a new error value using a Task-returning mapper.
    /// If the source Result is a success, the success is propagated, and the mapper is not called.
    /// </summary>
    public static async ValueTask<Result<TErrorOut, TSuccess>> MapErrorAsync<TError, TSuccess, TErrorOut>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TError, Task<TErrorOut>> mapper)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));
    
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());
    
        var mappedError = await mapper(result.ErrorValue()).ConfigureAwait(false);
        return Result<TErrorOut, TSuccess>.Error(mappedError);
    }
    
    /// <summary>
    /// Asynchronously maps the error value of a ValueTask of Result to a new error value using a Task-returning mapper and a state value.
    /// If the source Result is a success, the success is propagated, and the mapper is not called.
    /// </summary>
    public static async ValueTask<Result<TErrorOut, TSuccess>> MapErrorAsync<TError, TSuccess, TErrorOut, TState>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TError, TState, Task<TErrorOut>> mapper,
        TState state)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));
    
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());
    
        var mappedError = await mapper(result.ErrorValue(), state).ConfigureAwait(false);
        return Result<TErrorOut, TSuccess>.Error(mappedError);
    }

    // OnSuccessAsync: ValueTask<Result> source, ValueTask action

    /// <summary>
    /// Asynchronously performs an action on the success value of a ValueTask of Result.
    /// If the source Result is an error, the action is not performed.
    /// Returns the original ValueTask of Result.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccess>> OnSuccessAsync<TError, TSuccess>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, ValueTask> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            await action(result.SuccessValue()).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Asynchronously performs an action on the success value of a ValueTask of Result, using a state value.
    /// If the source Result is an error, the action is not performed.
    /// Returns the original ValueTask of Result.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccess>> OnSuccessAsync<TError, TSuccess, TState>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, TState, ValueTask> action,
        TState state)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            await action(result.SuccessValue(), state).ConfigureAwait(false);

        return result;
    }
    
    // OnSuccessAsync: ValueTask<Result> source, Task action
    
    /// <summary>
    /// Asynchronously performs an action on the success value of a ValueTask of Result.
    /// If the source Result is an error, the action is not performed.
    /// Returns the original ValueTask of Result.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccess>> OnSuccessAsync<TError, TSuccess>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, Task> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
    
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            await action(result.SuccessValue()).ConfigureAwait(false);
    
        return result;
    }
    
    /// <summary>
    /// Asynchronously performs an action on the success value of a ValueTask of Result, using a state value.
    /// If the source Result is an error, the action is not performed.
    /// Returns the original ValueTask of Result.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccess>> OnSuccessAsync<TError, TSuccess, TState>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TSuccess, TState, Task> action,
        TState state)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
    
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            await action(result.SuccessValue(), state).ConfigureAwait(false);
    
        return result;
    }

    // OnErrorAsync: ValueTask<Result> source, ValueTask action

    /// <summary>
    /// Asynchronously performs an action on the error value of a ValueTask of Result.
    /// If the source Result is a success, the action is not performed.
    /// Returns the original ValueTask of Result.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccess>> OnErrorAsync<TError, TSuccess>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TError, ValueTask> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            await action(result.ErrorValue()).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Asynchronously performs an action on the error value of a ValueTask of Result, using a state value.
    /// If the source Result is a success, the action is not performed.
    /// Returns the original ValueTask of Result.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccess>> OnErrorAsync<TError, TSuccess, TState>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TError, TState, ValueTask> action,
        TState state)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            await action(result.ErrorValue(), state).ConfigureAwait(false);

        return result;
    }
    
    // OnErrorAsync: ValueTask<Result> source, Task action
    
    /// <summary>
    /// Asynchronously performs an action on the error value of a ValueTask of Result.
    /// If the source Result is a success, the action is not performed.
    /// Returns the original ValueTask of Result.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccess>> OnErrorAsync<TError, TSuccess>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TError, Task> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
    
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            await action(result.ErrorValue()).ConfigureAwait(false);
    
        return result;
    }
    
    /// <summary>
    /// Asynchronously performs an action on the error value of a ValueTask of Result, using a state value.
    /// If the source Result is a success, the action is not performed.
    /// Returns the original ValueTask of Result.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccess>> OnErrorAsync<TError, TSuccess, TState>(
        this ValueTask<Result<TError, TSuccess>> resultTask,
        Func<TError, TState, Task> action,
        TState state)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
    
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsError)
            await action(result.ErrorValue(), state).ConfigureAwait(false);
    
        return result;
    }

    #endregion
    
    #region . Result<TError, TSuccess> Source Extensions (Async Operations) .

    // ThenAsync: Result source, Task<Result> binder
    
    /// <summary>
    /// Asynchronously binds the success value of a Result to a new Task of Result.
    /// If the source Result is an error, the error is propagated, and the binder is not called.
    /// </summary>
    public static async Task<Result<TError, TSuccessOut>> ThenAsync<TError, TSuccess, TSuccessOut>(
        this Result<TError, TSuccess> result,
        Func<TSuccess, Task<Result<TError, TSuccessOut>>> binder)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        return await binder(result.SuccessValue()).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously binds the success value of a Result to a new Task of Result, using a state value.
    /// If the source Result is an error, the error is propagated, and the binder is not called.
    /// </summary>
    public static async Task<Result<TError, TSuccessOut>> ThenAsync<TError, TSuccess, TSuccessOut, TState>(
        this Result<TError, TSuccess> result,
        Func<TSuccess, TState, Task<Result<TError, TSuccessOut>>> binder,
        TState state)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        return await binder(result.SuccessValue(), state).ConfigureAwait(false);
    }
    
    // ThenAsync: Result source, ValueTask<Result> binder

    /// <summary>
    /// Asynchronously binds the success value of a Result to a new ValueTask of Result.
    /// If the source Result is an error, the error is propagated, and the binder is not called.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccessOut>> ThenAsync<TError, TSuccess, TSuccessOut>(
        this Result<TError, TSuccess> result,
        Func<TSuccess, ValueTask<Result<TError, TSuccessOut>>> binder)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        return await binder(result.SuccessValue()).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Asynchronously binds the success value of a Result to a new ValueTask of Result, using a state value.
    /// If the source Result is an error, the error is propagated, and the binder is not called.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccessOut>> ThenAsync<TError, TSuccess, TSuccessOut, TState>(
        this Result<TError, TSuccess> result,
        Func<TSuccess, TState, ValueTask<Result<TError, TSuccessOut>>> binder,
        TState state)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        return await binder(result.SuccessValue(), state).ConfigureAwait(false);
    }
    
    // MapAsync: Result source, Task<TSuccessOut> mapper
    
    /// <summary>
    /// Asynchronously maps the success value of a Result to a new success value using a Task-returning mapper.
    /// If the source Result is an error, the error is propagated, and the mapper is not called.
    /// </summary>
    public static async Task<Result<TError, TSuccessOut>> MapAsync<TError, TSuccess, TSuccessOut>(
        this Result<TError, TSuccess> result,
        Func<TSuccess, Task<TSuccessOut>> mapper)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        var mappedValue = await mapper(result.SuccessValue()).ConfigureAwait(false);
        return Result<TError, TSuccessOut>.Success(mappedValue);
    }
    
    /// <summary>
    /// Asynchronously maps the success value of a Result to a new success value using a Task-returning mapper and a state value.
    /// If the source Result is an error, the error is propagated, and the mapper is not called.
    /// </summary>
    public static async Task<Result<TError, TSuccessOut>> MapAsync<TError, TSuccess, TSuccessOut, TState>(
        this Result<TError, TSuccess> result,
        Func<TSuccess, TState, Task<TSuccessOut>> mapper,
        TState state)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        var mappedValue = await mapper(result.SuccessValue(), state).ConfigureAwait(false);
        return Result<TError, TSuccessOut>.Success(mappedValue);
    }
    
    // MapAsync: Result source, ValueTask<TSuccessOut> mapper
    
    /// <summary>
    /// Asynchronously maps the success value of a Result to a new success value using a ValueTask-returning mapper.
    /// If the source Result is an error, the error is propagated, and the mapper is not called.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccessOut>> MapAsync<TError, TSuccess, TSuccessOut>(
        this Result<TError, TSuccess> result,
        Func<TSuccess, ValueTask<TSuccessOut>> mapper)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        var mappedValue = await mapper(result.SuccessValue()).ConfigureAwait(false);
        return Result<TError, TSuccessOut>.Success(mappedValue);
    }

    /// <summary>
    /// Asynchronously maps the success value of a Result to a new success value using a ValueTask-returning mapper and a state value.
    /// If the source Result is an error, the error is propagated, and the mapper is not called.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccessOut>> MapAsync<TError, TSuccess, TSuccessOut, TState>(
        this Result<TError, TSuccess> result,
        Func<TSuccess, TState, ValueTask<TSuccessOut>> mapper,
        TState state)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        if (result.IsError)
            return Result<TError, TSuccessOut>.Error(result.ErrorValue());

        var mappedValue = await mapper(result.SuccessValue(), state).ConfigureAwait(false);
        return Result<TError, TSuccessOut>.Success(mappedValue);
    }
    
    // OrElseAsync: Result source, Task<Result> binder
    
    /// <summary>
    /// Asynchronously binds the error value of a Result to a new Task of Result.
    /// If the source Result is a success, the success is propagated, and the binder is not called.
    /// </summary>
    public static async Task<Result<TErrorOut, TSuccess>> OrElseAsync<TError, TSuccess, TErrorOut>(
        this Result<TError, TSuccess> result,
        Func<TError, Task<Result<TErrorOut, TSuccess>>> binder)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());

        return await binder(result.ErrorValue()).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Asynchronously binds the error value of a Result to a new Task of Result, using a state value.
    /// If the source Result is a success, the success is propagated, and the binder is not called.
    /// </summary>
    public static async Task<Result<TErrorOut, TSuccess>> OrElseAsync<TError, TSuccess, TErrorOut, TState>(
        this Result<TError, TSuccess> result,
        Func<TError, TState, Task<Result<TErrorOut, TSuccess>>> binder,
        TState state)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());

        return await binder(result.ErrorValue(), state).ConfigureAwait(false);
    }
    
    // OrElseAsync: Result source, ValueTask<Result> binder
    
    /// <summary>
    /// Asynchronously binds the error value of a Result to a new ValueTask of Result.
    /// If the source Result is a success, the success is propagated, and the binder is not called.
    /// </summary>
    public static async ValueTask<Result<TErrorOut, TSuccess>> OrElseAsync<TError, TSuccess, TErrorOut>(
        this Result<TError, TSuccess> result,
        Func<TError, ValueTask<Result<TErrorOut, TSuccess>>> binder)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());

        return await binder(result.ErrorValue()).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Asynchronously binds the error value of a Result to a new ValueTask of Result, using a state value.
    /// If the source Result is a success, the success is propagated, and the binder is not called.
    /// </summary>
    public static async ValueTask<Result<TErrorOut, TSuccess>> OrElseAsync<TError, TSuccess, TErrorOut, TState>(
        this Result<TError, TSuccess> result,
        Func<TError, TState, ValueTask<Result<TErrorOut, TSuccess>>> binder,
        TState state)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());

        return await binder(result.ErrorValue(), state).ConfigureAwait(false);
    }
    
    // MapErrorAsync: Result source, Task<TErrorOut> mapper
    
    /// <summary>
    /// Asynchronously maps the error value of a Result to a new error value using a Task-returning mapper.
    /// If the source Result is a success, the success is propagated, and the mapper is not called.
    /// </summary>
    public static async Task<Result<TErrorOut, TSuccess>> MapErrorAsync<TError, TSuccess, TErrorOut>(
        this Result<TError, TSuccess> result,
        Func<TError, Task<TErrorOut>> mapper)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());

        var mappedError = await mapper(result.ErrorValue()).ConfigureAwait(false);
        return Result<TErrorOut, TSuccess>.Error(mappedError);
    }
    
    /// <summary>
    /// Asynchronously maps the error value of a Result to a new error value using a Task-returning mapper and a state value.
    /// If the source Result is a success, the success is propagated, and the mapper is not called.
    /// </summary>
    public static async Task<Result<TErrorOut, TSuccess>> MapErrorAsync<TError, TSuccess, TErrorOut, TState>(
        this Result<TError, TSuccess> result,
        Func<TError, TState, Task<TErrorOut>> mapper,
        TState state)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());

        var mappedError = await mapper(result.ErrorValue(), state).ConfigureAwait(false);
        return Result<TErrorOut, TSuccess>.Error(mappedError);
    }
    
    // MapErrorAsync: Result source, ValueTask<TErrorOut> mapper
    
    /// <summary>
    /// Asynchronously maps the error value of a Result to a new error value using a ValueTask-returning mapper.
    /// If the source Result is a success, the success is propagated, and the mapper is not called.
    /// </summary>
    public static async ValueTask<Result<TErrorOut, TSuccess>> MapErrorAsync<TError, TSuccess, TErrorOut>(
        this Result<TError, TSuccess> result,
        Func<TError, ValueTask<TErrorOut>> mapper)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());

        var mappedError = await mapper(result.ErrorValue()).ConfigureAwait(false);
        return Result<TErrorOut, TSuccess>.Error(mappedError);
    }
    
    /// <summary>
    /// Asynchronously maps the error value of a Result to a new error value using a ValueTask-returning mapper and a state value.
    /// If the source Result is a success, the success is propagated, and the mapper is not called.
    /// </summary>
    public static async ValueTask<Result<TErrorOut, TSuccess>> MapErrorAsync<TError, TSuccess, TErrorOut, TState>(
        this Result<TError, TSuccess> result,
        Func<TError, TState, ValueTask<TErrorOut>> mapper,
        TState state)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        if (result.IsSuccess)
            return Result<TErrorOut, TSuccess>.Success(result.SuccessValue());

        var mappedError = await mapper(result.ErrorValue(), state).ConfigureAwait(false);
        return Result<TErrorOut, TSuccess>.Error(mappedError);
    }
    
    // OnSuccessAsync: Result source, Task action
    
    /// <summary>
    /// Asynchronously performs an action on the success value of a Result.
    /// If the source Result is an error, the action is not performed.
    /// Returns the original Result as a Task.
    /// </summary>
    public static async Task<Result<TError, TSuccess>> OnSuccessAsync<TError, TSuccess>(
        this Result<TError, TSuccess> result,
        Func<TSuccess, Task> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (result.IsSuccess)
            await action(result.SuccessValue()).ConfigureAwait(false);

        return result;
    }
    
    /// <summary>
    /// Asynchronously performs an action on the success value of a Result, using a state value.
    /// If the source Result is an error, the action is not performed.
    /// Returns the original Result as a Task.
    /// </summary>
    public static async Task<Result<TError, TSuccess>> OnSuccessAsync<TError, TSuccess, TState>(
        this Result<TError, TSuccess> result,
        Func<TSuccess, TState, Task> action,
        TState state)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (result.IsSuccess)
            await action(result.SuccessValue(), state).ConfigureAwait(false);

        return result;
    }
    
    // OnSuccessAsync: Result source, ValueTask action
    
    /// <summary>
    /// Asynchronously performs an action on the success value of a Result.
    /// If the source Result is an error, the action is not performed.
    /// Returns the original Result as a ValueTask.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccess>> OnSuccessAsync<TError, TSuccess>(
        this Result<TError, TSuccess> result,
        Func<TSuccess, ValueTask> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (result.IsSuccess)
            await action(result.SuccessValue()).ConfigureAwait(false);

        return result;
    }
    
    /// <summary>
    /// Asynchronously performs an action on the success value of a Result, using a state value.
    /// If the source Result is an error, the action is not performed.
    /// Returns the original Result as a ValueTask.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccess>> OnSuccessAsync<TError, TSuccess, TState>(
        this Result<TError, TSuccess> result,
        Func<TSuccess, TState, ValueTask> action,
        TState state)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (result.IsSuccess)
            await action(result.SuccessValue(), state).ConfigureAwait(false);

        return result;
    }
    
    // OnErrorAsync: Result source, Task action
    
    /// <summary>
    /// Asynchronously performs an action on the error value of a Result.
    /// If the source Result is a success, the action is not performed.
    /// Returns the original Result as a Task.
    /// </summary>
    public static async Task<Result<TError, TSuccess>> OnErrorAsync<TError, TSuccess>(
        this Result<TError, TSuccess> result,
        Func<TError, Task> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (result.IsError)
            await action(result.ErrorValue()).ConfigureAwait(false);

        return result;
    }
    
    /// <summary>
    /// Asynchronously performs an action on the error value of a Result, using a state value.
    /// If the source Result is a success, the action is not performed.
    /// Returns the original Result as a Task.
    /// </summary>
    public static async Task<Result<TError, TSuccess>> OnErrorAsync<TError, TSuccess, TState>(
        this Result<TError, TSuccess> result,
        Func<TError, TState, Task> action,
        TState state)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (result.IsError)
            await action(result.ErrorValue(), state).ConfigureAwait(false);

        return result;
    }
    
    // OnErrorAsync: Result source, ValueTask action
    
    /// <summary>
    /// Asynchronously performs an action on the error value of a Result.
    /// If the source Result is a success, the action is not performed.
    /// Returns the original Result as a ValueTask.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccess>> OnErrorAsync<TError, TSuccess>(
        this Result<TError, TSuccess> result,
        Func<TError, ValueTask> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (result.IsError)
            await action(result.ErrorValue()).ConfigureAwait(false);

        return result;
    }
    
    /// <summary>
    /// Asynchronously performs an action on the error value of a Result, using a state value.
    /// If the source Result is a success, the action is not performed.
    /// Returns the original Result as a ValueTask.
    /// </summary>
    public static async ValueTask<Result<TError, TSuccess>> OnErrorAsync<TError, TSuccess, TState>(
        this Result<TError, TSuccess> result,
        Func<TError, TState, ValueTask> action,
        TState state)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (result.IsError)
            await action(result.ErrorValue(), state).ConfigureAwait(false);

        return result;
    }

    #endregion
}
