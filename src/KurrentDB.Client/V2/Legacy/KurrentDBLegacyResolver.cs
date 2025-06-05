using Grpc.Core;
using Grpc.Net.Client.Balancer;
using Kurrent.Grpc.Balancer;
using KurrentDB.Client;
using Microsoft.Extensions.Logging;

namespace Kurrent.Client;

/// <summary>
/// A resolver that uses the legacy cluster client to resolve addresses.
/// </summary>
sealed partial class KurrentDBLegacyResolver : PollingResolver {
	readonly LegacyClusterClient              LegacyClient;
	readonly ILogger<KurrentDBLegacyResolver> Logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="KurrentDBLegacyResolver"/> class.
	/// </summary>
	/// <param name="legacyClient">The legacy cluster client.</param>
	/// <param name="loggerFactory">The logger factory. If provided, it will be used to create a logger for this resolver.</param>
	public KurrentDBLegacyResolver(LegacyClusterClient legacyClient, ILoggerFactory loggerFactory) : base(loggerFactory) {
		LegacyClient = legacyClient;
		Logger       = loggerFactory.CreateLogger<KurrentDBLegacyResolver>();
	}

	protected override async Task ResolveAsync(CancellationToken cancellationToken) {
		LogResolvingAddress(Logger);

		try {
			var channelInfo = await LegacyClient
				.Connect(cancellationToken)
				.ConfigureAwait(false);

			var address = CreateBalancerAddress(channelInfo);

			LogResolvedAddress(Logger, address.EndPoint.ToString());
			Listener(ResolverResult.ForResult([address]));
		}
		catch (OperationCanceledException) {
			Listener(ResolverResult.ForFailure(Status.DefaultCancelled));
		}
		catch (Exception ex) when (ex is not OperationCanceledException) {
			LogErrorResolvingAddress(Logger, ex);
			Listener(ResolverResult.ForFailure(new Status(StatusCode.Unavailable, "Error resolving addresses", ex)));
		}

		return;

		static BalancerAddress CreateBalancerAddress(ChannelInfo channelInfo) {
			var port  = 0;
			var parts = channelInfo.Channel.Target.Split(':');
			if (parts.Length > 1 && int.TryParse(parts[^1], out var parsedPort))
				port = parsedPort;

			var address = new BalancerAddress(channelInfo.Channel.Target, port);
			address.Attributes.WithValue(nameof(ChannelInfo), channelInfo);

			return address;
		}
	}

	#region . Logging .

	[LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Resolving gossip address")]
	static partial void LogResolvingAddress(ILogger logger);

	[LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Resolved gossip address: {Address}")]
	static partial void LogResolvedAddress(ILogger logger, string address);

	[LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Failed to resolve any address")]
	static partial void LogErrorResolvingAddress(ILogger logger, Exception exception);

	#endregion
}

/// <summary>
/// A factory that creates a resolver that uses the legacy cluster client to resolve addresses.
/// </summary>
/// <param name="legacyClient"></param>
sealed class KurrentDBLegacyResolverFactory(LegacyClusterClient legacyClient) : ResolverFactory {
	LegacyClusterClient LegacyClient { get; } = legacyClient;

	public override string Name { get; } = legacyClient.ResolverScheme;

	public override Resolver Create(ResolverOptions options) =>
		new KurrentDBLegacyResolver(LegacyClient, options.LoggerFactory);
}
