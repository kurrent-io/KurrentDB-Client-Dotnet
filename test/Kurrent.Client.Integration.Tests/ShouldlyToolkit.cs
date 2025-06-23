using System.Runtime.CompilerServices;

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

    public static async ValueTask<TValue> ShouldNotThrowAsync<TValue>(this ValueTask<TValue> operation, string? customMessage = null) {
        TValue taskResult = default!;

        await Should
            .NotThrowAsync(async () => taskResult = await operation.AsTask().ConfigureAwait(false), customMessage)
            .ConfigureAwait(false);

        return taskResult;
    }

    public static async ValueTask<Result<TValue, Exception>> ShouldNotThrowAsync<TValue>(this ValueTask<Result<TValue, Exception>> operation, string? customMessage = null) {
        Result<TValue, Exception> result = default!;

        await Should
            .NotThrowAsync(async () => result = await operation.AsTask().ConfigureAwait(false), customMessage)
            .ConfigureAwait(false);

        return result;
    }

    public static async ValueTask<Result<TValue, TError>> ShouldNotThrowAsync<TValue, TError>(this ValueTask<Result<TValue, TError>> operation, string? customMessage = null) {
        Result<TValue, TError> result = default!;

        await Should
            .NotThrowAsync(async () => result = await operation.AsTask().ConfigureAwait(false), customMessage)
            .ConfigureAwait(false);

        return result;
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid ShouldBeGuid(this string guidString, string? customMessage = null) {
        if (Guid.TryParse(guidString, out Guid result)) {
            return result;
        }

        throw new Shouldly.ShouldAssertException(
            customMessage ?? $"String \"{guidString}\" should be a valid GUID but was not");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid ShouldBeGuid(this Guid guid) {
        return guid;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ShouldBeGuid<T>(this T obj, string? customMessage = null) {
        if (obj == null) {
            throw new Shouldly.ShouldAssertException(
                customMessage ?? "Value should be a valid GUID but was null");
        }

        Guid.TryParse(obj.ToString(), out Guid _).ShouldBeTrue(
            customMessage ?? $"Value \"{obj}\" should be a valid GUID but was not");

        return obj;
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
