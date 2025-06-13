---
description: Performance optimization guidelines for C# code
applyTo: "**/*.cs"
---

# Performance Guidelines

> **Usage**: Reference this guide when working on performance-critical code, optimizing hot paths, or designing high-throughput systems.

## Performance Optimization Principles

High-performance applications often process large volumes of data, making performance optimization critical. These guidelines ensure efficient code that minimizes resource usage and maximizes throughput.

## Memory Management

### Allocation Minimization

* **Minimize allocations in hot paths** - Profile and identify frequently called code
* **Use value types (structs, record structs)** for small, frequently used data structures
* **Initialize collections with capacity** when size is known or can be estimated:
  ```csharp
  var items = new List<EventData>(expectedCount);
  var lookup = new Dictionary<string, int>(expectedSize);
  ```
* **Use high-performance logging** with source generators:
  ```csharp
  [LoggerMessage(EventId = 1, Level = LogLevel.Information, 
                 Message = "Processing {EventCount} events for stream {StreamId}")]
  static partial void LogProcessingEvents(ILogger logger, int eventCount, string streamId);
  ```

### Advanced Memory Techniques

* **Use `Memory<T>` and `Span<T>`** for high-performance operations on contiguous memory:
  ```csharp
  public void ProcessEvents(ReadOnlySpan<byte> eventData) {
      // Zero-copy processing of event data
  }
  ```
* **Consider pooled objects** for frequently allocated/deallocated objects:
  ```csharp
  using var pooledBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
  var buffer = pooledBuffer.AsSpan(0, actualSize);
  ```
* **Avoid Large Object Heap (85KB+)** in performance-critical code
* **Use `stackalloc` for small arrays** (avoid in loops):
  ```csharp
  Span<int> buffer = stackalloc int[16]; // Only for small, fixed sizes
  ```
* **Use `Utf8StringInterpolation`** for string formatting to byte arrays:
  ```csharp
  var bytes = $"Event-{eventId}-{timestamp}"u8;
  ```

### Garbage Collection Optimization

* **Minimize object references** that create GC pressure
* **Use struct enumerators** for custom collections
* **Prefer `ref struct`** for stack-only types in hot paths:
  ```csharp
  public ref struct EventProcessor {
      public ReadOnlySpan<byte> Data { get; }
      // Guaranteed stack allocation
  }
  ```

## Closures and Lambda Expressions

### Allocation Avoidance

* **Avoid closures in performance-critical paths** to prevent heap allocations
* **Use static lambdas** to prevent capturing context:
  ```csharp
  // Instead of:
  var threshold = 100;
  var filtered = items.Where(x => x.Value > threshold);
  
  // Prefer:
  var filtered = items.Where(static (x, t) => x.Value > t, threshold);
  ```
* **Pass context explicitly** instead of capturing in lambdas:
  ```csharp
  public void ProcessEvents(IEnumerable<Event> events, EventProcessor processor) {
      // Pass processor explicitly rather than capturing it
      var results = events.Select(static (e, p) => p.Process(e), processor);
  }
  ```
* **Cache delegates** that are created repeatedly in hot paths:
  ```csharp
  static readonly Func<Event, bool> IsValidEvent = static e => e.IsValid;
  ```

## Collection Operations

### Specialized Collections

* **Use collection-specific operations** rather than LINQ for performance-critical code:
  ```csharp
  // Instead of: items.Where(x => x.IsActive).Count()
  // Use: items.Count(x => x.IsActive)
  
  // Instead of: items.FirstOrDefault(x => x.Id == id)
  // Use: dictionary.TryGetValue(id, out var item)
  ```
* **Consider `ImmutableArray<T>`** for read-only scenarios:
  ```csharp
  public ImmutableArray<EventType> SupportedEvents { get; }
  ```
* **Use `TryGetValue`** instead of containment check + indexer:
  ```csharp
  // Instead of:
  if (dictionary.ContainsKey(key)) {
      var value = dictionary[key];
  }
  
  // Use:
  if (dictionary.TryGetValue(key, out var value)) {
      // Use value
  }
  ```

### SIMD and Vectorization

* **Use `System.Numerics`** for SIMD-accelerated operations:
  ```csharp
  using System.Numerics;
  
  public void ProcessNumbers(Span<float> numbers) {
      for (int i = 0; i <= numbers.Length - Vector<float>.Count; i += Vector<float>.Count) {
          var vector = new Vector<float>(numbers.Slice(i));
          // SIMD operations
      }
  }
  ```

