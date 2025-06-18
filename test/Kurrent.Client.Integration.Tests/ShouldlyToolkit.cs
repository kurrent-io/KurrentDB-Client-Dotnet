using System.Runtime.CompilerServices;

namespace Kurrent.Client.Tests;

public static class Should {
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Exception RecordException(Exception exception, string customMessage) {
        ArgumentException.ThrowIfNullOrWhiteSpace(customMessage);
        Shouldly.Should.NotThrow(() => exception, customMessage);
        return exception;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Exception RecordException(Exception exception) {
        Shouldly.Should.NotThrow(() => exception);
        return exception;
    }
}

public static class ShouldlyThrowExtensions {
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async ValueTask<T> ShouldNotThrowAsync<T>(this ValueTask<T> operation, string customMessage) {
        ArgumentException.ThrowIfNullOrWhiteSpace(customMessage);

        T taskResult = default!;

        await Shouldly.Should
            .NotThrowAsync(async () => taskResult = await operation.AsTask().ConfigureAwait(false), customMessage)
            .ConfigureAwait(false);

        return taskResult;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async ValueTask<T> ShouldNotThrowAsync<T>(this ValueTask<T> operation) {
        T taskResult = default!;

        await Shouldly.Should
            .NotThrowAsync(async () => taskResult = await operation.AsTask().ConfigureAwait(false))
            .ConfigureAwait(false);

        return taskResult;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ShouldNotThrow<T>(this Func<T> operation, string customMessage) {
        ArgumentException.ThrowIfNullOrWhiteSpace(customMessage);
        T taskResult = default!;
        Shouldly.Should.NotThrow(operation, customMessage);
        return taskResult;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ShouldNotThrow<T>(this Func<T> operation) {
        T taskResult = default!;
        Shouldly.Should.NotThrow(operation);
        return taskResult;
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
        return guid; // Already a valid GUID by definition
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
}
