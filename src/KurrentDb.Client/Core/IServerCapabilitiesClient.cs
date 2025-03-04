using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace KurrentDb.Client {
	internal interface IServerCapabilitiesClient {
		public Task<ServerCapabilities> GetAsync(CallInvoker callInvoker, CancellationToken cancellationToken);
	}
}
