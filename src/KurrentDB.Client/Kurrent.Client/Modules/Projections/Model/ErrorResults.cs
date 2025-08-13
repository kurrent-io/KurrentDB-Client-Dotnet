using Kurrent.Variant;

namespace Kurrent.Client.Projections;

[PublicAPI]
public readonly partial record struct CreateProjectionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.AlreadyExists>;

[PublicAPI]
public readonly partial record struct UpdateProjectionDefinitionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound,
    ErrorDetails.FailedPrecondition>;

[PublicAPI]
public readonly partial record struct GetProjectionDefinitionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct UpdateProjectionSettingsError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound,
    ErrorDetails.FailedPrecondition>;

[PublicAPI]
public readonly partial record struct GetProjectionSettingsError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct DeleteProjectionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound,
    ErrorDetails.FailedPrecondition>;

[PublicAPI]
public readonly partial record struct EnableProjectionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct ResetProjectionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct DisableProjectionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct GetProjectionStateError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct GetProjectionResultError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct GetProjectionDetailsError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct ListProjectionsError : IVariantResultError<
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct RestartProjectionSubsystemError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.ProjectionsSubsystemRestartFailed>;
