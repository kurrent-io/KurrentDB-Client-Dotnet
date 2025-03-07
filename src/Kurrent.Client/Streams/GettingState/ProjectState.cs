using System.Runtime.CompilerServices;
using EventStore.Client;

namespace Kurrent.Client.Streams.GettingState;

public class ProjectStateOptions<TState> {
	public Func<ResolvedEvent, string>? GetProjectedId { get; set; }

	public IStateCache<TState>? StateCache { get; set; }
}

public static class KurrentClientProjectStateExtensions {
	public static async IAsyncEnumerable<StateAtPointInTime<TState>> ProjectState<TState>(
		this IAsyncEnumerable<ResolvedEvent> messages,
		Func<TState, ResolvedEvent, TState> evolve,
		Func<ResolvedEvent, CancellationToken, ValueTask<TState>> getInitialState,
		ProjectStateOptions<TState>? options,
		[EnumeratorCancellation] CancellationToken ct
	) where TState : notnull {
		var getProjectedId = options?.GetProjectedId ?? (resolvedEvent => resolvedEvent.OriginalStreamId);
		var stateCache     = options?.StateCache ?? new DictionaryStateCache<TState>();
		
		if (messages is KurrentClient.ReadStreamResult readStreamResult) {
			if (await readStreamResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
				yield break;
		}
		
		await foreach (var resolvedEvent in messages.WithCancellation(ct)) {
			var projectedId  = getProjectedId(resolvedEvent);
			var initialState = await getInitialState(resolvedEvent, ct);

			var state = await stateCache.GetValueOrDefaultAsync(projectedId, initialState, ct).ConfigureAwait(false);

			state = evolve(state, resolvedEvent);

			await stateCache.SetValueAsync(projectedId, state, ct).ConfigureAwait(false);

			yield return new StateAtPointInTime<TState>(
				state,
				resolvedEvent.Event.EventNumber,
				resolvedEvent.Event.Position
			);
		}
	}

	public static IAsyncEnumerable<StateAtPointInTime<TState>> ProjectState<TState>(
		this IAsyncEnumerable<ResolvedEvent> messages,
		Func<TState, ResolvedEvent, TState> evolve,
		Func<ResolvedEvent, TState> getInitialState,
		ProjectStateOptions<TState>? options,
		CancellationToken ct
	) where TState : notnull =>
		messages.ProjectState(
			evolve,
			(resolvedEvent, _) => new ValueTask<TState>(getInitialState(resolvedEvent)),
			options,
			ct
		);

	public static IAsyncEnumerable<StateAtPointInTime<TState>> ProjectState<TState>(
		this IAsyncEnumerable<ResolvedEvent> messages,
		Func<TState, ResolvedEvent, TState> evolve,
		Func<ResolvedEvent, TState> getInitialState,
		CancellationToken ct
	) where TState : notnull =>
		messages.ProjectState(
			evolve,
			(resolvedEvent, _) => new ValueTask<TState>(getInitialState(resolvedEvent)),
			null,
			ct
		);

	public static IAsyncEnumerable<StateAtPointInTime<TState>> ProjectState<TState>(
		this IAsyncEnumerable<ResolvedEvent> messages,
		Func<TState, ResolvedEvent, TState> evolve,
		Func<ResolvedEvent, CancellationToken, ValueTask<TState>> getInitialState,
		CancellationToken ct
	) where TState : notnull =>
		messages.ProjectState(evolve, getInitialState, null, ct);
}

public interface IStateCache<TState> {
	public ValueTask<TState> GetValueOrDefaultAsync(string key, TState defaultValue, CancellationToken ct = default);

	public ValueTask SetValueAsync(string key, TState state, CancellationToken ct = default);
}

public class DictionaryStateCache<TState> : IStateCache<TState> {
	readonly Dictionary<string, TState> _states = new Dictionary<string, TState>();

	public ValueTask<TState> GetValueOrDefaultAsync(string key, TState defaultValue, CancellationToken ct = default) {
#if NET48
		var state = _states.TryGetValue(key, out TState? value) ? value : defaultValue;
#else
		var state = _states.GetValueOrDefault(key, defaultValue);
#endif
		return new ValueTask<TState>(state);
	}

	public ValueTask SetValueAsync(string key, TState state, CancellationToken ct = default) {
		_states[key] = state;

		return new ValueTask();
	}
}
