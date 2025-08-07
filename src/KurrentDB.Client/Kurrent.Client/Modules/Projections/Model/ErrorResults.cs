using Kurrent.Variant;

namespace Kurrent.Client.Projections;

[PublicAPI]
public readonly partial record struct CreateProjectionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.AlreadyExists>;

[PublicAPI]
public readonly partial record struct DeleteProjectionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct ListProjectionsError : IVariantResultError<
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct GetProjectionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct UpdateProjectionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct EnableProjectionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct ResetProjectionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct AbortProjectionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct DisableProjectionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct RestartProjectionSubsystemError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.ProjectionsSubsystemRestartFailed>;

[PublicAPI]
public readonly partial record struct GetProjectionStateError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct GetProjectionResultError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;
