using Grpc.Core;
using KurrentDb.Client;
using Polly;
using Polly.Contrib.WaitAndRetry;
using static System.TimeSpan;

namespace KurrentDb.Client.Tests;

public static class KurrentDbClientWarmupExtensions {
	static readonly TimeSpan RediscoverTimeout = FromSeconds(5);

	/// <summary>
	/// max of 30 seconds (300 * 100ms)
	/// </summary>
	static readonly IEnumerable<TimeSpan> DefaultBackoffDelay = Backoff.ConstantBackoff(FromMilliseconds(100), 300);

	static async Task<T> TryWarmUp<T>(T client, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
		where T : KurrentDbClientBase {
		await Policy
			.Handle<RpcException>(ex => ex.StatusCode != StatusCode.Unimplemented)
			.Or<Exception>()
			.WaitAndRetryAsync(DefaultBackoffDelay)
			.WrapAsync(Policy.TimeoutAsync(RediscoverTimeout, (_, _, _) => client.RediscoverAsync()))
			.ExecuteAsync(
				async ct => {
					try {
						await action(ct).ConfigureAwait(false);
					}
					catch (Exception ex) when (ex is not OperationCanceledException) {
						// grpc throws a rpcexception when you cancel the token (which we convert into
						// invalid operation) - but polly expects operationcancelledexception or it wont
						// call onTimeoutAsync. so raise that here.
						ct.ThrowIfCancellationRequested();
						throw;
					}
				},
				cancellationToken
			);

		return client;
	}

	public static Task<KurrentDbDbClient> WarmUp(this KurrentDbDbClient dbDbClient, CancellationToken cancellationToken = default) =>
		TryWarmUp(
			dbDbClient,
			async ct => {
				// if we can read from $dbUsers then we know that
				// 1. the dbUsers exist
				// 2. we are connected to leader if we require it
				var users = await dbDbClient
					.ReadStreamAsync(
						direction: Direction.Forwards,
						streamName: "$dbUsers",
						revision: StreamPosition.Start,
						maxCount: 1,
						userCredentials: TestCredentials.Root,
						cancellationToken: ct
					)
					.ToArrayAsync(ct);

				if (users.Length == 0)
					throw new("System is not ready yet...");

				// the read from leader above is not enough to guarantee the next write goes to leader
				_ = await dbDbClient.AppendToStreamAsync(
					streamName: "warmup",
					expectedState: StreamState.Any,
					eventData: Enumerable.Empty<EventData>(),
					userCredentials: TestCredentials.Root,
					cancellationToken: ct
				);
			},
			cancellationToken
		);

	public static Task<KurrentDbOperationsClient> WarmUp(this KurrentDbOperationsClient client, CancellationToken cancellationToken = default) =>
		TryWarmUp(
			client,
			async ct => {
				await client.RestartPersistentSubscriptions(
					userCredentials: TestCredentials.Root,
					cancellationToken: ct
				);
			}, 
			cancellationToken
		);

	public static Task<KurrentDbPersistentSubscriptionsClient> WarmUp(
		this KurrentDbPersistentSubscriptionsClient client, CancellationToken cancellationToken = default
	) =>
		TryWarmUp(
			client,
			async ct => {
				var id = Guid.NewGuid();
				await client.CreateToStreamAsync(
					streamName: $"warmup-stream-{id}",
					groupName: $"warmup-group-{id}",
					settings: new(),
					userCredentials: TestCredentials.Root,
					cancellationToken: ct
				);
			},
			cancellationToken
		);

	public static Task<KurrentDbProjectionManagementClient> WarmUp(
		this KurrentDbProjectionManagementClient client, CancellationToken cancellationToken = default
	) =>
		TryWarmUp(
			client,
			async ct => {
				_ = await client
					.ListAllAsync(userCredentials: TestCredentials.Root, cancellationToken: ct)
					.Take(1)
					.ToArrayAsync(ct);

				// await client.RestartSubsystemAsync(userCredentials: TestCredentials.Root, cancellationToken: ct);
			},
			cancellationToken
		);

	public static Task<KurrentDbUserManagementClient> WarmUp(this KurrentDbUserManagementClient client, CancellationToken cancellationToken = default) =>
		TryWarmUp(
			client,
			async ct => {
				_ = await client
					.ListAllAsync(userCredentials: TestCredentials.Root, cancellationToken: ct)
					.Take(1)
					.ToArrayAsync(ct);
			},
			cancellationToken
		);
}
