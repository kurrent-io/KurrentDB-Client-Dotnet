using Kurrent.Variant;

namespace Kurrent.Client.Projections;

[PublicAPI]
public readonly partial record struct CreateOneTimeError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct CreateContinuousError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct CreateTransientError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct DeleteProjectionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct ListOneTimeError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct ListContinuousError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct ListAllError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct GetStatusError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct UpdateError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct EnableError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct ResetError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct AbortError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct DisableError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct RestartSubsystemError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct GetStateError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct GetResultError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;
