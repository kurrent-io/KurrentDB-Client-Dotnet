using ProjectionsServiceClient = KurrentDB.Protocol.Projections.V1.Projections.ProjectionsClient;

namespace Kurrent.Client.Projections;

public sealed partial class ProjectionsClient {
    internal ProjectionsClient(KurrentClient source) =>
        ServiceClient  = new(source.LegacyCallInvoker);

    internal ProjectionsServiceClient ServiceClient  { get; }
}
