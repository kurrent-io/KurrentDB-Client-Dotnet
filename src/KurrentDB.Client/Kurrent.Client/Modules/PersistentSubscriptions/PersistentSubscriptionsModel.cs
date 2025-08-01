using Kurrent.Variant;

namespace Kurrent.Client.Model.PersistentSubscriptions;

[PublicAPI]
public readonly partial record struct CreateToStreamError : IVariantResultError<
	Client.ErrorDetails.AccessDenied,
	Client.ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct CreateToAllError : IVariantResultError<
	Client.ErrorDetails.AccessDenied,
	Client.ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct DeleteToStreamError : IVariantResultError<
	Client.ErrorDetails.PersistentSubscriptionNotFound,
	Client.ErrorDetails.AccessDenied,
	Client.ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct DeleteToAllError : IVariantResultError<
	Client.ErrorDetails.AccessDenied,
	Client.ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct GetInfoToAllError : IVariantResultError<
	Client.ErrorDetails.AccessDenied,
	Client.ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct GetInfoToStreamError : IVariantResultError<
	Client.ErrorDetails.PersistentSubscriptionNotFound,
	Client.ErrorDetails.AccessDenied,
	Client.ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct ListToAllError : IVariantResultError<
	Client.ErrorDetails.AccessDenied,
	Client.ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct ListToStreamError : IVariantResultError<
	Client.ErrorDetails.PersistentSubscriptionNotFound,
	Client.ErrorDetails.AccessDenied,
	Client.ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct ListAllError : IVariantResultError<
	Client.ErrorDetails.AccessDenied,
	Client.ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct SubscribeToStreamError : IVariantResultError<
	Client.ErrorDetails.PersistentSubscriptionNotFound,
	Client.ErrorDetails.MaximumSubscribersReached,
	Client.ErrorDetails.PersistentSubscriptionDropped,
	Client.ErrorDetails.AccessDenied,
	Client.ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct SubscribeToAllError : IVariantResultError<
	Client.ErrorDetails.MaximumSubscribersReached,
	Client.ErrorDetails.PersistentSubscriptionDropped,
	Client.ErrorDetails.AccessDenied,
	Client.ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct ReplayParkedMessagesToAllError : IVariantResultError<
	Client.ErrorDetails.AccessDenied,
	Client.ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct ReplayParkedMessagesToStreamError : IVariantResultError<
	Client.ErrorDetails.PersistentSubscriptionNotFound,
	Client.ErrorDetails.AccessDenied,
	Client.ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct RestartSubsystemError : IVariantResultError<
	Client.ErrorDetails.AccessDenied,
	Client.ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct UpdateToStreamError : IVariantResultError<
	Client.ErrorDetails.PersistentSubscriptionNotFound,
	Client.ErrorDetails.AccessDenied,
	Client.ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct UpdateToAllError : IVariantResultError<
	Client.ErrorDetails.AccessDenied,
	Client.ErrorDetails.NotAuthenticated>;
