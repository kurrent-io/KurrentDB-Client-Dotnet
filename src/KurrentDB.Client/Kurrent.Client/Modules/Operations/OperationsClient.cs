// ReSharper disable InconsistentNaming

using static KurrentDB.Protocol.Operations.V1.OperationsService;

namespace Kurrent.Client.Operations;

public partial class OperationsClient {
    internal OperationsClient(KurrentClient source) =>
        ServiceClient = new(source.LegacyCallInvoker);

    OperationsServiceClient ServiceClient { get; }
}
