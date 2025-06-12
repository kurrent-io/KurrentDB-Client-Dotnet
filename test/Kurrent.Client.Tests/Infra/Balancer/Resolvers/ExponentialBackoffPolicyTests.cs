
using Kurrent.Grpc.Balancer;

namespace Kurrent.Client.Tests;

public class ExponentialBackoffPolicyTests {
	[Test]
	public void first_backoff_returns_initial_value() {
		// Arrange
		var initialBackoff = TimeSpan.FromSeconds(1);
		var maxBackoff     = TimeSpan.FromSeconds(60);
		var policy         = new ExponentialBackoffPolicy(initialBackoff.Ticks, maxBackoff.Ticks);

		// Act
		var backoff = policy.NextBackoff();

		// Assert
		// Should be approximately the initial backoff (allowing for jitter)
		backoff.ShouldBeInRange(
			TimeSpan.FromMilliseconds(800), // 20% jitter below
			TimeSpan.FromMilliseconds(1200) // 20% jitter above
		);
	}

	[Test]
	public void subsequent_backoffs_increase_exponentially() {
		// Arrange
		var initialBackoff = TimeSpan.FromSeconds(1);
		var maxBackoff     = TimeSpan.FromSeconds(60);
		var policy         = new ExponentialBackoffPolicy(initialBackoff.Ticks, maxBackoff.Ticks);

		// Act
		var first  = policy.NextBackoff();
		var second = policy.NextBackoff();
		var third  = policy.NextBackoff();

		// Assert
		// Each backoff should follow the exponential pattern with jitter
		// Expected base values:
		// - first: ~1 second (initialBackoff)
		// - second: ~1.6 seconds (first * Multiplier)
		// - third: ~2.56 seconds (second * Multiplier)

		// With jitter of ±20%, check that values are within expected ranges
		// For second backoff: It should be between 1.6*(1-0.2) and 1.6*(1+0.2) times first
		var multiplierWithMinJitter = ExponentialBackoffPolicy.Multiplier * (1 - ExponentialBackoffPolicy.Jitter);
		var multiplierWithMaxJitter = ExponentialBackoffPolicy.Multiplier * (1 + ExponentialBackoffPolicy.Jitter);

		// Second should be between ~1.28 and ~1.92 times first
		second.TotalMilliseconds.ShouldBeGreaterThan(first.TotalMilliseconds * multiplierWithMinJitter);
		second.TotalMilliseconds.ShouldBeLessThan(first.TotalMilliseconds * multiplierWithMaxJitter);

		// Third should be between ~1.28 and ~1.92 times second
		third.TotalMilliseconds.ShouldBeGreaterThan(second.TotalMilliseconds * multiplierWithMinJitter);
		third.TotalMilliseconds.ShouldBeLessThan(second.TotalMilliseconds * multiplierWithMaxJitter);
	}
}
