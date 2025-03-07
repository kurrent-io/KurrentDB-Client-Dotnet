using EventStore.Client;
using Kurrent.Client.Core.Serialization;

namespace Kurrent.Client.Streams.GettingState;

public record StateAtPointInTime<TState>(
	TState State,
	StreamPosition? LastStreamPosition = null,
	Position? LastPosition = null
) where TState : notnull;

public interface IStateBuilder<TState> where TState : notnull {
	public Task<StateAtPointInTime<TState>> GetAsync(
		IAsyncEnumerable<ResolvedEvent> messages,
		GetStateOptions<TState> options,
		CancellationToken ct = default
	);
}

public interface IState : IState<object>;

public interface IState<in TEvent> {
	public void Apply(TEvent @event);
}

public record StateBuilder<TState>(
	Func<TState, ResolvedEvent, TState> Evolve,
	Func<TState> GetInitialState
) : IStateBuilder<TState> where TState : notnull {
	public Task<StateAtPointInTime<TState>> GetAsync(
		IAsyncEnumerable<ResolvedEvent> messages,
		GetStateOptions<TState> options,
		CancellationToken ct
	) =>
		messages.GetStateAsync(
			options.CurrentState is { } state ? state.State : GetInitialState(),
			Evolve,
			ct
		);
}

public static class StateBuilder {
	public static StateBuilder<TState> For<TState, TEvent>(
		Func<TState, TEvent, TState> evolve,
		Func<TState> getInitialState
	) where TState : notnull =>
		new StateBuilder<TState>(
			(state, resolvedEvent) =>
				resolvedEvent.DeserializedData is TEvent @event
					? evolve(state, @event)
					: state,
			getInitialState
		);

	public static StateBuilder<TState> For<TState>(
		Func<TState, object, TState> evolve,
		Func<TState> getInitialState
	) where TState : notnull =>
		new StateBuilder<TState>(
			(state, resolvedEvent) => resolvedEvent.DeserializedData != null
				? evolve(state, resolvedEvent.DeserializedData)
				: state,
			getInitialState
		);

	public static StateBuilder<TState> For<TState>(
		Func<TState, Message, TState> evolve,
		Func<TState> getInitialState
	) where TState : notnull =>
		new StateBuilder<TState>(
			(state, resolvedEvent) => resolvedEvent.Message != null
				? evolve(state, resolvedEvent.Message)
				: state,
			getInitialState
		);

	public static StateBuilder<TState> For<TState, TEvent>()
		where TState : IState<TEvent>, new() =>
		new StateBuilder<TState>(
			(state, resolvedEvent) => {
				if (resolvedEvent.DeserializedData is TEvent @event)
					state.Apply(@event);

				return state;
			},
			() => new TState()
		);

	public static StateBuilder<TState> For<TState>()
		where TState : IState<object>, new() =>
		new StateBuilder<TState>(
			(state, resolvedEvent) => {
				if (resolvedEvent.DeserializedData != null)
					state.Apply(resolvedEvent.DeserializedData);

				return state;
			},
			() => new TState()
		);

	public static StateBuilder<TState> For<TState, TEvent>(Func<TState> getInitialState)
		where TState : IState<TEvent> =>
		new StateBuilder<TState>(
			(state, resolvedEvent) => {
				if (resolvedEvent.DeserializedData is TEvent @event)
					state.Apply(@event);

				return state;
			},
			getInitialState
		);

	public static StateBuilder<TState> For<TState>(Func<TState> getInitialState)
		where TState : IState<object>, new() =>
		new StateBuilder<TState>(
			(state, resolvedEvent) => {
				if (resolvedEvent.DeserializedData != null)
					state.Apply(resolvedEvent.DeserializedData);

				return state;
			},
			getInitialState
		);
}
