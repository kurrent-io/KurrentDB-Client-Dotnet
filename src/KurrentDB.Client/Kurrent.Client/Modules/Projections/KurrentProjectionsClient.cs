using Kurrent.Client.Legacy;
using KurrentDB.Client;
using static EventStore.Client.Projections.Projections;

namespace Kurrent.Client;

public sealed partial class KurrentProjectionsClient {
	internal KurrentProjectionsClient(KurrentClient source) {
		LegacySettings = source.Options.ConvertToLegacySettings();
		ServiceClient = new ProjectionsClient(source.LegacyCallInvoker);
	}

	internal KurrentDBClientSettings LegacySettings { get; }
	internal ProjectionsClient       ServiceClient  { get; }
}
