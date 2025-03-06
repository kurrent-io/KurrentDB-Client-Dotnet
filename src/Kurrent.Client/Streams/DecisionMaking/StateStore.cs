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

public interface IStateStore<TState, in TEvent>
	where TState : notnull
	where TEvent : notnull {
	Task<StateAtPointInTime<TState>> Get(
		string streamName,
		CancellationToken ct = default
	);

	Task<IWriteResult> AddAsync(
		string streamName,
		IEnumerable<TEvent> events,
		AppendToStreamOptions? appendToStreamOptions,
		CancellationToken ct = default
	);

	Task<IWriteResult> UpdateAsync(
		string streamName,
		IEnumerable<TEvent> events,
		AppendToStreamOptions? appendToStreamOptions,
		CancellationToken ct = default
	);

	Task<IWriteResult> Handle(
		string streamName,
		CommandHandler<TState> handle,
		DecideOptions<TState>? decideOptions,
		CancellationToken ct = default
	);
}

public interface IStateStore<TState> : IStateStore<TState, object>
	where TState : notnull;

public static class StateStoreExtensions {
	public static Task<IWriteResult> AddAsync<TState, TEvent>(
		this IStateStore<TState, TEvent> stateStore,
		string streamName,
		IEnumerable<TEvent> events,
		CancellationToken ct = default
	) where TState : notnull where TEvent : notnull =>
		stateStore.AddAsync(
			streamName,
			events,
			new AppendToStreamOptions { ExpectedStreamState = StreamState.NoStream },
			ct
		);

	public static Task<IWriteResult> UpdateAsync<TState, TEvent>(
		this IStateStore<TState, TEvent> stateStore,
		string streamName,
		IEnumerable<TEvent> events,
		CancellationToken ct = default
	) where TState : notnull where TEvent : notnull =>
		stateStore.UpdateAsync(
			streamName,
			events,
			new AppendToStreamOptions { ExpectedStreamState = StreamState.StreamExists },
			ct
		);

	public static Task<IWriteResult> UpdateAsync<TState, TEvent>(
		this IStateStore<TState, TEvent> stateStore,
		string streamName,
		IEnumerable<TEvent> events,
		StreamRevision expectedStreamRevision,
		CancellationToken ct = default
	) where TState : notnull where TEvent : notnull =>
		stateStore.UpdateAsync(
			streamName,
			events,
			new AppendToStreamOptions { ExpectedStreamRevision = expectedStreamRevision },
			ct
		);

	public static Task<IWriteResult> Handle<TState, TEvent>(
		this IStateStore<TState, TEvent> stateStore,
		string streamName,
		CommandHandler<TState> handle,
		CancellationToken ct = default
	) where TState : notnull where TEvent : notnull =>
		stateStore.Handle(
			streamName,
			handle,
			null,
			ct
		);

	public static Task<IWriteResult> Handle<TState, TEvent>(
		this IStateStore<TState, TEvent> stateStore,
		string streamName,
		Func<TState, TEvent[]> handle,
		CancellationToken ct = default
	) where TState : notnull where TEvent : notnull =>
		stateStore.Handle(
			streamName,
			(state, _) => new ValueTask<Message[]>(handle(state).Select(m => Message.From(m)).ToArray()),
			null,
			ct
		);

	public static Task<IWriteResult> Handle<TState, TEvent>(
		this IStateStore<TState, TEvent> stateStore,
		string streamName,
		Func<TState, TEvent[]> handle,
		DecideOptions<TState>? decideOptions,
		CancellationToken ct = default
	) where TState : notnull where TEvent : notnull =>
		stateStore.Handle(
			streamName,
			(state, _) => new ValueTask<Message[]>(handle(state).Select(m => Message.From(m)).ToArray()),
			decideOptions,
			ct
		);
}

public class StateStore<TState>(KurrentClient client, StateStoreOptions<TState> options)
	: StateStore<TState, object>(client, options), IStateStore<TState>
	where TState : notnull;

public class StateStore<TState, TEvent>(KurrentClient client, StateStoreOptions<TState> options)
	: IStateStore<TState, TEvent>
	where TState : notnull
	where TEvent : notnull {
	public Task<StateAtPointInTime<TState>> Get(string streamName, CancellationToken ct = default) =>
		client.GetStateAsync(streamName, options.StateBuilder, options.GetStreamStateOptions, ct);

	public Task<IWriteResult> AddAsync(
		string streamName,
		IEnumerable<TEvent> events,
		AppendToStreamOptions? appendToStreamOptions,
		CancellationToken ct = default
	) {
		appendToStreamOptions ??= new AppendToStreamOptions();

		if (appendToStreamOptions.ExpectedStreamState == null && appendToStreamOptions.ExpectedStreamRevision == null)
			appendToStreamOptions.ExpectedStreamState = StreamState.NoStream;

		return client.AppendToStreamAsync(streamName, events.Cast<object>(), appendToStreamOptions, ct);
	}

	public Task<IWriteResult> UpdateAsync(
		string streamName,
		IEnumerable<TEvent> events,
		AppendToStreamOptions? appendToStreamOptions,
		CancellationToken ct = default
	) {
		appendToStreamOptions ??= new AppendToStreamOptions();

		if (appendToStreamOptions.ExpectedStreamState == null && appendToStreamOptions.ExpectedStreamRevision == null)
			appendToStreamOptions.ExpectedStreamState = StreamState.StreamExists;

		return client.AppendToStreamAsync(streamName, events.Cast<object>(), appendToStreamOptions, ct);
	}

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
			decideOptions ?? new DecideOptions<TState>(),
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
