using Grpc.Core;

namespace KurrentDB.Client;

/// <summary>
/// The base class used by clients used to communicate with the KurrentDB.
/// </summary>
abstract class KurrentDBClientBase : IAsyncDisposable {
	/// Constructs a new <see cref="KurrentDBClientBase"/>.
	protected KurrentDBClientBase(KurrentDBClientSettings? settings, Dictionary<string, Func<RpcException, Exception>>? exceptionMap = null) {
		Settings = settings ?? new();

		Settings.ConnectionName ??= $"conn-{Guid.NewGuid():D}";

		LegacyClusterClient = new(Settings, exceptionMap ?? new());
	}

	internal KurrentDBClientSettings Settings { get; }

	LegacyClusterClient LegacyClusterClient { get;  }

	internal ValueTask<ChannelInfo> GetChannelInfo(CancellationToken cancellationToken = default) =>
		LegacyClusterClient.Connect(cancellationToken);

	protected internal ValueTask<ChannelInfo> RediscoverAsync() =>
		 LegacyClusterClient.ForceReconnect();

	protected virtual ValueTask DisposeAsyncCore() => new();

	public async ValueTask DisposeAsync() {
		await DisposeAsyncCore();

		await LegacyClusterClient
			.DisposeAsync()
			.ConfigureAwait(false);

		GC.SuppressFinalize(this);
	}
}
