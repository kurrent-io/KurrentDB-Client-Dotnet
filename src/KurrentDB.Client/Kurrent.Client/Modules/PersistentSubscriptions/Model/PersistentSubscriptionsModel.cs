using Kurrent.Variant;

namespace Kurrent.Client.Streams.PersistentSubscriptions;

[PublicAPI]
public readonly partial record struct CreateToStreamError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct CreateToAllError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct DeleteToStreamError : IVariantResultError<
	ErrorDetails.PersistentSubscriptionNotFound,
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct DeleteToAllError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct GetInfoToAllError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct GetInfoToStreamError : IVariantResultError<
	ErrorDetails.PersistentSubscriptionNotFound,
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct ListToAllError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct ListToStreamError : IVariantResultError<
	ErrorDetails.PersistentSubscriptionNotFound,
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct ListAllError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct SubscribeToStreamError : IVariantResultError<
	ErrorDetails.PersistentSubscriptionNotFound,
	ErrorDetails.MaximumSubscribersReached,
	ErrorDetails.PersistentSubscriptionDropped,
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct SubscribeToAllError : IVariantResultError<
	ErrorDetails.MaximumSubscribersReached,
	ErrorDetails.PersistentSubscriptionDropped,
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct ReplayParkedMessagesToAllError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct ReplayParkedMessagesToStreamError : IVariantResultError<
	ErrorDetails.PersistentSubscriptionNotFound,
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct RestartSubsystemError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct UpdateToStreamError : IVariantResultError<
	ErrorDetails.PersistentSubscriptionNotFound,
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated>;

[PublicAPI]
public readonly partial record struct UpdateToAllError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated>;
