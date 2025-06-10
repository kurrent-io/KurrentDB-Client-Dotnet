// using Grpc.Core;
// using Grpc.Net.Client.Balancer;
// using Grpc.Net.Client.Configuration;
// using Kurrent.Grpc.Balancer;
// using Microsoft.Extensions.Logging;
//
// namespace KurrentDB.Client;
//
// /// <summary>
// /// A factory for creating KurrentDB load balancers.
// /// </summary>
// public sealed class KurrentDBLegacyLoadBalancerFactory : LoadBalancerFactory {
//     readonly ILoggerFactory _loggerFactory;
//
//     /// <summary>
//     /// Initializes a new instance of the <see cref="KurrentDBLegacyLoadBalancerFactory"/> class.
//     /// </summary>
//     /// <param name="loggerFactory">The logger factory.</param>
//     public KurrentDBLegacyLoadBalancerFactory(ILoggerFactory loggerFactory) {
//         _loggerFactory = loggerFactory;
//     }
//
//     /// <summary>
//     /// Gets the name of the policy.
//     /// </summary>
//     public override string Name => "kurrentdb";
//
//     /// <summary>
//     /// Creates a new load balancer.
//     /// </summary>
//     /// <param name="options">The options for creating the load balancer.</param>
//     /// <returns>A new load balancer.</returns>
//     public override LoadBalancer Create(LoadBalancerOptions options) =>
//         new KurrentDBLegacyLoadBalancer(options, _loggerFactory.CreateLogger<KurrentDBLegacyLoadBalancer>());
// }
//
// /// <summary>
// /// A load balancer that uses KurrentDB resolver addresses.
// /// </summary>
// sealed partial class KurrentDBLegacyLoadBalancer : LoadBalancer {
//     // Attribute key for retrieving ChannelInfo from BalancerAddress
//     const string ChannelInfoKey = "ChannelInfo";
//
//     readonly LoadBalancerOptions _options;
//     readonly ILogger<KurrentDBLegacyLoadBalancer> _logger;
//     readonly object _lock = new();
//
//     Subchannel? _subchannel;
//     ConnectivityState _state = ConnectivityState.Idle;
//
//     /// <summary>
//     /// Initializes a new instance of the <see cref="KurrentDBLegacyLoadBalancer"/> class.
//     /// </summary>
//     /// <param name="options">The options for the load balancer.</param>
//     /// <param name="logger">The logger.</param>
//     public KurrentDBLegacyLoadBalancer(LoadBalancerOptions options, ILogger<KurrentDBLegacyLoadBalancer> logger) {
//         _options = options;
//         _logger = logger;
//     }
//
//     /// <summary>
//     /// Updates the addresses for the load balancer.
//     /// </summary>
//     /// <param name="state">The channel state.</param>
//     public override void UpdateChannelState(ChannelState state) {
//         if (state.Addresses?.Count == 0) {
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
//         var channelInfo = address.Attributes.GetValue<ChannelInfo>(ChannelInfoKey);
//
//         // Try to get the channel info from the address attributes
//         if (!address.Attributes.GetValueOrDefault(ChannelInfoKey, null!)) {
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
//         // Create a subchannel wrapper for the channel info
//         var subchannel = new KurrentDBSubchannel(channelInfo);
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
//     /// Shuts down the load balancer.
//     /// </summary>
//     public override void Dispose() {
//         // No need to do anything here - our resolver handles cleanup
//         LogLoadBalancerDisposing(_logger);
//     }
//
//     /// <summary>
//     /// A picker that picks subchannels for the KurrentDB load balancer.
//     /// </summary>
//     sealed partial class KurrentDBPicker : SubchannelPicker {
//         readonly KurrentDBLegacyLoadBalancer _legacyLoadBalancer;
//         readonly ILogger _logger;
//
//         /// <summary>
//         /// Initializes a new instance of the <see cref="KurrentDBPicker"/> class.
//         /// </summary>
//         /// <param name="legacyLoadBalancer">The load balancer.</param>
//         public KurrentDBPicker(KurrentDBLegacyLoadBalancer legacyLoadBalancer) {
//             _legacyLoadBalancer = legacyLoadBalancer;
//             _logger = legacyLoadBalancer._logger;
//         }
//
//         /// <summary>
//         /// Picks a subchannel for the call.
//         /// </summary>
//         /// <param name="pickContext">The pick context.</param>
//         /// <returns>The pick result.</returns>
//         public override PickResult Pick(PickContext pickContext) {
//             var subchannel = _legacyLoadBalancer._subchannel;
//             if (subchannel is null) {
//                 LogNoSubchannelAvailable(_logger);
//                 return PickResult.ForFailure(Status.DefaultCancelled);
//             }
//
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
// //
// // /// <summary>
// // /// A subchannel wrapper that uses a ChannelInfo.
// // /// </summary>
// // internal sealed class KurrentDBSubchannel : Subchannel {
// //     readonly ChannelInfo _channelInfo;
// //
// //     /// <summary>
// //     /// Initializes a new instance of the <see cref="KurrentDBSubchannel"/> class.
// //     /// </summary>
// //     /// <param name="channelInfo">The channel info to use.</param>
// //     public KurrentDBSubchannel(ChannelInfo channelInfo) {
// //         _channelInfo = channelInfo;
// //     }
// //
// //     /// <summary>
// //     /// Creates a call invoker for the subchannel.
// //     /// </summary>
// //     /// <returns>A call invoker.</returns>
// //     public override CallInvoker CreateCallInvoker() => _channelInfo.CallInvoker;
// // }
