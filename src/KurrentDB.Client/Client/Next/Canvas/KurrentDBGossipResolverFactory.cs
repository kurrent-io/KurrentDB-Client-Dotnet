// using Grpc.Net.Client.Balancer;
// using KurrentDB.Client.LoadBalancing;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace KurrentDB.Client.LoadBalancer;
//
// /// <summary>
// /// Factory for creating KurrentDbPollingResolver instances to handle service discovery.
// /// </summary>
// public sealed class KurrentDBGossipResolverFactory : ResolverFactory {
// 	readonly IGossipClient           _gossipClient;
// 	readonly KurrentDBClientSettings _settings;
//
// 	/// <summary>
// 	/// Initializes a new instance of the KurrentDbResolverFactory with client settings.
// 	/// </summary>
// 	/// <param name="settings">The KurrentDB client settings</param>
// 	/// <param name="gossipClient">The gossip client for cluster discovery</param>
// 	public KurrentDBGossipResolverFactory(KurrentDBClientSettings settings, IGossipClient gossipClient) {
// 		_settings     = settings;
// 		_gossipClient = gossipClient;
// 	}
//
// 	/// <summary>
// 	/// Gets the name of the resolver scheme.
// 	/// </summary>
// 	public override string Name => "kurrentdb+discover";
//
// 	/// <summary>
// 	/// Creates a new KurrentDbPollingResolver instance.
// 	/// </summary>
// 	/// <param name="options">The resolver options</param>
// 	/// <returns>A new resolver instance</returns>
// 	public override Resolver Create(ResolverOptions options) {
// 		var backoffPolicyFactory = options.ChannelOptions.ServiceProvider?.GetService<IBackoffPolicyFactory>()
// 		                        ?? new ExponentialBackoffPolicyFactory(
// 			                           options.ChannelOptions.InitialReconnectBackoff,
// 			                           options.ChannelOptions.MaxReconnectBackoff);
//
// 		return new KurrentDBGossipResolver(_settings.ConnectivitySettings, options.ChannelOptions, options.LoggerFactory, backoffPolicyFactory, _gossipClient);
// 	}
// }
