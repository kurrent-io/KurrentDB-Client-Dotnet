using Kurrent.Client.Legacy;
using KurrentDB.Client;
using static EventStore.Client.Projections.Projections;

namespace Kurrent.Client;

public sealed partial class KurrentProjectionManagementClient {
	internal KurrentProjectionManagementClient(KurrentClient source, KurrentClientOptions options) {
		LegacySettings = options.ConvertToLegacySettings();

		ServiceClient = new ProjectionsClient(source.LegacyCallInvoker);
	}

	internal KurrentDBClientSettings LegacySettings { get; }
	internal ProjectionsClient       ServiceClient  { get; }
}
