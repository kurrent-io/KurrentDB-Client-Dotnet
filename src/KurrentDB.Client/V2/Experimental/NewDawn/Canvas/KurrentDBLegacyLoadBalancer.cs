// using Grpc.Core;
// using Grpc.Net.Client.Balancer;
// using Kurrent.Grpc.Balancer;
// using Microsoft.Extensions.Logging;
//
// namespace KurrentDB.Client;
//
// sealed partial class KurrentDBLegacyLoadBalancer(IChannelControlHelper controller, ILoggerFactory loggerFactory) : SubchannelsLoadBalancer(controller, loggerFactory) {
// 	const string ChannelInfoKey = "ChannelInfo";
//
// 	ILogger<KurrentDBLegacyLoadBalancer> Logger { get; } = loggerFactory.CreateLogger<KurrentDBLegacyLoadBalancer>();
//
// 	protected override SubchannelPicker CreatePicker(IReadOnlyList<Subchannel> readySubchannels) =>
// 		new RoundRobinPicker(readySubchannels);
//
// 	/// <summary>
// 	/// Request the <see cref="KurrentDBLegacyLoadBalancer"/> to establish connections now (if applicable) so that
// 	/// future calls can use a ready connection without waiting for a connection.
// 	/// </summary>
// 	public override void RequestConnection() {
// 		LogConnectionRequested(Logger);
// 		base.RequestConnection();
// 	}
//
// 	/// <summary>
// 	/// Triggers the legacy gossip resolver to refresh its addresses.
// 	/// </summary>
// 	public void Refresh() {
// 		LogConnectionRequested(Logger);
// 		Controller.RefreshResolver();
// 	}
//
// 	class RoundRobinPicker(IReadOnlyList<Subchannel> subchannels) : SubchannelPicker {
// 		public override PickResult Pick(PickContext context) {
// 			return subchannels.Count > 0
// 				? PickResult.ForSubchannel(subchannels[Random.Shared.Next(subchannels.Count)])
// 				: PickResult.ForFailure(new Status(StatusCode.Unavailable, "No subchannels available"));
// 		}
// 	}
//
// 	#region . Logging .
//
// 	[LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Resolver refresh requested")]
// 	static partial void LogResolverRefreshRequested(ILogger logger);
//
// 	[LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "No addresses available for load balancing")]
// 	static partial void LogNoAddressesAvailable(ILogger logger);
//
// 	[LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Address {Host}:{Port} does not contain ChannelInfo")]
// 	static partial void LogInvalidAddress(ILogger logger, string host, int port);
//
// 	[LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Using address {Host}:{Port}")]
// 	static partial void LogUsingAddress(ILogger logger, string host, int port);
//
// 	[LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "State changed from {OldState} to {NewState} with status {StatusCode}")]
// 	static partial void LogStateChanged(ILogger logger, string oldState, string newState, string statusCode);
//
// 	[LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Connection requested")]
// 	static partial void LogConnectionRequested(ILogger logger);
//
// 	[LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "Load balancer disposing")]
// 	static partial void LogLoadBalancerDisposing(ILogger logger);
//
// 	#endregion
// }
//
// sealed class KurrentDBLegacyLoadBalancerFactory : LoadBalancerFactory {
// 	public KurrentDBLegacyLoadBalancerFactory(bool enableDiscovery) =>
// 		Name = enableDiscovery ? "kurrentdb+discover" : "kurrentdb";
//
// 	public KurrentDBLegacyLoadBalancerFactory(KurrentDBConnectionString connectionString)
// 		: this(connectionString.IsDiscoveryScheme) { }
//
// 	public KurrentDBLegacyLoadBalancerFactory(string connectionString)
// 		: this(KurrentDBConnectionString.Parse(connectionString)) { }
//
// 	public override string Name { get; }
//
// 	public override LoadBalancer Create(LoadBalancerOptions options) =>
// 		new KurrentDBLegacyLoadBalancer(options.Controller, options.LoggerFactory);
// }
//
//
//
//
//
//
//
// /// <summary>
// /// A load balancer that uses KurrentDB resolver addresses.
// /// </summary>
// internal sealed partial class KurrentDBLoadBalancer : LoadBalancer {
//     // Attribute key for retrieving ChannelInfo from BalancerAddress
//     static readonly BalancerAttributesKey<ChannelInfo> ChannelInfoKey = new();
//
//     readonly LoadBalancerOptions _options;
//     readonly ILogger<KurrentDBLoadBalancer> _logger;
//     readonly object _lock = new();
//
//     Subchannel? _subchannel;
//     ConnectivityState _state = ConnectivityState.Idle;
//
//     /// <summary>
//     /// Initializes a new instance of the <see cref="KurrentDBLoadBalancer"/> class.
//     /// </summary>
//     /// <param name="options">The options for the load balancer.</param>
//     /// <param name="logger">The logger.</param>
//     public KurrentDBLoadBalancer(LoadBalancerOptions options, ILogger<KurrentDBLoadBalancer> logger) {
//         _options = options;
//         _logger = logger;
//     }
//
//     /// <summary>
//     /// Updates the addresses for the load balancer.
//     /// </summary>
//     /// <param name="state">The channel state.</param>
//     public override void UpdateChannelState(ChannelState state) {
//
// 	    RoundRobinBalancerFactory
//         if (state.Addresses.Count == 0) {
//             LogNoAddressesAvailable(_logger);
//
//             lock (_lock) {
//                 _subchannel = null;
//                 UpdateState(ConnectivityState.Idle, new Status(StatusCode.Unavailable, "No addresses"));
//             }
//             return;
//         }
//
//         // Get the first address
//         var address = state.Addresses[0];
//
//         // Try to get the channel info from the address attributes
//         if (!address.Attributes.TryGet(ChannelInfoKey, out var channelInfo)) {
//             LogInvalidAddress(_logger, address.Host, address.Port);
//
//             lock (_lock) {
//                 _subchannel = null;
//                 UpdateState(ConnectivityState.TransientFailure,
//                     new Status(StatusCode.Internal, "Address does not contain ChannelInfo"));
//             }
//             return;
//         }
//
//         LogUsingAddress(_logger, address.Host, address.Port);
//
//         // Create a subchannel using built-in methods with our CallInvoker from attributes
//         var subchannel = state.CreateSubchannel(address, new SubchannelOptions {
//             ChannelCredentials = ChannelCredentials.Insecure // Using default credentials since our CallInvoker will handle auth
//         });
//
//         // Create a custom attribute on subchannel to store our CallInvoker for later retrieval
//         subchannel.Attributes.Set(ChannelInfoKey, channelInfo);
//
//         lock (_lock) {
//             _subchannel = subchannel;
//             UpdateState(ConnectivityState.Ready, Status.DefaultSuccess);
//         }
//     }
//
//     /// <summary>
//     /// Called when the connectivity state changes.
//     /// </summary>
//     /// <param name="newState">The new state.</param>
//     /// <param name="status">The status.</param>
//     private void UpdateState(ConnectivityState newState, Status status) {
//         if (_state != newState) {
//             LogStateChanged(_logger, _state.ToString(), newState.ToString(), status.StatusCode.ToString());
//             _state = newState;
//             _options.Controller.UpdateState(new BalancerState(newState, new KurrentDBPicker(this)));
//         }
//     }
//
//     /// <summary>
//     /// Requests a connection.
//     /// </summary>
//     public override void RequestConnection() {
//         // No need to do anything here - our resolver handles connections
//         LogConnectionRequested(_logger);
//     }
//
//     /// <summary>
//     /// A picker that picks subchannels for the KurrentDB load balancer.
//     /// </summary>
//     private sealed partial class KurrentDBPicker : SubchannelPicker {
//         readonly KurrentDBLoadBalancer _loadBalancer;
//         readonly ILogger _logger;
//
//         /// <summary>
//         /// Initializes a new instance of the <see cref="KurrentDBPicker"/> class.
//         /// </summary>
//         /// <param name="loadBalancer">The load balancer.</param>
//         public KurrentDBPicker(KurrentDBLoadBalancer loadBalancer) {
//             _loadBalancer = loadBalancer;
//             _logger = loadBalancer._logger;
//         }
//
//         /// <summary>
//         /// Picks a subchannel for the call.
//         /// </summary>
//         /// <param name="pickContext">The pick context.</param>
//         /// <returns>The pick result.</returns>
//         public override PickResult Pick(PickContext pickContext) {
//             var subchannel = _loadBalancer._subchannel;
//             if (subchannel is null) {
//                 LogNoSubchannelAvailable(_logger);
//                 return PickResult.ForFailure(Status.DefaultCancelled);
//             }
//
//             // Here's where we intercept the subchannel and use our custom CallInvoker
//             if (subchannel.Attributes.TryGet(ChannelInfoKey, out var channelInfo)) {
//                 LogPickedSubchannel(_logger);
//
//                 // Create a PickResult that uses our custom CallInvoker from the attributes
//                 return new PickResult(subchannel, new KurrentDBPickContext(channelInfo.CallInvoker));
//             }
//
//             // Fallback to standard behavior if attributes not found
//             LogPickedSubchannel(_logger);
//             return PickResult.ForSubchannel(subchannel);
//         }
//
//         #region . Logging .
//         [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "No subchannel available for picking")]
//         static partial void LogNoSubchannelAvailable(ILogger logger);
//
//         [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Picked subchannel for request")]
//         static partial void LogPickedSubchannel(ILogger logger);
//         #endregion
//     }
//
//     /// <summary>
//     /// Custom pick context that uses our cached CallInvoker.
//     /// </summary>
//     private class KurrentDBPickContext : PickContext {
//         readonly CallInvoker _callInvoker;
//
//         public KurrentDBPickContext(CallInvoker callInvoker) {
//             _callInvoker = callInvoker;
//         }
//
//         public override CallInvoker CreateCallInvoker() => _callInvoker;
//     }
//
//     #region . Logging .
//     [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "No addresses available for load balancing")]
//     static partial void LogNoAddressesAvailable(ILogger logger);
//
//     [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Address {Host}:{Port} does not contain ChannelInfo")]
//     static partial void LogInvalidAddress(ILogger logger, string host, int port);
//
//     [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Using address {Host}:{Port}")]
//     static partial void LogUsingAddress(ILogger logger, string host, int port);
//
//     [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "State changed from {OldState} to {NewState} with status {StatusCode}")]
//     static partial void LogStateChanged(ILogger logger, string oldState, string newState, string statusCode);
//
//     [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Connection requested")]
//     static partial void LogConnectionRequested(ILogger logger);
//
//     [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Load balancer disposing")]
//     static partial void LogLoadBalancerDisposing(ILogger logger);
//     #endregion
// }
