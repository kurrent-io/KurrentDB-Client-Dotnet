# SharingProvider: Resilient Resource Management Guide

> ⚠️ **LEGACY COMPONENT NOTICE**  
> The SharingProvider is a legacy infrastructure component that predates modern gRPC .NET capabilities. While critical for current operations, **it will be replaced** with proper gRPC resolvers, load balancers, and service retry configurations as recommended by Microsoft's latest guidance.
>
> **Timeline**: Future architecture migration planned when team capacity allows.  
> **Current Status**: Stable and production-critical - continue using for existing code.  
> **New Development**: Consider modern gRPC patterns for greenfield projects.

## Overview

The `SharingProvider<TInput, TOutput>` is a critical infrastructure component in the KurrentDB .NET Client that manages expensive, shareable resources (primarily gRPC channels) with built-in resilience and automatic recovery capabilities. This guide explains how it works, why it was designed this way, and how to use it effectively.

**Historical Context**: This component was built when gRPC .NET lacked mature retry policies, load balancing, and connection management. It provided essential functionality that is now available through the framework itself.

## Core Design Principles

### 1. **Resource Sharing**
- Expensive resources (like gRPC channels) are shared across multiple consumers
- Only one instance of a resource exists at any given time
- Multiple consumers can access the same resource concurrently

### 2. **Automatic Recovery**
- Resources can become "broken" due to network failures, server issues, etc.
- The provider automatically attempts to create new resources when current ones fail
- **Critical: Never gives up** - continues retrying indefinitely until success

### 3. **Thread Safety**
- Multiple threads can safely request resources simultaneously
- Factory function is never called concurrently (no thread-safety requirements for factory)
- Internal state is protected with appropriate synchronization

## Architecture Overview

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Consumer A    │    │                  │    │                 │
│                 ├───►│  SharingProvider │◄──►│  Factory Func   │
│   Consumer B    │    │                  │    │                 │
│                 ├───►│  _currentBox     │    │                 │
│   Consumer C    │    │                  │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                              │
                              ▼
                       ┌─────────────────┐
                       │  Shared         │
                       │  Resource       │
                       │  (e.g. gRPC     │
                       │   Channel)      │
                       └─────────────────┘
```

## Key Components

### TaskCompletionSource<TOutput> (`_currentBox`)
- **Purpose**: Container for the current resource or its creation task
- **States**:
  - `Pending`: Factory is currently creating the resource
  - `Completed Successfully`: Resource is available for use
  - `Completed with Exception`: Resource creation failed
- **Thread Safety**: Replaced atomically using `Interlocked.CompareExchange`

### Factory Function
```csharp
Func<TInput, Action<TInput>, Task<TOutput>> factory
```
- **Parameters**:
  - `TInput input`: Configuration/parameters for resource creation
  - `Action<TInput> onBroken`: Callback to signal when resource becomes broken
- **Responsibilities**:
  - Create the expensive resource (e.g., establish gRPC connection)
  - Store the `onBroken` callback for later use
  - Return the created resource

### OnBroken Callback
- **Purpose**: Allows the resource itself to signal when it becomes unusable
- **Example**: gRPC channel detects connection failure and calls `onBroken`
- **Thread Safety**: Can be called from any thread at any time

## Operational Flow

### 1. Initial Creation
```csharp
var provider = new SharingProvider<ConnectionSettings, GrpcChannel>(
    factory: async (settings, onBroken) => {
        var channel = CreateGrpcChannel(settings);
        // Store onBroken callback for when channel fails
        channel.StateChanged += (state) => {
            if (state == ConnectivityState.TransientFailure) {
                onBroken(settings); // Signal failure
            }
        };
        return channel;
    },
    factoryRetryDelay: TimeSpan.FromSeconds(5),
    initialInput: connectionSettings
);
```

### 2. Resource Access
```csharp
// Consumer requests resource
var channel = await provider.CurrentAsync;
// Use the channel for gRPC calls
```

### 3. Failure Handling & Recovery

#### When Factory Fails (Network Down)
```
1. Consumer calls: await provider.CurrentAsync
2. Factory throws: ConnectionException("Network unreachable")
3. SharingProvider:
   - Waits: _factoryRetryDelay (back-pressure)
   - Sets exception: box.TrySetException(ex)
   - Consumer gets: ConnectionException immediately
   - Triggers retry: OnBroken(box, _initialInput)
4. New attempt:
   - Creates new TaskCompletionSource
   - Calls factory again after delay
   - Process repeats until success
