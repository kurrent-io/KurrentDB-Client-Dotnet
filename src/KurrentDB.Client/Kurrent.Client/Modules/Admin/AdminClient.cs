using OperationsServiceClient = KurrentDB.Protocol.Operations.V1.Operations.OperationsClient;
using FeaturesServiceClient = KurrentDB.Protocol.ServerFeatures.V1.ServerFeatures.ServerFeaturesClient;

namespace Kurrent.Client.Admin;

public partial class AdminClient {
    internal AdminClient(KurrentClient source) {
        OperationsServiceClient = new(source.LegacyCallInvoker);
        FeaturesServiceClient   = new(source.LegacyCallInvoker);
    }

    OperationsServiceClient OperationsServiceClient { get; }
    FeaturesServiceClient   FeaturesServiceClient   { get; }
}
