# Comprehensive C# Standards Guide v1.0

Last updated: 2025-06-09

> **Usage**: Reference this guide for detailed C# formatting, organization, and advanced language patterns. This guide extends the essential standards in `core-prompt.md` with comprehensive examples and edge cases.

## Integration with Other Resources

**Core Standards**: This guide extends the essential C# guidelines in `core-prompt.md` with detailed examples  
**Project Context**: Combine with project-specific instructions for domain-relevant patterns  
**Performance Details**: Reference `guides/performance-guide.md` for optimization-specific patterns  
**Testing Standards**: Reference `guides/testing-guide.md` for test-specific organization patterns

**IMPORTANT**: This guide contains the complete class organization rules. The core prompt focuses on principles and adaptive pattern analysis, while this guide provides comprehensive formatting details.

## Class Organization - Complete Reference

### Complete Member Ordering Rules

```csharp
public class ComprehensiveExample {
    // 1. CONSTRUCTORS (in order of parameter count)
    public ComprehensiveExample() { }
    
    public ComprehensiveExample(string name) {
        Name = name;
    }
    
    public ComprehensiveExample(string name, ILogger logger) : this(name) {
        Logger = logger;
    }
    
    // 2. PUBLIC PROPERTIES (logical grouping, then alphabetical)
    // Core properties first
    public string Name { get; init; }
    public bool IsActive { get; set; }
    
    // Configuration properties
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    public int MaxRetries { get; init; } = 3;
    
    // 3. PRIVATE/INTERNAL PROPERTIES (same grouping as public)
    ILogger Logger { get; }
    IConfiguration Configuration { get; }
    
    // 4. PUBLIC METHODS (logical grouping, main operations first)
    // Primary operations
    public async Task<ProcessResult> ProcessAsync(CancellationToken cancellationToken = default) {
        // Implementation
    }
    
    public async Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default) {
        // Implementation
    }
    
    // Secondary operations
    public void Reset() {
        // Implementation
    }
    
    public override string ToString() => Name;
    
    // 5. PRIVATE/INTERNAL METHODS (same grouping as public)
    async Task<bool> InternalValidateAsync() {
        // Implementation
    }
    
    void LogOperation(string operation) {
        // Implementation
    }
    
    // 6. INNER TYPES (records, enums, nested classes)
    public record ProcessResult(bool Success, string Message);
    
    public enum ValidationResult {
        Valid,
        Invalid,
        RequiresReview
    }
    
    record InternalState(DateTime LastUpdate, bool IsProcessing);
}
```

### Section Comments and Organization

```csharp
public class OrganizedService {
    // === CONSTRUCTORS ===
    public OrganizedService() { }
    
    // === PUBLIC PROPERTIES ===
    public string Name { get; init; }
    
    // === PRIVATE PROPERTIES ===
    ILogger Logger { get; }
    
    // === PUBLIC METHODS ===
    // Core Operations
    public async Task ProcessAsync() { }
    
    // Utility Methods  
    public void Reset() { }
    
    // === PRIVATE METHODS ===
    // Validation Logic
    async Task<bool> ValidateInputAsync() { }
    
    // Helper Methods
    void LogError(string message) { }
    
    // === INNER TYPES ===
    public record ProcessOptions();
}
```

## Advanced Vertical Alignment Patterns

### Record Properties with Complex Types

```csharp
public record StreamConfiguration {
    public int                    MaxEvents         { get; init; } = 100;
    public int                    StartPosition     { get; init; } = 0;
    public bool                   IncludeMetadata   { get; init; } = true;
    public StreamDirection        Direction         { get; init; } = StreamDirection.Forward;
    public TimeSpan               Timeout           { get; init; } = TimeSpan.FromSeconds(30);
    public CancellationToken      CancellationToken { get; init; } = default;
    public IReadOnlyList<string>  EventTypes        { get; init; } = [];
}
```

### Property Declarations with Varied Types

```csharp
public class ServiceConfiguration {
    // Connection Properties
    public string                    ConnectionString     { get; init; }
    public TimeSpan                  ConnectionTimeout    { get; init; } = TimeSpan.FromSeconds(30);
    public int                       MaxRetries           { get; init; } = 3;
    
    // Service Dependencies  
    public IEventRepository          EventRepository      { get; }
    public ILogger<ServiceConfig>    Logger               { get; }
    public IMemoryCache              Cache                { get; }
    
    // Advanced Configuration
    public Func<string, bool>        EventFilter          { get; init; } = _ => true;
    public Action<ProcessResult>     OnProcessComplete    { get; init; } = _ => { };
}
```

### Switch Expressions with Complex Patterns

