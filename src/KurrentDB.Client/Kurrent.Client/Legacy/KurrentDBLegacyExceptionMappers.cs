using System.Diagnostics.CodeAnalysis;
using Grpc.Core;
using Kurrent.Client.Streams;
using KurrentDB.Client;

namespace Kurrent.Client.Legacy;

static class LegacyErrorCodes {
    public const string AccessDenied              = "access-denied";
    // public const string InvalidTransaction        = "invalid-transaction";
    public const string StreamDeleted             = "stream-deleted";
    public const string WrongExpectedVersion      = "wrong-expected-version";
    // public const string StreamNotFound            = "stream-not-found";
    public const string MaximumAppendSizeExceeded = "maximum-append-size-exceeded";

    public const string MissingRequiredMetadataProperty = "missing-required-metadata-property";

    public const string NotLeader                       = "not-leader";

    // public const string UserNotFound     = "user-not-found";
    // public const string UserConflict     = "user-conflict";
    // public const string ScavengeNotFound = "scavenge-not-found";

    public const string PersistentSubscriptionFailed       = "persistent-subscription-failed";
    public const string PersistentSubscriptionDoesNotExist = "persistent-subscription-does-not-exist";
    public const string PersistentSubscriptionExists       = "persistent-subscription-exists";
    public const string MaximumSubscribersReached          = "maximum-subscribers-reached";
    public const string PersistentSubscriptionDropped      = "persistent-subscription-dropped";
}


static class KurrentDBLegacyExceptionMappers {
    // public static ErrorDetails.AccessDenied AsAccessDeniedError(this RpcException ex, StreamName stream) => new(x => x.With("Stream", stream));
    //
    // public static ErrorDetails.AccessDenied AsAccessDeniedError(this RpcException ex) =>
    //     new(x => x.With("Reason", "Access denied while reading all streams."));

   //  public static ErrorDetails.UserNotFound AsUserNotFoundError(this Exception ex) =>
   //      ex is RpcException
			// ? new(x => x.With("Reason", "User not found."))
			// : throw new InvalidCastException($"Expected {nameof(UserNotFoundException)} but got {ex.GetType().Name} while mapping to {nameof(ErrorDetails.UserNotFound)}.", ex);

    // public static ErrorDetails.NotAuthenticated AsNotAuthenticatedError(this Exception ex) =>
	   //  ex is RpcException
		  //   ? new(x => x.With("Reason", "User is not authenticated."))
		  //   : throw new InvalidCastException($"Expected {nameof(NotAuthenticatedException)} but got {ex.GetType().Name} while mapping to {nameof(ErrorDetails.NotAuthenticated)}.", ex);

    // public static ErrorDetails.ScavengeNotFound AsScavengeNotFoundError(this Exception ex) =>
	   //  ex is ScavengeNotFoundException
		  //   ? new(x => x.With("Reason", ex.Message))
		  //   : throw new InvalidCastException($"Expected {nameof(ScavengeNotFoundException)} but got {ex.GetType().Name} while mapping to {nameof(ErrorDetails.ScavengeNotFound)}.", ex);


    //
    // public static bool IsAccessDeniedError(this RpcException rex) => rex.IsLegacyError(LegacyErrorCodes.AccessDenied);
    //
    // public static bool IsMaximumAppendSizeExceededError(this RpcException rex) => rex.IsLegacyError(LegacyErrorCodes.MaximumAppendSizeExceeded);
    //
    // public static bool IsMissingRequiredMetadataPropertyError(this RpcException rex) => rex.IsLegacyError(LegacyErrorCodes.MissingRequiredMetadataProperty);
    //
    // public static bool IsNotLeaderError(this RpcException rex)        => rex.IsLegacyError(LegacyErrorCodes.NotLeader);
    // public static bool IsUserNotFoundError(this RpcException rex)     => rex.IsLegacyError(LegacyErrorCodes.UserNotFound);
    // public static bool IsUserConflictError(this RpcException rex)     => rex.IsLegacyError(LegacyErrorCodes.UserConflict);
    // public static bool IsScavengeNotFoundError(this RpcException rex) => rex.IsLegacyError(LegacyErrorCodes.ScavengeNotFound);
    //
    // public static bool IsPersistentSubscriptionFailedError(this RpcException rex)       => rex.IsLegacyError(LegacyErrorCodes.PersistentSubscriptionFailed);
    // public static bool IsPersistentSubscriptionDoesNotExistError(this RpcException rex) => rex.IsLegacyError(LegacyErrorCodes.PersistentSubscriptionDoesNotExist);
    // public static bool IsPersistentSubscriptionExistsError(this RpcException rex)       => rex.IsLegacyError(LegacyErrorCodes.PersistentSubscriptionExists);
    // public static bool IsMaximumSubscribersReachedError(this RpcException rex)          => rex.IsLegacyError(LegacyErrorCodes.MaximumSubscribersReached);
    // public static bool IsPersistentSubscriptionDroppedError(this RpcException rex)      => rex.IsLegacyError(LegacyErrorCodes.PersistentSubscriptionDropped);
}

static class KurrentDBLegacyExceptionExtensions {
    const string LegacyErrorCodeKey = "exception";

    // public static string? GetLegacyErrorCode(this RpcException rex) =>
    //     rex.Trailers.FirstOrDefault(x => x.Key == LegacyErrorCodeKey)?.Value;
    //
    // public static bool TryGetLegacyErrorCode(this RpcException rex, [MaybeNullWhen(false)] out string errorCode) =>
    //     (errorCode = rex.GetLegacyErrorCode()) != null;
    //

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

    // public static T MapToResultError<T>(this Exception ex, string legacyErrorCode, Func<RpcException, T> map) where T : IResultError {
    //     if (ex is not RpcException rex || !rex.TryGetLegacyErrorCode(out var code) || code != legacyErrorCode)
    //         throw new InvalidCastException($"Expected {nameof(RpcException)} with legacy error code {legacyErrorCode} but got {ex.GetType().Name} while mapping to {typeof(T).Name}.", ex);
    //
    //     try {
    //         return map(rex);
    //     }
    //     catch (Exception mex) {
    //         throw new InvalidCastException($"Failed to map {nameof(RpcException)} with legacy error code {legacyErrorCode} to {typeof(T).Name}.", mex);
    //     }
    // }
    //
    // public static T MapToKurrentException<T>(this Exception ex, string legacyErrorCode, Func<RpcException, T> map) where T : KurrentException {
    //     if (ex is not RpcException rex || !rex.TryGetLegacyErrorCode(out var code) || code != legacyErrorCode)
    //         throw new InvalidCastException($"Expected {nameof(RpcException)} with legacy error code {legacyErrorCode} but got {ex.GetType().Name} while mapping to {typeof(T).Name}.", ex);
    //
    //     try {
    //         return map(rex);
    //     }
    //     catch (Exception mex) {
    //         throw new InvalidCastException($"Failed to map {nameof(RpcException)} with legacy error code {legacyErrorCode} to {typeof(T).Name}.", mex);
    //     }
    // }

    // public static Metadata ConvertTrailersToMetadata(this RpcException rex) =>
    //     new(rex.Trailers.ToDictionary(x => x.Key, object? (x) => x.Value));
}
