using Kurrent.Client.Features;
using KurrentDB.Client;
using KurrentDB.Client.Legacy;

namespace Kurrent.Client;

[PublicAPI]
public class KurrentClient : IAsyncDisposable {
    KurrentClient(KurrentClientOptions options) {
        options.EnsureConfigIsValid();

        Options = options;

        var settings = Options.ConvertToLegacySettings();

        LegacyCallInvoker = new KurrentDBLegacyCallInvoker(new LegacyClusterClient(settings));

        Streams  = new KurrentStreamsClient(LegacyCallInvoker, settings);
        Registry = new KurrentRegistryClient(LegacyCallInvoker);
        Features = new KurrentFeaturesClient(LegacyCallInvoker);
    }

    // public KurrentClient(KurrentDBClientSettings? settings) {
    //     settings ??= new();
    //
    //     settings.ConnectionName ??= $"conn-{Guid.NewGuid():D}";
    //
    //     LegacyCallInvoker = new KurrentDBLegacyCallInvoker(new LegacyClusterClient(settings));
    //
    //     Options = null!;
    //
    //     Streams  = new KurrentStreamsClient(LegacyCallInvoker, settings);
    //     Registry = new KurrentRegistryClient(LegacyCallInvoker);
    //     Features = new KurrentFeaturesClient(LegacyCallInvoker);
    // }

    KurrentClientOptions       Options           { get; }
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



    public static KurrentClient Create(KurrentClientOptions? options = null) =>
        new(options ?? new KurrentClientOptions());

    public static KurrentClient Create(string connectionString) =>
        Create(KurrentDBConnectionString.Parse(connectionString).ToClientOptions());

    public static KurrentClient Create(KurrentDBClientSettings settings) =>
        throw new NotSupportedException("Use KurrentClientOptions instead of KurrentDBClientSettings. The legacy settings are no longer supported.");

}
