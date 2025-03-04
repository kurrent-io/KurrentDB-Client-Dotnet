namespace KurrentDb.Client;

/// <summary>
///  A set of extension methods for an <see cref="KurrentDBUserManagementClient"/>.
/// </summary>
public static class KurrentDBUserManagerClientExtensions {
    /// <summary>
    /// Gets the <see cref="UserDetails"/> of the internal user specified by the supplied <see cref="UserCredentials"/>.
    /// </summary>
    /// <param name="dbUsers"></param>
    /// <param name="userCredentials"></param>
    /// <param name="deadline"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task<UserDetails> GetCurrentUserAsync(
        this KurrentDBUserManagementClient dbUsers,
        UserCredentials userCredentials, TimeSpan? deadline = null, CancellationToken cancellationToken = default
    ) =>
        dbUsers.GetUserAsync(
            userCredentials.Username!, deadline, userCredentials,
            cancellationToken
        );
}
