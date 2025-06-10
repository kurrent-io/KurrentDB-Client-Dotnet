// using Grpc.Core;
// using Grpc.Net.Client.Balancer;
// using Microsoft.Extensions.Logging;
//
// namespace KurrentDB.Client;
//
// /// <summary>
// /// A custom resolver factory that creates KurrentDB legacy resolvers.
// /// </summary>
// public sealed class KurrentDBLegacyResolverFactory : ResolverFactory {
// 	readonly ILegacyClusterClient _legacyClient;
// 	readonly ILoggerFactory       _loggerFactory;
//
// 	/// <summary>
// 	/// Initializes a new instance of the <see cref="KurrentDBLegacyResolverFactory"/> class.
// 	/// </summary>
// 	/// <param name="legacyClient">The legacy cluster client.</param>
// 	/// <param name="loggerFactory">The logger factory.</param>
// 	internal KurrentDBLegacyResolverFactory(ILegacyClusterClient legacyClient, ILoggerFactory loggerFactory) {
// 		_legacyClient  = legacyClient;
// 		_loggerFactory = loggerFactory;
// 	}
//
// 	/// <summary>
// 	/// Gets the scheme used for this resolver factory.
// 	/// </summary>
// 	public override string Name => "kurrentdb";
//
// 	/// <summary>
// 	/// Creates a new resolver.
// 	/// </summary>
// 	/// <param name="options">The resolver options.</param>
// 	/// <returns>A new resolver.</returns>
// 	public override Resolver Create(ResolverOptions options) =>
// 		new KurrentDBLegacyResolver(_legacyClient, _loggerFactory.CreateLogger<KurrentDBLegacyResolver>());
// }
//
// /// <summary>
// /// A resolver that uses the legacy cluster client to resolve addresses.
// /// </summary>
// sealed partial class KurrentDBLegacyResolver : Resolver {
// 	// Attribute key for storing ChannelInfo in BalancerAddress
// 	static readonly BalancerAttributesKey<ChannelInfo> ChannelInfoKey = new();
//
// 	readonly ILegacyClusterClient             _legacyClient;
// 	readonly ILogger<KurrentDBLegacyResolver> _logger;
// 	readonly SemaphoreSlim                    _refreshLock = new(1, 1);
//
// 	Action<ResolverResult>?  _listener;
// 	CancellationTokenSource? _refreshCts;
// 	bool                     _started;
//
// 	/// <summary>
// 	/// Initializes a new instance of the <see cref="KurrentDBLegacyResolver"/> class.
// 	/// </summary>
// 	/// <param name="legacyClient">The legacy cluster client.</param>
// 	/// <param name="logger">The logger.</param>
// 	public KurrentDBLegacyResolver(ILegacyClusterClient legacyClient, ILogger<KurrentDBLegacyResolver> logger) {
// 		_legacyClient = legacyClient;
// 		_logger       = logger;
// 	}
//
// 	/// <summary>
// 	/// Starts the resolver with the specified listener.
// 	/// </summary>
// 	/// <param name="listener">The callback to receive updates on the target.</param>
// 	public override void Start(Action<ResolverResult> listener) {
// 		if (_started)
// 			throw new InvalidOperationException("Resolver has already been started.");
//
// 		_listener   = listener;
// 		_started    = true;
// 		_refreshCts = new CancellationTokenSource();
//
// 		LogResolverStarting(_logger);
//
// 		// Start initial resolution
// 		_ = ResolveAsync(_refreshCts.Token);
// 	}
//
// 	/// <summary>
// 	/// Refreshes the resolver.
// 	/// </summary>
// 	public override void Refresh() {
// 		if (!_started)
// 			throw new InvalidOperationException("Resolver has not been started.");
//
// 		LogRefreshingResolver(_logger);
//
// 		// Cancel any ongoing delay
// 		var oldCts = Interlocked.Exchange(ref _refreshCts, new CancellationTokenSource());
// 		oldCts?.Cancel();
// 		oldCts?.Dispose();
//
// 		// Start a new resolution task
// 		_ = ResolveAsync(_refreshCts.Token);
// 	}
//
// 	async Task ResolveAsync(CancellationToken cancellationToken) {
// 		// Ensure only one resolve operation at a time
// 		await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
//
// 		try {
// 			if (cancellationToken.IsCancellationRequested)
// 				return;
//
// 			LogResolvingAddresses(_logger);
//
// 			try {
// 				// Get channel info from legacy client
// 				var channelInfo = await _legacyClient.Connect(cancellationToken).ConfigureAwait(false);
//
// 				// Create a balancer address for the channel target and store the channel info in attributes
// 				// Use a placeholder endpoint if we can't get a real one
// 				var target = "kurrentdb.internal";
// 				var port   = 0;
//
// 				// Try to extract target information from channel if possible
// 				if (channelInfo.Channel is not null && !string.IsNullOrEmpty(channelInfo.Channel.Target)) {
// 					target = channelInfo.Channel.Target;
//
// 					// Attempt to parse out port if present in target
// 					var parts = target.Split(':');
// 					if (parts.Length > 1 && int.TryParse(parts[^1], out var parsedPort))
// 						port = parsedPort;
// 				}
//
// 				var address = new BalancerAddress(target, port);
// 				address.Attributes.Set(ChannelInfoKey, channelInfo);
//
// 				var addresses = new List<BalancerAddress>(1) { address };
//
// 				// Create a result and notify the listener
// 				var result = ResolverResult.ForResult(addresses);
// 				_listener?.Invoke(result);
//
// 				LogResolvedAddresses(_logger, addresses.Count);
// 			}
// 			catch (Exception ex) when (ex is not OperationCanceledException) {
// 				LogErrorResolvingAddresses(_logger, ex);
//
// 				// Notify the listener of the error
// 				var result = ResolverResult.ForFailure(new Status(StatusCode.Unavailable, "Error resolving addresses", ex));
// 				_listener?.Invoke(result);
// 			}
// 		}
// 		finally {
// 			_refreshLock.Release();
// 		}
// 	}
//
// 	/// <summary>
// 	/// Disposes the resolver.
// 	/// </summary>
// 	/// <param name="disposing">Whether we're disposing managed resources.</param>
// 	protected override void Dispose(bool disposing) {
// 		if (disposing) {
// 			if (_refreshCts is not null) {
// 				_refreshCts.Cancel();
// 				_refreshCts.Dispose();
// 				_refreshCts = null;
// 			}
//
// 			_refreshLock.Dispose();
// 		}
//
// 		base.Dispose(disposing);
// 	}
//
// 	#region . Logging .
//
// 	[LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "KurrentDB resolver starting")]
// 	static partial void LogResolverStarting(ILogger logger);
//
// 	[LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Refreshing KurrentDB resolver")]
// 	static partial void LogRefreshingResolver(ILogger logger);
//
// 	[LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Resolving addresses via LegacyClusterClient")]
// 	static partial void LogResolvingAddresses(ILogger logger);
//
// 	[LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Resolved {AddressCount} addresses")]
// 	static partial void LogResolvedAddresses(ILogger logger, int addressCount);
//
// 	[LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Error resolving addresses")]
// 	static partial void LogErrorResolvingAddresses(ILogger logger, Exception exception);
//
// 	#endregion
// }
