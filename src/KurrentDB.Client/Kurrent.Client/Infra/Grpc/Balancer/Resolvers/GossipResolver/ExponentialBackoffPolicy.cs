using Grpc.Net.Client.Balancer;

namespace Kurrent.Grpc.Balancer;

sealed class ExponentialBackoffPolicy(long initialBackoffTicks, long maxBackoffTicks) : IBackoffPolicy {
	internal const double Multiplier = 1.6;
	internal const double Jitter     = 0.2;

	long _nextBackoffTicks = initialBackoffTicks;

	public TimeSpan NextBackoff() {
		var currentBackoffTicks = _nextBackoffTicks;

		_nextBackoffTicks = Math.Min((long)Math.Round(currentBackoffTicks * Multiplier), maxBackoffTicks);

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
