using Kurrent.Variant;

namespace Kurrent.Client.PersistentSubscriptions;

[PublicAPI]
public readonly partial record struct CreateStreamSubscriptionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.AlreadyExists>;

[PublicAPI]
public readonly partial record struct CreateAllStreamSubscriptionError : IVariantResultError<
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct DeleteSubscription : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct GetInfoToAllError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct GetInfoToStreamError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct ListToAllError : IVariantResultError<
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct ListToStreamError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct ListAllError : IVariantResultError<
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct SubscribeToStreamError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound,
    ErrorDetails.MaximumSubscribersReached,
    ErrorDetails.PersistentSubscriptionDropped>;

[PublicAPI]
public readonly partial record struct SubscribeToAllError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.MaximumSubscribersReached,
    ErrorDetails.PersistentSubscriptionDropped>;

[PublicAPI]
public readonly partial record struct ReplayParkedMessagesToAllError : IVariantResultError<
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct ReplayParkedMessagesToStreamError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct RestartPersistentSubscriptionsSubsystemError : IVariantResultError<
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct UpdateSubscriptionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;
