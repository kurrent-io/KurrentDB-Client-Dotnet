using EventStore.Client;
using Kurrent.Client.Core.Serialization;
using Kurrent.Client.Streams.GettingState;

namespace Kurrent.Client.Streams.DecisionMaking;

public delegate ValueTask<Message[]> CommandHandler<in TState>(TState state, CancellationToken ct = default);

public record AsyncDecider<TState, TCommand>(
	Func<TCommand, TState, CancellationToken, ValueTask<Message[]>> Decide,
	Func<TState, ResolvedEvent, TState> Evolve,
	Func<TState> GetInitialState
) : StateBuilder<TState>(
	Evolve,
	GetInitialState
) where TState : notnull;

public record AsyncDecider<TState>(
	Func<object, TState, CancellationToken, ValueTask<Message[]>> Decide,
	Func<TState, ResolvedEvent, TState> Evolve,
	Func<TState> GetInitialState
) : AsyncDecider<TState, object>(
	Decide,
	Evolve,
	GetInitialState
) where TState : notnull;

public record Decider<TState, TCommand, TEvent>(
	Func<TCommand, TState, TEvent[]> Decide,
	Func<TState, TEvent, TState> Evolve,
	Func<TState> GetInitialState
) where TEvent : notnull
  where TState : notnull;

public record Decider<TState, TCommand>(
	Func<TCommand, TState, object[]> Decide,
	Func<TState, object, TState> Evolve,
	Func<TState> GetInitialState
) : Decider<TState, TCommand, object>(Decide, Evolve, GetInitialState)
	where TState : notnull;

public static class AsyncDeciderExtensions {
	public static AsyncDecider<TState, TCommand> ToAsyncDecider<TState, TCommand, TEvent>(
		this Decider<TState, TCommand, TEvent> decider
	) where TEvent : notnull
	  where TState : notnull =>
		new AsyncDecider<TState, TCommand>(
			(command, state, _) =>
				new ValueTask<Message[]>(decider.Decide(command, state).Select(m => Message.From(m)).ToArray()),
			(state, resolvedEvent) =>
				resolvedEvent.DeserializedData is TEvent @event
					? decider.Evolve(state, @event)
					: state,
			decider.GetInitialState
		);
}
