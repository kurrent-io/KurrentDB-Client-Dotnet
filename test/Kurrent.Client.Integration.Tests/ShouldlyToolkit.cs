using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Kurrent.Client.Model;

namespace Kurrent.Client.Tests;

public static class ShouldlyResultExtensions {
    #region . should-not-throw .

    public static TValue ShouldNotThrow<TValue>(this Func<TValue> operation, string? customMessage = null) =>
        Should.NotThrow(operation, customMessage);

    public static TValue ShouldNotThrow<TValue>(this Result<TValue, Exception> result) {
        if (result.IsFailure)
            Should.NotThrow(() => throw result.Error);

        return result.Value;
    }

    public static async ValueTask<TValue> ShouldNotThrowAsync<TValue>(this ValueTask<TValue> operation, string? customMessage = null, [CallerMemberName] string shouldlyMethod = null!) =>
        await NotThrowAsync(() => operation, customMessage, shouldlyMethod).ConfigureAwait(false);

    public static async ValueTask<Result<TValue, Exception>> ShouldNotThrowAsync<TValue>(this ValueTask<Result<TValue, Exception>> operation, string? customMessage = null, [CallerMemberName] string shouldlyMethod = null!) =>
        await NotThrowAsync(() => operation, customMessage, shouldlyMethod).ConfigureAwait(false);

    public static async ValueTask<Result<TValue, TError>> ShouldNotThrowAsync<TValue, TError>(this ValueTask<Result<TValue, TError>> operation, string? customMessage = null, [CallerMemberName] string shouldlyMethod = null!) =>
        await NotThrowAsync(() => operation, customMessage, shouldlyMethod).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ValueTask<T> NotThrowAsync<T>(Func<ValueTask<T>> actual, string? customMessage = null, string shouldlyMethod = null!) =>
        NotThrowAsyncInternal(actual, customMessage, shouldlyMethod);

    internal static async ValueTask<T> NotThrowAsyncInternal<T>([InstantHandle] Func<ValueTask<T>> actual, string? customMessage, string shouldlyMethod = null!) {
        var operation = actual();

        if (operation.IsCompletedSuccessfully)
            return operation.Result;

        return await operation.AsTask().ContinueWith(t => {
                if (!t.IsFaulted) return t.Result;

                var ex = t.Exception!.Flatten() is { InnerExceptions.Count: 1, InnerException: { } inner } ? inner : t.Exception;

                throw new ShouldAssertException(
                    new AsyncShouldlyNotThrowShouldlyMessage(
                        ex.GetType(), null, new StackTrace(ex),
                        ex.ToString(), shouldlyMethod
                    ).ToString(), ex
                );

                // throw new ShouldAssertException(
                //     new AsyncShouldlyNotThrowShouldlyMessage(
                //         ex.GetType(), null, new StackTrace(ex),
                //         ex is KurrentClientException kex ? kex.Message : ex.Message, shouldlyMethod
                //     ).ToString(), ex
                // );

                // var flattened = t.Exception!.Flatten();
                // if (flattened.InnerExceptions.Count == 1 && flattened.InnerException is { } inner) {
                //     throw new ShouldAssertException(
                //         new AsyncShouldlyNotThrowShouldlyMessage(
                //             inner.GetType(), customMessage, new StackTrace(inner),
                //             inner.Message, shouldlyMethod
                //         ).ToString(), inner
                //     );
                // }
                //
                // throw new ShouldAssertException(
                //     new AsyncShouldlyNotThrowShouldlyMessage(
                //         t.Exception.GetType(), customMessage, new StackTrace(t.Exception),
                //         t.Exception.Message, shouldlyMethod
                //     ).ToString(), t.Exception
                // );
            }
        );
    }

    #endregion

    #region . should not fail .

