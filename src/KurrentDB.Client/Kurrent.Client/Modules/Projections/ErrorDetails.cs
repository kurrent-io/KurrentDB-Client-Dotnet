// ReSharper disable CheckNamespace

using static KurrentDB.Protocol.Projections.V2.ProjectionsErrorDetails;

namespace Kurrent.Client;

public static partial class ErrorDetails {
    [KurrentOperationError(typeof(Types.ProjectionsSubsystemRestartFailed))]
    public readonly partial record struct ProjectionsSubsystemRestartFailed;
}
