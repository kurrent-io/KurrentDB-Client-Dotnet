// ReSharper disable InconsistentNaming

using KurrentDB.Protocol.Operations.V1;
using KurrentDB.Protocol.Users.V1;

namespace Kurrent.Client.Admin;

public partial class AdminClient {
    internal AdminClient(KurrentClient source) {
        OperationsServiceClient = new(source.LegacyCallInvoker);
        FeaturesServiceClient   = new(source.LegacyCallInvoker);
    }

    OperationsService.OperationsServiceClient         OperationsServiceClient { get; }
    ServerFeaturesService.ServerFeaturesServiceClient FeaturesServiceClient   { get; }
}
