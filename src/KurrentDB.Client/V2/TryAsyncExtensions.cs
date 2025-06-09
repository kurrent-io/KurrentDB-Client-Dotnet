namespace KurrentDB.Client;

/// <summary>
/// Provides asynchronous extension methods for the <see cref="Try{TSuccess}"/> monad,
/// enabling fluent chaining of asynchronous operations.
/// </summary>
[PublicAPI]
public static class TryAsyncExtensions {
    #region . ThenAsync .

    /// <summary>
    /// Asynchronously chains a computation that returns a <see cref="Try{TOut}"/> if the source <see cref="Task{TResult}"/>
    /// completes successfully and its result is a success.
    /// If the source task fails, is cancelled, or its result is an error, the error/exception is propagated.
    /// Exceptions thrown by the <paramref name="binder"/> itself are caught and returned as an error <see cref="Try{TOut}"/>.
    /// </summary>
    /// <typeparam name="TSuccessIn">The type of the success value of the source <see cref="Try{TSuccess}"/>.</typeparam>
    /// <typeparam name="TSuccessOut">The type of the success value of the <see cref="Try{TSuccess}"/> returned by the binder.</typeparam>
    /// <param name="source">The source asynchronous operation yielding a <see cref="Try{TSuccessIn}"/>.</param>
    /// <param name="binder">A function that takes the success value and returns a <see cref="Task{TResult}"/> yielding a <see cref="Try{TSuccessOut}"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation, yielding the <see cref="Try{TSuccessOut}"/> from the binder or an error.</returns>
    public static async Task<Try<TSuccessOut>> ThenAsync<TSuccessIn, TSuccessOut>(
        this Task<Try<TSuccessIn>> source,
        Func<TSuccessIn, Task<Try<TSuccessOut>>> binder,
        CancellationToken cancellationToken = default
    ) {
        try {
            cancellationToken.ThrowIfCancellationRequested();
            var tryResult = await source.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (tryResult.IsError) return new Try<TSuccessOut>(tryResult.ErrorValue());

            // Exceptions from the binder itself are caught by the outer try-catch
            return await binder(tryResult.SuccessValue()).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) {
            return new Try<TSuccessOut>(ex);
        }
    }

    /// <summary>
    /// Asynchronously chains a computation that returns a <see cref="Try{TOut}"/> if the source <see cref="Task{TResult}"/>
    /// completes successfully and its result is a success.
    /// This overload accepts a binder that returns a <see cref="ValueTask{TResult}"/>.
    /// </summary>
    /// <typeparam name="TSuccessIn">The type of the success value of the source <see cref="Try{TSuccess}"/>.</typeparam>
    /// <typeparam name="TSuccessOut">The type of the success value of the <see cref="Try{TSuccess}"/> returned by the binder.</typeparam>
    /// <param name="source">The source asynchronous operation yielding a <see cref="Try{TSuccessIn}"/>.</param>
    /// <param name="binder">A function that takes the success value and returns a <see cref="ValueTask{TResult}"/> yielding a <see cref="Try{TSuccessOut}"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation, yielding the <see cref="Try{TSuccessOut}"/> from the binder or an error.</returns>
    public static async Task<Try<TSuccessOut>> ThenAsync<TSuccessIn, TSuccessOut>(
        this Task<Try<TSuccessIn>> source,
        Func<TSuccessIn, ValueTask<Try<TSuccessOut>>> binder,
        CancellationToken cancellationToken = default
    ) {
        try {
            cancellationToken.ThrowIfCancellationRequested();
            var tryResult = await source.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (tryResult.IsError) return new Try<TSuccessOut>(tryResult.ErrorValue());

            return await binder(tryResult.SuccessValue()).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) {
            return new Try<TSuccessOut>(ex);
        }
    }

