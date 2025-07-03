using static KurrentDB.Protocol.Streams.V2.ErrorDetails;

namespace Kurrent.Client.Model;

/// <summary>
/// Provides a set of error detail types for representing specific append operation failures when interacting with KurrentDB.
/// </summary>
[PublicAPI]
public static partial class ErrorDetails {
    /// <summary>
    /// Represents an error indicating that the specified stream could not be found.
    /// </summary>
    [KurrentOperationError(typeof(Types.StreamNotFound))]
    public readonly partial record struct StreamNotFound;

    /// <summary>
    /// Represents an error indicating that the specified stream has been deleted.
    /// </summary>
    [KurrentOperationError(typeof(Types.StreamDeleted))]
    public readonly partial record struct StreamDeleted;

    /// <summary>
    /// Represents an error indicating that access to the requested resource has been denied.
    /// </summary>
    [KurrentOperationError(typeof(Types.AccessDenied))]
    public readonly partial record struct AccessDenied;

    /// <summary>
    /// Indicates an error where the maximum allowable size of a transaction has been exceeded.
    /// </summary>
    [KurrentOperationError(typeof(Types.TransactionMaxSizeExceeded))]
    public readonly partial record struct TransactionMaxSizeExceeded;

    /// <summary>
    /// Represents a failure due to an unexpected revision conflict during an append operation.
    /// </summary>
    [KurrentOperationError(typeof(Types.StreamRevisionConflict))]
    public readonly partial record struct StreamRevisionConflict;

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