```

#### When Resource Becomes Broken (Connection Lost)
```
1. gRPC channel detects failure
2. Channel calls: onBroken(originalSettings)
3. SharingProvider:
   - Checks if current box is completed
   - Creates new TaskCompletionSource
   - Starts factory to create new resource
4. Next consumer gets fresh resource
```

## Critical Behavior: Infinite Retry

### Why Infinite Retry?

**Event sourcing systems require persistent connectivity:**
- Events must be reliably stored and retrieved
- Temporary network issues should not break the application
- Connection recovery should be automatic and transparent

### Stack Overflow Prevention

The original design includes `await Task.Yield()` to prevent stack overflow:
```csharp
catch (Exception ex) {
    await Task.Yield(); // Breaks synchronous call stack
    Logger.LogDebug(ex, "Production failed. Retrying in {delay}", _factoryRetryDelay);
    await Task.Delay(_factoryRetryDelay).ConfigureAwait(false);
    box.TrySetException(ex);
    OnBroken(box, _initialInput); // Recursive call becomes async continuation
}
```

This pattern ensures that even with infinite retries, the call stack doesn't grow unbounded.

## Usage Patterns

### 1. Connection Management (Primary Use Case)
```csharp
var channelProvider = new SharingProvider<ReconnectionRequired, ChannelInfo>(
    factory: async (reconnectionRequired, onBroken) => {
        var channel = await CreateChannelAsync(reconnectionRequired);
        var capabilities = await GetServerCapabilitiesAsync(channel);
        
        // Set up failure detection
        channel.StateChanged += state => {
            if (IsFailureState(state)) {
                onBroken(ReconnectionRequired.Rediscover.Instance);
            }
        };
        
        return new ChannelInfo(channel, capabilities, invoker);
    },
    factoryRetryDelay: TimeSpan.FromSeconds(5),
    initialInput: ReconnectionRequired.Rediscover.Instance
);
```

### 2. Resource Access
```csharp
// Multiple consumers can share the same resource
public async Task<StreamMetadata> GetStreamMetadataAsync(string streamName) {
    var channelInfo = await _channelProvider.CurrentAsync;
    var client = new StreamsService.StreamsServiceClient(channelInfo.CallInvoker);
    return await client.GetStreamMetadataAsync(new GetStreamMetadataRequest {
        StreamName = streamName
    });
}
```

### 3. Manual Reset
```csharp
// Force recreation of resource (e.g., when credentials change)
_channelProvider.Reset(newConnectionSettings);
```

## Error Scenarios & Handling

### Network Outage
```
Time: T0  - Consumer requests resource
Time: T0  - Factory fails (network down)
Time: T0  - Consumer gets exception immediately
Time: T5  - Automatic retry attempt #1 (fails)
Time: T10 - Automatic retry attempt #2 (fails)
Time: T15 - Automatic retry attempt #3 (succeeds)
Time: T15+- Next consumer gets working resource
```

### Partial Connectivity
```
Time: T0  - Resource created successfully
Time: T30 - Connection degrades, onBroken() called
Time: T30 - New resource creation starts
Time: T35 - New resource ready
Time: T35+- Consumers transparently use new resource
```

### Server Restart
```
Time: T0  - Client connected to server
Time: T60 - Server restarts, all connections drop
Time: T60 - onBroken() called for existing resource
Time: T60 - Factory starts creating new resource
Time: T65 - Connection re-established
Time: T65+- Operations resume normally
```

## Monitoring & Debugging

### Logging
The SharingProvider provides detailed logging for troubleshooting:

```csharp
// Success
Logger.LogDebug("{type} being produced...", typeof(TOutput).Name);
Logger.LogDebug("{type} produced!", typeof(TOutput).Name);

// Failures
Logger.LogDebug(ex, "{type} production failed. Retrying in {delay}", typeof(TOutput).Name, _factoryRetryDelay);

// State Changes
Logger.LogInformation("{type} marked as broken. Creating replacement.", typeof(TOutput).Name);
Logger.LogDebug("{type} returned to factory. Producing a new one.", typeof(TOutput).Name);
```

### Health Monitoring
Monitor these aspects in production:
- **Retry frequency**: High retry rates indicate network/server issues
- **Resource lifetime**: Short lifetimes suggest instability
- **Factory duration**: Long creation times indicate performance issues

## Best Practices

### 1. Factory Function Design
```csharp
// ✅ Good: Proper error handling and cleanup
async (settings, onBroken) => {
    GrpcChannel? channel = null;
    try {
        channel = CreateChannel(settings);
        await channel.ConnectAsync();
        SetupFailureDetection(channel, onBroken);
        return new ChannelInfo(channel, capabilities);
    }
    catch {
        channel?.Dispose(); // Cleanup on failure
        throw;
    }
}

