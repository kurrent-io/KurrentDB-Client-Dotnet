using EventStore.Client;
using EventStore.Client.Operations;

namespace KurrentDB.Client;

public partial class KurrentDBOperationsClient {
	static readonly Empty EmptyResult = new Empty();

	/// <summary>
	/// Shuts down the KurrentDB node.
	/// </summary>
	/// <param name="deadline"></param>
	/// <param name="userCredentials"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task ShutdownAsync(
		TimeSpan? deadline = null,
		UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default) {
		var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
		using var call = new Operations.OperationsClient(
			channelInfo.CallInvoker).ShutdownAsync(EmptyResult,
			KurrentDBCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
		await call.ResponseAsync.ConfigureAwait(false);
	}

	/// <summary>
	/// Initiates an index merge operation.
	/// </summary>
	/// <param name="deadline"></param>
	/// <param name="userCredentials"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task MergeIndexesAsync(
		TimeSpan? deadline = null,
		UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default) {
		var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
		using var call = new Operations.OperationsClient(
			channelInfo.CallInvoker).MergeIndexesAsync(EmptyResult,
			KurrentDBCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
		await call.ResponseAsync.ConfigureAwait(false);
	}

	/// <summary>
	/// Resigns a node.
	/// </summary>
	/// <param name="deadline"></param>
	/// <param name="userCredentials"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task ResignNodeAsync(
		TimeSpan? deadline = null,
		UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default) {
		var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
		using var call = new Operations.OperationsClient(
			channelInfo.CallInvoker).ResignNodeAsync(EmptyResult,
			KurrentDBCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
		await call.ResponseAsync.ConfigureAwait(false);
	}

	/// <summary>
	/// Sets the node priority.
	/// </summary>
	/// <param name="nodePriority"></param>
	/// <param name="deadline"></param>
	/// <param name="userCredentials"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task SetNodePriorityAsync(int nodePriority,
	                                       TimeSpan? deadline = null,
	                                       UserCredentials? userCredentials = null,
	                                       CancellationToken cancellationToken = default) {
		var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
		using var call = new Operations.OperationsClient(
			channelInfo.CallInvoker).SetNodePriorityAsync(
			new SetNodePriorityReq {Priority = nodePriority},
			KurrentDBCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
		await call.ResponseAsync.ConfigureAwait(false);
	}

	/// <summary>
	/// Restart persistent subscriptions
	/// </summary>
	/// <param name="deadline"></param>
	/// <param name="userCredentials"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task RestartPersistentSubscriptions(
		TimeSpan? deadline = null,
		UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default) {
		var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
		using var call = new Operations.OperationsClient(
			channelInfo.CallInvoker).RestartPersistentSubscriptionsAsync(
			EmptyResult,
			KurrentDBCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
		await call.ResponseAsync.ConfigureAwait(false);
	}
}