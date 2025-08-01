// ReSharper disable InconsistentNaming

using static KurrentDB.Protocol.Operations.V1.OperationsService;

#pragma warning disable CS8509

namespace Kurrent.Client.Operations;

public partial class OperationsClient {
    internal OperationsClient(KurrentClient source) =>
        ServiceClient = new OperationsServiceClient(source.LegacyCallInvoker);

    OperationsServiceClient ServiceClient { get; }
}
