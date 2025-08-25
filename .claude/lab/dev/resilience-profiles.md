# KurrentDB Client Resilience Profiles

This document demonstrates how different resilience profiles affect network failure recovery and retry behavior in the KurrentDB client.

## Resilience Profile Examples

When network issues or server errors occur, the KurrentDB client uses different resilience strategies depending on the chosen profile. Here's how the different profiles behave:

### Default Profile

**Configuration**:
```csharp
public static KurrentClientResilienceOptions Default => new() {
    KeepAliveInterval = TimeSpan.FromSeconds(60),
    KeepAliveTimeout  = TimeSpan.FromSeconds(30),
    Deadline          = TimeSpan.FromSeconds(15),
    Retry             = new RetryOptions {
        Enabled           = true,
        MaxAttempts       = 3,
        InitialBackoff    = TimeSpan.FromMilliseconds(250),
        MaxBackoff        = TimeSpan.FromSeconds(10),
        BackoffMultiplier = 1.5
    }
};
```

**Retry Sequence**:
1. Initial attempt fails
2. Retry after 250ms
3. Retry after 375ms (250ms * 1.5)
4. Retry after 562ms (375ms * 1.5)

**Time to failure**: ~1.19 seconds of retry time + initial attempt

**Ideal for**: Standard event read operations, non-critical event appends

### NoRetry Profile

**Configuration**:
```csharp
public static KurrentClientResilienceOptions NoRetry => new() {
    KeepAliveInterval = TimeSpan.FromSeconds(60),
    KeepAliveTimeout  = TimeSpan.FromSeconds(30),
    Deadline          = TimeSpan.FromSeconds(10),
    Retry             = new RetryOptions { Enabled = false }
};
```

**Retry Sequence**:
1. Initial attempt fails
2. No retries

**Time to failure**: Only the initial attempt (up to 10s deadline)

**Ideal for**: Quick probing operations, when you handle retry logic at a higher level

### RetryForever Profile

**Configuration**:
```csharp
public static KurrentClientResilienceOptions RetryForever => new() {
    KeepAliveInterval = TimeSpan.FromSeconds(60),
    KeepAliveTimeout  = TimeSpan.FromSeconds(30),
    Deadline          = Timeout.InfiniteTimeSpan,
    Retry             = new RetryOptions {
        Enabled           = true,
        MaxAttempts       = -1, // Infinite retries
        InitialBackoff    = TimeSpan.FromMilliseconds(100),
        MaxBackoff        = TimeSpan.FromSeconds(30),
        BackoffMultiplier = 2.0
    }
};
```

**Retry Sequence** (showing first 10 attempts):
1. Initial attempt fails
2. Retry after 100ms
3. Retry after 200ms (100ms * 2.0)
4. Retry after 400ms (200ms * 2.0)
5. Retry after 800ms (400ms * 2.0)
6. Retry after 1600ms (800ms * 2.0)
7. Retry after 3200ms (1600ms * 2.0)
8. Retry after 6400ms (3200ms * 2.0)
9. Retry after 12800ms (6400ms * 2.0)
10. Retry after 25600ms (12800ms * 2.0)
11. Continues with 30s maximum backoff (capped)

**Time to failure**: Will continue retrying indefinitely until success

**Ideal for**: Critical event writes that must eventually succeed

### Subscription Profile

**Configuration**:
```csharp
public static KurrentClientResilienceOptions Subscription => new() {
    KeepAliveInterval = TimeSpan.FromSeconds(60),
    KeepAliveTimeout  = TimeSpan.FromSeconds(30),
    Deadline          = Timeout.InfiniteTimeSpan,
    Retry             = new RetryOptions {
        Enabled           = true,
        MaxAttempts       = 8,
        InitialBackoff    = TimeSpan.FromMilliseconds(100),
        MaxBackoff        = TimeSpan.FromSeconds(15),
        BackoffMultiplier = 2.0,
        RetryableStatusCodes = [
            StatusCode.Unavailable, StatusCode.Unknown,
            StatusCode.DeadlineExceeded, StatusCode.ResourceExhausted,
            StatusCode.Internal, StatusCode.Cancelled
        ]
    }
};
```

**Retry Sequence**:
1. Initial attempt fails
2. Retry after 100ms
3. Retry after 200ms (100ms * 2.0)
4. Retry after 400ms (200ms * 2.0)
5. Retry after 800ms (400ms * 2.0)
6. Retry after 1600ms (800ms * 2.0)
7. Retry after 3200ms (1600ms * 2.0)
8. Retry after 6400ms (3200ms * 2.0)
9. Retry after 12800ms (6400ms * 2.0)

**Time to failure**: ~25.5 seconds of retry time + initial attempt

**Ideal for**: Long-running subscriptions to event streams

### HighAvailability Profile

