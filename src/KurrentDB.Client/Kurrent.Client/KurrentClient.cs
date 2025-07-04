using Kurrent.Client.Features;
using Kurrent.Client.Legacy;
using KurrentDB.Client;

namespace Kurrent.Client;

[PublicAPI]
public class KurrentClient : IAsyncDisposable {
    public KurrentClient(KurrentClientOptions options) {
        options.EnsureConfigIsValid();

        Options = options;

        LegacyCallInvoker = new KurrentDBLegacyCallInvoker(new LegacyClusterClient(Options.ConvertToLegacySettings()));

        Streams                = new KurrentStreamsClient(LegacyCallInvoker, options);
        Registry               = new KurrentRegistryClient(LegacyCallInvoker);
        Features               = new KurrentFeaturesClient(LegacyCallInvoker);
        UserManagement         = new KurrentUserManagementClient(LegacyCallInvoker, options);
        PersistentSubscription = new KurrentPersistentSubscriptionsClient(LegacyCallInvoker, options);
    }

    KurrentClientOptions       Options           { get; }
	KurrentDBLegacyCallInvoker LegacyCallInvoker { get; }

	public KurrentStreamsClient                 Streams                { get; }
	public KurrentRegistryClient                Registry               { get; }
	public KurrentFeaturesClient                Features               { get; }
	public KurrentUserManagementClient          UserManagement         { get; }
	public KurrentPersistentSubscriptionsClient PersistentSubscription { get; }

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
}
