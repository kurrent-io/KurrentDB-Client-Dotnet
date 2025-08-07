using System.Diagnostics.CodeAnalysis;
using Grpc.Core;

namespace Kurrent.Client.Legacy;

static class KurrentDBLegacyExceptionExtensions {
    const string LegacyErrorCodeKey = "exception";

    public static string? GetLegacyErrorCode(this Exception ex) =>
        ex is RpcException rex
            ? rex.Trailers.FirstOrDefault(x => x.Key == LegacyErrorCodeKey)?.Value
            : null;

    public static bool TryGetLegacyErrorCode(this Exception ex, [MaybeNullWhen(false)] out string errorCode) =>
        (errorCode = ex.GetLegacyErrorCode()) != null;

    public static bool IsLegacyError(this RpcException rex, string legacyErrorCode) =>
        rex.GetLegacyErrorCode() == legacyErrorCode;

    public static T MapToResultError<T>(this Exception ex, string legacyErrorCode, Func<RpcException, T> map) where T : IResultError {
        if (ex is not RpcException rex || !rex.TryGetLegacyErrorCode(out var code) || code != legacyErrorCode)
            throw new InvalidCastException($"Expected {nameof(RpcException)} with legacy error code {legacyErrorCode} but got {ex.GetType().Name} while mapping to {typeof(T).Name}.", ex);

        try {
            return map(rex);
        }
        catch (Exception mex) {
            throw new InvalidCastException($"Failed to map {nameof(RpcException)} with legacy error code {legacyErrorCode} to {typeof(T).Name}.", mex);
        }
    }

    public static bool TryMapToErrorResult<T>(this Exception ex, string legacyErrorCode, Func<RpcException, T> map, [MaybeNullWhen(false)] out T errorResult) where T : IResultError {
        if (ex is not RpcException rex || !rex.TryGetLegacyErrorCode(out var code) || code != legacyErrorCode) {
            errorResult = default(T);
            return false;
        }

        try {
            errorResult = map(rex);
            return true;
        }
        catch (Exception mex) {
            throw new InvalidCastException($"Failed to map {nameof(RpcException)} with legacy error code {legacyErrorCode} to {typeof(T).Name}.", mex);
        }
    }
}
