// ReSharper disable InconsistentNaming

#pragma warning disable CS8509

using Grpc.Core;
using KurrentDB.Client;
using EventStore.Client.Operations;
using Kurrent.Client.Legacy;

namespace Kurrent.Client;

public partial class KurrentOperationsClient {
	internal KurrentOperationsClient(KurrentClient source, KurrentClientOptions options) {
		LegacySettings    = options.ConvertToLegacySettings();
		LegacyCallInvoker = new KurrentDBLegacyCallInvoker(new LegacyClusterClient(LegacySettings, ExceptionMap));
		ServiceClient     = new Operations.OperationsClient(LegacyCallInvoker);
	}

	Operations.OperationsClient ServiceClient { get; }

	internal KurrentDBClientSettings    LegacySettings    { get; }
	internal KurrentDBLegacyCallInvoker LegacyCallInvoker { get; }

	static readonly Dictionary<string, Func<RpcException, Exception>> ExceptionMap =
		new() {
			[Constants.Exceptions.ScavengeNotFound] = ex => new ScavengeNotFoundException(
				ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.ScavengeId)?.Value
			)
		};
}
