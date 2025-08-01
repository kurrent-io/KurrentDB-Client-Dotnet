// ReSharper disable InconsistentNaming

#pragma warning disable CS8509

namespace Kurrent.Client.Operations;

public partial class OperationsClient {
    internal OperationsClient(KurrentClient source) =>
        ServiceClient = new EventStore.Client.Operations.Operations.OperationsClient(source.LegacyCallInvoker);

    EventStore.Client.Operations.Operations.OperationsClient ServiceClient { get; }
}
