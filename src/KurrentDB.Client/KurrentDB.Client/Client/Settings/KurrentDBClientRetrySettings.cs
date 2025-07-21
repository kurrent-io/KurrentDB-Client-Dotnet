using Grpc.Core;
using Grpc.Net.Client.Configuration;

namespace KurrentDB.Client;

/// <summary>
/// Defines retry configuration for gRPC operations.
/// </summary>
public record KurrentDBClientRetrySettings {
	/// <summary>
	/// Gets or sets whether retry is enabled.
	/// </summary>
	public bool IsEnabled { get; init; } = true;

	/// <summary>
	/// Gets or sets the maximum number of retry attempts.
	/// </summary>
	public int MaxAttempts { get; init; } = 3;

	/// <summary>
	/// Gets or sets the initial backoff delay.
	/// </summary>
	public TimeSpan InitialBackoff { get; init; } = TimeSpan.FromSeconds(250);

	/// <summary>
	/// Gets or sets the maximum backoff delay.
	/// </summary>
	public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromSeconds(10);

	/// <summary>
	/// Gets or sets the backoff multiplier. Each successive backoff increases by this multiplier.
	/// </summary>
	public double BackoffMultiplier { get; init; } = 2.0;

	/// <summary>
	/// Gets or sets the gRPC status codes that should trigger a retry.
	/// </summary>
	public StatusCode[] RetryableStatusCodes { get; init; } = [
		StatusCode.Unavailable,       // Server temporarily unavailable
		StatusCode.Unknown,           // Unknown error (often network issues)
		StatusCode.DeadlineExceeded,  // Server took too long to respond
		StatusCode.ResourceExhausted, // Server overloaded
	];

	/// <summary>
	/// Default retry settings with the default values.
	/// </summary>
	public static KurrentDBClientRetrySettings Default => new();

	/// <summary>
	/// Retry settings with retry disabled.
	/// </summary>
	public static KurrentDBClientRetrySettings NoRetry => new() { IsEnabled = false };

    internal MethodConfig GetRetryMethodConfig() {
        var retryPolicy = new RetryPolicy {
            MaxAttempts       = MaxAttempts,
            InitialBackoff    = InitialBackoff,
            MaxBackoff        = MaxBackoff,
            BackoffMultiplier = BackoffMultiplier
        };

        foreach (var statusCode in RetryableStatusCodes)
            retryPolicy.RetryableStatusCodes.Add(statusCode);

        return new() {
            Names       = { MethodName.Default },
            RetryPolicy = retryPolicy
        };
    }
}
