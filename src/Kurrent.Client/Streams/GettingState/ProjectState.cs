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
		TState initialState,
		Func<TState, ResolvedEvent, TState> evolve,
		ProjectStateOptions<TState>? options,
		[EnumeratorCancellation] CancellationToken ct
	) where TState : notnull {
		if (messages is KurrentClient.ReadStreamResult readStreamResult) {
			if (await readStreamResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound) {
				yield return new StateAtPointInTime<TState>(initialState);

				yield break;
			}
		}

		var getProjectedId = options?.GetProjectedId ?? (resolvedEvent => resolvedEvent.OriginalStreamId);
		var stateCache     = options?.StateCache ?? new DictionaryStateCache<TState>();

		await foreach (var resolvedEvent in messages.WithCancellation(ct)) {
			var projectedId = getProjectedId(resolvedEvent);
			
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
		TState initialState,
		Func<TState, ResolvedEvent, TState> evolve,
		CancellationToken ct
	) where TState : notnull =>
		messages.ProjectState(initialState, evolve, null, ct);
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