    // public static TValue ShouldNotFail<TValue, TError>(this Result<TValue, TError> result, string? customMessage = null)
    //     where TError : IResultError where TValue : notnull {
    //     if (result.IsFailure) Should.NotThrow(() => result.Error.Throw(), customMessage);
    //     return result.Value;
    // }
    //
    // public static TOut ShouldNotFail<TValue, TError, TOut>(this Result<TValue, TError> result, Func<TValue, TOut> onSuccess, string? customMessage = null)
    //     where TError : IResultError where TValue : notnull {
    //     if (result.IsFailure) Should.NotThrow(() => result.Error.Throw(), customMessage);
    //     return onSuccess(result.Value);
    // }

    // public static async ValueTask<TOut> ShouldNotFailAsync<TValue, TError, TOut>(this ValueTask<Result<TValue, TError>> resultTask, Func<TValue, TOut> assert, string? customMessage = null)
    //     where TError : IResultError where TValue : notnull {
    //     var result = await resultTask.ConfigureAwait(false);
    //     if (result.IsFailure)
    //         return assert(result.Value);
    //
    //     throw Should.NotThrow(() => result.Error.Throw(), customMessage);
    // }

    public static async ValueTask<TValue> ShouldNotFailAsync<TValue, TError>(this ValueTask<Result<TValue, TError>> resultTask, Action<TValue> assert, string? customMessage = null)
        where TError : IResultError where TValue : notnull {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure) Should.NotThrow(() => result.Error.Throw(), customMessage);
        assert(result.Value);
        return result.Value;
    }

    public static async ValueTask<TValue> ShouldNotFailAsync<TValue, TError>(this ValueTask<Result<TValue, TError>> resultTask, string? customMessage = null)
        where TError : IResultError where TValue : notnull {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure) Should.NotThrow(() => result.Error.Throw(), customMessage);
        return result.Value;
    }

    #endregion

    #region . should not throw or fail .

    public static async ValueTask<TValue> ShouldNotThrowOrFailAsync<TValue, TError>(this ValueTask<Result<TValue, TError>> operation, string? customMessage = null)
        where TError : IResultError where TValue : notnull{
        return await operation
            .ShouldNotThrowAsync(customMessage)
            .ShouldNotFailAsync(customMessage)
            .ConfigureAwait(false);
    }

    // public static async ValueTask<TOut> ShouldNotThrowOrFailAsync<TValue, TError, TOut>(this ValueTask<Result<TValue, TError>> operation, Func<TValue, TOut> assert, string? customMessage = null)
    //     where TError : IResultError where TValue : notnull {
    //     return await operation
    //         .ShouldNotThrowAsync(customMessage)
    //         .ShouldNotFailAsync(assert, customMessage)
    //         .ConfigureAwait(false);
    // }

    public static async ValueTask<TValue> ShouldNotThrowOrFailAsync<TValue, TError>(this ValueTask<Result<TValue, TError>> operation, Action<TValue> assert, string? customMessage = null)
        where TError : IResultError where TValue : notnull {
        return await operation
            .ShouldNotThrowAsync(customMessage)
            .ShouldNotFailAsync(assert, customMessage)
            .ConfigureAwait(false);
    }

    #endregion

    #region . should fail .

    public static async ValueTask<TError> ShouldFailAsync<TValue, TError>(this ValueTask<Result<TValue, TError>> operation)
        where TError : IResultError where TValue : notnull {
        var errorMessage = $"Expected operation to fail with error of type {typeof(TError).Name}";
        var result = await operation.ShouldNotThrowAsync(errorMessage).ConfigureAwait(false);

        return result.Case switch {
            ResultCase.Failure => result.Error,
            ResultCase.Success => result.Value.ShouldBeOfType<TError>(errorMessage)
        };
    }

    public static async ValueTask ShouldFailAsync<TValue, TError>(this ValueTask<Result<TValue, TError>> operation, Action<TError> assert)
        where TError : IResultError where TValue : notnull {
        var errorMessage = $"Expected operation to fail with error of type {typeof(TError).Name}";

        var result = await operation.ShouldNotThrowAsync(errorMessage).ConfigureAwait(false);

        var error = result.Case switch {
            ResultCase.Failure => result.Error,
            ResultCase.Success => result.Value.ShouldBeOfType<TError>(errorMessage)
        };

        assert(error);
    }

    #endregion
}