## Optimization Techniques

### Method Optimization

* **Use `static` members** to avoid virtual dispatch and null checks:
  ```csharp
  public static class EventValidator {
      public static bool IsValid(Event @event) => // No virtual dispatch
  }
  ```
* **Consider `ref return` and `ref local`** for large value types:
  ```csharp
  public ref readonly LargeStruct GetItem(int index) {
      return ref _items[index]; // No copying
  }
  ```
* **Use `String.Create`** for building strings without intermediate allocations:
  ```csharp
  var result = string.Create(length, data, static (span, state) => {
      // Fill span directly without intermediate allocations
  });
  ```

### Intrinsics and Hardware Acceleration

* **Use .NET intrinsics** for hardware-accelerated operations:
  ```csharp
  using System.Runtime.Intrinsics;
  using System.Runtime.Intrinsics.X86;
  
  if (Sse2.IsSupported) {
      // Use SSE2 instructions
  }
  ```
* **Consider `ReadOnlySpan<char>`** for string parsing without allocations:
  ```csharp
  public bool TryParseEventId(ReadOnlySpan<char> input, out int eventId) {
      return int.TryParse(input, out eventId);
  }
  ```

## Parallel and Asynchronous Processing

### High-Performance Async

* **Use `ValueTask`** for methods that often complete synchronously:
  ```csharp
  public ValueTask<Event> GetCachedEventAsync(int id) {
      if (_cache.TryGetValue(id, out var cached)) {
          return new ValueTask<Event>(cached); // Synchronous completion
      }
      return LoadEventAsync(id); // Asynchronous path
  }
  ```
* **Implement `IValueTaskSource`** for custom high-performance async operations
* **Use `ConfigureAwait(false)`** in library code to avoid context overhead
* **Use `System.Threading.Channels`** for producer-consumer scenarios:
  ```csharp
  var channel = Channel.CreateBounded<Event>(1000);
  ```

### Parallel Processing

* **Use `System.Threading.Tasks.Parallel`** for CPU-bound operations:
  ```csharp
  Parallel.ForEach(events, new ParallelOptions {
      MaxDegreeOfParallelism = Environment.ProcessorCount
  }, ProcessEvent);
  ```
* **Use concurrent collections** from `System.Collections.Concurrent`
* **Use `await using`** for asynchronous resource cleanup:
  ```csharp
  await using var connection = await GetConnectionAsync();
  ```

## Trimming and Deployment

### Native AOT Optimization

* **Design for Native AOT compatibility** by avoiding reflection:
  ```csharp
  // Instead of: typeof(T).GetProperty("Name")
  // Use: Source generators for metadata
  ```
* **Use trimming annotations** to guide the trimmer:
  ```csharp
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
  public Type EventType { get; set; }
  ```
* **Mark assemblies** with appropriate trimming attributes
* **Use PublishTrimmed and PublishAot** MSBuild properties:
  ```xml
  <PropertyGroup>
    <PublishAot>true</PublishAot>
    <PublishTrimmed>true</PublishTrimmed>
  </PropertyGroup>
  ```

## Profiling and Measurement

### Performance Monitoring

* **Use BenchmarkDotNet** for accurate performance measurements:
  ```csharp
  [Benchmark]
  [MemoryDiagnoser]
  public void ProcessEvents() {
      // Benchmark code
  }
  ```
* **Profile for database N+1 queries** and use eager loading
* **Monitor allocation rates** in production using ETW or Application Insights
* **Use `System.Diagnostics.Activity`** for distributed tracing

### Optimization Workflow

1. **Measure first** - Profile before optimizing
2. **Focus on hot paths** - Optimize the 20% that uses 80% of resources
3. **Validate improvements** - Measure after optimization
4. **Consider readability trade-offs** - Document complex optimizations

## ? Domain-Specific Patterns

### High-Throughput Scenarios

* **Batch operations** when possible to reduce overhead
* **Use appropriate read strategies** based on expected data volume
* **Consider caching** for frequently accessed data
* **Optimize serialization** especially for large payloads
* **Use connection pooling** for network-intensive operations
* **Implement backpressure** for real-time streaming scenarios

### Memory-Intensive Operations

* **Stream large datasets** instead of loading entirely into memory
* **Use lazy evaluation** for expensive computations
* **Implement pagination** for large result sets
* **Consider compression** for large data structures stored in memory
