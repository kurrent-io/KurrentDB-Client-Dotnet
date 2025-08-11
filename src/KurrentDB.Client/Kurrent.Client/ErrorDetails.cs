using Types = KurrentDB.Protocol.CoreErrorDetails.Types;

namespace Kurrent.Client;

/// <summary>
/// Provides a set of error detail types for representing specific operation failures when interacting with KurrentDB.
/// </summary>
[PublicAPI]
public static partial class ErrorDetails {
    [KurrentOperationError(typeof(Types.AccessDenied))]
    public readonly partial record struct AccessDenied;

    [KurrentOperationError(typeof(Types.NotFound))]
    public readonly partial record struct NotFound;

    [KurrentOperationError(typeof(Types.AlreadyExists))]
    public readonly partial record struct AlreadyExists;

    [KurrentOperationError(typeof(Types.FailedPrecondition))]
    public readonly partial record struct FailedPrecondition;

    [KurrentOperationError(typeof(Types.Unknown))]
    public readonly partial record struct Unknown;
}