// ❌ Bad: No cleanup, no failure detection
async (settings, onBroken) => {
    return CreateChannel(settings); // Leaks on failure, no monitoring
}
```

### 2. Resource Disposal
```csharp
// The SharingProvider automatically disposes IDisposable resources
public void Dispose() {
    _sharingProvider.Dispose(); // This will dispose the contained resource
}
```

### 3. Input Parameter Design
```csharp
// ✅ Good: Immutable input with all necessary information
public record ReconnectionSettings(
    string ConnectionString,
    TimeSpan Timeout,
    Dictionary<string, string> Headers
);

// ❌ Bad: Mutable state or missing information
public class ConnectionState {
    public string? Endpoint { get; set; } // Nullable, mutable
}
```

### 4. Testing
```csharp
[Test]
public async Task handles_factory_failures_gracefully() {
    var attempts = 0;
    var provider = new SharingProvider<int, string>(
        factory: async (input, onBroken) => {
            attempts++;
            if (attempts <= 2) throw new("Simulated failure");
            return $"Success on attempt {attempts}";
        },
        factoryRetryDelay: TimeSpan.FromMilliseconds(10),
        initialInput: 0
    );

    // First call fails, triggers retry
    var ex = await Assert.ThrowsAsync<Exception>(() => provider.CurrentAsync);
    Assert.Equal("Simulated failure", ex.Message);

    // Wait for retries to complete
    await Task.Delay(50);

    // Eventually succeeds
    var result = await provider.CurrentAsync;
    Assert.Equal("Success on attempt 3", result);
}
```

## Common Pitfalls

### 1. **Blocking the Factory**
```csharp
// ❌ Bad: Synchronous blocking
factory: (input, onBroken) => {
    var result = SomeSlowOperation(); // Blocks thread
    return Task.FromResult(result);
}

// ✅ Good: Proper async
factory: async (input, onBroken) => {
    var result = await SomeSlowOperationAsync();
    return result;
}
```

### 2. **Not Handling onBroken Properly**
```csharp
// ❌ Bad: Ignoring the callback
factory: async (input, onBroken) => {
    var resource = CreateResource();
    // onBroken never called - broken resources never recovered
    return resource;
}

// ✅ Good: Proper failure detection
factory: async (input, onBroken) => {
    var resource = CreateResource();
    resource.OnFailure += () => onBroken(input);
    return resource;
}
```

### 3. **Inappropriate Reset Usage**
```csharp
// ❌ Bad: Resetting on every error
try {
    await DoOperation();
} catch {
    provider.Reset(); // Creates unnecessary churn
}

// ✅ Good: Reset only when input changes
if (credentialsChanged) {
    provider.Reset(newCredentials);
}
```

## Performance Considerations

### 1. **Factory Efficiency**
- Keep factory operations as fast as possible
- Use connection pooling where appropriate
- Implement proper timeout handling

### 2. **Resource Lifetime**
- Longer-lived resources are more efficient
- Implement proper health checking to avoid premature replacement
- Consider keep-alive mechanisms

### 3. **Retry Delays**
- Balance between quick recovery and resource usage
- Consider exponential backoff for repeated failures
- Typical values: 1-10 seconds for production systems

## Future Migration: Modern gRPC Approaches

### Current vs. Future Architecture

#### **Legacy Approach (Current - SharingProvider)**
```csharp
// Manual connection management with custom retry logic
var channelProvider = new SharingProvider<ReconnectionRequired, ChannelInfo>(
    factory: async (reconnectionRequired, onBroken) => {
        var channel = await CreateChannelManually(reconnectionRequired);
        SetupCustomFailureDetection(channel, onBroken);
        return new ChannelInfo(channel, capabilities, invoker);
    },
    factoryRetryDelay: TimeSpan.FromSeconds(5),
    initialInput: ReconnectionRequired.Rediscover.Instance
);
```

#### **Modern Approach (Future - gRPC Framework)**
```csharp
// Framework-managed connections with built-in resilience
var channel = GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions {
    // Built-in retry policy
    ServiceConfig = new ServiceConfig {
        RetryPolicy = new RetryPolicy {
            MaxAttempts = 5,
            InitialBackoff = TimeSpan.FromSeconds(1),
            MaxBackoff = TimeSpan.FromSeconds(5),
            BackoffMultiplier = 1.5,
            RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.DeadlineExceeded }
        }
    },
    
    // Built-in load balancing
    Credentials = ChannelCredentials.SecureSsl,
    MaxRetryAttempts = 3,
    
    // Built-in health checking
    ThrowOperationCanceledOnCancellation = true
});

