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
)
	where TState : notnull;

public record AsyncDecider<TState>(
	Func<object, TState, CancellationToken, ValueTask<Message[]>> Decide,
	Func<TState, ResolvedEvent, TState> Evolve,
	Func<TState> GetInitialState
) : AsyncDecider<TState, object>(Decide, Evolve, GetInitialState)
	where TState : notnull;

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

public class DecideOptions<TState> where TState : notnull {
	public GetStreamStateOptions<TState>? GetStateOptions       { get; set; }
	public AppendToStreamOptions?         AppendToStreamOptions { get; set; }
}

public static class KurrentClientDecisionMakingExtensions {
	public static async Task<IWriteResult> DecideAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		CommandHandler<TState> decide,
		IStateBuilder<TState> stateBuilder,
		DecideOptions<TState>? options,
		CancellationToken ct = default
	) where TState : notnull {
		var (state, streamPosition, position) =
			await eventStore.GetStateAsync(streamName, stateBuilder, options?.GetStateOptions, ct);

		var events = await decide(state, ct);

		if (events.Length == 0) {
			return new SuccessResult(
				streamPosition.HasValue ? StreamRevision.FromStreamPosition(streamPosition.Value) : StreamRevision.None,
				position ?? Position.Start
			);
		}

		var appendToStreamOptions = options?.AppendToStreamOptions ?? new AppendToStreamOptions();

		if (streamPosition.HasValue)
			appendToStreamOptions.ExpectedStreamRevision = StreamRevision.FromStreamPosition(streamPosition.Value);
		else
			appendToStreamOptions.ExpectedStreamState = StreamState.NoStream;

		return await eventStore.AppendToStreamAsync(
			streamName,
			events.Cast<object>(),
			appendToStreamOptions,
			cancellationToken: ct
		);
	}

	public static Task<IWriteResult> DecideAsync<TState, TCommand>(
		this KurrentClient eventStore,
		string streamName,
		TCommand command,
		Decider<TState, TCommand> decider,
		CancellationToken ct
	) where TState : notnull =>
		eventStore.DecideAsync(
			streamName,
			command,
			decider.ToAsyncDecider(),
			ct
		);

	public static Task<IWriteResult> DecideAsync<TState, TCommand>(
		this KurrentClient eventStore,
		string streamName,
		TCommand command,
		Decider<TState, TCommand> decider,
		DecideOptions<TState>? options,
		CancellationToken ct
	) where TState : notnull =>
		eventStore.DecideAsync(
			streamName,
			command,
			decider.ToAsyncDecider(),
			options,
			ct
		);

	public static Task<IWriteResult> DecideAsync<TState, TCommand>(
		this KurrentClient eventStore,
		string streamName,
		TCommand command,
		AsyncDecider<TState, TCommand> asyncDecider,
		CancellationToken ct
	) where TState : notnull =>
		eventStore.DecideAsync(
			streamName,
			(state, token) => asyncDecider.Decide(command, state, token),
			asyncDecider,
			ct
		);

	public static Task<IWriteResult> DecideAsync<TState, TCommand>(
		this KurrentClient eventStore,
		string streamName,
		TCommand command,
		AsyncDecider<TState, TCommand> asyncDecider,
		DecideOptions<TState>? options,
		CancellationToken ct
	) where TState : notnull =>
		eventStore.DecideAsync(
			streamName,
			(state, token) => asyncDecider.Decide(command, state, token),
			asyncDecider,
			options,
			ct
		);

	public static Task<IWriteResult> DecideAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		CommandHandler<TState> handle,
		IStateBuilder<TState> stateBuilder,
		CancellationToken ct = default
	) where TState : notnull =>
		eventStore.DecideAsync(
			streamName,
			handle,
			stateBuilder,
			new DecideOptions<TState>(),
			ct
		);

	public static Task<IWriteResult> DecideAsync<TState, TEvent>(
		this KurrentClient eventStore,
		string streamName,
		CommandHandler<TState> handle,
		CancellationToken ct = default
	) where TState : IState<TEvent>, new() =>
		eventStore.DecideAsync(
			streamName,
			handle,
			StateBuilder.For<TState, TEvent>(),
			new DecideOptions<TState>(),
			ct
		);
}