    /// <summary>
    /// Asynchronously chains a computation that returns a <see cref="Try{TOut}"/> if the source <see cref="ValueTask{TResult}"/>
    /// completes successfully and its result is a success.
    /// </summary>
    /// <typeparam name="TSuccessIn">The type of the success value of the source <see cref="Try{TSuccess}"/>.</typeparam>
    /// <typeparam name="TSuccessOut">The type of the success value of the <see cref="Try{TSuccess}"/> returned by the binder.</typeparam>
    /// <param name="source">The source asynchronous operation yielding a <see cref="Try{TSuccessIn}"/>.</param>
    /// <param name="binder">A function that takes the success value and returns a <see cref="Task{TResult}"/> yielding a <see cref="Try{TSuccessOut}"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, yielding the <see cref="Try{TSuccessOut}"/> from the binder or an error.</returns>
    public static async ValueTask<Try<TSuccessOut>> ThenAsync<TSuccessIn, TSuccessOut>(
        this ValueTask<Try<TSuccessIn>> source,
        Func<TSuccessIn, Task<Try<TSuccessOut>>> binder,
        CancellationToken cancellationToken = default
    ) {
        try {
            cancellationToken.ThrowIfCancellationRequested();
            var tryResult = await source.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (tryResult.IsError) return new Try<TSuccessOut>(tryResult.ErrorValue());

            // Await Task inside ValueTask returning method
            return await binder(tryResult.SuccessValue()).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) {
            return new Try<TSuccessOut>(ex);
        }
    }

    /// <summary>
    /// Asynchronously chains a computation that returns a <see cref="Try{TOut}"/> if the source <see cref="ValueTask{TResult}"/>
    /// completes successfully and its result is a success.
    /// This overload accepts a binder that returns a <see cref="ValueTask{TResult}"/>.
    /// </summary>
    /// <typeparam name="TSuccessIn">The type of the success value of the source <see cref="Try{TSuccess}"/>.</typeparam>
    /// <typeparam name="TSuccessOut">The type of the success value of the <see cref="Try{TSuccess}"/> returned by the binder.</typeparam>
    /// <param name="source">The source asynchronous operation yielding a <see cref="Try{TSuccessIn}"/>.</param>
    /// <param name="binder">A function that takes the success value and returns a <see cref="ValueTask{TResult}"/> yielding a <see cref="Try{TSuccessOut}"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, yielding the <see cref="Try{TSuccessOut}"/> from the binder or an error.</returns>
    public static async ValueTask<Try<TSuccessOut>> ThenAsync<TSuccessIn, TSuccessOut>(
        this ValueTask<Try<TSuccessIn>> source,
        Func<TSuccessIn, ValueTask<Try<TSuccessOut>>> binder,
        CancellationToken cancellationToken = default
    ) {
        try {
            cancellationToken.ThrowIfCancellationRequested();
            var tryResult = await source.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (tryResult.IsError) return new Try<TSuccessOut>(tryResult.ErrorValue());

            return await binder(tryResult.SuccessValue()).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) {
            return new Try<TSuccessOut>(ex);
        }
    }

    /// <summary>
    /// Asynchronously chains a computation if the source <see cref="Try{TSuccessIn}"/> is a success.
    /// The binder function itself returns a <see cref="Task{TResult}"/> yielding a <see cref="Try{TSuccessOut}"/>.
    /// Exceptions from the binder are caught.
    /// </summary>
    /// <typeparam name="TSuccessIn">The type of the success value of the source <see cref="Try{TSuccess}"/>.</typeparam>
    /// <typeparam name="TSuccessOut">The type of the success value of the <see cref="Try{TSuccess}"/> returned by the binder.</typeparam>
    /// <param name="source">The source <see cref="Try{TSuccessIn}"/>.</param>
    /// <param name="binder">A function that takes the success value and returns a <see cref="Task{TResult}"/> yielding a <see cref="Try{TSuccessOut}"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation (primarily for the binder).</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation, yielding the <see cref="Try{TSuccessOut}"/> from the binder or an error.</returns>
    public static async Task<Try<TSuccessOut>> ThenAsync<TSuccessIn, TSuccessOut>(
        this Try<TSuccessIn> source,
        Func<TSuccessIn, Task<Try<TSuccessOut>>> binder,
        CancellationToken cancellationToken = default
    ) {
        if (source.IsError) return new Try<TSuccessOut>(source.ErrorValue());

        try {
            cancellationToken.ThrowIfCancellationRequested();
            return await binder(source.SuccessValue()).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) {
            return new Try<TSuccessOut>(ex);
        }
    }

