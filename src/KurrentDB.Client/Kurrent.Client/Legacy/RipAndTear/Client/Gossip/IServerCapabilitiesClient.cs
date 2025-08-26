using Grpc.Core;

namespace KurrentDB.Client;

interface IServerCapabilitiesClient {
    public Task<ServerCapabilities> GetAsync(CallInvoker callInvoker, CancellationToken cancellationToken);
}
