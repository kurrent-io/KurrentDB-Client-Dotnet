# ServiceOperationError Source Generator

This document describes the ultra-clean source generator approach for creating error types with automatic protobuf annotation resolution.

## Overview

The `[ServiceOperationError]` attribute combined with the source generator provides the cleanest possible way to define error types that automatically implement `IResultError` with protobuf annotation support.

## Basic Usage

### What You Write (Ultra-Clean)

```csharp
[ServiceOperationError(typeof(Types.StreamNotFound))]
public readonly partial record struct StreamNotFound;

[ServiceOperationError(typeof(Types.AccessDenied))]
public readonly partial record struct AccessDenied;
```

That's it! Just one line per error type.

### What Gets Generated Automatically

```csharp
public readonly partial record struct StreamNotFound(Metadata? Metadata = null) : IResultError {
    private static readonly Lazy<(string Code, string Message, bool IsFatal)> _errorInfo = new(static () => {
        try {
            // Get protobuf message type and extract annotations at runtime (cached)
            var messageType = Types.StreamNotFound;
            var descriptor = messageType.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as MessageDescriptor;
            if (descriptor?.GetOptions()?.HasExtension(CoreExtensions.ErrorInfo) == true) {
                var annotations = descriptor.GetOptions().GetExtension(CoreExtensions.ErrorInfo);
                return (annotations.Code, annotations.HasMessage ? annotations.Message : "The specified stream was not found.", annotations.Severity == ErrorAnnotations.Types.Severity.Fatal);
            }
        } catch { }
        return ("STREAM_NOT_FOUND", "The specified stream was not found.", false);
    });

    public string ErrorCode => _errorInfo.Value.Code;                    // "STREAM_NOT_FOUND"
    public string ErrorMessage => _errorInfo.Value.Message;              // From protobuf annotation  
    public bool IsFatal => _errorInfo.Value.IsFatal;                     // false (RECOVERABLE)
    
    public Exception CreateException(Exception? innerException = null) =>
        new KurrentClientException(ErrorCode, ErrorMessage, Metadata ?? new Metadata(), innerException);
        
    public override string ToString() => ErrorMessage;
}
```

## Key Features

### ✅ Ultra-Clean Syntax
- **Before**: 6-10 lines of boilerplate per error
- **After**: 1 line per error
- 90%+ reduction in code

### ✅ Automatic Implementation
- `IResultError` interface automatically implemented
- All properties and methods generated
- Constructor with `(Metadata? Metadata = null)` parameter
- Exception creation and ToString() methods

### ✅ Performance Optimized
- Lazy initialization of annotation data
- Static caching (resolved once per type)
- Value types (zero heap allocation for error instances)
- Direct field access after caching

### ✅ Protobuf Integration
- Automatic resolution of `error_info` annotations
- ErrorCode, ErrorMessage, and Severity from protobuf
- Graceful fallbacks for missing annotations

### ✅ Full Result Pattern Support
```csharp
Result<string, StreamNotFound> result = new StreamNotFound();
if (result.IsError) {
    result.AsError.Throw(); // Full IResultError functionality
}
```

## Usage Examples

### Creating Errors
```csharp
// Simple creation
var error = new StreamNotFound();

// With metadata
var metadata = new Metadata { ["userId"] = "12345" };
var errorWithMetadata = new StreamNotFound(metadata);
```

### Accessing Properties
```csharp
var error = new StreamNotFound();

// All properties resolved from protobuf annotations
Console.WriteLine(error.ErrorCode);    // "STREAM_NOT_FOUND"
Console.WriteLine(error.ErrorMessage); // "The specified stream was not found."
Console.WriteLine(error.IsFatal);      // false

// String representation
Console.WriteLine(error);              // "The specified stream was not found."
```

### Exception Handling
```csharp
var error = new StreamNotFound();

// Create exception
var exception = error.CreateException();

// Or throw directly (using IResultError.Throw extension)
error.Throw(); // Throws KurrentClientException
```

### Result Pattern Integration
```csharp
public Result<string, StreamNotFound> GetStreamData(string streamName) {
    if (!StreamExists(streamName)) {
        return new StreamNotFound();
    }
    
    return GetData(streamName);
}

// Usage
var result = GetStreamData("nonexistent");
if (result.IsError) {
    Console.WriteLine($"Error: {result.AsError.ErrorMessage}");
}
```

