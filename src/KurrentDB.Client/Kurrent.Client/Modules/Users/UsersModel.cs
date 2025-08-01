using Kurrent.Variant;

namespace Kurrent.Client.Users;

/// <summary>
/// Represents detailed information about a user in KurrentDB.
/// </summary>
public record UserDetails {
	/// <summary>
	/// The user's login name.
	/// </summary>
	public required string LoginName { get; init; }

	/// <summary>
	/// The full name of the user.
	/// </summary>
	public required string FullName { get; init; }

	/// <summary>
	/// The groups the user is a member of.
	/// </summary>
	public IReadOnlyList<string> Groups { get; init; } = [];

	/// <summary>
	/// The date/time the user was last updated in UTC format.
	/// </summary>
	public DateTimeOffset? DateLastUpdated { get; init; }

	/// <summary>
	/// Whether the user is disabled or not.
	/// </summary>
	public bool Disabled { get; init; }

	/// <summary>
	/// Gets a value indicating whether the user has any group memberships.
	/// </summary>
	public bool HasGroups => Groups.Count > 0;

	/// <summary>
	/// Gets a value indicating whether the user is active (not disabled).
	/// </summary>
	public bool IsActive => !Disabled;

	/// <summary>
	/// Gets a value indicating whether the user has been updated.
	/// </summary>
	public bool HasBeenUpdated => DateLastUpdated.HasValue;
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
