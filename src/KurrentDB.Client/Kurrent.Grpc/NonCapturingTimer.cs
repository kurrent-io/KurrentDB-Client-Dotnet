namespace Kurrent.Client.Grpc;

/// <summary>
/// Provides methods to create timers that avoid capturing the current <see cref="ExecutionContext"/> and <see cref="AsyncLocal{T}"/> instances.
/// </summary>
static class NonCapturingTimer {
	/// <summary>
	/// Creates a new timer instance without capturing the current ExecutionContext or AsyncLocals.
	/// </summary>
	/// <param name="callback">The method to execute. This method is invoked by the timer.</param>
	/// <param name="state">An object containing information to be used by the callback method, or null.</param>
	/// <param name="dueTime">The amount of time to delay before the callback is invoked. Specify Timeout.InfiniteTimeSpan to prevent the timer from starting.</param>
	/// <param name="period">The time interval between invocations of the callback method. Specify Timeout.InfiniteTimeSpan to disable periodic signaling.</param>
	/// <returns>A new instance of the <see cref="Timer"/> configured with the specified parameters.</returns>
	public static Timer Create(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) {
		ArgumentNullException.ThrowIfNull(callback);

		// Don't capture the current ExecutionContext and its AsyncLocals onto the timer
		var restoreFlow = false;
		try {
			if (!ExecutionContext.IsFlowSuppressed()) {
				ExecutionContext.SuppressFlow();
				restoreFlow = true;
			}

			return new Timer(callback, state, dueTime, period);
		}
		finally {
			// Restore the current ExecutionContext
			if (restoreFlow) ExecutionContext.RestoreFlow();
		}
	}

	/// <summary>
	/// Creates a new timer instance without capturing the current ExecutionContext or AsyncLocals.
	/// </summary>
	/// <param name="callback">The method to execute. This method is invoked by the timer.</param>
	/// <param name="period">The time interval between invocations of the callback method. Specify Timeout.InfiniteTimeSpan to disable periodic signaling.</param>
	/// <returns>A new instance of the Timer configured with the specified parameters.</returns>
	public static Timer Create(TimerCallback callback, TimeSpan period) =>
		Create(callback, null, period, period);
}
