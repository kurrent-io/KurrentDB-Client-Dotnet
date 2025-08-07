using Kurrent.Variant;

namespace Kurrent.Client.Admin;

[PublicAPI]
public readonly partial record struct ShutdownError : IVariantResultError<
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct MergeIndexesError : IVariantResultError<
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct ResignNodeError : IVariantResultError<
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct SetNodePriorityError : IVariantResultError<
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct RestartPersistentSubscriptionsError : IVariantResultError<
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct StartScavengeError : IVariantResultError<
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct StopScavengeError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;
