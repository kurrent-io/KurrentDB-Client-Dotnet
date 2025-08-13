// ReSharper disable CheckNamespace

using Grpc.Core;
using static KurrentDB.Protocol.Connectors.V2.ConnectorsErrorDetails;

namespace Kurrent.Client;

public static partial class ErrorDetails {
    /// <summary>
    /// Represents an error indicating that the specified connector could not be found.
    /// </summary>
    [KurrentOperationError(typeof(Types.ConnectorNotFound))]
    public readonly partial record struct ConnectorNotFound;
}

public static partial class ErrorDetails {
    public static ConnectorNotFound AsConnectorNotFoundError(this RpcException rex, string connectorId) =>
        new(x => x.With("connectorId", connectorId));
}