```csharp
public string ProcessEventData(EventData eventData) => eventData switch {
    { Type: var type, Data: var data } when type.StartsWith("order") && !string.IsNullOrEmpty(data)     => ProcessOrderEvent(data),
    { Type: var type, Data: var data } when type.StartsWith("payment") && data.Length > 10             => ProcessPaymentEvent(data),
    { Type: var type, Metadata.Source: var source } when type.Contains("user") && source == "api"      => ProcessUserEvent(eventData),
    { Type: "system.heartbeat", Timestamp: var time } when time > DateTime.UtcNow.AddMinutes(-5)       => "HeartbeatProcessed",
    { Type: null }                                                                                       => "InvalidEvent",
    _                                                                                                    => "UnknownEvent"
};
```

### Object Initializers with Nested Properties

```csharp
return new ComplexConfiguration {
    // Basic Properties
    Name               = "ProductionConfig",
    IsEnabled          = true,
    MaxConcurrency     = Environment.ProcessorCount,
    
    // Nested Object Properties
    DatabaseSettings   = new DatabaseConfiguration {
        ConnectionString     = connectionString,
        CommandTimeout       = TimeSpan.FromSeconds(30),
        MaxPoolSize          = 100
    },
    
    // Collection Properties
    AllowedEventTypes  = [
        "order.created",
        "order.updated", 
        "payment.processed"
    ],
    
    // Complex Lambda Properties
    EventTransform     = eventData => new ProcessedEvent {
        Id        = Guid.NewGuid(),
        Source    = eventData.Type,
        Data      = eventData.Data,
        Timestamp = DateTime.UtcNow
    }
};
```

### Method Chaining with Complex Operations

```csharp
var processedResults = await sourceEvents
    .Where(evt => evt.Type.StartsWith("order") && evt.IsValid)
    .Select(evt => new {
        Event     = evt,
        ProcessId = Guid.NewGuid(),
        Timestamp = DateTime.UtcNow
    })
    .GroupBy(item => item.Event.CustomerId)
    .Select(async group => await ProcessCustomerEventsAsync(
        customerId: group.Key,
        events:     group.Select(item => item.Event).ToList(),
        processId:  group.First().ProcessId,
        cancellationToken
    ))
    .ConfigureAwait(false);
```

### Fluent Interface Alignment

```csharp
public class FluentConfigurationBuilder {
    public FluentConfigurationBuilder WithConnectionString(string connectionString)  => this with { ConnectionString = connectionString };
    public FluentConfigurationBuilder WithTimeout(TimeSpan timeout)                  => this with { Timeout = timeout };
    public FluentConfigurationBuilder WithMaxRetries(int maxRetries)                 => this with { MaxRetries = maxRetries };
    public FluentConfigurationBuilder WithEventFilter(Func<string, bool> filter)     => this with { EventFilter = filter };
    public FluentConfigurationBuilder EnableCaching(bool enabled = true)             => this with { CachingEnabled = enabled };
    public FluentConfigurationBuilder WithLogger(ILogger logger)                     => this with { Logger = logger };
}
```

## Advanced Language Patterns

### Pattern Matching with Guards and Deconstruction

```csharp
public ProcessResult HandleEvent(EventData eventData) => eventData switch {
    // Deconstruction with guards
    var (type, data, metadata) when type == "order.created" && ValidateOrderData(data) => 
        ProcessOrderCreation(data, metadata),
    
    // Property patterns with nested matching
    { Type: "payment.processed", Metadata: { Source: "stripe", Amount: > 0 } } => 
        ProcessStripePayment(eventData),
    
    // List patterns (C# 11+)
    { Tags: [var firstTag, ..var remainingTags] } when firstTag == "priority" => 
        ProcessPriorityEvent(eventData, remainingTags),
    
    // Null patterns with guards
    { Data: null } or { Type: null or "" } => 
        ProcessResult.Failure("Invalid event data"),
    
    // Default case
    _ => ProcessResult.Success("Event processed with default handler")
};
```

### Advanced Generic Constraints and Patterns

```csharp
public interface IEventProcessor<TEvent, TResult> 
    where TEvent : IEvent
    where TResult : IProcessResult, new() {
    
    Task<TResult> ProcessAsync<TContext>(
        TEvent @event, 
        TContext context,
        CancellationToken cancellationToken = default
    ) where TContext : IProcessingContext;
}

public class EventProcessor<TEvent> : IEventProcessor<TEvent, ProcessResult>
    where TEvent : IEvent, IValidatable {
    
    public async Task<ProcessResult> ProcessAsync<TContext>(
        TEvent @event,
        TContext context, 
        CancellationToken cancellationToken = default
    ) where TContext : IProcessingContext {
        // Implementation with full type safety
        if (!@event.IsValid()) {
            return ProcessResult.Failure("Event validation failed");
        }
        
        return await context.ProcessWithRetryAsync(@event, cancellationToken);
    }
}
```

### Collection Expressions and Spread Operators

