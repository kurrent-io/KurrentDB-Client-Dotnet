// ReSharper disable InconsistentNaming

#pragma warning disable CS8509

using EventStore.Client.Operations;

namespace Kurrent.Client;

public partial class KurrentOperationsClient {
    internal KurrentOperationsClient(KurrentClient source) =>
        ServiceClient = new Operations.OperationsClient(source.LegacyCallInvoker);

    Operations.OperationsClient ServiceClient { get; }
}
