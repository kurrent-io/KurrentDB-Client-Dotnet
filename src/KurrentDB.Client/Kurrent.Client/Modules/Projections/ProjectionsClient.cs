using Kurrent.Client.Legacy;
using KurrentDB.Client;

namespace Kurrent.Client.Projections;

public sealed partial class ProjectionsClient {
    internal ProjectionsClient(KurrentClient source) {
        LegacySettings = source.Options.ConvertToLegacySettings();
        ServiceClient  = new EventStore.Client.Projections.Projections.ProjectionsClient(source.LegacyCallInvoker);
    }

    internal KurrentDBClientSettings                                     LegacySettings { get; }
    internal EventStore.Client.Projections.Projections.ProjectionsClient ServiceClient  { get; }
}