```csharp
public class EventBatchProcessor {
    // Collection expressions with complex initialization
    readonly List<string> _supportedEventTypes = [
        "order.created",
        "order.updated", 
        "order.cancelled",
        ..GetDynamicEventTypes(),
        ..Configuration.CustomEventTypes ?? []
    ];
    
    // Span and range patterns
    public ProcessResult ProcessEventBatch(ReadOnlySpan<EventData> events) {
        var priorityEvents = events[..5];           // First 5 events
        var regularEvents = events[5..^1];          // Middle events  
        var finalEvent = events[^1];                // Last event
        
        return ProcessEvents([
            ..priorityEvents.ToArray(),
            ..ProcessRegularEvents(regularEvents),
            ProcessFinalEvent(finalEvent)
        ]);
    }
}
```

### Raw String Literals and Interpolation

```csharp
public class QueryBuilder {
    // Raw string literals for complex content
    const string SqlTemplate = """
        SELECT e.Id, e.Type, e.Data, e.Timestamp
        FROM Events e
        WHERE e.StreamId = @streamId
          AND e.Position >= @startPosition
          AND e.Type IN ({0})
        ORDER BY e.Position
        LIMIT @maxEvents
        """;
    
    // UTF-8 string literals for performance
    static ReadOnlySpan<byte> EventHeaderUtf8 => "Event-Type: "u8;
    
    // Interpolated raw strings
    public string BuildEventQuery(string streamId, int position, string[] eventTypes) {
        var eventTypesList = string.Join(", ", eventTypes.Select(t => $"'{t}'"));
        
        return $$"""
            -- Generated query for stream: {{streamId}}
            -- Position: {{position}}
            {{string.Format(SqlTemplate, eventTypesList)}}
            """;
    }
}
```

## Performance-Oriented Patterns

### Struct and Record Struct Usage

```csharp
// Immutable struct for frequent allocations
public readonly record struct EventPosition(long StreamPosition, int EventIndex) {
    public static EventPosition Start => new(0, 0);
    public static EventPosition End => new(long.MaxValue, int.MaxValue);
    
    public EventPosition Next() => this with { EventIndex = EventIndex + 1 };
    public bool IsAfter(EventPosition other) => StreamPosition > other.StreamPosition;
}

// Mutable struct for high-performance scenarios
public struct EventProcessingContext {
    public int ProcessedCount;
    public int ErrorCount;
    public TimeSpan ElapsedTime;
    
    public readonly double SuccessRate => ProcessedCount > 0 
        ? (double)(ProcessedCount - ErrorCount) / ProcessedCount 
        : 0.0;
        
    public void IncrementProcessed() => ProcessedCount++;
    public void IncrementErrors() => ErrorCount++;
}
```

### Span and Memory Patterns

```csharp
public class EventDataProcessor {
    // Efficient string processing with spans
    public bool IsValidEventType(ReadOnlySpan<char> eventType) {
        return eventType switch {
            "order.created" or "order.updated" => true,
            var type when type.StartsWith("payment.") => ValidatePaymentEventType(type),
            _ => false
        };
    }
    
    // Memory-efficient batch processing
    public async Task ProcessEventBatchAsync(ReadOnlyMemory<byte> eventBatch) {
        var processed = 0;
        var remaining = eventBatch;
        
        while (!remaining.IsEmpty) {
            var (eventSize, nextOffset) = ReadEventHeader(remaining.Span);
            var eventData = remaining.Slice(0, eventSize);
            
            await ProcessSingleEventAsync(eventData);
            
            remaining = remaining.Slice(nextOffset);
            processed++;
        }
    }
}
```

## Error Handling Patterns

### Comprehensive Exception Handling

```csharp
public class RobustEventProcessor {
    public async Task<ProcessResult> ProcessAsync(EventData eventData) {
        try {
            // Validation phase
            ValidateEvent(eventData);
            
            // Processing phase with different exception types
            var result = await ProcessEventCoreAsync(eventData);
            
            return ProcessResult.Success(result);
        }
        catch (ArgumentException ex) when (ex.ParamName == nameof(eventData)) {
            Logger.LogWarning("Invalid event data: {Message}", ex.Message);
            return ProcessResult.InvalidInput(ex.Message);
        }
        catch (TimeoutException ex) {
            Logger.LogWarning("Processing timeout for event {EventId}: {Message}", eventData.Id, ex.Message);
            return ProcessResult.Timeout(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("connection")) {
            Logger.LogError(ex, "Connection error during event processing");
            return ProcessResult.ConnectionFailure(ex.Message);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Unexpected error processing event {EventId}", eventData.Id);
            return ProcessResult.UnexpectedError(ex.Message);
        }
    }
}
```

This comprehensive guide provides detailed examples for all the patterns that were removed from the core prompt, ensuring developers have access to complete formatting and organizational guidance when needed.