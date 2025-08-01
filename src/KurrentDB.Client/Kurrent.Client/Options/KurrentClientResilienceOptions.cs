using System.Diagnostics.CodeAnalysis;
using Grpc.Core;
using Grpc.Net.Client.Configuration;

// Added for Timeout.InfiniteTimeSpan

namespace Kurrent.Client;

/// <summary>
/// Defines resilience configuration for gRPC operations.
/// </summary>
/// <remarks>
/// <para>
/// Provides configuration options for controlling client-side resilience behaviors including
/// timeouts, retry policies, and keepalive settings for gRPC connections. The base default for
/// <see cref="Deadline"/> is <see cref="Timeout.InfiniteTimeSpan"/>, meaning operations will not
/// time out on the client-side unless a specific deadline is configured in a profile or instance.
/// </para>
/// <para>
/// Multiple predefined configurations are available as static properties to cover common scenarios:
/// <see cref="Default"/>, <see cref="NoResilience"/>, <see cref="FailFast"/>,
/// <see cref="RetryForever"/>, <see cref="Subscription"/>, <see cref="HighAvailability"/>,
/// and <see cref="CautiousWrite"/>.
/// </para>
/// </remarks>
[PublicAPI]
public record KurrentClientResilienceOptions : OptionsBase<KurrentClientResilienceOptions, KurrentClientResilienceOptionsValidator> {
    /// <summary>
    /// The amount of time to wait after which a keepalive ping is sent on the transport.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Keepalive pings help detect dead connections and prevent intermediary network
    /// devices from closing idle connections.
    /// </para>
    /// <para>
    /// Default value is 60 seconds, which aligns with gRPC best practices.
    /// </para>
    /// </remarks>
    public TimeSpan KeepAliveInterval { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The amount of time to wait after which a sent keepalive ping is considered timed out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If a keepalive ping is not acknowledged within this timeframe, the connection
    /// is considered dead, and operations will fail with a connectivity error.
    /// </para>
    /// <para>
    /// Default value is 30 seconds.
    /// </para>
    /// </remarks>
    public TimeSpan KeepAliveTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum time to wait for a response for a gRPC call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If set to <see cref="Timeout.InfiniteTimeSpan"/> (the default for this record) or <see langword="null"/>,
    /// no client-side deadline will be applied to the call. The call might still be affected by server-side timeouts
    /// or transport-level timeouts (like keepalive).
    /// </para>
    /// <para>
    /// For long-running operations like subscriptions, or when relying on server-side timeouts or explicit
    /// cancellation, <see cref="Timeout.InfiniteTimeSpan"/> is appropriate.
    /// </para>
    /// <para>
    /// If a finite deadline is set, it represents the total time allowed for the call, including all retry attempts.
    /// If this deadline is exceeded, the call will fail with <see cref="StatusCode.DeadlineExceeded"/>, and no
    /// further retries will occur for that call.
    /// </para>
    /// <para>
    /// Default value for new instances of <see cref="KurrentClientResilienceOptions"/> is <see cref="Timeout.InfiniteTimeSpan"/>.
    /// Specific profiles may override this.
    /// </para>
    /// </remarks>
    public TimeSpan? Deadline { get; init; }

    /// <summary>
    /// Retry configuration options.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls how operations are retried when they fail due to transient errors.
    /// </para>
    /// <para>
    /// By default, when creating a new <see cref="RetryOptions"/> instance, retry is enabled with 3 attempts and moderate backoff settings.
    /// Predefined profiles like <see cref="Default"/> may customize these further.
    /// </para>
    /// </remarks>
    public RetryOptions Retry { get; init; } = new();

    /// <summary>
    /// Default profile: Resilient general-purpose configuration.
    /// </summary>
    /// <remarks>
    /// <para>Application: General use for most reads and common idempotent writes.
    /// Biased towards resilience, allowing operations to complete without client-side time limits.</para>
    /// <para>Call Deadline: Infinite. Keepalive pings help detect idle connection issues (ping interval: 60s, timeout: 30s).</para>
    /// <para>Retries: 3 attempts with moderate exponential backoff (250ms initial, up to 10s, 1.5x multiplier).</para>
    /// <para>Retryable Status Codes: Unavailable, Unknown, ResourceExhausted, Internal.</para>
    /// </remarks>
    /// <returns>A new <see cref="KurrentClientResilienceOptions"/> instance with default settings.</returns>
    public static KurrentClientResilienceOptions Default => new() {
        // Deadline is Timeout.InfiniteTimeSpan by record default
        // KeepAliveInterval and KeepAliveTimeout are standard by record default
        Retry = new() {
            Enabled           = true,
            MaxAttempts       = 3,
            InitialBackoff    = TimeSpan.FromMilliseconds(250),
            MaxBackoff        = TimeSpan.FromSeconds(10),
            BackoffMultiplier = 1.5,
            RetryableStatusCodes = [
                StatusCode.Unavailable,
                StatusCode.Unknown,
                StatusCode.ResourceExhausted,
                StatusCode.Internal
            ]
        }
    };

    /// <summary>
    /// NoResilience profile: Disables client-side retries and relies on an infinite client-side deadline.
    /// </summary>
    /// <remarks>
    /// <para>Application: Use when no client-side resilience (retries or specific deadlines) is desired.
    /// Relies entirely on server behavior, network stability, or external resilience mechanisms.</para>
    /// <para>Call Deadline: Infinite. Keepalive pings help detect idle connection issues (ping interval: 60s, timeout: 30s).</para>
    /// <para>Retries: Disabled.</para>
    /// </remarks>
    /// <returns>A new <see cref="KurrentClientResilienceOptions"/> instance with retries disabled and an infinite deadline.</returns>
    public static KurrentClientResilienceOptions NoResilience => new() {
        // Deadline is Timeout.InfiniteTimeSpan by record default
        // KeepAliveInterval and KeepAliveTimeout are standard by record default
        Retry = new() { Enabled = false }
    };

    /// <summary>
    /// FailFast profile: Configured for immediate success or failure with a very short deadline and no retries.
    /// </summary>
    /// <remarks>
    /// <para>Application: For operations where an immediate response (success or failure) is critical, typically UI-facing actions.</para>
    /// <para>Call Deadline: 5 seconds. Keepalive pings help detect idle connection issues (ping interval: 60s, timeout: 30s).</para>
    /// <para>Retries: Disabled.</para>
    /// </remarks>
    /// <returns>A new <see cref="KurrentClientResilienceOptions"/> instance optimized for failing fast.</returns>
    public static KurrentClientResilienceOptions FailFast => new() {
        // KeepAliveInterval and KeepAliveTimeout are standard by record default
        Deadline = TimeSpan.FromSeconds(5),
        Retry    = new() { Enabled = false }
    };

    /// <summary>
    /// RetryForever profile: For critical operations that must eventually succeed.
    /// </summary>
    /// <remarks>
    /// <para>Application: Critical reads or highly critical idempotent writes that must eventually succeed and can tolerate long retry periods.</para>
    /// <para>Call Deadline: Infinite. Keepalive pings help detect idle connection issues (ping interval: 60s, timeout: 30s).</para>
    /// <para>Retries: Infinite attempts with aggressive exponential backoff (100ms initial, up to 30s, 2.0x multiplier).</para>
    /// <para>Retryable Status Codes: Unavailable, Unknown, ResourceExhausted, Internal.</para>
    /// </remarks>
    /// <returns>A new <see cref="KurrentClientResilienceOptions"/> instance with unlimited retry attempts.</returns>
    public static KurrentClientResilienceOptions RetryForever => new() {
        // Deadline is Timeout.InfiniteTimeSpan by record default
        // KeepAliveInterval and KeepAliveTimeout are standard by record default
        Retry = new() {
            Enabled           = true,
            MaxAttempts       = -1, // -1 indicates infinite retries
            InitialBackoff    = TimeSpan.FromMilliseconds(100),
            MaxBackoff        = TimeSpan.FromSeconds(30),
            BackoffMultiplier = 2.0,
            RetryableStatusCodes = [
                StatusCode.Unavailable,
                StatusCode.Unknown,
                StatusCode.ResourceExhausted,
                StatusCode.Internal
            ]
        }
    };

    /// <summary>
    /// Subscription profile: Optimized for long-running streaming reads.
    /// </summary>
    /// <remarks>
    /// <para>Application: Long-running streaming reads that need to remain connected and recover from transient issues.</para>
    /// <para>Call Deadline: Infinite. Keepalive pings help detect idle connection issues (ping interval: 60s, timeout: 30s).</para>
    /// <para>Retries: 8 attempts with aggressive exponential backoff (100ms initial, up to 15s, 2.0x multiplier).</para>
    /// <para>Retryable Status Codes: Unavailable, Unknown, ResourceExhausted, Internal.</para>
    /// </remarks>
    /// <returns>A new <see cref="KurrentClientResilienceOptions"/> instance optimized for subscriptions.</returns>
    public static KurrentClientResilienceOptions Subscription => new() {
        // Deadline is Timeout.InfiniteTimeSpan by record default
        // KeepAliveInterval and KeepAliveTimeout are standard by record default
        Retry = new() {
            Enabled           = true,
            MaxAttempts       = 8,
            InitialBackoff    = TimeSpan.FromMilliseconds(100),
            MaxBackoff        = TimeSpan.FromSeconds(15),
            BackoffMultiplier = 2.0,
            RetryableStatusCodes = [
                StatusCode.Unavailable,
                StatusCode.Unknown,
                StatusCode.ResourceExhausted,
                StatusCode.Internal
            ]
        }
    };

    /// <summary>
    /// HighAvailability profile: For operations needing quick, predictable recovery within a fixed time window.
    /// </summary>
    /// <remarks>
    /// <para>Application: Reads or idempotent writes requiring fast, predictable retry attempts within a moderate, fixed deadline.</para>
    /// <para>Call Deadline: 30 seconds. Keepalive pings help detect idle connection issues (ping interval: 60s, timeout: 30s).</para>
    /// <para>Retries: 5 attempts with fixed 200ms backoff interval.</para>
    /// <para>Retryable Status Codes: Unavailable, Unknown, ResourceExhausted, Internal, DeadlineExceeded.</para>
    /// </remarks>
    /// <returns>A new <see cref="KurrentClientResilienceOptions"/> instance with high-availability settings.</returns>
    public static KurrentClientResilienceOptions HighAvailability => new() {
        // KeepAliveInterval and KeepAliveTimeout are standard by record default
        Deadline = TimeSpan.FromSeconds(30),
        Retry = new() {
            Enabled           = true,
            MaxAttempts       = 5,
            InitialBackoff    = TimeSpan.FromMilliseconds(200),
            MaxBackoff        = TimeSpan.FromMilliseconds(200), // Aligned with InitialBackoff for fixed interval
            BackoffMultiplier = 1.0, // No multiplier - fixed interval retries
            RetryableStatusCodes = [
                StatusCode.Unavailable,
                StatusCode.Unknown,
                StatusCode.ResourceExhausted,
                StatusCode.Internal,
                StatusCode.DeadlineExceeded // Safe for idempotent ops with finite deadline
            ]
        }
    };

    /// <summary>
    /// CautiousWrite profile: For write operations where idempotency is not guaranteed or caution is preferred.
    /// </summary>
    /// <remarks>
    /// <para>Application: Write operations where idempotency isn't guaranteed, or to explicitly avoid retrying on client-side timeouts.</para>
    /// <para>Call Deadline: 60 seconds. Keepalive pings help detect idle connection issues (ping interval: 60s, timeout: 30s).</para>
    /// <para>Retries: 3 attempts with moderate exponential backoff (500ms initial, up to 15s, 1.5x multiplier).</para>
    /// <para>Retryable Status Codes: Unavailable, Unknown, ResourceExhausted, Internal. (Notably, does NOT include DeadlineExceeded).</para>
    /// </remarks>
    /// <returns>A new <see cref="KurrentClientResilienceOptions"/> instance for cautious writes.</returns>
    public static KurrentClientResilienceOptions CautiousWrite => new() {
        // KeepAliveInterval and KeepAliveTimeout are standard by record default
        Deadline = TimeSpan.FromSeconds(60),
        Retry = new() {
            Enabled           = true,
            MaxAttempts       = 3,
            InitialBackoff    = TimeSpan.FromMilliseconds(500),
            MaxBackoff        = TimeSpan.FromSeconds(15),
            BackoffMultiplier = 1.5,
            RetryableStatusCodes = [
                StatusCode.Unavailable,
                StatusCode.Unknown,
                StatusCode.ResourceExhausted,
                StatusCode.Internal
            ]
        }
    };

    /// <summary>
    /// Converts these resilience options to a gRPC MethodConfig that can be used with the gRPC client.
    /// </summary>
    /// <param name="methodName">Optional method name to apply this configuration to. If null, applies to all methods.</param>
    /// <returns>A MethodConfig object configured according to these options.</returns>
    /// <remarks>
    /// <para>
    /// The returned MethodConfig contains the retry policy configured according to the retry settings.
    /// </para>
    /// <para>
    /// If retry is not enabled, the returned MethodConfig will not include a retry policy.
    /// </para>
    /// </remarks>
    public MethodConfig ToMethodConfig(MethodName? methodName = null) {
        methodName ??= MethodName.Default; // Default to all methods if none specified

        if (!Retry.Enabled)
            return new() { Names = { methodName } };

        var config = new MethodConfig {
            Names = { methodName },
            RetryPolicy = new() {
                MaxAttempts       = Retry.MaxAttempts < 0 ? int.MaxValue : Retry.MaxAttempts,
                InitialBackoff    = Retry.InitialBackoff,
                MaxBackoff        = Retry.MaxBackoff,
                BackoffMultiplier = Retry.BackoffMultiplier
            }
        };

        foreach (var statusCode in Retry.RetryableStatusCodes)
            config.RetryPolicy.RetryableStatusCodes.Add(statusCode);

        return config;
    }

    /// <summary>
    /// Retry configuration options.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defines how operations should be retried when they fail due to transient errors.
    /// </para>
    /// <para>
    /// Includes options for controlling the number of retry attempts, backoff strategy,
    /// and which specific error conditions should trigger retries.
    /// </para>
    /// </remarks>
    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public record RetryOptions {
        /// <summary>
        /// Whether retry is enabled.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When set to <see langword="false"/>, no retry attempts will be made.
        /// </para>
        /// <para>
        /// Default value is <see langword="true"/>.
        /// </para>
        /// </remarks>
        public bool Enabled { get; init; } = true;

        /// <summary>
        /// Maximum number of retry attempts.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The total number of attempts will be this value plus the original attempt.
        /// </para>
        /// <para>
        /// Set to -1 for unlimited retries.
        /// </para>
        /// <para>
        /// Default value is 3.
        /// </para>
        /// </remarks>
        public int MaxAttempts { get; init; } = 3;

        /// <summary>
        /// Initial backoff delay for reconnection attempts.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The backoff delay for the first retry attempt.
        /// </para>
        /// <para>
        /// This value increases with subsequent attempts according to BackoffMultiplier.
        /// </para>
        /// <para>
        /// Default value is 250 milliseconds.
        /// </para>
        /// </remarks>
        public TimeSpan InitialBackoff { get; init; } = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Maximum backoff delay for reconnection attempts.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The upper limit for backoff delay, regardless of how many retry attempts have occurred.
        /// </para>
        /// <para>
        /// Default value is 10 seconds.
        /// </para>
        /// </remarks>
        public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Backoff multiplier. Each successive backoff increases by this multiplier.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Values between 1.5 and 2.0 are typically effective for event sourcing systems.
        /// </para>
        /// <para>
        /// A value of 1.0 results in fixed-interval retries.
        /// </para>
        /// <para>
        /// Default value is 1.5.
        /// </para>
        /// </remarks>
        public double BackoffMultiplier { get; init; } = 1.5;

        /// <summary>
        /// The gRPC status codes that should trigger a retry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// By default, includes:
        /// </para>
        /// <list type="table">
        ///   <item><description><see cref="StatusCode.Unavailable"/> - Server temporarily unavailable</description></item>
        ///   <item><description><see cref="StatusCode.Unknown"/> - Unknown error (often network issues)</description></item>
        ///   <item><description><see cref="StatusCode.DeadlineExceeded"/> - Server took too long to respond</description></item>
        ///   <item><description><see cref="StatusCode.ResourceExhausted"/> - Server overloaded</description></item>
        /// </list>
        /// </remarks>
        public StatusCode[] RetryableStatusCodes { get; init; } = [
            StatusCode.Unavailable,      // Server temporarily unavailable
            StatusCode.Unknown,          // Unknown error (often network issues)
            StatusCode.DeadlineExceeded, // Server took too long to respond
            StatusCode.ResourceExhausted // Server overloaded
        ];
    }
}
