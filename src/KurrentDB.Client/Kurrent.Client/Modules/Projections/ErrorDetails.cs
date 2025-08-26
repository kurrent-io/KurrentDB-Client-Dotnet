// ReSharper disable CheckNamespace

using static KurrentDB.Protocol.Projections.V2.ProjectionsErrorDetails;

namespace Kurrent.Client;

public static partial class ErrorDetails {
    [KurrentOperationError(typeof(Types.ProjectionsSubsystemRestartFailed))]
    public partial record ProjectionsSubsystemRestartFailed;
}
