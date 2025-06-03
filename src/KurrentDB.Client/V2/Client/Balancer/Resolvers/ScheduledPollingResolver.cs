using Grpc.Core;
using Grpc.Net.Client.Balancer;
using Microsoft.Extensions.Logging;

namespace Kurrent.Grpc.Balancer;

/// <summary>
/// An implementation of <see cref="Resolver"/> that supports asynchronous polling logic with scheduled refresh capability.
/// <para>
/// ScheduledPollingResolver adds support for automatic periodic refreshes via the <see cref="RefreshInterval"/> property
/// and on-demand refreshes with the <see cref="RefreshAsync"/> method.
/// </para>
/// </summary>
public abstract partial class ScheduledPollingResolver : Resolver {
	/// <summary>
	/// Initializes a new instance of the <see cref="ScheduledPollingResolver"/>.
	/// </summary>
	/// <param name="loggerFactory">The logger factory.</param>
	protected ScheduledPollingResolver(ILoggerFactory loggerFactory)
		: this(null, loggerFactory) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="ScheduledPollingResolver"/>.
	/// </summary>
	/// <param name="loggerFactory">The logger factory.</param>
	/// <param name="backoffPolicyFactory">The backoff policy factory.</param>
	protected ScheduledPollingResolver(IBackoffPolicyFactory? backoffPolicyFactory, ILoggerFactory loggerFactory)
		: this(TimeSpan.Zero, backoffPolicyFactory, loggerFactory) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="ScheduledPollingResolver"/>.
	/// </summary>
	/// <param name="refreshInterval">The interval at which the resolver will automatically refresh.</param>
	/// <param name="backoffPolicyFactory">The backoff policy factory.</param>
	/// <param name="loggerFactory">The logger factory.</param>
	protected ScheduledPollingResolver(TimeSpan refreshInterval, IBackoffPolicyFactory? backoffPolicyFactory, ILoggerFactory loggerFactory) {
		Logger               = loggerFactory.CreateLogger<ScheduledPollingResolver>();
		BackoffPolicyFactory = backoffPolicyFactory;
		RefreshInterval      = refreshInterval;

		ResolverTypeName = GetType().Name;
		ResolveTask      = Task.CompletedTask;
		RefreshLock      = new();
		Cancellator      = new();
		Listener         = null!;
	}

	// Internal for testing
	internal Task ResolveTask { get; set; }

	string                  ResolverTypeName    { get; }
	object                  RefreshLock          { get; }
	CancellationTokenSource Cancellator          { get; }
	TimeSpan                RefreshInterval      { get; }
	IBackoffPolicyFactory?  BackoffPolicyFactory { get; }
	ILogger                 Logger               { get; }

	Timer? RefreshTimer      { get; set; }
	bool   Disposed          { get; set; }
	bool   ResolveSuccessful { get; set; }

	/// <summary>
	/// Gets the listener.
	/// </summary>
	protected Action<ResolverResult> Listener { get; set; }

	/// <summary>
	/// Starts listening to resolver for results with the specified callback. Can only be called once.
	/// <para>
	/// The <see cref="ResolverResult"/> passed to the callback has addresses when successful,
	/// otherwise a <see cref="Status"/> details the resolution error.
	/// </para>
	/// </summary>
	/// <param name="listener">The callback used to receive updates on the target.</param>
	public sealed override void Start(Action<ResolverResult> listener) {
		ArgumentNullException.ThrowIfNull(listener);

		if (Listener is not null)
			throw new InvalidOperationException("Resolver has already been started.");

		Listener = result => {
			if (result.Status.StatusCode == StatusCode.OK) ResolveSuccessful = true;
			LogResolveResult(Logger, ResolverTypeName, result.Status.StatusCode, result.Addresses?.Count ?? 0);
			listener(result);
		};

		OnStarted();

		// Start the refresh timer if RefreshInterval is set
		if (RefreshInterval > TimeSpan.Zero)
			StartRefreshTimer();

		return;

		void StartRefreshTimer() {
			lock (RefreshLock) {
				RefreshTimer?.Dispose();
				RefreshTimer = new Timer(OnRefreshTimerTick, null, RefreshInterval, RefreshInterval);
				LogRefreshTimerStarted(Logger, ResolverTypeName, RefreshInterval);
			}

			return;

			void OnRefreshTimerTick(object? state) {
				try {
					LogScheduledRefreshStarting(Logger, ResolverTypeName);
					Refresh();
				}
				catch (Exception ex) {
					LogRefreshTimerError(Logger, ResolverTypeName, ex);
				}
			}
		}
	}

