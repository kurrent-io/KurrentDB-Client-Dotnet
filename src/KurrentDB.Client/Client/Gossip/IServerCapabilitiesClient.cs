using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace KurrentDB.Client;

internal interface IServerCapabilitiesClient {
	public Task<ServerCapabilities> GetAsync(CallInvoker callInvoker, CancellationToken cancellationToken);
}