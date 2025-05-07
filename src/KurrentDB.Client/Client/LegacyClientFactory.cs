using Grpc.Core;

namespace KurrentDB.Client;

/// <summary>
/// Delegate that defines a method for retrieving channel information.
/// </summary>
/// <param name="ct">A token to monitor for cancellation requests.</param>
/// <remarks>
/// IMPORTANT! Scheduled to disappear in the very near future.
/// </remarks>
public delegate ValueTask<ChannelInfo> GetLegacyChannelInfo(CancellationToken ct);

/// <summary>
/// Helper class to simplify the creation of gRPC clients using the current (legacy) custom technique to load balancing and discovery.
/// This technique forces the creation of a new instance of an internal grpc client per request... -_-'
/// </summary>
/// <param name="getChannelInfo">A delegate that retrieves channel information.</param>
/// <remarks>
/// IMPORTANT! Scheduled to disappear in the very near future.
/// </remarks>
public class LegacyClientFactory(GetLegacyChannelInfo getChannelInfo) {
	GetLegacyChannelInfo GetChannelInfo { get; } = getChannelInfo;

	public async ValueTask<TClient> CreateAsync<TClient>(CancellationToken ct) where TClient : ClientBase<TClient> {
		var (channel, serverCapabilities, callInvoker) = await GetChannelInfo(ct).ConfigureAwait(false);

		try {
			return (TClient)Activator.CreateInstance(typeof(TClient), callInvoker)!;
		}
		catch (Exception ex) {
			throw new InvalidOperationException($"Could not create instance of {typeof(TClient).Name} using the provided CallInvoker.", ex);
		}
	}

	public TClient Create<TClient>() where TClient : ClientBase<TClient> =>
		CreateAsync<TClient>(CancellationToken.None).AsTask().GetAwaiter().GetResult();


	// public async ValueTask<TResponse> Execute<TClient, TResponse>(Func<CancellationToken, ValueTask<TResponse>> handle, CancellationToken ct) where TClient : ClientBase<TClient> {
	// 	var client = await CreateAsync<TClient>(ct).ConfigureAwait(false);
	//
	// 	try {
	// 		return await client.RequestAsync(ct).ConfigureAwait(false);
	// 	}
	// 	catch (RpcException ex) {
	// 		throw new InvalidOperationException($"Error occurred while making a request using {typeof(TClient).Name}.", ex);
	// 	}
	// }
}
