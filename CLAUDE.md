# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

KurrentDB .NET Client is an event-sourcing database client library built for .NET 8.0/9.0. This library provides a production-ready interface for .NET applications to interact with KurrentDB - an event-native database purpose-built for event sourcing.

## Key Architecture

- **V1 API**: Legacy gRPC-based client with support for streams, persistent subscriptions, projection management, operations, and user management
- **V2 API**: Modern client architecture with improved performance, type safety, and schema registry integration
- **Dual Namespace**: The project is transitioning from `KurrentDB.Client` to `Kurrent.Client` namespace
- **Protocol Buffers**: Uses gRPC with protobuf for v1 and v2 protocol definitions in `/proto`
- **Event Sourcing**: Append-only, immutable event storage with stream-based organization
- **Schema Registry**: Advanced schema management and serialization in V2

## Build and Development Commands

### Building the Solution
```bash
dotnet build KurrentDB.Client.sln
dotnet build samples/Samples.sln
```

### Running Tests
```bash
# All tests
dotnet test

# Specific test project
dotnet test test/KurrentDB.Client.Tests/KurrentDB.Client.Tests.csproj
dotnet test test/Kurrent.Client.Tests/Kurrent.Client.Tests.csproj

# Run tests with specific framework
dotnet test --framework net8.0
dotnet test --framework net9.0
```

