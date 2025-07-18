using System.Runtime.InteropServices;
using Kurrent.Variant;

namespace Kurrent.Client.Model;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct Success {
	public static readonly Success Instance = new();
}

[PublicAPI]
public readonly partial record struct CreateUserError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated
>;

[PublicAPI]
public readonly partial record struct GetUserError : IVariantResultError<
	ErrorDetails.AccessDenied
>;

[PublicAPI]
public readonly partial record struct DeleteUserError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.UserNotFound,
	ErrorDetails.NotAuthenticated
>;

[PublicAPI]
public readonly partial record struct EnableUserError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated
>;

[PublicAPI]
public readonly partial record struct DisableUserError : IVariantResultError<
	ErrorDetails.NotAuthenticated,
	ErrorDetails.AccessDenied
>;

[PublicAPI]
public readonly partial record struct ListAllUsersError : IVariantResultError<
	ErrorDetails.AccessDenied
>;

[PublicAPI]
public readonly partial record struct ChangePasswordError : IVariantResultError<
	ErrorDetails.AccessDenied
>;

[PublicAPI]
public readonly partial record struct ResetPasswordError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated
>;
