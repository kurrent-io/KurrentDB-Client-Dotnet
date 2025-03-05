using EventStore.Client;
using Kurrent.Client.Core.Serialization;
using Kurrent.Client.Streams.GettingState;

namespace Kurrent.Client.Streams.DecisionMaking;

public class StateStoreOptions<TState> where TState : notnull {
#if NET48
	public IStateBuilder<TState> StateBuilder { get; set; } = null!;
#else
	public required IStateBuilder<TState> StateBuilder { get; set; }
#endif
}

public class StateStore<TState>(KurrentClient client, StateStoreOptions<TState> options) where TState : notnull {
	public Task<StateAtPointInTime<TState>> Get(string streamName, CancellationToken ct = default) =>
		client.GetStateAsync(streamName, options.StateBuilder, ct);

	public Task<IWriteResult> Handle(
		string streamName,
		CommandHandler<TState> decide,
		CancellationToken ct = default
	) =>
		client.DecideAsync(streamName, decide, options.StateBuilder, ct);
}

public class AggregateStore<TState, TEvent>(KurrentClient client, StateStoreOptions<TState> options)
	where TState : IAggregate<TEvent> where TEvent : notnull {
	public Task<StateAtPointInTime<TState>> Get(string streamName, CancellationToken ct = default) =>
		client.GetStateAsync(streamName, options.StateBuilder, ct);

	public Task<IWriteResult> Handle(
		string streamName,
		Func<TState, CancellationToken, ValueTask> handle,
		CancellationToken ct = default
	) =>
		client.DecideAsync(
			streamName,
			async (state, token) => {
				await handle(state, token);
				return state.DequeueUncommittedEvents().Select(e => Message.From(e)).ToArray();
			},
			options.StateBuilder,
			ct
		);
}
