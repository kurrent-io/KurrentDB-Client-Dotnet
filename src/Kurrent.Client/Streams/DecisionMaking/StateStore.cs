using EventStore.Client;
using Kurrent.Client.Streams.GettingState;

namespace Kurrent.Client.Streams.DecisionMaking;

public class StateStoreOptions<TState> {
#if NET48
	public IStateBuilder<TState> StateBuilder { get; set; } = null!;
#else
	public required IStateBuilder<TState> StateBuilder { get; set; }
#endif
}

public class StateStore<TState>(KurrentClient client, StateStoreOptions<TState> options) {
	public Task<StateAtPointInTime<TState>> Get(string streamName, CancellationToken ct = default) =>
		client.GetStateAsync(streamName, options.StateBuilder, ct);
	
	public async Task<IWriteResult> Handle(
		string streamName,
		CommandHandler<TState> decide,
		CancellationToken ct = default
	) {
		var (state, streamPosition, position) = await client.GetStateAsync(streamName, options.StateBuilder, ct);

		var events = await decide(state, ct);

		if (events.Length == 0) {
			return new SuccessResult(
				streamPosition.HasValue ? StreamRevision.FromStreamPosition(streamPosition.Value) : StreamRevision.None,
				position ?? Position.Start
			);
		}

		var appendToStreamOptions = streamPosition.HasValue
			? new AppendToStreamOptions
				{ ExpectedStreamRevision = StreamRevision.FromStreamPosition(streamPosition.Value) }
			: new AppendToStreamOptions { ExpectedStreamState = StreamState.NoStream };

		return await client.AppendToStreamAsync(
			streamName,
			events.Cast<object>(),
			appendToStreamOptions,
			cancellationToken: ct
		);
	}
}



public class AggregateStore<TState, TEvent>(KurrentClient client, IStateBuilder<TState> stateBuilder) where TState: IAggregate<TEvent> {
	public Task<StateAtPointInTime<TState>> Get(string streamName, CancellationToken ct = default) =>
		client.GetStateAsync(streamName, stateBuilder, ct);
	
	public async Task<IWriteResult> Handle(
		string streamName,
		Func<TState, CancellationToken, ValueTask> handle,
		CancellationToken ct = default
	) {
		var (state, streamPosition, position) = await client.GetStateAsync(streamName, stateBuilder, ct);

		await handle(state, ct);
		var events = state.DequeueUncommittedEvents();

		if (events.Length == 0) {
			return new SuccessResult(
				streamPosition.HasValue ? StreamRevision.FromStreamPosition(streamPosition.Value) : StreamRevision.None,
				position ?? Position.Start
			);
		}

		var appendToStreamOptions = streamPosition.HasValue
			? new AppendToStreamOptions
				{ ExpectedStreamRevision = StreamRevision.FromStreamPosition(streamPosition.Value) }
			: new AppendToStreamOptions { ExpectedStreamState = StreamState.NoStream };

		return await client.AppendToStreamAsync(
			streamName,
			events.Cast<object>(),
			appendToStreamOptions,
			cancellationToken: ct
		);
	}
}
