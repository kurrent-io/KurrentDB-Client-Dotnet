using Types = KurrentDB.Protocol.CoreErrorDetails.Types;

namespace Kurrent.Client;

/// <summary>
/// Provides a set of error detail types for representing specific operation failures when interacting with KurrentDB.
/// </summary>
[PublicAPI]
public static partial class ErrorDetails {
    /// <summary>
    /// Represents an error indicating that access to the requested resource has been denied.
    /// </summary>
    [KurrentOperationError(typeof(Types.AccessDenied))]
    public readonly partial record struct AccessDenied;

    /// <summary>
    /// Represents an error indicating that the specified user could not be found.
    /// </summary>
    [KurrentOperationError(typeof(Types.UserNotFound))]
    public readonly partial record struct UserNotFound;

    /// <summary>
    /// Represents an error indicating that the user is not authenticated, typically due to missing or incorrect credentials.
    /// </summary>
    [KurrentOperationError(typeof(Types.NotAuthenticated))]
    public readonly partial record struct NotAuthenticated;
}