	/// <summary>
	/// Executes after the resolver starts.
	/// </summary>
	protected virtual void OnStarted() { }

	/// <summary>
	/// Refresh resolution. Can only be called after <see cref="Start(Action{ResolverResult})"/>.
	/// <para>
	/// The resolver runs one asynchronous resolve task at a time. Calling <see cref="Refresh()"/> on the resolver when a
	/// resolve task is already running has no effect.
	/// </para>
	/// </summary>
	public sealed override void Refresh() {
		ObjectDisposedException.ThrowIf(Disposed, ResolverTypeName);

		if (Listener is null)
			throw new InvalidOperationException("Resolver hasn't been started.");

		lock (RefreshLock) {
			LogResolverRefreshRequested(Logger, ResolverTypeName);

			if (!ResolveTask.IsCompleted) {
				LogResolverRefreshIgnored(Logger, ResolverTypeName);
			}
			else {
				// Don't capture the current ExecutionContext and its AsyncLocals onto the connect
				var restoreFlow = false;
				try {
					if (!ExecutionContext.IsFlowSuppressed()) {
						ExecutionContext.SuppressFlow();
						restoreFlow = true;
					}

					// Run ResolveAsync in a background task.
					// This is done to prevent synchronous block inside ResolveAsync from blocking future Refresh calls.
					ResolveTask = Task.Run(() => ResolveNowAsync(Cancellator.Token));
					ResolveTask.ContinueWith(
						static (_, state) => {
							var resolver = (ScheduledPollingResolver)state!;
							LogResolveTaskCompleted(resolver.Logger, resolver.ResolverTypeName);
						},
						this
					);
				}
				finally {
					// Restore the current ExecutionContext
					if (restoreFlow)
						ExecutionContext.RestoreFlow();
				}
			}
		}
	}

	/// <summary>
	/// Ensures a refresh is performed and waits for its completion.
	/// If a refresh is already in progress, it waits for that refresh to complete.
	/// Otherwise, it initiates a new refresh.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
	/// <returns>A task that completes when the refresh operation finishes.</returns>
	public async Task RefreshAsync(CancellationToken cancellationToken = default) {
		ObjectDisposedException.ThrowIf(Disposed, ResolverTypeName);

		if (Listener is null)
			throw new InvalidOperationException("Resolver hasn't been started.");

		lock (RefreshLock) {
			// Check if a refresh is already in progress
			// otherwise, start a new refresh
			if (ResolveTask.IsCompleted) {
				LogRefreshAsyncStarting(Logger, ResolverTypeName);
				// Start a new refresh
				Refresh();
			}
			else {
				LogRefreshWaitingForOngoing(Logger, ResolverTypeName);
			}
		}

		// Wait for the refresh to complete
		if (cancellationToken.CanBeCanceled)
			await ResolveTask.WaitAsync(cancellationToken);
		else
			await ResolveTask;
	}

