// using Grpc.Core;
// using Kurrent.Client.Features;
// using KurrentDB.Client;
//
// namespace Kurrent.Client;
//
// #pragma warning disable CS8509
// static class LegacyConnectionExtensions {
// 	internal static ServiceProxy<T> GetProxyConnection<T>(this KurrentDBClient legacyClient) where T : ClientBase<T> =>
// 		ServiceProxy<T>.Create(async ct => {
// 			var (_, capabilities, invoker) = await legacyClient.GetChannelInfo(ct).ConfigureAwait(false);
// 			return (invoker, new ServerInfo { Version = capabilities.Version });
// 		});
//
// 	internal static async ValueTask<(CallInvoker CallInvoker, ServerInfo ServerInfo)> GetConnection<T>(this KurrentDBClient legacyClient, CancellationToken ct) where T : ClientBase<T> {
// 		var (_, capabilities, invoker) = await legacyClient.GetChannelInfo(ct).ConfigureAwait(false);
// 		return (invoker, new ServerInfo { Version = capabilities.Version });
// 	}
//
// 	internal static Func<CancellationToken, ValueTask<(CallInvoker CallInvoker, ServerInfo ServerInfo)>> Connect(this KurrentDBClient legacyClient) =>
// 		async ct => {
// 			var (_, capabilities, invoker) = await legacyClient.GetChannelInfo(ct).ConfigureAwait(false);
// 			return (invoker, new ServerInfo { Version = capabilities.Version });
// 		};
// }