    /// <summary>
    /// Asynchronously chains a computation if the source <see cref="Try{TSuccessIn}"/> is a success.
    /// The binder function itself returns a <see cref="ValueTask{TResult}"/> yielding a <see cref="Try{TSuccessOut}"/>.
    /// Exceptions from the binder are caught.
    /// </summary>
    /// <typeparam name="TSuccessIn">The type of the success value of the source <see cref="Try{TSuccess}"/>.</typeparam>
    /// <typeparam name="TSuccessOut">The type of the success value of the <see cref="Try{TSuccess}"/> returned by the binder.</typeparam>
    /// <param name="source">The source <see cref="Try{TSuccessIn}"/>.</param>
    /// <param name="binder">A function that takes the success value and returns a <see cref="ValueTask{TResult}"/> yielding a <see cref="Try{TSuccessOut}"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation (primarily for the binder).</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, yielding the <see cref="Try{TSuccessOut}"/> from the binder or an error.</returns>
    public static async ValueTask<Try<TSuccessOut>> ThenAsync<TSuccessIn, TSuccessOut>(
        this Try<TSuccessIn> source,
        Func<TSuccessIn, ValueTask<Try<TSuccessOut>>> binder,
        CancellationToken cancellationToken = default
    ) {
        if (source.IsError) return new Try<TSuccessOut>(source.ErrorValue());

        try {
            cancellationToken.ThrowIfCancellationRequested();
            return await binder(source.SuccessValue()).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) {
            return new Try<TSuccessOut>(ex);
        }
    }

    #endregion

    #region . MapAsync .

