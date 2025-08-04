using Kurrent.Variant;

namespace Kurrent.Client.Operations;

[PublicAPI]
public readonly partial record struct ShutdownError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct MergeIndexesError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct ResignNodeError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct SetNodePriorityError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct RestartPersistentSubscriptionsError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct StartScavengeError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct StopScavengeError : IVariantResultError<
    ErrorDetails.ScavengeNotFound,
    ErrorDetails.AccessDenied,
    ErrorDetails.NotAuthenticated>;
