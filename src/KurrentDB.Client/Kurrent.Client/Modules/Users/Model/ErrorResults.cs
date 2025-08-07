using Kurrent.Variant;

namespace Kurrent.Client.Users;

[PublicAPI]
public readonly partial record struct CreateUserError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.AlreadyExists>;

[PublicAPI]
public readonly partial record struct DeleteUserError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct EnableUserError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct DisableUserError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct ChangeUserPasswordError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct ResetUserPasswordError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct GetUserError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct ListAllUsersError : IVariantResultError<
    ErrorDetails.AccessDenied>;
