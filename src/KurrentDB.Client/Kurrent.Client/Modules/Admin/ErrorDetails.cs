// ReSharper disable CheckNamespace

using Types = KurrentDB.Protocol.Admin.V2.AdminErrorDetails.Types;

namespace Kurrent.Client;

public static partial class ErrorDetails {
    /// <summary>
    /// Represents an error indicating that the specified scavenge operation could not be found.
    /// </summary>
    [KurrentOperationError(typeof(Types.ScavengeNotFound))]
    public readonly partial record struct ScavengeNotFound;
}
