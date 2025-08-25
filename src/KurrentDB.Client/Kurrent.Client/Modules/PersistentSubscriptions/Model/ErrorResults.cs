using Kurrent.Variant;

namespace Kurrent.Client.PersistentSubscriptions;

[PublicAPI]
public readonly partial record struct CreateSubscriptionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.AlreadyExists>;

[PublicAPI]
public readonly partial record struct UpdateSubscriptionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct DeleteSubscriptionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct GetSubscriptionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct ListSubscriptionsError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct ReplaySubscriptionParkedMessagesError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

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
public readonly partial record struct RestartPersistentSubscriptionsSubsystemError : IVariantResultError<
    ErrorDetails.AccessDenied>;