### Test Infrastructure
- **Primary Test Framework**: TUnit (https://tunit.dev/docs/intro) - modern, fast testing framework
- **Legacy Test Framework**: xUnit (being phased out in favor of TUnit)
- **Assertions**: Shouldly (https://docs.shouldly.org/) for fluent assertions
- **Mocking**: FakeItEasy (https://fakeiteasy.github.io/docs/8.3.0/) for test doubles
- **Test Containers**: Docker-based KurrentDB instances using FluentDocker
- **Test Data**: Bogus for realistic test data generation, AutoFixture for object creation

### Development Environment
```bash
# Generate TLS certificates for secure testing
./gencert.sh  # Unix/macOS
./gencert.ps1 # Windows PowerShell

# Run samples (requires KurrentDB server)
cd samples/quick-start
dotnet run
```

## Code Organization

### Source Structure
- `src/KurrentDB.Client/` - Main client library
  - `V1/` - Legacy API implementation (streams, subscriptions, operations, etc.)
  - `V2/` - Modern API with improved performance and schema registry
  - `Internal/` - Shared utilities, diagnostics, and extensions

### Test Structure
- `test/KurrentDB.Client.Tests/` - Legacy API tests
- `test/Kurrent.Client.Tests/` - Modern API tests  
- `test/KurrentDB.Client.Tests.Common/` - Shared test infrastructure
- `test/Kurrent.Client.Testing/` - Modern test toolkit

### Protocol Definitions
- `proto/kurrentdb/protocol/v1/` - V1 gRPC protocol definitions
- `proto/kurrentdb/protocol/v2/` - V2 enhanced protocol with dynamic values and features

## Development Guidelines

### Event Sourcing Principles
- Events are immutable facts representing state changes
- Streams organize related events logically
- Use optimistic concurrency control for handling conflicts
- The global event log maintains consistent ordering

### Client Usage Patterns
1. Configure client settings and connection
2. Connect to KurrentDB server/cluster
3. Append events to streams or read existing events
4. Handle responses and manage subscriptions
5. Implement proper error handling and retries

### Performance Principles
- **Memory Management**: Minimize allocations in hot paths, use value types for small data
- **Collections**: Initialize with known capacity, use `ArrayPool<T>` for temporary arrays
- **High-Performance Types**: Use `Memory<T>`, `Span<T>`, `ReadOnlySpan<char>` for contiguous memory operations
- **Async Operations**: Use `ValueTask` for often-synchronous operations, `ConfigureAwait(false)` in library code
- **Closures**: Avoid in performance-critical paths, use static lambdas when possible
- **Logging**: Use source generators with `LoggerMessageAttribute` for high-performance logging
- **KurrentDB-Specific**: Batch event appends, use connection pooling, cache stream metadata
- **Advanced**: Consider Native AOT compatibility, use SIMD operations for numeric processing

### Code Standards
- **Language**: Use latest C# features (C# 14) with experimental features enabled
- **Namespaces**: Use file-scoped namespace declarations
- **Usings**: Place using directives outside namespaces, sorted alphabetically  
- **Types**: Use record types for immutable data, prefer properties over fields
- **Patterns**: Use pattern matching, switch expressions, and modern C# syntax
- **Async**: Follow async-first patterns with `ConfigureAwait(false)` in library code
- **Nullability**: Enable nullable reference types, use `is null`/`is not null` patterns
- **API Design**: Maintain backward compatibility, use fluent interfaces for configuration

### Pattern Analysis Implementation Examples

**Mandatory Pre-Implementation Protocol:**
Before implementing any new component, complete this analysis:

**Step 1: File Selection**
- Modern API: `src/Kurrent.Client/Streams/KurrentStreamsClient.cs`
- Legacy API: `src/KurrentDB.Client/KurrentDBClient.cs` 
- Similar component: Choose files in same architectural layer

**Step 2: Pattern Documentation**
Document findings in this format:
```
"Based on analyzing [KurrentStreamsClient.cs, KurrentRegistryClient.cs], I've identified:
- Naming: PascalCase with 'Kurrent' prefix for modern API classes
- Error Handling: Result<TValue, TError> pattern with IVariantResultError unions
- Async: ConfigureAwait(false) applied consistently in library code
- Organization: Constructor, public properties, public methods, private helpers, inner types
- Documentation: XML docs with <example> blocks for complex functional methods

I will apply these patterns consistently in my implementation."
```

**Step 3: Architecture-Specific Patterns**
- **Modern API (`Kurrent.Client`)**: Result patterns, functional composition, source-generated errors
- **Legacy API (`KurrentDB.Client`)**: Exception-based, traditional async patterns
- **Bridge Components**: Translation between modern and legacy patterns

**Example Analysis Output:**
```
PATTERN ANALYSIS COMPLETE for StreamSubscriptionClient:
- File Type: Modern API client
- Error Pattern: Returns Result<SubscriptionResult, SubscriptionFailure>
- Method Style: Fluent builder with WithOptions() chaining
- Async Pattern: ValueTask with ConfigureAwait(false)
- Testing: TUnit with snake_case naming (subscribe_succeeds_when_stream_exists)
```

### Code Style
- **Braces**: K&R style (opening brace on same line) for all constructs
- **Indentation**: 4 spaces for C#, 2 spaces for XML/JSON/Proto
- **Line Length**: Maximum 160 characters
- **Alignment**: Use vertical alignment for related properties and method chains
- **Documentation**: XML doc comments for all public APIs with examples

## Testing Strategy

### Test Framework Standards
- **Primary**: Use TUnit for all new tests (modern, fast testing framework)
- **Legacy**: xUnit tests exist but migrate to TUnit for new development
- **Assertions**: Use Shouldly for fluent, readable assertions
- **Mocking**: Use FakeItEasy for creating test doubles and mocks

### Test Naming Convention
Use snake_case with pattern: `[what_happens]_when_[condition]`

**Examples:**
```csharp
// Good test names
returns_empty_list_when_no_items_found
throws_argument_exception_when_id_is_zero  
updates_user_status_to_active_when_email_verified
connects_to_server_when_valid_credentials_provided

// Exception testing pattern
throws_[exception_type]_when_[condition]

// State change pattern  
[changes/updates/sets]_[state]_to_[value]_when_[condition]
```

### Test Structure
- Use Arrange-Act-Assert pattern (without comments)
- Focus on single behavior per test
- Test both happy paths and edge cases
- Keep tests deterministic and environment-independent

### Integration Tests
- Docker-based KurrentDB test containers for realistic testing
- Tests cover both secure (TLS) and insecure connections
- Cross-platform compatibility testing
- Clean up test data after each test

### Test Configuration
- Uses appsettings.json for test configuration
- Docker Compose for complex test scenarios
- Certificate management for TLS testing

### Running Specific Test Categories
```bash
# Run only integration tests
dotnet test --filter "Category=Integration"

# Run specific test class
dotnet test --filter "FullyQualifiedName~StreamsTests"
```

## Memory and Persistence Protocol

### Working Memory vs File Updates
- **Working Memory**: Information from instruction files (CLAUDE.md, CLAUDE.PROJECT.md, core-prompt.md) persists only within individual conversation sessions
- **No Auto-Updates**: Claude does not automatically update instruction files during conversations
- **Memory Refresh**: Use explicit "REMEMBER" or "RECALL" commands to re-read instructions when needed
- **Context Loading**: Instructions are loaded at conversation start, not modified during sessions
- **File Modifications**: Only occur when explicitly requested and approved by the user

### Memory Refresh Protocol
When instructed to **"REMEMBER"** or **"RECALL"**:
- Immediately re-read CLAUDE.md, CLAUDE.PROJECT.md, and core-prompt.md
- Review complete project context and architectural patterns
- Pay special attention to dual API architecture and Result patterns
- Explicitly acknowledge completion of refresh process
- Apply refreshed understanding to current task

## Dependencies and Tools

### Core Dependencies
- **Grpc.Net.Client**: gRPC communication
- **Google.Protobuf**: Protocol buffer serialization  
- **OpenTelemetry.Api**: Observability and tracing
- **NJsonSchema**: Schema generation and validation
- **OneOf**: Union types for result handling

### Development Dependencies
- **TUnit**: Modern testing framework (preferred for new tests)
- **xUnit**: Legacy testing framework (being phased out)
- **Shouldly**: Fluent assertions for tests
- **FakeItEasy**: Mocking framework for test doubles
- **FluentDocker**: Container management for tests
- **Bogus**: Test data generation
- **AutoFixture**: Object creation for tests

## Class Structure and Organization

### Member Ordering
1. Constructors first
2. Public properties with accessors  
3. Private/internal properties with accessors
4. Public methods
5. Private/internal methods
6. Inner types (records, enums, etc.) at the end

### Examples of Preferred Patterns

**Vertical Alignment for Readability:**
```csharp
public record StreamOptions {
    public int             MaxEvents       { get; init; } = 100;
    public int             StartPosition   { get; init; } = 0;
    public bool            IncludeMetadata { get; init; } = true;
    public StreamDirection Direction       { get; init; } = StreamDirection.Forward;
}
```

**Method Chaining:**
```csharp
var response = await Connection
    .ReadStreamAsync(Id.Value, Options.MaxEvents, cancellationToken)
    .ConfigureAwait(false);
```

**Performance-Conscious Lambda Usage:**
```csharp
// Avoid closures in hot paths - pass context explicitly
var filtered = items.Where(static (x, threshold) => x.Value > threshold, 100);
```

## Architectural Patterns (Updated: 2025-06-23)

### Dual API Architecture Pattern (Critical)
- **Modern API (`Kurrent.Client`)**: Production-ready functional API with `Result<TValue, TError>` patterns
- **Legacy API (`KurrentDB.Client`)**: Exception-based API maintained for backward compatibility
- **Bridge Pattern**: `KurrentDBLegacyCallInvoker` enables modern API to use legacy infrastructure
- **Usage Guidelines**: Always use `Kurrent.Client` for new development, specify which API when discussing issues
- **Communication Pattern**: When reporting errors, clarify if they're from modern Result patterns or legacy exceptions

**Example Result Pattern Communication:**
```csharp
// Modern API - functional error handling
var result = await client.Streams.AppendAsync(streamName, messages);
result.Match(
    success => HandleSuccess(success.StreamRevision),
    error => error.Switch(
        streamNotFound => HandleStreamNotFound(),
        accessDenied => HandleAccessDenied(),
        expectedRevisionMismatch => HandleRevisionMismatch()
    )
);

// vs Legacy API - exception-based
try {
    await legacyClient.AppendToStreamAsync(streamName, StreamRevision.NoStream, eventData);
} catch (WrongExpectedVersionException ex) {
    // Handle exception
}
```

### Lock/Unlock Immutability Pattern
- **Description**: Explicit immutability control with mutable copy creation
- **Rationale**: User preference for explicit operations over implicit copy-on-write for clarity
- **Implementation**: Boolean `_isLocked` field + `Lock()` method + `CreateUnlockedCopy()` method
- **Usage Guidelines**: Use for shared data structures that need mutation protection
- **Error Handling**: Throw `InvalidOperationException` with message: "Cannot modify locked metadata. Use CreateUnlockedCopy() to create a mutable copy."
- **Examples**:
```csharp
var metadata = new Metadata().With("key", "value").Lock();
var mutable = metadata.CreateUnlockedCopy().With("new", "value");
```

### Result Pattern & Error Handling Architecture (Production Standard)
- **IResultError Interface**: All errors implement `IResultError` with `ErrorCode`, `ErrorMessage`, `IsFatal`
- **Source-Generated Variants**: Use `IVariantResultError` for type-safe discriminated unions
- **Result Type Usage**: Use `Result.Success<T, E>()` and `Result.Failure<T, E>()` factory methods
- **Fluent Chaining**: Use `.OnSuccess()`, `.OnError()`, `.ThrowOnError()` for pipeline operations
- **Async Support**: Use async variants: `OnSuccessAsync()`, `OnErrorAsync()`, `ThrowOnErrorAsync()`
- **Pattern Matching**: Use `.Match()` and `.MatchAsync()` for handling both success and error cases

**Comprehensive Result Pattern Examples:**
```csharp
// Source-generated error variants (from KurrentOperationErrorGenerator)
[GenerateVariant]
public partial record struct AppendStreamFailure : IVariantResultError {
    [Variant] public static implicit operator AppendStreamFailure(StreamNotFoundError error) => new(error);
    [Variant] public static implicit operator AppendStreamFailure(AccessDeniedError error) => new(error);
    [Variant] public static implicit operator AppendStreamFailure(ExpectedRevisionMismatchError error) => new(error);
}

// Modern API usage with exhaustive error handling
var appendResult = await kurrentClient.Streams.AppendAsync(streamName, messages, ct);
var finalResult = appendResult
    .OnSuccess(success => LogSuccess($"Appended to revision {success.StreamRevision}"))
    .OnError(error => error.Switch(
        streamNotFound => Result.Failure<Unit, AppendStreamFailure>(
            new ValidationError("Stream must be created first")),
        accessDenied => Result.Failure<Unit, AppendStreamFailure>(
            new SecurityError("Insufficient permissions for stream")),
        revisionMismatch => Result.Failure<Unit, AppendStreamFailure>(
            new ConcurrencyError($"Expected revision {revisionMismatch.ExpectedRevision}, got {revisionMismatch.ActualRevision}"))
    ));

// Async composition patterns
var result = await appendResult
    .OnSuccessAsync(async success => await UpdateProjectionAsync(success.StreamRevision))
    .OnErrorAsync(async error => await LogErrorAsync(error))
    .ConfigureAwait(false);

// Exception bridge for legacy compatibility
try {
    var value = result.ThrowOnError(); // Converts Result failures to KurrentClientException
    return value;
} catch (KurrentClientException ex) when (ex.ErrorCode == "STREAM_NOT_FOUND") {
    // Handle specific error type
}
```

## Advanced Testing Patterns (Updated: 2025-06-23)

### TUnit Framework Migration (Production Standard)
- **Primary Framework**: TUnit for all new development (production standard since 2025)
- **Legacy Coexistence**: xUnit tests remain for backward compatibility, but no new xUnit development
- **Test Naming**: Use snake_case format: `[what_happens]_when_[condition]`
- **Required Attributes**: `[Test]`, `[Timeout(60000)]` for long-running tests
- **Container Testing**: Docker-based KurrentDB containers via `KurrentDBTestContainer`

**TUnit Test Structure Example:**
```csharp
public class StreamTests : KurrentClientTestFixture {
    [Test]
    [Timeout(30000)]
    public async Task append_succeeds_when_stream_exists(CancellationToken ct) {
        // Arrange
        var streamName = $"test-stream-{Guid.NewGuid()}";
        var message = Message.New().WithValue(new UserRegistered(Guid.NewGuid(), "test@example.com")).Build();
        
        // Act  
        var result = await Client.Streams.AppendAsync(streamName, [message], ct);
        
        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.StreamRevision.ShouldBeGreaterThan(StreamRevision.None);
    }
}
```

### Docker Container Testing (Advanced Integration)
- **KurrentDBTestContainer**: Standardized containerized test infrastructure
- **Container Lifecycle**: Managed automatically via `KurrentClientTestFixture` base class
- **TLS Testing**: Both secure and insecure connection patterns supported
- **Cross-Platform**: Consistent testing via Docker containers across development environments

### Result Pattern Testing
- **Success Path Testing**: Use `.IsSuccess.ShouldBeTrue()` and `.Value.ShouldBe(expected)`
- **Error Path Testing**: Use `.IsFailure.ShouldBeTrue()` and `.Error.Switch()` for variant testing
- **Async Result Testing**: Use `ShouldNotThrowAsync()` for Result-based async operations

**Result Pattern Test Examples:**
```csharp
[Test]
public async Task append_fails_when_stream_deleted(CancellationToken ct) {
    var result = await Client.Streams.AppendAsync("deleted-stream", messages, ct);
    
    result.IsFailure.ShouldBeTrue();
    result.Error.Switch(
        streamNotFound => streamNotFound.StreamName.ShouldBe("deleted-stream"),
        accessDenied => throw new InvalidOperationException("Expected StreamNotFound"),
        revisionMismatch => throw new InvalidOperationException("Expected StreamNotFound")
    );
}
```

## Source Generator Integration Patterns (New: 2025-06-23)

### KurrentOperationErrorGenerator Communication
- **Purpose**: Generates C# error types from Protobuf `ErrorDetails` annotations
- **Location**: Source generator output in `obj/` directories, consumed via partial classes
- **Communication Pattern**: Always mention both the Protobuf source and generated C# type
- **Debugging**: Include both compilation errors and runtime behavior when reporting issues

**Example Source Generator Issue Communication:**
```csharp
// Problem: KurrentOperationErrorGenerator not creating StreamNotFoundError from proto definition
// Protobuf Source: proto/v2/streams/shared.proto line 45
message StreamNotFoundError {
  option (error_details) = {
    error_code: "STREAM_NOT_FOUND"
    error_message: "Stream '{stream_name}' was not found"
  };
  string stream_name = 1;
}

// Expected Generated C# (not appearing):
public readonly record struct StreamNotFoundError(string StreamName) : IVariantResultError {
  public string ErrorCode => "STREAM_NOT_FOUND";
  public string ErrorMessage => $"Stream '{StreamName}' was not found";
  public bool IsFatal => false;
}
```

### VariantGenerator Communication Patterns  
- **Purpose**: Creates discriminated union boilerplate for `IVariantResultError` types
- **Attributes**: `[GenerateVariant]` on partial record struct, `[Variant]` on implicit operators
- **Generated Code**: Switch methods, pattern matching, and type-safe operations
- **Communication**: Include both the partial definition and expected generated methods

**Example Variant Generator Issue Communication:**
```csharp
// Partial definition in source:
[GenerateVariant]
public partial record struct AppendStreamFailure : IVariantResultError {
    [Variant] public static implicit operator AppendStreamFailure(StreamNotFoundError error) => new(error);
    [Variant] public static implicit operator AppendStreamFailure(AccessDeniedError error) => new(error);
}

// Expected generated Switch method (missing):
public TResult Switch<TResult>(
    Func<StreamNotFoundError, TResult> onStreamNotFound,
    Func<AccessDeniedError, TResult> onAccessDenied) {
    // Generated implementation
}
```

### Source Generator Debugging Guidelines
- **Build Output**: Check `obj/Debug/net8.0/generated/` for actual generated files
- **Compilation Issues**: Include both source generator errors and resulting compilation failures
- **Runtime Issues**: Distinguish between source generator problems vs runtime Result pattern issues
- **Performance**: Source generators run at compile-time, so mention build-time vs runtime context

## Error Handling Best Practices (Updated: 2025-06-23)

### Exception Creation Patterns
- **Structured Messages**: Consistent error message format with context
- **Inner Exception Support**: Always support inner exception chaining
- **Metadata Preservation**: Include relevant context in error metadata
- **Fail-Fast Philosophy**: Use `.ThrowOnError()` for immediate failure when appropriate

### ServiceOperationError Usage
- **Base Class**: Inherit from `ServiceOperationError` abstract record for domain errors
- **Protobuf Integration**: Errors derived from protobuf message annotations
- **Metadata Support**: Use `Action<Metadata>` constructor parameter for configurable metadata
- **Annotation Resolution**: Leverage compile-time error code/message resolution

## Documentation Standards (Added: 2025-01-19)

### Functional Programming Documentation Standards
- **Code Examples Required**: All functional programming methods must include realistic `<example>` blocks
- **Scenario Selection**: Use familiar business concepts (users, orders, files, APIs) rather than abstract examples  
- **Error Handling**: Always show both success and error paths in examples
- **Async Patterns**: Demonstrate proper `ConfigureAwait(false)` usage in all async examples
- **Migration Focus**: Show benefits over traditional exception-based approaches
- **Variable Naming**: Use meaningful domain names (`user`, `order`) not generic names (`x`, `item`)

### Documentation Philosophy for Complex APIs
- **Education Over Brevity**: Complex functional APIs need comprehensive examples showing complete usage
- **Real-World Context**: Every example should solve a recognizable business problem
- **Pattern Demonstration**: Show chaining, error handling, and async composition in context
- **Migration Assistance**: Examples explicitly contrast with imperative/exception-based approaches
- **Complete Workflows**: Show multi-step processes, not just single method calls

### Result Type Error Handling Examples
- **Exception Mapping**: Use pattern matching in Try methods for specific exception types
- **Error Transformation**: Show MapError and MapErrorAsync for user-friendly error conversion
- **Async Error Patterns**: Demonstrate proper async error handling with ConfigureAwait(false)
- **Business Error Types**: Use domain-specific error types (ValidationError, ApiError, DatabaseError)
- **Chaining Error Context**: Show how errors propagate through Result chains

### XML Documentation Quality Standards
- **Generic Type Docs**: Always document generic type parameters with business context
- **Parameter Context**: Explain not just what parameters do, but when they're used
- **Return Value Details**: Describe success and failure return scenarios
- **Example Integration**: `<example>` blocks are required for complex functional methods
- **Cross-References**: Use `<see cref="">` for proper IntelliSense integration

### Async Method Documentation Standards
- **ConfigureAwait Consistency**: Every async example must use `.ConfigureAwait(false)`
- **Async Composition**: Show how async Result methods chain together
- **ValueTask Usage**: Document when and why ValueTask is preferred over Task
- **Exception Handling**: Show async exception conversion patterns
- **State Management**: Document async methods with state parameters clearly

## Task Management

### TodoWrite Integration Protocol
Use TodoWrite extensively for all tasks not only the complex ones

**Task Categories for KurrentDB Client:**
1. **Modern API Development** (`Kurrent.Client`)
   - Result<T,E> pattern implementation
   - Source-generated error variant creation
   - Functional composition and chaining
   - Schema registry integration

2. **Legacy API Maintenance** (`KurrentDB.Client`)
   - Exception-based pattern preservation
   - Bridge compatibility validation
   - Backward compatibility testing

3. **Cross-Cutting Concerns**
   - Performance optimization (hot paths, memory allocation)
   - TUnit test creation with Docker containers
   - gRPC protocol handling (v1/v2)
   - Documentation with functional examples

### Example TodoWrite Structure for Dual API Feature:
```
todos: [
  {
    id: "1",
    content: "Analyze pattern in KurrentStreamsClient.cs for append operations",
    status: "in_progress", 
    priority: "high"
  },
  {
    id: "2", 
    content: "Implement AppendAsync with Result<AppendResult, AppendStreamFailure> pattern",
    status: "pending",
    priority: "high"
  },
  {
    id: "3",
    content: "Add source-generated error variants (StreamNotFound, AccessDenied, RevisionMismatch)",
    status: "pending", 
    priority: "medium"
  },
  {
    id: "4",
    content: "Create TUnit tests with snake_case naming and Docker containers",
    status: "pending",
    priority: "medium"
  },
  {
    id: "5",
    content: "Validate legacy API bridge compatibility",
    status: "pending",
    priority: "low"
  }
]
```

### Task Completion Criteria by API Type:
- **Modern API**: Result pattern functional, source generators working, TUnit tests passing, performance validated
- **Legacy API**: Exception handling preserved, bridge translation working, existing tests unbroken  
- **Integration**: Both APIs tested, documentation complete, performance benchmarks passing

## Important Notes

- Uses preview C# language features (C# 14) and .NET 8.0/9.0
- Performance optimizations enabled (TieredCompilation, etc.)
- InternalsVisibleTo configured for test assemblies
- Protocol buffer files generate internal classes for encapsulation  
- Transitioning from KurrentDB.Client to Kurrent.Client namespace
- Never modify global.json, package.json, or NuGet.config without explicit permission


# Using Gemini CLI for Large Codebase Analysis

When analyzing large codebases or multiple files that might exceed context limits, use the Gemini CLI with its massive
context window. Use `gemini -p` to leverage Google Gemini's large context capacity.

## File and Directory Inclusion Syntax

Use the `@` syntax to include files and directories in your Gemini prompts. The paths should be relative to WHERE you run the
gemini command:

### Examples:

**Single file analysis:**
gemini -p "@src/main.py Explain this file's purpose and structure"

Multiple files:
gemini -p "@package.json @src/index.js Analyse the dependencies used in the code"

Entire directory:
gemini -p "@src/ Summarize the architecture of this codebase"

Multiple directories:
gemini -p "@src/ @tests/ Analyse test coverage for the source code"

Current directory and subdirectories:
gemini -p "@./ Give me an overview of this entire project"

# Or use --all_files flag:
gemini --all_files -p "Analyse the project structure and dependencies"

Implementation Verification Examples

Check if a feature is implemented:
gemini -p "@src/ @lib/ Has dark mode been implemented in this codebase? Show me the relevant files and functions"

Verify authentication implementation:
gemini -p "@src/ @middleware/ Is JWT authentication implemented? List all auth-related endpoints and middleware"

Check for specific patterns:
gemini -p "@src/ Are there any React hooks that handle WebSocket connections? List them with file paths"

Verify error handling:
gemini -p "@src/ @api/ Is proper error handling implemented for all API endpoints? Show examples of try-catch blocks"

Check for rate limiting:
gemini -p "@backend/ @middleware/ Is rate limiting implemented for the API? Show the implementation details"

Verify caching strategy:
gemini -p "@src/ @lib/ @services/ Is Redis caching implemented? List all cache-related functions and their usage"

Check for specific security measures:
gemini -p "@src/ @api/ Are SQL injection protections implemented? Show how user inputs are sanitized"

Verify test coverage for features:
gemini -p "@src/payment/ @tests/ Is the payment processing module fully tested? List all test cases"

## When to Use Gemini CLI

Use gemini -p when:
- Analyzing entire codebases or large directories
- Comparing multiple large files
- Need to understand project-wide patterns or architecture
- The current context window is not enough for the task
- Working with files totalling more than 100KB
- Verifying if specific features, patterns, or security measures are implemented
- Checking for the presence of certain coding patterns across the entire codebase

### Important Notes

- Paths in @ syntax are relative to your current working directory when invoking gemini
- The CLI will include file contents directly in the context
- No need for --yolo flag for read-only analysis
- Gemini's context window can handle entire codebases that would overflow Claude's context
- When checking implementations, be specific about what you're looking for to get accurate results