	async Task ResolveNowAsync(CancellationToken cancellationToken) {
		LogResolveStarting(Logger, ResolverTypeName);

		// Reset resolve success to false. Will be set to true when an OK result is sent to listener.
		ResolveSuccessful = false;

		try {
			var backoffPolicy = BackoffPolicyFactory?.Create();

			for (var attempt = 1; ; attempt++) {
				try {
					LogResolveAttempt(Logger, ResolverTypeName, attempt);

					await ResolveAsync(cancellationToken).ConfigureAwait(false);

					// ResolveAsync may report a failure but not throw. Check to see whether an OK result
					// has been reported. If not then start retry loop.
					if (ResolveSuccessful) return;
				}
				catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
					// Ignore cancellation.
					break;
				}
				catch (Exception ex) {
					LogResolveError(Logger, ResolverTypeName, attempt, ex);
					var status = new Status(StatusCode.Internal, $"Error refreshing resolver (attempt {attempt}): {ex.Message}");
					Listener(ResolverResult.ForFailure(status));
				}

				// No backoff policy specified. Exit immediately.
				if (backoffPolicy is null) break;

				var backoffTicks = backoffPolicy.NextBackoff().Ticks;
				// Task.Delay supports up to Int32.MaxValue milliseconds.
				// Force an upper bound here to ensure an unsupported backoff is never used.
				backoffTicks = Math.Min(backoffTicks, TimeSpan.TicksPerMillisecond * int.MaxValue);

				var backoff = TimeSpan.FromTicks(backoffTicks);
				LogStartingResolveBackoff(Logger, ResolverTypeName, backoff);
				await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
			}
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			// Ignore cancellation.
		}
		catch (Exception ex) {
			LogErrorRetryingResolve(Logger, ResolverTypeName, ex);
		}
	}

	/// <summary>
	/// Resolve the target. Updated results are passed to the callback
	/// registered by <see cref="Start(Action{ResolverResult})"/>. Can only be called
	/// after the resolver has started.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task.</returns>
	protected abstract Task ResolveAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Releases the unmanaged resources used by the <see cref="ScheduledPollingResolver"/> and optionally releases
	/// the managed resources.
	/// </summary>
	/// <param name="disposing">
	/// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
	/// </param>
	protected override void Dispose(bool disposing) {
		if (Disposed)
			return;

		// Cancel any future operations
		Cancellator.Cancel();

		if (disposing) {
			RefreshTimer?.Dispose();
			RefreshTimer = null;

			// Wait for any ongoing resolve task to complete
			// We use Wait() instead of GetAwaiter().GetResult() to avoid
			// unwrapping any exceptions that might be thrown
			if (!ResolveTask.IsCompleted) {
				LogDisposingWaitingForOngoing(Logger, ResolverTypeName);
				try {
					// Use a timeout to avoid hanging indefinitely if something goes wrong
					ResolveTask.Wait(TimeSpan.FromSeconds(5));
				}
				catch (Exception ex) when (ex is TaskCanceledException or TimeoutException) {
					// Ignore cancellation and timeout exceptions
					LogDisposingTaskCancelled(Logger, ResolverTypeName);
				}
				catch (AggregateException ex) {
					// Log but don't rethrow
					LogDisposingTaskFailed(Logger, ResolverTypeName, ex.InnerException ?? ex);
				}
			}
		}

		Disposed = true;
	}

	[LoggerMessage(Level = LogLevel.Trace, EventId = 1, EventName = "ResolverRefreshRequested", Message = "{ResolverType} refresh requested.")]
	private static partial void LogResolverRefreshRequested(ILogger logger, string resolverType);

	[LoggerMessage(Level = LogLevel.Trace, EventId = 2, EventName = "ResolverRefreshIgnored", Message = "{ResolverType} refresh ignored because resolve is already in progress.")]
	private static partial void LogResolverRefreshIgnored(ILogger logger, string resolverType);

	[LoggerMessage(Level = LogLevel.Error, EventId = 3, EventName = "ResolveError", Message = "{ResolverType} error resolving (attempt {attemptNumber}).")]
	private static partial void LogResolveError(ILogger logger, string resolverType, int attemptNumber, Exception ex);

	[LoggerMessage(Level = LogLevel.Trace, EventId = 4, EventName = "ResolveResult", Message = "{ResolverType} result with status code '{StatusCode}' and {AddressCount} addresses.")]
	private static partial void LogResolveResult(ILogger logger, string resolverType, StatusCode statusCode, int addressCount);

	[LoggerMessage(Level = LogLevel.Trace, EventId = 5, EventName = "StartingResolveBackoff", Message = "{ResolverType} starting resolve backoff of {BackoffDuration}.")]
	private static partial void LogStartingResolveBackoff(ILogger logger, string resolverType, TimeSpan backoffDuration);

	[LoggerMessage(Level = LogLevel.Error, EventId = 6, EventName = "ErrorRetryingResolve", Message = "{ResolverType} error retrying resolve.")]
	private static partial void LogErrorRetryingResolve(ILogger logger, string resolverType, Exception ex);

	[LoggerMessage(Level = LogLevel.Trace, EventId = 7, EventName = "ResolveTaskCompleted", Message = "{ResolverType} resolve task completed.")]
	private static partial void LogResolveTaskCompleted(ILogger logger, string resolverType);

	[LoggerMessage(Level = LogLevel.Trace, EventId = 8, EventName = "ResolveStarting", Message = "{ResolverType} resolve starting.")]
	private static partial void LogResolveStarting(ILogger logger, string resolverType);

	[LoggerMessage(Level = LogLevel.Trace, EventId = 9, EventName = "RefreshAsyncStarting", Message = "{ResolverType} async refresh initiated.")]
	private static partial void LogRefreshAsyncStarting(ILogger logger, string resolverType);

	[LoggerMessage(Level = LogLevel.Trace, EventId = 10, EventName = "RefreshTimerStarted", Message = "{ResolverType} refresh timer started with interval {RefreshInterval}.")]
	private static partial void LogRefreshTimerStarted(ILogger logger, string resolverType, TimeSpan refreshInterval);

	[LoggerMessage(Level = LogLevel.Trace, EventId = 11, EventName = "ScheduledRefreshStarting", Message = "{ResolverType} scheduled refresh starting.")]
	private static partial void LogScheduledRefreshStarting(ILogger logger, string resolverType);

	[LoggerMessage(Level = LogLevel.Error, EventId = 12, EventName = "RefreshTimerError", Message = "{ResolverType} error in refresh timer callback.")]
	private static partial void LogRefreshTimerError(ILogger logger, string resolverType, Exception ex);

	[LoggerMessage(Level = LogLevel.Trace, EventId = 13, EventName = "RefreshWaitingForOngoing", Message = "{ResolverType} refresh waiting for ongoing refresh to complete.")]
	private static partial void LogRefreshWaitingForOngoing(ILogger logger, string resolverType);

	[LoggerMessage(Level = LogLevel.Trace, EventId = 14, EventName = "DisposingWaitingForOngoing", Message = "{ResolverType} dispose waiting for ongoing refresh to complete.")]
	private static partial void LogDisposingWaitingForOngoing(ILogger logger, string resolverType);

	[LoggerMessage(Level = LogLevel.Warning, EventId = 15, EventName = "DisposingTaskCancelled", Message = "{ResolverType} dispose task was cancelled or timed out.")]
	private static partial void LogDisposingTaskCancelled(ILogger logger, string resolverType);

	[LoggerMessage(Level = LogLevel.Error, EventId = 16, EventName = "DisposingTaskFailed", Message = "{ResolverType} dispose task failed.")]
	private static partial void LogDisposingTaskFailed(ILogger logger, string resolverType, Exception ex);

	[LoggerMessage(Level = LogLevel.Trace, EventId = 17, EventName = "ResolveAttempt", Message = "{ResolverType} starting resolve attempt {attemptNumber}.")]
	private static partial void LogResolveAttempt(ILogger logger, string resolverType, int attemptNumber);
}