// Framework handles all connection management, retries, and load balancing
var client = new StreamsService.StreamsServiceClient(channel);
```

### Microsoft's Recommended Patterns

#### **1. Service Configuration (Replaces Custom Retry Logic)**
```csharp
var serviceConfig = new ServiceConfig {
    MethodConfigs = {
        new MethodConfig {
            Names = { MethodName.Default },
            RetryPolicy = new RetryPolicy {
                MaxAttempts = 3,
                InitialBackoff = TimeSpan.FromMilliseconds(1000),
                MaxBackoff = TimeSpan.FromMilliseconds(5000),
                BackoffMultiplier = 2,
                RetryableStatusCodes = { StatusCode.Unavailable }
            }
        }
    }
};
```

#### **2. Load Balancing (Replaces Custom Channel Selection)**
```csharp
// DNS-based load balancing
var channel = GrpcChannel.ForAddress("dns:///my-service", new GrpcChannelOptions {
    Credentials = ChannelCredentials.Insecure,
    ServiceConfig = serviceConfig
});

// Static load balancing for known endpoints
var staticResolver = new StaticResolverFactory(new[] {
    new BalancerAddress("server1.example.com", 443),
    new BalancerAddress("server2.example.com", 443),
    new BalancerAddress("server3.example.com", 443)
});
```

#### **3. Health Checking (Replaces Custom Failure Detection)**
```csharp
var channel = GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions {
    // Framework automatically manages health checking
    ServiceConfig = new ServiceConfig {
        HealthCheckConfig = new HealthCheckConfig {
            ServiceName = "my-service"
        }
    }
});
```

### Migration Strategy

#### **Phase 1: Parallel Implementation**
- Keep SharingProvider for existing functionality
- Implement new features using modern gRPC patterns
- Compare reliability and performance

#### **Phase 2: Gradual Migration**
- Replace non-critical paths first
- Migrate high-traffic areas after validation
- Maintain fallback capabilities

#### **Phase 3: Complete Migration**
- Remove SharingProvider infrastructure
- Simplify codebase with framework-managed connections
- Update documentation and team training

### Benefits of Modern Approach

| Aspect | Legacy (SharingProvider) | Modern (gRPC Framework) |
|--------|-------------------------|-------------------------|
| **Complexity** | High - Custom implementation | Low - Framework managed |
| **Maintenance** | Manual updates required | Automatic framework updates |
| **Testing** | Complex custom test scenarios | Standard framework testing |
| **Performance** | Good, but manual optimization | Optimized by Microsoft |
| **Features** | Limited to our implementation | Full gRPC specification |
| **Debugging** | Custom logging and metrics | Built-in telemetry and tracing |

### Timeline Considerations

#### **Short Term (Current)**
- Continue using SharingProvider for stability
- Fix critical issues as they arise
- Document known limitations

#### **Medium Term (6-12 months)**
- Research team capacity for migration
- Prototype modern approaches for new features
- Plan migration strategy

#### **Long Term (1-2 years)**
- Complete migration to modern gRPC patterns
- Remove legacy infrastructure
- Benefit from framework improvements

### References & Further Reading

- [Microsoft: gRPC Client Configuration](https://docs.microsoft.com/en-us/aspnet/core/grpc/configuration)
- [gRPC .NET: Retry Policy](https://docs.microsoft.com/en-us/aspnet/core/grpc/retries)
- [gRPC Load Balancing](https://docs.microsoft.com/en-us/aspnet/core/grpc/loadbalancing)
- [gRPC Health Checking](https://docs.microsoft.com/en-us/aspnet/core/grpc/health-checks)

## Conclusion

The SharingProvider is designed for the challenging requirements of event sourcing systems where connectivity must be maintained at all costs. Its infinite retry mechanism ensures that temporary network issues don't break applications, while its resource sharing capabilities provide efficiency and proper lifecycle management.

**Current Importance**: Understanding this component is crucial for debugging connectivity issues and ensuring robust operation in production environments. The key insight is that it never gives up - it continues trying to provide working resources until it succeeds, making applications resilient to infrastructure instability.

**Future Direction**: While this component serves us well today, the future lies in leveraging the mature gRPC .NET framework capabilities that now provide the same resilience with less complexity. The migration will simplify our codebase while maintaining the same level of reliability that event sourcing applications require.