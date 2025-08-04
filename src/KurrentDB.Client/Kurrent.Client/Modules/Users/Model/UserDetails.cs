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
