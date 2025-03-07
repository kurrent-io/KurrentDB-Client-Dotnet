using EventStore.Client;

namespace Kurrent.Client.Streams.GettingState;

public class GetStreamStateOptions<TState> : ReadStreamOptions where TState : notnull {
	public GetSnapshot<TState>? GetSnapshot { get; set; }
}

public delegate ValueTask<StateAtPointInTime<TState>> GetSnapshot<TState>(
	GetSnapshotOptions options,
	CancellationToken ct = default
) where TState : notnull;

public record GetSnapshotOptions {
	public string? StreamName { get; set; }

	public string? SnapshotVersion { get; set; }

	public static GetSnapshotOptions ForStream(string streamName) =>
		new GetSnapshotOptions { StreamName = streamName };

	public static GetSnapshotOptions ForAll() =>
		new GetSnapshotOptions();
}

public static class KurrentClientGettingStateClientExtensions {
	public static async Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		IStateBuilder<TState> stateBuilder,
		GetStreamStateOptions<TState>? options,
		CancellationToken ct = default
	) where TState : notnull {
		StateAtPointInTime<TState>? stateAtPointInTime = null;

		options ??= new GetStreamStateOptions<TState>();

		if (options.GetSnapshot != null)
			stateAtPointInTime = await options.GetSnapshot(
				GetSnapshotOptions.ForStream(streamName),
				ct
			);

		// TODO: CHeck if I'm passing the actual snapshot state
		options.StreamPosition = stateAtPointInTime?.LastStreamPosition ?? StreamPosition.Start;

		return await eventStore.ReadStreamAsync(streamName, options, ct)
			.GetStateAsync(stateBuilder, ct);
	}

	public static async Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
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
			options,
			ct
		);

	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState, TEvent>(
		this KurrentClient eventStore,
		string streamName,
		CancellationToken ct = default
	) where TState : IState<TEvent>, new() =>
		eventStore.GetStateAsync<TState, TEvent>(streamName, new GetStreamStateOptions<TState>(), ct);
	
	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		CancellationToken ct = default
	) where TState : IState<object>, new() =>
		eventStore.GetStateAsync<TState, object>(streamName, new GetStreamStateOptions<TState>(), ct);
	
	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState, TEvent>(
		this KurrentClient eventStore,
		string streamName,
		Func<TState> getInitialState,
		CancellationToken ct = default
	) where TState : IState<TEvent> =>
		eventStore.GetStateAsync(
			streamName,
			StateBuilder.For<TState, TEvent>(getInitialState),
			ct
		);
	
	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState, TEvent>(
		this KurrentClient eventStore,
		string streamName,
		Func<TState> getInitialState,
		GetStreamStateOptions<TState> options,
		CancellationToken ct = default
	) where TState : IState<TEvent> =>
		eventStore.GetStateAsync(
			streamName,
			StateBuilder.For<TState, TEvent>(getInitialState),
			options,
			ct
		);
	
	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		Func<TState> getInitialState,
		CancellationToken ct = default
	) where TState : IState<object> =>
		eventStore.GetStateAsync(
			streamName,
			StateBuilder.For(getInitialState),
			ct
		);
	
	public static Task<StateAtPointInTime<TState>> GetStateAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		Func<TState> getInitialState,
		GetStreamStateOptions<TState> options,
		CancellationToken ct = default
	) where TState : IState<object> =>
		eventStore.GetStateAsync(
			streamName,
			StateBuilder.For(getInitialState),
			options,
			ct
		);
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