    /// <summary>
    /// Asynchronously maps the success value of a <see cref="Task{TResult}"/> yielding a <see cref="Try{TSuccessIn}"/>
    /// to a <see cref="Try{TSuccessOut}"/> using the specified asynchronous mapper function.
    /// Exceptions thrown by the <paramref name="mapper"/> are caught and wrapped in a <see cref="Try{TSuccessOut}"/>.
    /// </summary>
    /// <typeparam name="TSuccessIn">The type of the success value of the source <see cref="Try{TSuccess}"/>.</typeparam>
    /// <typeparam name="TSuccessOut">The type of the success value of the resulting <see cref="Try{TSuccess}"/>.</typeparam>
    /// <param name="source">The source asynchronous operation yielding a <see cref="Try{TSuccessIn}"/>.</param>
    /// <param name="mapper">An asynchronous function to transform the success value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous mapping operation.</returns>
    public static async Task<Try<TSuccessOut>> MapAsync<TSuccessIn, TSuccessOut>(
        this Task<Try<TSuccessIn>> source,
        Func<TSuccessIn, Task<TSuccessOut>> mapper,
        CancellationToken cancellationToken = default
    ) {
        try {
            cancellationToken.ThrowIfCancellationRequested();
            var tryResult = await source.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (tryResult.IsError) return new Try<TSuccessOut>(tryResult.ErrorValue());

            return await JustTry.CatchingAsync(() => mapper(tryResult.SuccessValue()), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) {
            return new Try<TSuccessOut>(ex);
        }
    }

    /// <summary>
    /// Asynchronously maps the success value of a <see cref="Task{TResult}"/> yielding a <see cref="Try{TSuccessIn}"/>
    /// using an asynchronous mapper function that returns a <see cref="ValueTask{TResult}"/>.
    /// Exceptions thrown by the <paramref name="mapper"/> are caught.
    /// </summary>
    /// <typeparam name="TSuccessIn">The type of the success value of the source <see cref="Try{TSuccess}"/>.</typeparam>
    /// <typeparam name="TSuccessOut">The type of the success value of the resulting <see cref="Try{TSuccess}"/>.</typeparam>
    /// <param name="source">The source asynchronous operation yielding a <see cref="Try{TSuccessIn}"/>.</param>
    /// <param name="mapper">An asynchronous function (returning <see cref="ValueTask{TResult}"/>) to transform the success value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous mapping operation.</returns>
    public static async Task<Try<TSuccessOut>> MapAsync<TSuccessIn, TSuccessOut>(
        this Task<Try<TSuccessIn>> source,
        Func<TSuccessIn, ValueTask<TSuccessOut>> mapper,
        CancellationToken cancellationToken = default
    ) {
        try {
            cancellationToken.ThrowIfCancellationRequested();
            var tryResult = await source.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (tryResult.IsError) return new Try<TSuccessOut>(tryResult.ErrorValue());

            return await JustTry.CatchAsync(() => mapper(tryResult.SuccessValue()), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) {
            return new Try<TSuccessOut>(ex);
        }
    }

    /// <summary>
    /// Asynchronously maps the success value of a <see cref="ValueTask{TResult}"/> yielding a <see cref="Try{TSuccessIn}"/>
    /// using an asynchronous mapper function that returns a <see cref="Task{TResult}"/>.
    /// Exceptions thrown by the <paramref name="mapper"/> are caught.
    /// </summary>
    /// <typeparam name="TSuccessIn">The type of the success value of the source <see cref="Try{TSuccess}"/>.</typeparam>
    /// <typeparam name="TSuccessOut">The type of the success value of the resulting <see cref="Try{TSuccess}"/>.</typeparam>
    /// <param name="source">The source asynchronous operation yielding a <see cref="Try{TSuccessIn}"/>.</param>
    /// <param name="mapper">An asynchronous function (returning <see cref="Task{TResult}"/>) to transform the success value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous mapping operation.</returns>
    public static async ValueTask<Try<TSuccessOut>> MapAsync<TSuccessIn, TSuccessOut>(
        this ValueTask<Try<TSuccessIn>> source,
        Func<TSuccessIn, Task<TSuccessOut>> mapper,
        CancellationToken cancellationToken = default
    ) {
        try {
            cancellationToken.ThrowIfCancellationRequested();
            var tryResult = await source.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (tryResult.IsError) return new Try<TSuccessOut>(tryResult.ErrorValue());

            return await JustTry.CatchingAsync(() => mapper(tryResult.SuccessValue()), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) {
            return new Try<TSuccessOut>(ex);
        }
    }

    /// <summary>
    /// Asynchronously maps the success value of a <see cref="ValueTask{TResult}"/> yielding a <see cref="Try{TSuccessIn}"/>
    /// using an asynchronous mapper function that returns a <see cref="ValueTask{TResult}"/>.
    /// Exceptions thrown by the <paramref name="mapper"/> are caught.
    /// </summary>
    /// <typeparam name="TSuccessIn">The type of the success value of the source <see cref="Try{TSuccess}"/>.</typeparam>
    /// <typeparam name="TSuccessOut">The type of the success value of the resulting <see cref="Try{TSuccess}"/>.</typeparam>
    /// <param name="source">The source asynchronous operation yielding a <see cref="Try{TSuccessIn}"/>.</param>
    /// <param name="mapper">An asynchronous function (returning <see cref="ValueTask{TResult}"/>) to transform the success value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous mapping operation.</returns>
    public static async ValueTask<Try<TSuccessOut>> MapAsync<TSuccessIn, TSuccessOut>(
        this ValueTask<Try<TSuccessIn>> source,
        Func<TSuccessIn, ValueTask<TSuccessOut>> mapper,
        CancellationToken cancellationToken = default
    ) {
        try {
            cancellationToken.ThrowIfCancellationRequested();
            var tryResult = await source.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (tryResult.IsError) return new Try<TSuccessOut>(tryResult.ErrorValue());

            return await JustTry.CatchAsync(() => mapper(tryResult.SuccessValue()), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) {
            return new Try<TSuccessOut>(ex);
        }
    }

    /// <summary>
    /// Asynchronously maps the success value of a <see cref="Try{TSuccessIn}"/>
    /// using an asynchronous mapper function that returns a <see cref="Task{TResult}"/>.
    /// Exceptions thrown by the <paramref name="mapper"/> are caught.
    /// </summary>
    /// <typeparam name="TSuccessIn">The type of the success value of the source <see cref="Try{TSuccess}"/>.</typeparam>
    /// <typeparam name="TSuccessOut">The type of the success value of the resulting <see cref="Try{TSuccess}"/>.</typeparam>
    /// <param name="source">The source <see cref="Try{TSuccessIn}"/>.</param>
    /// <param name="mapper">An asynchronous function to transform the success value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous mapping operation.</returns>
    public static async Task<Try<TSuccessOut>> MapAsync<TSuccessIn, TSuccessOut>(
        this Try<TSuccessIn> source,
        Func<TSuccessIn, Task<TSuccessOut>> mapper,
        CancellationToken cancellationToken = default
    ) {
        if (source.IsError) return new Try<TSuccessOut>(source.ErrorValue());

        return await JustTry.CatchingAsync(() => mapper(source.SuccessValue()), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously maps the success value of a <see cref="Try{TSuccessIn}"/>
    /// using an asynchronous mapper function that returns a <see cref="ValueTask{TResult}"/>.
    /// Exceptions thrown by the <paramref name="mapper"/> are caught.
    /// </summary>
    /// <typeparam name="TSuccessIn">The type of the success value of the source <see cref="Try{TSuccess}"/>.</typeparam>
    /// <typeparam name="TSuccessOut">The type of the success value of the resulting <see cref="Try{TSuccess}"/>.</typeparam>
    /// <param name="source">The source <see cref="Try{TSuccessIn}"/>.</param>
    /// <param name="mapper">An asynchronous function (returning <see cref="ValueTask{TResult}"/>) to transform the success value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous mapping operation.</returns>
    public static async ValueTask<Try<TSuccessOut>> MapAsync<TSuccessIn, TSuccessOut>(
        this Try<TSuccessIn> source,
        Func<TSuccessIn, ValueTask<TSuccessOut>> mapper,
        CancellationToken cancellationToken = default
    ) {
        if (source.IsError) return new Try<TSuccessOut>(source.ErrorValue());

        return await JustTry.CatchAsync(() => mapper(source.SuccessValue()), cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region . OnSuccessAsync .

    /// <summary>
    /// Asynchronously performs an action on the success value if the <see cref="Task{TResult}"/> completes
    /// successfully and its result is a success. The original <see cref="Try{TSuccess}"/> is returned.
    /// Exceptions from the <paramref name="action"/> are not caught.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="source">The source asynchronous operation yielding a <see cref="Try{TSuccess}"/>.</param>
    /// <param name="action">An asynchronous action to perform on the success value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> that completes with the original <see cref="Try{TSuccess}"/> after the action has been performed, or an error if the source failed.</returns>
    public static async Task<Try<TSuccess>> OnSuccessAsync<TSuccess>(
        this Task<Try<TSuccess>> source,
        Func<TSuccess, Task> action,
        CancellationToken cancellationToken = default
    ) {
        Try<TSuccess> tryResult = null!;
        try {
            cancellationToken.ThrowIfCancellationRequested();
            tryResult = await source.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (tryResult.IsSuccess) await action(tryResult.SuccessValue()).ConfigureAwait(false);
            return tryResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) when (tryResult is null) {
            return new Try<TSuccess>(ex);
        }
    }

    /// <summary>
    /// Asynchronously performs an action (returning <see cref="ValueTask"/>) on the success value if the <see cref="Task{TResult}"/>
    /// completes successfully and its result is a success. The original <see cref="Try{TSuccess}"/> is returned.
    /// Exceptions from the <paramref name="action"/> are not caught.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="source">The source asynchronous operation yielding a <see cref="Try{TSuccess}"/>.</param>
    /// <param name="action">An asynchronous action (returning <see cref="ValueTask"/>) to perform on the success value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> that completes with the original <see cref="Try{TSuccess}"/>.</returns>
    public static async Task<Try<TSuccess>> OnSuccessAsync<TSuccess>(
        this Task<Try<TSuccess>> source,
        Func<TSuccess, ValueTask> action,
        CancellationToken cancellationToken = default
    ) {
        Try<TSuccess> tryResult = null!;
        try {
            cancellationToken.ThrowIfCancellationRequested();
            tryResult = await source.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (tryResult.IsSuccess) await action(tryResult.SuccessValue()).ConfigureAwait(false);
            return tryResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) when (tryResult is null) {
            return new Try<TSuccess>(ex);
        }
    }

    /// <summary>
    /// Asynchronously performs an action on the success value if the <see cref="ValueTask{TResult}"/> completes
    /// successfully and its result is a success. The original <see cref="Try{TSuccess}"/> is returned.
    /// Exceptions from the <paramref name="action"/> are not caught.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="source">The source asynchronous operation yielding a <see cref="Try{TSuccess}"/>.</param>
    /// <param name="action">An asynchronous action to perform on the success value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the original <see cref="Try{TSuccess}"/>.</returns>
    public static async ValueTask<Try<TSuccess>> OnSuccessAsync<TSuccess>(
        this ValueTask<Try<TSuccess>> source,
        Func<TSuccess, Task> action,
        CancellationToken cancellationToken = default
    ) {
        Try<TSuccess> tryResult = null!;
        try {
            cancellationToken.ThrowIfCancellationRequested();
            tryResult = await source.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (tryResult.IsSuccess) await action(tryResult.SuccessValue()).ConfigureAwait(false);
            return tryResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) when (tryResult is null) {
            return new Try<TSuccess>(ex);
        }
    }

    /// <summary>
    /// Asynchronously performs an action (returning <see cref="ValueTask"/>) on the success value if the <see cref="ValueTask{TResult}"/>
    /// completes successfully and its result is a success. The original <see cref="Try{TSuccess}"/> is returned.
    /// Exceptions from the <paramref name="action"/> are not caught.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="source">The source asynchronous operation yielding a <see cref="Try{TSuccess}"/>.</param>
    /// <param name="action">An asynchronous action (returning <see cref="ValueTask"/>) to perform on the success value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the original <see cref="Try{TSuccess}"/>.</returns>
    public static async ValueTask<Try<TSuccess>> OnSuccessAsync<TSuccess>(
        this ValueTask<Try<TSuccess>> source,
        Func<TSuccess, ValueTask> action,
        CancellationToken cancellationToken = default
    ) {
        Try<TSuccess> tryResult = null!;
        try {
            cancellationToken.ThrowIfCancellationRequested();
            tryResult = await source.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (tryResult.IsSuccess) await action(tryResult.SuccessValue()).ConfigureAwait(false);
            return tryResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) when (tryResult is null) {
            return new Try<TSuccess>(ex);
        }
    }

    /// <summary>
    /// Asynchronously performs an action if the source <see cref="Try{TSuccess}"/> is a success.
    /// The original <see cref="Try{TSuccess}"/> is returned.
    /// Exceptions from the <paramref name="action"/> are not caught.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="source">The source <see cref="Try{TSuccess}"/>.</param>
    /// <param name="action">An asynchronous action to perform on the success value.</param>
    /// <param name="cancellationToken">A token to cancel the operation (primarily for the action).</param>
    /// <returns>A <see cref="Task{TResult}"/> that completes with the original <see cref="Try{TSuccess}"/>.</returns>
    public static async Task<Try<TSuccess>> OnSuccessAsync<TSuccess>(
        this Try<TSuccess> source,
        Func<TSuccess, Task> action,
        CancellationToken cancellationToken = default
    ) {
        if (!source.IsSuccess) return source;

        try {
            cancellationToken.ThrowIfCancellationRequested();
            await action(source.SuccessValue()).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }

        return source;
    }

    /// <summary>
    /// Asynchronously performs an action (returning <see cref="ValueTask"/>) if the source <see cref="Try{TSuccess}"/> is a success.
    /// The original <see cref="Try{TSuccess}"/> is returned.
    /// Exceptions from the <paramref name="action"/> are not caught.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="source">The source <see cref="Try{TSuccess}"/>.</param>
    /// <param name="action">An asynchronous action (returning <see cref="ValueTask"/>) to perform on the success value.</param>
    /// <param name="cancellationToken">A token to cancel the operation (primarily for the action).</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the original <see cref="Try{TSuccess}"/>.</returns>
    public static async ValueTask<Try<TSuccess>> OnSuccessAsync<TSuccess>(
        this Try<TSuccess> source,
        Func<TSuccess, ValueTask> action,
        CancellationToken cancellationToken = default
    ) {
        if (!source.IsSuccess) return source;

        try {
            cancellationToken.ThrowIfCancellationRequested();
            await action(source.SuccessValue()).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }

        return source;
    }

    #endregion

    #region . OnErrorAsync .

    /// <summary>
    /// Asynchronously performs an action on the error value (exception) if the <see cref="Task{TResult}"/>
    /// completes successfully but its result is an error. The original <see cref="Try{TSuccess}"/> is returned.
    /// Exceptions from the <paramref name="action"/> are not caught.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="source">The source asynchronous operation yielding a <see cref="Try{TSuccess}"/>.</param>
    /// <param name="action">An asynchronous action to perform on the error value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> that completes with the original <see cref="Try{TSuccess}"/>.</returns>
    public static async Task<Try<TSuccess>> OnErrorAsync<TSuccess>(
        this Task<Try<TSuccess>> source,
        Func<Exception, Task> action,
        CancellationToken cancellationToken = default
    ) {
        Try<TSuccess> tryResult = null!;
        try {
            cancellationToken.ThrowIfCancellationRequested();
            tryResult = await source.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (tryResult.IsError) await action(tryResult.ErrorValue()).ConfigureAwait(false);
            return tryResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) when (tryResult is null) {
            return new Try<TSuccess>(ex);
        }
    }

    /// <summary>
    /// Asynchronously performs an action (returning <see cref="ValueTask"/>) on the error value (exception)
    /// if the <see cref="Task{TResult}"/> completes successfully but its result is an error. The original <see cref="Try{TSuccess}"/> is returned.
    /// Exceptions from the <paramref name="action"/> are not caught.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="source">The source asynchronous operation yielding a <see cref="Try{TSuccess}"/>.</param>
    /// <param name="action">An asynchronous action (returning <see cref="ValueTask"/>) to perform on the error value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> that completes with the original <see cref="Try{TSuccess}"/>.</returns>
    public static async Task<Try<TSuccess>> OnErrorAsync<TSuccess>(
        this Task<Try<TSuccess>> source,
        Func<Exception, ValueTask> action,
        CancellationToken cancellationToken = default
    ) {
        Try<TSuccess> tryResult = null!;
        try {
            cancellationToken.ThrowIfCancellationRequested();
            tryResult = await source.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (tryResult.IsError) await action(tryResult.ErrorValue()).ConfigureAwait(false);
            return tryResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) when (tryResult is null) {
            return new Try<TSuccess>(ex);
        }
    }

    /// <summary>
    /// Asynchronously performs an action on the error value (exception) if the <see cref="ValueTask{TResult}"/>
    /// completes successfully but its result is an error. The original <see cref="Try{TSuccess}"/> is returned.
    /// Exceptions from the <paramref name="action"/> are not caught.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="source">The source asynchronous operation yielding a <see cref="Try{TSuccess}"/>.</param>
    /// <param name="action">An asynchronous action to perform on the error value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the original <see cref="Try{TSuccess}"/>.</returns>
    public static async ValueTask<Try<TSuccess>> OnErrorAsync<TSuccess>(
        this ValueTask<Try<TSuccess>> source,
        Func<Exception, Task> action,
        CancellationToken cancellationToken = default
    ) {
        Try<TSuccess> tryResult = null!;
        try {
            cancellationToken.ThrowIfCancellationRequested();
            tryResult = await source.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (tryResult.IsError) await action(tryResult.ErrorValue()).ConfigureAwait(false);
            return tryResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) when (tryResult is null) {
            return new Try<TSuccess>(ex);
        }
    }

    /// <summary>
    /// Asynchronously performs an action (returning <see cref="ValueTask"/>) on the error value (exception)
    /// if the <see cref="ValueTask{TResult}"/> completes successfully but its result is an error. The original <see cref="Try{TSuccess}"/> is returned.
    /// Exceptions from the <paramref name="action"/> are not caught.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="source">The source asynchronous operation yielding a <see cref="Try{TSuccess}"/>.</param>
    /// <param name="action">An asynchronous action (returning <see cref="ValueTask"/>) to perform on the error value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the original <see cref="Try{TSuccess}"/>.</returns>
    public static async ValueTask<Try<TSuccess>> OnErrorAsync<TSuccess>(
        this ValueTask<Try<TSuccess>> source,
        Func<Exception, ValueTask> action,
        CancellationToken cancellationToken = default
    ) {
        Try<TSuccess> tryResult = null!;
        try {
            cancellationToken.ThrowIfCancellationRequested();
            tryResult = await source.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (tryResult.IsError) await action(tryResult.ErrorValue()).ConfigureAwait(false);
            return tryResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) when (tryResult is null) {
            return new Try<TSuccess>(ex);
        }
    }

    /// <summary>
    /// Asynchronously performs an action if the source <see cref="Try{TSuccess}"/> is an error.
    /// The original <see cref="Try{TSuccess}"/> is returned.
    /// Exceptions from the <paramref name="action"/> are not caught.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="source">The source <see cref="Try{TSuccess}"/>.</param>
    /// <param name="action">An asynchronous action to perform on the error value.</param>
    /// <param name="cancellationToken">A token to cancel the operation (primarily for the action).</param>
    /// <returns>A <see cref="Task{TResult}"/> that completes with the original <see cref="Try{TSuccess}"/>.</returns>
    public static async Task<Try<TSuccess>> OnErrorAsync<TSuccess>(
        this Try<TSuccess> source,
        Func<Exception, Task> action,
        CancellationToken cancellationToken = default
    ) {
        if (source.IsError)
            try {
                cancellationToken.ThrowIfCancellationRequested();
                await action(source.ErrorValue()).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            }

        return source;
    }

    /// <summary>
    /// Asynchronously performs an action (returning <see cref="ValueTask"/>) if the source <see cref="Try{TSuccess}"/> is an error.
    /// The original <see cref="Try{TSuccess}"/> is returned.
    /// Exceptions from the <paramref name="action"/> are not caught.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="source">The source <see cref="Try{TSuccess}"/>.</param>
    /// <param name="action">An asynchronous action (returning <see cref="ValueTask"/>) to perform on the error value.</param>
    /// <param name="cancellationToken">A token to cancel the operation (primarily for the action).</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the original <see cref="Try{TSuccess}"/>.</returns>
    public static async ValueTask<Try<TSuccess>> OnErrorAsync<TSuccess>(
        this Try<TSuccess> source,
        Func<Exception, ValueTask> action,
        CancellationToken cancellationToken = default
    ) {
        if (!source.IsError) return source;

        try {
            cancellationToken.ThrowIfCancellationRequested();
            await action(source.ErrorValue()).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }

        return source;
    }

    #endregion
}
