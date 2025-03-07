using EventStore.Client;
using Kurrent.Client.Streams.GettingState;
using Polly;

namespace Kurrent.Client.Streams.DecisionMaking;

using static AsyncDecider;

public class DecideOptions<TState> where TState : notnull {
	public GetStreamStateOptions<TState>? GetStateOptions       { get; set; }
	public AppendToStreamOptions?         AppendToStreamOptions { get; set; }
	public IAsyncPolicy<IWriteResult>?    RetryPolicy           { get; set; }
}

public static class KurrentClientDecisionMakingExtensions {
	public static Task<IWriteResult> DecideAsync<TState>(
		this KurrentClient eventStore,
		string streamName,
		CommandHandler<TState> decide,
		IStateBuilder<TState> stateBuilder,
		DecideOptions<TState>? options,
		CancellationToken cancellationToken = default
	) where TState : notnull =>
		DecideRetryPolicy(options).ExecuteAsync(
			async ct => {
				var (state, streamPosition, position) =
					await eventStore.GetStateAsync(streamName, stateBuilder, options?.GetStateOptions, ct)
						.ConfigureAwait(false);

				var messages = await decide(state, ct).ConfigureAwait(false);

				if (messages.Length == 0) {
					return new SuccessResult(
						streamPosition.HasValue
							? StreamRevision.FromStreamPosition(streamPosition.Value)
							: StreamRevision.None,
						position ?? Position.Start
					);
				}

				var appendToStreamOptions = options?.AppendToStreamOptions ?? new AppendToStreamOptions();

				if (streamPosition.HasValue)
					appendToStreamOptions.ExpectedStreamRevision ??=
						StreamRevision.FromStreamPosition(streamPosition.Value);
				else
					appendToStreamOptions.ExpectedStreamState ??= StreamState.NoStream;

				return await eventStore.AppendToStreamAsync(
					streamName,
					messages,
					appendToStreamOptions,
					ct
				).ConfigureAwait(false);
			},
			cancellationToken
		);

	public static Task<IWriteResult> DecideAsync<TState, TCommand, TEvent>(
		this KurrentClient eventStore,
		string streamName,
		TCommand command,
		Decider<TState, TCommand, TEvent> decider,
		CancellationToken ct = default
	) where TState : notnull
	  where TEvent : notnull =>
		eventStore.DecideAsync(
			streamName,
			command,
			decider.ToAsyncDecider(),
			ct
		);

	public static Task<IWriteResult> DecideAsync<TState, TCommand, TEvent>(
		this KurrentClient eventStore,
		string streamName,
		TCommand command,
		Decider<TState, TCommand, TEvent> decider,
		DecideOptions<TState>? options,
		CancellationToken ct = default
	) where TState : notnull
	  where TEvent : notnull =>
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
		Decider<TState, TCommand> decider,
		CancellationToken ct = default
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
		AsyncDecider<TState, TCommand> asyncDecider,
		CancellationToken ct = default
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
		CancellationToken ct = default
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

public static class AsyncDecider {
	public static readonly IAsyncPolicy<IWriteResult> DefaultRetryPolicy =
		Policy<IWriteResult>
			.Handle<WrongExpectedVersionException>()
			.WaitAndRetryAsync(
				retryCount: 3,
				sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(20 * retryAttempt)
			);

	public static bool HasUserProvidedExpectedVersioning(AppendToStreamOptions? options) =>
		options != null && (options.ExpectedStreamState.HasValue || options.ExpectedStreamRevision.HasValue);

	public static IAsyncPolicy<IWriteResult> DecideRetryPolicy<TState>(DecideOptions<TState>? options)
		where TState : notnull =>
		options?.RetryPolicy ??
		(HasUserProvidedExpectedVersioning(options?.AppendToStreamOptions)
			// it doesn't make sense to retry, as expected state will be always the same
			? Policy.NoOpAsync<IWriteResult>()
			: DefaultRetryPolicy);
}
