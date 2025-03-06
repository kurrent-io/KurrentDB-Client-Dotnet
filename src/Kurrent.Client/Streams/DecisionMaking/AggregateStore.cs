using EventStore.Client;
using Kurrent.Client.Core.Serialization;
using Kurrent.Client.Streams.GettingState;

namespace Kurrent.Client.Streams.DecisionMaking;

public interface IAggregateStore<TAggregate, TEvent>
	where TAggregate : IAggregate<TEvent> {
	Task<StateAtPointInTime<TAggregate>> Get(
		string streamName,
		CancellationToken ct = default
	);

	Task<IWriteResult> AddAsync(
		string streamName,
		TAggregate aggregate,
		AppendToStreamOptions? appendToStreamOptions,
		CancellationToken ct = default
	);

	Task<IWriteResult> UpdateAsync(
		string streamName,
		TAggregate aggregate,
		AppendToStreamOptions? appendToStreamOptions,
		CancellationToken ct = default
	);

	Task<IWriteResult> HandleAsync(
		string streamName,
		Func<TAggregate, CancellationToken, ValueTask> handle,
		DecideOptions<TAggregate>? decideOption,
		CancellationToken ct = default
	);
}

public static class AggregateStoreExtensions {
	public static Task<IWriteResult> AddAsync<TAggregate, TEvent>(
		IAggregateStore<TAggregate, TEvent> aggregateStore,
		string streamName,
		TAggregate aggregate,
		CancellationToken ct = default
	) where TAggregate : IAggregate<TEvent> =>
		aggregateStore.AddAsync(
			streamName,
			aggregate,
			new AppendToStreamOptions { ExpectedStreamState = StreamState.NoStream },
			ct
		);

	public static Task<IWriteResult> HandleAsync<TAggregate, TEvent>(
		IAggregateStore<TAggregate, TEvent> aggregateStore,
		string streamName,
		Func<TAggregate, CancellationToken, ValueTask> handle,
		CancellationToken ct = default
	) where TAggregate : IAggregate<TEvent> =>
		aggregateStore.HandleAsync(
			streamName,
			handle,
			new DecideOptions<TAggregate>(),
			ct
		);

	public static Task<IWriteResult> HandleAsync<TAggregate, TEvent>(
		IAggregateStore<TAggregate, TEvent> aggregateStore,
		string streamName,
		Action<TAggregate> handle,
		CancellationToken ct = default
	) where TAggregate : IAggregate<TEvent> =>
		aggregateStore.HandleAsync(
			streamName,
			(state, _) => {
				handle(state);
				return new ValueTask();
			},
			new DecideOptions<TAggregate>(),
			ct
		);

	public static Task<IWriteResult> HandleAsync<TAggregate, TEvent>(
		IAggregateStore<TAggregate, TEvent> aggregateStore,
		string streamName,
		Action<TAggregate> handle,
		DecideOptions<TAggregate>? decideOption,
		CancellationToken ct = default
	) where TAggregate : IAggregate<TEvent> =>
		aggregateStore.HandleAsync(
			streamName,
			(state, _) => {
				handle(state);
				return new ValueTask();
			},
			decideOption,
			ct
		);
}

public class AggregateStore<TAggregate, TEvent>(KurrentClient client, AggregateStoreOptions<TAggregate> options)
	: IAggregateStore<TAggregate, TEvent>
	where TAggregate : IAggregate<TEvent>
	where TEvent : notnull {
	public Task<StateAtPointInTime<TAggregate>> Get(string streamName, CancellationToken ct = default) =>
		client.GetStateAsync(streamName, options.StateBuilder, ct);

	public Task<IWriteResult> AddAsync(
		string streamName,
		TAggregate aggregate,
		AppendToStreamOptions? appendToStreamOptions,
		CancellationToken ct = default
	) {
		appendToStreamOptions ??= new AppendToStreamOptions();

		if (appendToStreamOptions.ExpectedStreamState == null && appendToStreamOptions.ExpectedStreamRevision == null)
			appendToStreamOptions.ExpectedStreamState = StreamState.NoStream;

		return client.AppendToStreamAsync(streamName, aggregate.DequeueUncommittedMessages(), appendToStreamOptions, ct);
	}

	public Task<IWriteResult> UpdateAsync(
		string streamName,
		TAggregate aggregate,
		AppendToStreamOptions? appendToStreamOptions,
		CancellationToken ct = default
	) {
		appendToStreamOptions ??= new AppendToStreamOptions();

		if (appendToStreamOptions.ExpectedStreamState == null && appendToStreamOptions.ExpectedStreamRevision == null)
			appendToStreamOptions.ExpectedStreamState = StreamState.StreamExists;

		return client.AppendToStreamAsync(streamName, aggregate.DequeueUncommittedMessages(), appendToStreamOptions, ct);
	}

	public Task<IWriteResult> HandleAsync(
		string streamName,
		Func<TAggregate, CancellationToken, ValueTask> handle,
		DecideOptions<TAggregate>? decideOption,
		CancellationToken ct = default
	) =>
		client.DecideAsync(
			streamName,
			async (state, token) => {
				await handle(state, token);
				return state.DequeueUncommittedMessages();
			},
			options.StateBuilder,
			decideOption ?? options.DecideOptions,
			ct
		);
}
