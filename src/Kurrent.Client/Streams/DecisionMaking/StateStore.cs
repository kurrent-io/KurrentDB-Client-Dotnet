using EventStore.Client;
using Kurrent.Client.Core.Serialization;
using Kurrent.Client.Streams.GettingState;

namespace Kurrent.Client.Streams.DecisionMaking;

public interface IStateStore<TState> where TState : notnull {
	Task<StateAtPointInTime<TState>> GetAsync(
		string streamName,
		GetStreamStateOptions<TState>? getStreamStateOptions,
		CancellationToken ct = default
	);

	Task<IWriteResult> AddAsync(
		string streamName,
		IEnumerable<Message> events,
		AppendToStreamOptions? appendToStreamOptions,
		CancellationToken ct = default
	);

	Task<IWriteResult> UpdateAsync(
		string streamName,
		IEnumerable<Message> events,
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

public class StateStoreOptions<TState> where TState : notnull {
#if NET48
	public IStateBuilder<TState> StateBuilder { get; set; } = null!;
#else
	public required IStateBuilder<TState> StateBuilder { get; set; }
#endif

	public GetStreamStateOptions<TState>? GetStreamStateOptions { get; set; }
}

public class StateStore<TState>(KurrentClient client, StateStoreOptions<TState> options)
	: IStateStore<TState>
	where TState : notnull {
	public virtual Task<StateAtPointInTime<TState>> GetAsync(
		string streamName,
		GetStreamStateOptions<TState>? getStreamStateOptions,
		CancellationToken ct = default
	) =>
		client.GetStateAsync(
			streamName,
			options.StateBuilder,
			getStreamStateOptions ?? options.GetStreamStateOptions,
			ct
		);

	public virtual Task<IWriteResult> AddAsync(
		string streamName,
		IEnumerable<Message> events,
		AppendToStreamOptions? appendToStreamOptions,
		CancellationToken ct = default
	) {
		appendToStreamOptions ??= new AppendToStreamOptions();

		if (appendToStreamOptions.ExpectedStreamState == null && appendToStreamOptions.ExpectedStreamRevision == null)
			appendToStreamOptions.ExpectedStreamState = StreamState.NoStream;

		return client.AppendToStreamAsync(streamName, events, appendToStreamOptions, ct);
	}

	public virtual Task<IWriteResult> UpdateAsync(
		string streamName,
		IEnumerable<Message> messages,
		AppendToStreamOptions? appendToStreamOptions,
		CancellationToken ct = default
	) {
		appendToStreamOptions ??= new AppendToStreamOptions();

		if (appendToStreamOptions.ExpectedStreamState == null && appendToStreamOptions.ExpectedStreamRevision == null)
			appendToStreamOptions.ExpectedStreamState = StreamState.StreamExists;

		return client.AppendToStreamAsync(streamName, messages, appendToStreamOptions, ct);
	}

	public virtual Task<IWriteResult> Handle(
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

public static class StateStoreExtensions {
	public static Task<StateAtPointInTime<TState>> GetAsync<TState>(
		this IStateStore<TState> stateStore,
		string streamName,
		CancellationToken ct = default
	) where TState : notnull =>
		stateStore.GetAsync(streamName, null, ct);
	
	public static Task<IWriteResult> AddAsync<TState>(
		this IStateStore<TState> stateStore,
		string streamName,
		IEnumerable<Message> messages,
		CancellationToken ct = default
	) where TState : notnull =>
		stateStore.AddAsync(
			streamName,
			messages,
			null,
			ct
		);

	public static Task<IWriteResult> AddAsync<TState>(
		this IStateStore<TState> stateStore,
		string streamName,
		IEnumerable<object> events,
		CancellationToken ct = default
	) where TState : notnull =>
		stateStore.AddAsync(
			streamName,
			events.Select(e => Message.From(e)),
			new AppendToStreamOptions { ExpectedStreamState = StreamState.NoStream },
			ct
		);

	public static Task<IWriteResult> AddAsync<TState, TEvent>(
		this IStateStore<TState> stateStore,
		string streamName,
		IEnumerable<TEvent> events,
		CancellationToken ct = default
	) where TState : notnull
	  where TEvent : notnull =>
		stateStore.AddAsync(
			streamName,
			events.Select(e => Message.From(e)),
			new AppendToStreamOptions { ExpectedStreamState = StreamState.NoStream },
			ct
		);

	public static Task<IWriteResult> UpdateAsync<TState, TEvent>(
		this IStateStore<TState> stateStore,
		string streamName,
		IEnumerable<TEvent> events,
		CancellationToken ct = default
	) where TState : notnull
	  where TEvent : notnull =>
		stateStore.UpdateAsync(
			streamName,
			events.Select(e => Message.From(e)),
			new AppendToStreamOptions { ExpectedStreamState = StreamState.StreamExists },
			ct
		);

	public static Task<IWriteResult> UpdateAsync<TState>(
		this IStateStore<TState> stateStore,
		string streamName,
		IEnumerable<object> events,
		CancellationToken ct = default
	) where TState : notnull =>
		stateStore.UpdateAsync(
			streamName,
			events.Select(e => Message.From(e)),
			new AppendToStreamOptions { ExpectedStreamState = StreamState.StreamExists },
			ct
		);

	public static Task<IWriteResult> UpdateAsync<TState, TEvent>(
		this IStateStore<TState> stateStore,
		string streamName,
		IEnumerable<TEvent> events,
		StreamRevision expectedStreamRevision,
		CancellationToken ct = default
	) where TState : notnull
	  where TEvent : notnull =>
		stateStore.UpdateAsync(
			streamName,
			events.Select(e => Message.From(e)),
			new AppendToStreamOptions { ExpectedStreamRevision = expectedStreamRevision },
			ct
		);

	public static Task<IWriteResult> UpdateAsync<TState>(
		this IStateStore<TState> stateStore,
		string streamName,
		IEnumerable<object> events,
		StreamRevision expectedStreamRevision,
		CancellationToken ct = default
	) where TState : notnull =>
		stateStore.UpdateAsync(
			streamName,
			events.Select(e => Message.From(e)),
			new AppendToStreamOptions { ExpectedStreamRevision = expectedStreamRevision },
			ct
		);

	public static Task<IWriteResult> Handle<TState>(
		this IStateStore<TState> stateStore,
		string streamName,
		CommandHandler<TState> handle,
		CancellationToken ct = default
	) where TState : notnull =>
		stateStore.Handle(
			streamName,
			handle,
			null,
			ct
		);

	public static Task<IWriteResult> Handle<TState, TEvent>(
		this IStateStore<TState> stateStore,
		string streamName,
		Func<TState, TEvent[]> handle,
		CancellationToken ct = default
	) where TState : notnull
	  where TEvent : notnull =>
		stateStore.Handle(
			streamName,
			(state, _) => new ValueTask<Message[]>(handle(state).Select(m => Message.From(m)).ToArray()),
			null,
			ct
		);

	public static Task<IWriteResult> Handle<TState>(
		this IStateStore<TState> stateStore,
		string streamName,
		Func<TState, object[]> handle,
		CancellationToken ct = default
	) where TState : notnull =>
		stateStore.Handle(
			streamName,
			(state, _) => new ValueTask<Message[]>(handle(state).Select(m => Message.From(m)).ToArray()),
			null,
			ct
		);

	public static Task<IWriteResult> Handle<TState, TEvent>(
		this IStateStore<TState> stateStore,
		string streamName,
		Func<TState, TEvent[]> handle,
		DecideOptions<TState>? decideOptions,
		CancellationToken ct = default
	) where TState : notnull
	  where TEvent : notnull =>
		stateStore.Handle(
			streamName,
			(state, _) => new ValueTask<Message[]>(handle(state).Select(m => Message.From(m)).ToArray()),
			decideOptions,
			ct
		);

	public static Task<IWriteResult> Handle<TState>(
		this IStateStore<TState> stateStore,
		string streamName,
		Func<TState, object[]> handle,
		DecideOptions<TState>? decideOptions,
		CancellationToken ct = default
	) where TState : notnull =>
		stateStore.Handle(
			streamName,
			(state, _) => new ValueTask<Message[]>(handle(state).Select(m => Message.From(m)).ToArray()),
			decideOptions,
			ct
		);
}