**Configuration**:
```csharp
public static KurrentClientResilienceOptions HighAvailability => new() {
    KeepAliveInterval = TimeSpan.FromSeconds(60),
    KeepAliveTimeout  = TimeSpan.FromSeconds(30),
    Deadline          = TimeSpan.FromSeconds(10),
    Retry             = new RetryOptions {
        Enabled           = true,
        MaxAttempts       = 5,
        InitialBackoff    = TimeSpan.FromMilliseconds(200),
        MaxBackoff        = TimeSpan.FromMilliseconds(250),
        BackoffMultiplier = 1.0 // Fixed interval retries
    }
};
```

**Retry Sequence**:
1. Initial attempt fails
2. Retry after 200ms
3. Retry after 200ms (fixed interval)
4. Retry after 200ms (fixed interval)
5. Retry after 200ms (fixed interval)
6. Retry after 200ms (fixed interval)

**Time to failure**: ~1.0 second of retry time (5 x 200ms) + initial attempt

**Ideal for**: Production systems with critical reliability requirements

## Connection Failure Detection

All profiles use industry-standard gRPC keepalive settings for detecting broken connections:

- **KeepAliveInterval**: 60 seconds - Time between keepalive pings
- **KeepAliveTimeout**: 30 seconds - Time to wait for ping response

These settings are optimized to:
- Keep connections alive through proxies and load balancers
- Detect network failures within a reasonable timeframe
- Minimize unnecessary network traffic from keepalives

## Recommended Use Cases

### For Financial Transaction Processing

**Recommended Configuration**: RetryForever

```csharp
// Configure client for critical financial operations
var options = new KurrentClientOptions {
    Resilience = KurrentClientResilienceOptions.RetryForever
};
```

**Rationale**:
- Financial transactions require guaranteed delivery
- Eventual consistency is acceptable as long as the event is stored
- Critical business events should be persisted despite temporary outages

### For Real-time Monitoring Systems

**Recommended Configuration**: Subscription

```csharp
// Configure client for long-running subscriptions
var options = new KurrentClientOptions {
    Resilience = KurrentClientResilienceOptions.Subscription
};
```

**Rationale**:
- Subscriptions are long-running connections
- No deadline ensures connections remain open indefinitely
- Additional retryable status codes cover more failure scenarios
- More retry attempts provide resilience against longer outages

### For Batch Processing Historical Data

**Recommended Configuration**: Default

```csharp
// Configure client for batch processing
var options = new KurrentClientOptions {
    Resilience = KurrentClientResilienceOptions.Default
};
```

**Rationale**:
- Balanced retry approach for processing large datasets
- Reasonable deadline (15s) to detect stuck operations
- Standard keepalive interval sufficient for batch operations

### For High-Traffic Production Systems

**Recommended Configuration**: HighAvailability

```csharp
// Configure client for high-traffic production usage
var options = new KurrentClientOptions {
    Resilience = KurrentClientResilienceOptions.HighAvailability
};
```

**Rationale**:
- Fixed-interval retries (no backoff multiplier) for predictable behavior
- Quick initial retry (200ms) for fast recovery from transient issues
- Shorter deadline (10s) fails faster when services are unavailable
- Optimized for reliability in high-load production environments

## Custom Resilience Configuration

For specialized workloads, custom configurations can be created:

```csharp
// Custom configuration for intensive read operations
var readIntensiveOptions = new KurrentClientResilienceOptions {
    KeepAliveInterval = TimeSpan.FromSeconds(60),
    KeepAliveTimeout = TimeSpan.FromSeconds(30),
    Deadline = TimeSpan.FromSeconds(45),
    Retry = new RetryOptions {
        Enabled = true,
        MaxAttempts = 4,
        InitialBackoff = TimeSpan.FromMilliseconds(200),
        MaxBackoff = TimeSpan.FromSeconds(8),
        BackoffMultiplier = 1.5,
        RetryableStatusCodes = [
            StatusCode.Unavailable, 
            StatusCode.Unknown, 
            StatusCode.DeadlineExceeded,
            StatusCode.ResourceExhausted,
            StatusCode.Internal // Additionally retry on internal errors
        ]
    }
};

// Configure client with custom resilience options
var options = new KurrentClientOptions {
    Resilience = readIntensiveOptions
};
```

## Backoff Multiplier Comparison

The choice of backoff multiplier significantly impacts retry timing:

| Multiplier | Behavior                      | Best For                            |
|------------|-------------------------------|-------------------------------------|
| 1.0        | Fixed intervals               | Predictable recovery patterns       |
| 1.5        | Moderate growth               | Most KurrentDB operations           |
| 2.0        | Faster growth                 | Long-running resilient connections  |
| 3.0        | Aggressive growth             | Severe overload situations          |

### Multiplier = 1.5 (Default Setting)
Starting with 250ms initial backoff:
- Retry 1: 250ms
- Retry 2: 375ms
- Retry 3: 562ms
- Retry 4: 843ms
- Retry 5: 1265ms

### Multiplier = 2.0 (Subscription/RetryForever Setting)
Starting with 100ms initial backoff:
- Retry 1: 100ms
- Retry 2: 200ms
- Retry 3: 400ms
- Retry 4: 800ms
- Retry 5: 1600ms
