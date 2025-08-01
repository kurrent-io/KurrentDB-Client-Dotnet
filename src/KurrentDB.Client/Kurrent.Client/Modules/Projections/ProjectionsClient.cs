using Kurrent.Client.Legacy;
using KurrentDB.Client;
using static KurrentDB.Protocol.Projections.V1.ProjectionsService;

namespace Kurrent.Client.Projections;

public sealed partial class ProjectionsClient {
    internal ProjectionsClient(KurrentClient source) {
        LegacySettings = source.Options.ConvertToLegacySettings();
        ServiceClient  = new ProjectionsServiceClient(source.LegacyCallInvoker);
    }

    internal KurrentDBClientSettings  LegacySettings { get; }
    internal ProjectionsServiceClient ServiceClient  { get; }
}
