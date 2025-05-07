using Grpc.Net.Client.Balancer;

namespace KurrentDB.LoadBalancer;

sealed class ExponentialBackoffPolicy : IBackoffPolicy {
	internal const double Multiplier = 1.6;
	internal const double Jitter     = 0.2;

	readonly long _maxBackoffTicks;
	long          _nextBackoffTicks;

	public ExponentialBackoffPolicy(
		long initialBackoffTicks,
		long maxBackoffTicks
	) {
		_nextBackoffTicks = initialBackoffTicks;
		_maxBackoffTicks  = maxBackoffTicks;
	}

	public TimeSpan NextBackoff() {
		var currentBackoffTicks = _nextBackoffTicks;

		_nextBackoffTicks = Math.Min((long)Math.Round(currentBackoffTicks * Multiplier), _maxBackoffTicks);

		currentBackoffTicks += UniformRandom(-Jitter * currentBackoffTicks, Jitter * currentBackoffTicks);

		return TimeSpan.FromTicks(currentBackoffTicks);

		static long UniformRandom(double low, double high) =>
			(long)(Random.Shared.NextDouble() * (high - low) + low);
	}
}

sealed class ExponentialBackoffPolicyFactory(TimeSpan initialReconnectBackoff, TimeSpan? maxReconnectBackoff) : IBackoffPolicyFactory {
	public IBackoffPolicy Create() =>
		// Limit ticks to Int32.MaxValue. Task.Delay can't use larger values,
		// and larger values mean we need to worry about overflows.
		new ExponentialBackoffPolicy(
			Math.Min(initialReconnectBackoff.Ticks, int.MaxValue),
			Math.Min(maxReconnectBackoff?.Ticks ?? long.MaxValue, int.MaxValue)
		);
}
