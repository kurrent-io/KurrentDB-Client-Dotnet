using EventStore.Client;
using Kurrent.Client.Core.Serialization;

namespace Kurrent.Client.Streams.GettingState;

public record StateAtPointInTime<TState>(
	TState State,
	StreamPosition? LastStreamPosition = null,
	Position? LastPosition = null
) where TState : notnull;

public record GetStateOptions<TState> where TState : notnull {
	public StateAtPointInTime<TState>? CurrentState { get; set; }
}

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

public record GetSnapshotOptions {
	public string? StreamName { get; set; }

	public string? SnapshotVersion { get; set; }

	public static GetSnapshotOptions ForStream(string streamName) =>
		new GetSnapshotOptions { StreamName = streamName };

	public static GetSnapshotOptions ForAll() =>
		new GetSnapshotOptions();
}

public delegate ValueTask<StateAtPointInTime<TState>> GetSnapshot<TState>(
	GetSnapshotOptions options,
	CancellationToken ct = default
) where TState : notnull;

public record StateBuilder<TState>(
	Func<TState, ResolvedEvent, TState> Evolve,
	Func<TState> GetInitialState
) : IStateBuilder<TState> where TState : notnull {
	public Task<StateAtPointInTime<TState>> GetAsync(
		IAsyncEnumerable<ResolvedEvent> messages,
		GetStateOptions<TState> options,
		CancellationToken ct
	) =>
		messages.GetState(
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

	public static async Task<StateAtPointInTime<TState>> GetState<TState>(
		this IAsyncEnumerable<ResolvedEvent> messages,
		TState initialState,
		Func<TState, ResolvedEvent, TState> evolve,
		CancellationToken ct
	) where TState : notnull {
		var state = initialState;

		if (messages is KurrentClient.ReadStreamResult readStreamResult) {
			if (await readStreamResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
				return new StateAtPointInTime<TState>(state);
		}

		ResolvedEvent? lastEvent = null;

		await foreach (var resolvedEvent in messages.WithCancellation(ct)) {
			lastEvent = resolvedEvent;

			state = evolve(state, resolvedEvent);
		}

		return new StateAtPointInTime<TState>(state, lastEvent?.Event.EventNumber, lastEvent?.Event.Position);
	}
}

public class GetStreamStateOptions<TState> : ReadStreamOptions where TState : notnull {
	public GetSnapshot<TState>? GetSnapshot { get; set; }
}

public static class KurrentClientGettingStateClientExtensions {
	public static async Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		IStateBuilder<TState> stateBuilder,
		GetStreamStateOptions<TState> options,
		CancellationToken ct = default
	) where TState : notnull {
		StateAtPointInTime<TState>? stateAtPointInTime = null;

		if (options.GetSnapshot != null) {
			stateAtPointInTime = await options.GetSnapshot(
				GetSnapshotOptions.ForStream(streamName),
				ct
			);
		}

		options.StreamPosition = stateAtPointInTime?.LastStreamPosition ?? StreamPosition.Start;

		return await eventStore.ReadStreamAsync(streamName, options, ct)
			.GetStateAsync(stateBuilder, ct);
	}

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		IStateBuilder<TState> streamStateBuilder,
		CancellationToken ct = default
	) where TState : notnull =>
		eventStore.GetStateAsync(streamName, streamStateBuilder, new GetStreamStateOptions<TState>(), ct);

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState, TEvent>(
		this KurrentClient eventStore,
		string streamName,
		GetStreamStateOptions<TState> options,
		CancellationToken ct = default
	) where TState : IState<TEvent>, new() =>
		eventStore.GetStateAsync(
			streamName,
			StateBuilder.For<TState, TEvent>(),
			new GetStreamStateOptions<TState>(),
			ct
		);

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState, TEvent>(
		this KurrentClient eventStore,
		string streamName,
		CancellationToken ct = default
	) where TState : IState<TEvent>, new() =>
		eventStore.GetStateAsync<TState, TEvent>(streamName, new GetStreamStateOptions<TState>(), ct);
}

public static class KurrentClientGettingStateReadAndSubscribeExtensions {
	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient.ReadStreamResult readStreamResult,
		IStateBuilder<TState> stateBuilder,
		GetStateOptions<TState> options,
		CancellationToken ct = default
	) where TState : notnull =>
		stateBuilder.GetAsync(readStreamResult, options, ct);

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient.ReadStreamResult readStreamResult,
		IStateBuilder<TState> stateBuilder,
		CancellationToken ct = default
	) where TState : notnull =>
		stateBuilder.GetAsync(readStreamResult, new GetStateOptions<TState>(), ct);

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient.ReadAllStreamResult readAllStreamResult,
		IStateBuilder<TState> stateBuilder,
		GetStateOptions<TState> options,
		CancellationToken ct = default
	) where TState : notnull =>
		stateBuilder.GetAsync(readAllStreamResult, options, ct);

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient.ReadAllStreamResult readAllStreamResult,
		IStateBuilder<TState> stateBuilder,
		CancellationToken ct = default
	) where TState : notnull =>
		stateBuilder.GetAsync(readAllStreamResult, new GetStateOptions<TState>(), ct);

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient.StreamSubscriptionResult subscriptionResult,
		IStateBuilder<TState> stateBuilder,
		GetStateOptions<TState> options,
		CancellationToken ct = default
	) where TState : notnull =>
		stateBuilder.GetAsync(subscriptionResult, options, ct);

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient.StreamSubscriptionResult subscriptionResult,
		IStateBuilder<TState> stateBuilder,
		CancellationToken ct = default
	) where TState : notnull =>
		stateBuilder.GetAsync(subscriptionResult, new GetStateOptions<TState>(), ct);

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentPersistentSubscriptionsClient.PersistentSubscriptionResult subscriptionResult,
		IStateBuilder<TState> stateBuilder,
		GetStateOptions<TState> options,
		CancellationToken ct = default
	) where TState : notnull =>
		stateBuilder.GetAsync(subscriptionResult, options, ct);

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentPersistentSubscriptionsClient.PersistentSubscriptionResult subscriptionResult,
		IStateBuilder<TState> stateBuilder,
		CancellationToken ct = default
	) where TState : notnull =>
		stateBuilder.GetAsync(subscriptionResult, new GetStateOptions<TState>(), ct);
}
