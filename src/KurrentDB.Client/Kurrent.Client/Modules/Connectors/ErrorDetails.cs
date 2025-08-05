// ReSharper disable CheckNamespace

using static KurrentDB.Protocol.Connectors.V2.ConnectorsErrorDetails;

namespace Kurrent.Client;

public static partial class ErrorDetails {
    /// <summary>
    /// Represents an error indicating that the specified connector could not be found.
    /// </summary>
    [KurrentOperationError(typeof(Types.ConnectorNotFound))]
    public readonly partial record struct ConnectorNotFound;
}
