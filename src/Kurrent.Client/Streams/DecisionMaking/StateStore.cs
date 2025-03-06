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

	public GetStreamStateOptions<TState>? GetStreamStateOptions { get; set; }
}

public class StateStore<TState, TEvent>(KurrentClient client, StateStoreOptions<TState> options)
	where TState : notnull
	where TEvent : notnull {
	public Task<StateAtPointInTime<TState>> Get(string streamName, CancellationToken ct = default) =>
		client.GetStateAsync(streamName, options.StateBuilder, options.GetStreamStateOptions, ct);

	public Task<IWriteResult> AddAsync(string streamName, IEnumerable<TEvent> events, CancellationToken ct = default) =>
		AddAsync(
			streamName,
			events,
			new AppendToStreamOptions { ExpectedStreamState = StreamState.NoStream },
			ct
		);

	public Task<IWriteResult> AddAsync(
		string streamName,
		IEnumerable<TEvent> events,
		AppendToStreamOptions appendToStreamOptions,
		CancellationToken ct = default
	) {
		if (appendToStreamOptions.ExpectedStreamState == null && appendToStreamOptions.ExpectedStreamRevision == null)
			appendToStreamOptions.ExpectedStreamState = StreamState.NoStream;

		return client.AppendToStreamAsync(streamName, events.Cast<object>(), appendToStreamOptions, ct);
	}

	public Task<IWriteResult> UpdateAsync(
		string streamName,
		IEnumerable<TEvent> events,
		CancellationToken ct = default
	) =>
		UpdateAsync(
			streamName,
			events,
			new AppendToStreamOptions { ExpectedStreamState = StreamState.StreamExists },
			ct
		);

	public Task<IWriteResult> UpdateAsync(
		string streamName,
		IEnumerable<TEvent> events,
		StreamRevision expectedStreamRevision,
		CancellationToken ct = default
	) =>
		UpdateAsync(
			streamName,
			events,
			new AppendToStreamOptions { ExpectedStreamRevision = expectedStreamRevision },
			ct
		);

	public Task<IWriteResult> UpdateAsync(
		string streamName,
		IEnumerable<TEvent> events,
		AppendToStreamOptions appendToStreamOptions,
		CancellationToken ct = default
	) {
		if (appendToStreamOptions.ExpectedStreamState == null && appendToStreamOptions.ExpectedStreamRevision == null)
			appendToStreamOptions.ExpectedStreamState = StreamState.StreamExists;

		return client.AppendToStreamAsync(streamName, events.Cast<object>(), appendToStreamOptions, ct);
	}

	public Task<IWriteResult> Handle(
		string streamName,
		CommandHandler<TState> handle,
		CancellationToken ct = default
	) =>
		client.DecideAsync(
			streamName,
			handle,
			options.StateBuilder,
			new DecideOptions<TState>(),
			ct
		);

	public Task<IWriteResult> Handle(
		string streamName,
		CommandHandler<TState> handle,
		DecideOptions<TState>? decideOptions,
		CancellationToken ct = default
	) =>
		client.DecideAsync(
			streamName,
			handle,
			options.StateBuilder,
			new DecideOptions<TState>(),
			ct
		);
}


public class AggregateStoreOptions<TState> where TState : notnull {
#if NET48
	public IStateBuilder<TState> StateBuilder { get; set; } = null!;
#else
	public required IStateBuilder<TState> StateBuilder { get; set; }
#endif

	public DecideOptions<TState>? DecideOptions { get; set; }
}

public class AggregateStore<TState, TEvent>(KurrentClient client, AggregateStoreOptions<TState> options)
	where TState : IAggregate<TEvent> where TEvent : notnull {
	public Task<StateAtPointInTime<TState>> Get(string streamName, CancellationToken ct = default) =>
		client.GetStateAsync(streamName, options.StateBuilder, ct);

	public Task<IWriteResult> AddAsync(string streamName, TState state, CancellationToken ct = default) =>
		AddAsync(streamName, state, new AppendToStreamOptions { ExpectedStreamState = StreamState.NoStream }, ct);

	public Task<IWriteResult> AddAsync(
		string streamName,
		TState state,
		AppendToStreamOptions appendToStreamOptions,
		CancellationToken ct = default
	) {
		if (appendToStreamOptions.ExpectedStreamState == null && appendToStreamOptions.ExpectedStreamRevision == null)
			appendToStreamOptions.ExpectedStreamState = StreamState.NoStream;

		return client.AppendToStreamAsync(streamName, [Message.From(state)], appendToStreamOptions, ct);
	}

	public Task<IWriteResult> UpdateAsync(
		string streamName,
		TState state,
		AppendToStreamOptions appendToStreamOptions,
		CancellationToken ct = default
	) {
		if (appendToStreamOptions.ExpectedStreamState == null && appendToStreamOptions.ExpectedStreamRevision == null)
			appendToStreamOptions.ExpectedStreamState = StreamState.StreamExists;

		return client.AppendToStreamAsync(streamName, [Message.From(state)], appendToStreamOptions, ct);
	}

	public Task<IWriteResult> HandleAsync(
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
			options.DecideOptions,
			ct
		);
}
