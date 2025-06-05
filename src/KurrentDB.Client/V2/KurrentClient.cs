using Kurrent.Client.Features;
using KurrentDB.Client;
using KurrentDB.Client.Legacy;

namespace Kurrent.Client;

[PublicAPI]
public class KurrentClient : IAsyncDisposable {
	public KurrentClient(KurrentDBClientSettings? settings) {
		Settings = settings ?? new();

		Settings.ConnectionName ??= $"conn-{Guid.NewGuid():D}";

		LegacyCallInvoker = new KurrentDBLegacyCallInvoker(new LegacyClusterClient(Settings));

		Streams  = new KurrentStreamsClient(LegacyCallInvoker, Settings);
		Registry = new KurrentRegistryClient(LegacyCallInvoker);
		Features = new KurrentFeaturesClient(LegacyCallInvoker);
	}

	KurrentDBClientSettings    Settings          { get; }
	KurrentDBLegacyCallInvoker LegacyCallInvoker { get; }

	public KurrentStreamsClient  Streams  { get; }
	public KurrentRegistryClient Registry { get; }
	public KurrentFeaturesClient Features { get; }

	internal async Task<ServerFeatures> ForceRefresh(CancellationToken cancellationToken = default) {
		await LegacyCallInvoker.ForceRefresh(cancellationToken).ConfigureAwait(false);

		return new ServerFeatures {
			Version = LegacyCallInvoker.ServerCapabilities.Version
		};
	}

	public ValueTask DisposeAsync() =>
		LegacyCallInvoker.DisposeAsync();
}