## Performance Characteristics

### Memory Usage
- **Error instances**: Zero heap allocation (value types)
- **First property access**: One-time annotation resolution with caching
- **Subsequent access**: Direct cached field access (near-zero overhead)

### Speed
- **Error creation**: Instant (value type construction)
- **Property access**: Microseconds after caching
- **Exception creation**: Standard exception construction cost

### Caching
```csharp
// First access resolves and caches annotations
var error1 = new StreamNotFound();
var code = error1.ErrorCode; // Cache miss - resolves protobuf

// All subsequent accesses are blazing fast
for (int i = 0; i < 1000; i++) {
    var error = new StreamNotFound(); // No allocation
    var fastCode = error.ErrorCode;   // Cache hit - direct access
}
```

## Advanced Features

### Metadata Support
Every error automatically gets a consistent metadata parameter:

```csharp
var metadata = new Metadata {
    ["operation"] = "append",
    ["stream"] = "my-stream",
    ["user"] = "admin"
};

var error = new StreamNotFound(metadata);
var exception = error.CreateException(); // Metadata included in exception
```

### Error Severity
Protobuf annotations can define error severity:

```csharp
var error = new StreamNotFound();
if (error.IsFatal) {
    // Requires session termination
    TerminateSession();
} else {
    // Recoverable error - can continue
    RetryOperation();
}
```

## Protobuf Annotation Schema

The source generator reads annotations defined in your protobuf files:

```protobuf
message StreamNotFound {
  option (error_info) = {
    code: "STREAM_NOT_FOUND"
    severity: RECOVERABLE
    message: "The specified stream was not found."
  };
}
```

## Fallback Behavior

If protobuf annotations are missing or unavailable:

1. **Error Code**: Generated from struct name (e.g., `StreamNotFound` → `"STREAM_NOT_FOUND"`)
2. **Error Message**: Generic default based on struct name
3. **Severity**: Defaults to recoverable (non-fatal)

## Best Practices

### ✅ DO
- Use descriptive struct names (they become fallback error codes)
- Include metadata for debugging and tracing
- Test both annotation-based and fallback behavior
- Use consistent constructor patterns across all errors

### ❌ DON'T
- Add custom constructors (interferes with source generation)
- Implement IResultError manually (source generator handles this)
- Cache error instances (they're value types - create as needed)
- Modify generated code (it's overwritten on rebuild)

## Migration from Old Patterns

### Before (Manual Implementation)
```csharp
public readonly record struct StreamNotFound(string Stream) : IKurrentClientError {
    public string ErrorCode => nameof(StreamNotFound);
    public string ErrorMessage => $"Stream '{Stream}' not found.";
    
    public Exception CreateException(Exception? innerException = null) =>
        new KurrentClientException(ErrorCode, ErrorMessage, new Metadata(), innerException);
}
```

### After (Source Generator)
```csharp
[ServiceOperationError(typeof(Types.StreamNotFound))]
public readonly partial record struct StreamNotFound;
```

**Benefits**: 75% less code, automatic protobuf integration, consistent API, better performance.

## Troubleshooting

### Source Generator Not Running
1. Verify MSBuild analyzer reference is correct
2. Check that `ServiceOperationErrorAttribute` is accessible
3. Ensure protobuf message types are available at compile time
4. Clean and rebuild solution

### Missing Annotations
- Source generator provides fallbacks for missing protobuf annotations
- Check protobuf file syntax and extension usage
- Verify `error_info` extension is properly defined

### Compilation Errors
- Ensure struct is declared as `readonly partial record struct`
- Use `[ServiceOperationError(typeof(ProtobufType))]` attribute
- Don't add custom constructors or IResultError implementation

## Performance Testing

The solution includes comprehensive performance tests that verify:

- Zero allocation after caching
- Sub-millisecond property access
- Thread-safe concurrent access
- Minimal memory footprint

Run tests with:
```bash
dotnet test test/Kurrent.Client.Tests/SourceGenerator/ErrorPerformanceTests.cs
```