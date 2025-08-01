# KurrentDB .NET Client - Project Instructions

**Last Updated**: 2025-06-23

> **Claude-Specific Instructions**: This file provides project-specific context, patterns, and guidelines optimized for Claude Code AI assistant interactions with the KurrentDB .NET Client repository.

## Version Information

**Instructions Version**: v3.1.0
**Project Phase**: Production/Advanced Dual Architecture
**Last Updated**: 2025-06-27

### **Changelog**
- **v3.1.0** (2025-06-27): Added comprehensive Source Generator Debugging Protocol with 5-step workflow, communication patterns, and troubleshooting examples for KurrentOperationErrorGenerator and VariantGenerator issues.
- **v3.0.0** (2025-06-23): Major update for 2025 architectural patterns. Enhanced dual API guidance, Result pattern communication, source generator integration, TUnit migration, and Claude-specific interaction patterns.
- **v2.1.0** (2024-07-23): Added in-depth details about the dual API architecture (`Kurrent.Client` vs. `KurrentDB.Client`), the role of source generators, and the testing strategy. Refined all sections with more specific examples and patterns.
- **v2.0.0** (2024-07-23): Initial generation of project instructions.


### **Critical 2025 Architectural Updates**
- **Source Generators**: `KurrentOperationErrorGenerator` and `VariantGenerator` are now core to error handling
- **Testing Migration**: TUnit is the primary framework, xUnit is legacy
- **Result Patterns**: Functional error handling with `Result<TValue, TError>` is standard
- **Dual API Maturity**: Modern `Kurrent.Client` is production-ready, legacy `KurrentDB.Client` bridge maintained

## Project Overview

**KurrentDB .NET Client** provides a production-ready, high-performance .NET SDK for interacting with KurrentDB, an event-native database engineered for modern, event-driven architectures. This library is undergoing a transition to a modern, fluent API (`Kurrent.Client`) that improves ergonomics and type-safety, while maintaining a stable legacy API (`KurrentDB.Client`) for backward compatibility.

### Key Features

- **Dual API for Smooth Transition**: A modern, fluent `Kurrent.Client` API for new development, and a stable, legacy `KurrentDB.Client` API for existing applications.
- **Type-Safe, Functional Error Handling**: The modern API uses a `Result<TValue, TError>` type to handle predictable outcomes without exceptions, leveraging source-generated `Variant` types for discriminated error unions.
- **Comprehensive KurrentDB Functionality**: Full support for stream operations (append, read, delete, tombstone), subscriptions (volatile and persistent), server-side projections, and user management.
- **Advanced Schema Registry Integration**: First-class support for schema registration, validation, and serialization/deserialization for JSON and Protobuf formats, with configurable naming strategies.
- **Resilient Cluster Discovery**: Automatic cluster node discovery via a gossip protocol, with configurable node preferences (Leader, Follower, etc.) and connection resilience options.
- **Modern .NET & Observability**: Built on .NET 8/9, leveraging modern C# features like `IAsyncEnumerable`, and providing built-in tracing support via OpenTelemetry.

### Technical Foundation

- **.NET 8.0/9.0**: Built for modern .NET runtimes, leveraging the latest language and framework features.
- **gRPC**: Uses gRPC for high-performance, cross-platform communication with the KurrentDB server. Protocol contracts are defined in `.proto` files (v1 for legacy, v2 for the future).
- **API Design**: The modern API is async-first, fluent, and composable, while the legacy API follows a more traditional, exception-based pattern.
- **Compatibility**: The core library targets modern .NET, while source generators target `netstandard2.0` for broad tooling compatibility.

### Ideal Use Cases

- **Event Sourcing & CQRS**: Building applications using event sourcing as the primary persistence model.
- **Event-Driven Microservices**: Facilitating robust, event-based communication between services.
- **Real-time Data Processing**: Subscribing to streams (including `$all`) for real-time analytics, projections, or process management.
- **Data-Intensive Applications**: High-throughput scenarios requiring efficient batching and asynchronous operations.

### Implementation Goals

To provide a client that is intuitive, performant, and resilient. The ongoing architectural evolution prioritizes a superior developer experience with type safety and predictable error handling, while ensuring a stable migration path for existing users.

---

## Architecture Overview

### Key Architectural Concepts

#### 1. Dual API: Production Modern + Legacy Bridge (2025 Status)
The client maintains two API namespaces with distinct purposes:
*   **`Kurrent.Client` (Modern API)**: **Production-ready** functional API with type-safe error handling. Uses `Result<TValue, TError>` patterns, source-generated error types, and schema registry integration. Designed for fluent, composable operations with explicit error handling.
*   **`KurrentDB.Client` (Legacy API)**: Maintained for backward compatibility. Exception-based error handling with types like `WrongExpectedVersionException`. Used internally by the bridge pattern and for existing client code.

**Bridge Architecture (Mature)**: The `KurrentDBLegacyCallInvoker` and `KurrentDBLegacyConverter` provide stable translation between modern and legacy patterns. This allows `Kurrent.Client` to leverage battle-tested connection management while presenting a modern functional interface.

**Communication Pattern for Claude**: When discussing errors or architectural decisions, always specify which API layer (modern Result-based vs legacy exception-based) you're working with, as patterns differ significantly.

#### 2. Functional & Type-Safe Error Handling
* **`Result<TValue, TError>`**: The modern API's core return type. Instead of `try-catch`, developers use `Match`, `Switch`, `Then`, and `Map` to handle success and failure paths, leading to more predictable and composable code.
* **Source-Generated Error Variants**: Errors from operations are modeled as `IVariantResultError` discriminated unions (e.g., `AppendStreamFailure`). The `VariantGenerator` creates the necessary boilerplate, allowing for compile-time checked, exhaustive error handling. The `KurrentOperationErrorGenerator` links these C# error types to Protobuf `ErrorDetails` annotations.

#### 3. Source-Generated Domain Model (Advanced)
* **Protobuf as Source of Truth**: The `.proto` files in `proto/` define both gRPC contracts and error domain models.
* **KurrentOperationErrorGenerator**: Generates C# error types from Protobuf `ErrorDetails` annotations, ensuring protocol/domain sync.
* **VariantGenerator**: Creates discriminated union types for `IVariantResultError`, enabling compile-time-safe error handling.

**Claude Communication Guide for Source Generators**: 
- When discussing errors, mention if they're source-generated vs hand-written
- Include both the Protobuf definition and generated C# type when explaining error scenarios
- Example: "The `AppendStreamFailure` is source-generated from `proto/v2/streams/shared.proto` with three variants: `StreamNotFound`, `ExpectedRevisionMismatch`, and `AccessDenied`"

### Layered Architecture

1.  **gRPC Communication Layer**: The lowest level, consisting of the `grpc-dotnet` library and the auto-generated C# stubs from the `.proto` files.
2.  **Cluster & Connection Management (Legacy Core)**:
    *   `LegacyClusterClient`: Manages the overall connection state.
    *   `GossipChannelSelector`: Implements the logic for discovering cluster nodes via gossip seeds.
    *   `NodeSelector`: Selects a specific node from the discovered cluster members based on the configured `NodePreference`.
    *   `ChannelCache`: Caches `GrpcChannel` instances to endpoints for reuse.
    *   `SharingProvider`: A key utility that manages the lifecycle of a shared `ChannelInfo` object, handling reconnections when a channel is reported as "broken."
3.  **Modern API Facade (`Kurrent.Client`)**:
    *   `KurrentClient`: The primary entry point.
    *   `KurrentStreamsClient`, `KurrentRegistryClient`: Expose methods for specific feature sets.
    *   **Bridge**: These new clients use the `KurrentDBLegacyCallInvoker`, which wraps the `LegacyClusterClient` to perform gRPC calls. This is the "magic" that allows the new API to use the old connection logic.
    *   **Data Translation**: `KurrentDBLegacyConverter` translates between modern models (e.g., `Kurrent.Client.Model.Message`) and legacy DTOs (`KurrentDB.Client.EventData`).

## Core Components

### Client Entry Points
- **`KurrentClient`**: The main entry point for the modern API. Provides access to `Streams`, `Registry`, and `Features` sub-clients.
- **`KurrentDBClient`**: The main entry point for the legacy streams API. It is still used under the hood by the modern client.

### Configuration
- **`KurrentClientOptionsBuilder`**: The fluent builder for creating `KurrentClientOptions`. This is the recommended way to configure the client.
- **`KurrentClientOptions`**: An immutable record representing the complete configuration for the modern client.
- **`KurrentDBClientSettings`**: The mutable settings class for the legacy client. The `KurrentDBLegacySettingsConverter` translates `KurrentClientOptions` into this format for the underlying connection logic.

### Error Handling
- **`Result<TValue, TError>`**: The functional result type used throughout the modern API.
- **`IVariantResultError`**: Interface for discriminated unions of error types. Error types are defined as partial `readonly record struct` and implemented by a source generator (e.g., `AppendStreamFailure`).

### Cluster & Connection Management
- **`KurrentDBLegacyCallInvoker`**: A `CallInvoker` that intelligently uses the `LegacyClusterClient` to ensure a valid connection is available before each gRPC call.
- **`GossipResolver`**: Implements gRPC's `Resolver` API using KurrentDB's gossip protocol to discover nodes.

## Integration Patterns

### gRPC Communication
* **Protobuf Definitions**: The `proto/v1` and `proto/v2` directories define the service contracts. `v1` is for the legacy client, and `v2` is for the future direction of the modern client.
* **Interceptors**: `TypedExceptionInterceptor` (legacy) translates gRPC `RpcException`s into strongly-typed .NET exceptions. `HeadersInterceptor` injects required headers like `requires-leader`.

### Cluster Discovery (Gossip)
* The client is initialized with one or more gossip seeds (`DnsEndPoint`).
* The `GossipChannelSelector` picks a seed and uses a `GrpcGossipClient` to request the cluster topology.
* The `NodeSelector` filters and sorts the returned members based on their state (`IsAlive`, `VNodeState`) and the user's `NodePreference`.
* A connection is made to the selected node. If the connection fails or is later reported broken, the `SharingProvider` triggers a reconnection, potentially with a full rediscovery cycle.

### Schema Registry
* **`SchemaManager`**: Orchestrates schema registration and validation logic.
* **`ISchemaSerializer`**: Interface for different serialization formats. Implementations like `JsonSchemaSerializer` and `ProtobufSchemaSerializer` handle the specific logic for each format.
* **`ISchemaNameStrategy`**: Interface for defining how a schema name is generated from a .NET type. Implementations like `MessageSchemaNameStrategy` (uses `Type.FullName`) and `CategorySchemaNameStrategy` (derives from stream name) are provided.

## Design Principles

### Immutability & Records
* Core data carriers like `StreamPosition`, `LogPosition`, `EventRecord`, and the new API's configuration objects are immutable `readonly record struct` or `record` types. This ensures thread safety and predictable behavior.

### Fluent and Composable API
* The `KurrentClientOptionsBuilder` provides a clear and discoverable way to configure the client.
* The `Result<T, E>` type's methods (`Then`, `Map`, `OnSuccess`) enable a declarative, functional style for composing complex workflows.

### Separation of Concerns
* The new API separates concerns into logical clients: `KurrentStreamsClient`, `KurrentRegistryClient`, `KurrentFeaturesClient`.
* The legacy API separates concerns into `KurrentDBClient` (streams), `KurrentDBPersistentSubscriptionsClient`, `KurrentDBProjectionManagementClient`, etc.

### Bridge to Legacy for Stability
* The modern `Kurrent.Client` intentionally leverages the battle-tested `KurrentDB.Client`'s connection and discovery logic. This provides stability and a gradual path for migration, rather than a "big bang" rewrite. New features should be built in the modern API, wrapping legacy components only when necessary.

---

## Project Structure

The repository is organized to separate the client libraries, source generators, tests, and samples.

### Root Level Organization

```
[src/]                         # Contains all source code for the NuGet packages.
  [KurrentDB.Client/]          # The main project for both legacy (KurrentDB.Client) and modern (Kurrent.Client) APIs.
    [KurrentDB.Client/]        # Legacy API implementation.
    [Kurrent.Client/]          # Modern API implementation, including models, options, and facades.
    [Variant/]                 # The core Result and Variant/OneOf types.
  [Kurrent.Client.SourceGenerators/] # Source generator for KurrentOperationError.
  [Variant.SourceGenerators/]      # Source generator for IVariant implementations.
[test/]                        # Contains all test projects.
  [KurrentDB.Client.Tests/]      # Integration tests for the legacy API.
  [Kurrent.Client.Tests/]        # Unit and integration tests for the modern API.
  [Kurrent.Client.Testing/]      # Shared testing infrastructure (fixtures, docker helpers).
  [Variant.Tests/]               # Tests for the Variant/Result types and source generator.
[samples/]                     # Example console and web applications demonstrating usage.
[proto/]                       # Protocol Buffer definitions.
  [v1/]                        # gRPC contracts for the legacy API.
  [v2/]                        # New gRPC contracts for the modern API.
[Directory.Build.props]        # Shared MSBuild properties for the solution.
```

## Event Sourcing with KurrentDB-Specific Principles

### Core Data Types
* **`EventData`**: A DTO for *writing* new events. It contains the event ID, type, data, and metadata.
* **`EventRecord`**: Represents an event that has been *read* from KurrentDB. It is an immutable record.
* **`ResolvedEvent`**: Represents a read event. If the original event was a link (`$>` event), `Link` will contain the link event and `Event` will contain the resolved event.
* **`StreamState` / `ExpectedStreamState`**: Used for optimistic concurrency. `StreamState` represents the known state of a stream, while `ExpectedStreamState` is used in write operations to assert the state has not changed.
* **`Position` / `StreamPosition`**: `Position` refers to a global position in the `$all` stream (a `ulong` commit/prepare position). `StreamPosition` refers to a position within a specific stream (an event number, `ulong`).

### API Design Principles
* **Use the Modern API**: For all new development, prefer `Kurrent.Client` over `KurrentDB.Client`. Use the `KurrentClientOptionsBuilder` for configuration.
* **Handle Results, Don't Catch Exceptions**: Expect `Result<T, E>` from modern API calls. Use `Match` for exhaustive handling or `OnFailure` for side effects. Only use `try-catch` for truly exceptional circumstances (e.g., `KurrentClientException` for fatal errors).
* **Be Explicit with Stream Positions**: Use `FromAll.Start` or `FromAll.End` for subscriptions to the `$all` stream, and `FromStream.Start`/`End` for specific streams. `StreamPosition` is for event numbers within a stream.

## Common Pitfalls to Avoid

### Anti-Patterns
* **Mixing APIs**: Avoid mixing calls from `Kurrent.Client` and `KurrentDB.Client` in the same logical workflow. Use the modern API and let it handle the legacy interaction.
* **Ignoring the `Result`**: Do not ignore the return value of modern API methods. Every call can result in a failure that must be handled.
* **Mutable Settings**: Do not create and then modify `KurrentDBClientSettings` instances. Use the `KurrentClientOptionsBuilder` to create an immutable `KurrentClientOptions` object.
* **Blocking on `CurrentAsync`**: In `SharingProvider`, avoid blocking on `CurrentAsync` without a proper timeout or cancellation, as the factory method could be in a retry loop.

### Performance Traps
* **Over-fetching in Subscriptions**: When subscribing to `$all`, always use a server-side `SubscriptionFilterOptions` to avoid pulling all events across the network only to filter them on the client.
* **Incorrect Batching**: The legacy `BatchAppend` is different from the new multi-stream append. For high-throughput writes to *one* stream, use `AppendToStreamAsync` with a large `IEnumerable<EventData>`. For transactional writes to *multiple* streams, use `KurrentClient.Streams.Append(IAsyncEnumerable<AppendStreamRequest>)`.
* **Not Reusing Clients**: The `KurrentClient` is thread-safe and designed to be a long-lived singleton. Do not create a new client for each request.

> **Performance Alert**: For detailed optimization strategies specific to these patterns, reference `guides/performance-guide.md`

## Implementation Priorities

1.  **Correctness & API Stability**: Ensure the client correctly implements the KurrentDB protocol. The `KurrentDB.Client` API is considered stable, while the `Kurrent.Client` API is evolving but should be used for new work.
2.  **Developer Experience**: The modern API must be intuitive and guide developers toward best practices (e.g., explicit error handling, fluent configuration).
3.  **Performance**: Minimize allocations and CPU usage in hot paths (serialization, network I/O). The use of `ValueTask`, `IAsyncEnumerable`, and `ReadOnlyMemory<byte>` is critical.
4.  **Resilience**: The client must gracefully handle transient network failures and cluster topology changes (e.g., leader elections).
5.  **Backward Compatibility**: The legacy API must remain functional to support existing applications, and the modern API's bridge must be robust.

## Development Workflow Integration

### **With Core Standards**
These project instructions work with `core-prompt.md` which provides:
- C# coding standards and formatting rules
- Adaptive pattern analysis protocol
- General performance guidelines
- Documentation requirements
- Workflow protocols

### **Specialized Resource Integration**
- **C# Formatting**: Reference `guides/csharp-standards-guide.md` for comprehensive formatting and organization patterns
- **Testing**: Reference `guides/testing-guide.md` for TUnit/Shouldly/FluentDocker patterns
- **Performance**: Reference `guides/performance-guide.md` for memory management and concurrent patterns
- **Documentation**: Reference `guides/documentation-guide.md` for comprehensive XML documentation

### **Context Loading Examples**
```bash
# General development
core-prompt.md + project-instructions-dotnet-client.md

# Performance work  
core-prompt.md + project-instructions-dotnet-client.md + guides/performance-guide.md

# Testing setup
core-prompt.md + project-instructions-dotnet-client.md + guides/testing-guide.md

# Detailed C# formatting questions
core-prompt.md + project-instructions-dotnet-client.md + guides/csharp-standards-guide.md
```

### Unique Aspects

The most unique aspect of this project is its **transitional architecture**. The modern `Kurrent.Client` is intentionally built as a layer on top of the legacy `KurrentDB.Client`. 
This allows the project to evolve its public API toward a more robust, functional style without immediately replacing the complex, battle-tested connection and gossip logic. 
Understanding this "bridge" is key to contributing effectively.
The heavy reliance on **source generators** to create core parts of the domain model (variants, errors) from Protobuf contracts is another distinguishing feature that enforces consistency and reduces boilerplate.

### Assumptions

*   The `v2` protocol definitions in `proto/v2` represent the future direction, and the `Kurrent.Client` API is being designed to align with them, even though it currently uses the `v1` communication layer.
*   The `KurrentDB.Client` namespace is considered "legacy" and will likely be deprecated and removed in a future major version once the modern client's communication layer is fully implemented against the `v2` protocol.
*   Performance is a high priority, as evidenced by the use of `ValueTask`, `IAsyncEnumerable`, and `ReadOnlyMemory<byte>`.

### Adaptation Notes

The analysis was adapted for a **Library/SDK Project** by focusing on:
*   **Public API Surface**: Differentiating between the legacy and modern APIs and documenting the intended usage patterns for each.
*   **Backward Compatibility**: Highlighting the transitional architecture and the importance of the legacy compatibility bridge.
*   **Developer Experience**: Emphasizing the fluent builder, `Result` type, and source generators as key features that improve the experience for developers consuming the library.
*   **Domain-Specific Types**: Providing detailed explanations of core types like `EventData`, `StreamPosition`, etc., which are fundamental to using the SDK correctly.
*   **Testing Strategy**: Detailing the use of `FluentDocker` for integration testing, which is critical for ensuring the library works correctly against a real KurrentDB instance.

### Project Type Identification

This is a **Library/SDK Project** undergoing a significant architectural evolution. The primary goal is to provide a robust, high-performance .NET client for the KurrentDB database.

**Analysis Approach**: My analysis focuses on the public API surface, backward compatibility, performance, and developer experience. Special attention is given to the dual API structure, identifying the patterns of the new `Kurrent.Client` and how it interoperates with the established `KurrentDB.Client`. The goal is to create instructions that guide developers to use the modern API while understanding its relationship with the legacy components it currently wraps.

### Analysis Summary

The repository contains the official .NET client for KurrentDB. A key architectural feature is the coexistence of two distinct client APIs within the same library, representing a transition from a traditional, exception-based model to a modern, functional-style API.

*   **Legacy Client (`KurrentDB.Client` namespace)**: This is the established, production-ready client. It relies on a custom implementation for cluster discovery (`LegacyClusterClient`, `GossipChannelSelector`) and uses exceptions for flow control and error handling (e.g., `WrongExpectedVersionException`, `AccessDeniedException`).

*   **Modern Client (`Kurrent.Client` namespace)**: This is the new, evolving API designed for improved ergonomics, type safety, and predictability.
    *   It uses a fluent builder (`KurrentClientOptionsBuilder`) for configuration.
    *   It employs a functional approach to error handling with a `Result<TValue, TError>` type, avoiding exceptions for predictable operational outcomes.
    *   It uses source-generated `Variant` types (discriminated unions) to model different success or failure states (e.g., `AppendStreamFailure`).
    *   It has deep integration with a Schema Registry, using strategies (`ISchemaNameStrategy`) and serializers (`ISchemaSerializer`) to manage data contracts.

*   **Bridge Architecture**: The modern `Kurrent.Client` currently acts as a facade over the legacy `KurrentDB.Client`. The `KurrentDBLegacyCallInvoker` and `KurrentDBLegacyConverter` classes form a bridge, allowing the new API to leverage the battle-tested gRPC and cluster discovery logic of the legacy client while presenting a modern interface to the developer. This is an intentional, transitional architecture until the underlying gRPC communication is updated to a new protocol version (`proto/v2`).

*   **Source Generation**: The project makes extensive use of C# Source Generators.
    *   `VariantGenerator`: Creates the boilerplate for discriminated unions (`IVariant`, `IVariantResultError`), enabling type-safe `Match` and `Switch` operations.
    *   `KurrentOperationErrorGenerator`: Generates error types directly from Protobuf annotations, ensuring consistency between the protocol definition and the C# domain model.

*   **Testing**: The testing strategy is comprehensive. `TUnit` is used as the primary test runner, with `Shouldly` for assertions. Integration tests rely heavily on `Ductus.FluentDocker` to spin up and manage containerized KurrentDB instances (`KurrentDBPermanentFixture`, `KurrentDBTemporaryFixture`), ensuring tests run against a real database environment.

## Advanced Testing Patterns (2025 Standards)

### TUnit Framework Migration (Production Standard)
- **Primary Framework**: TUnit for all new development (production standard since 2025)
- **Legacy Support**: xUnit tests remain for backward compatibility but no new xUnit tests
- **Test Naming Convention**: Use snake_case format: `[what_happens]_when_[condition]`
- **Test Attributes**: `[Test]`, `[Timeout(60000)]`, `[Before(HookType.Test)]`, `[After(HookType.Test)]`

**TUnit Project Structure Example:**
```csharp
public class StreamOperationTests : KurrentClientTestFixture {
    [Test]
    public async Task append_succeeds_when_stream_exists(CancellationToken ct) {
        // TUnit test implementation
    }
}
```

### Docker Container Testing (Production Pattern)
- **KurrentDBTestContainer**: Standardized Docker-based test infrastructure
- **Container Lifecycle**: Managed via `KurrentClientTestFixture` base class
- **TLS Support**: Both secure and insecure connection testing patterns
- **Cross-Platform**: Docker containers ensure consistent testing across environments

### Claude Communication for Testing Issues
- **Framework Clarity**: Always specify "TUnit test" vs "legacy xUnit test" when discussing test issues
- **Container Context**: Mention if issues are Docker-related vs local KurrentDB connection issues
- **Error Pattern**: For failed tests, include both the test name and the container state
- **Example**: "The TUnit test `append_fails_when_stream_deleted` in KurrentDBTestContainer shows `StreamDeletedException` from gRPC layer"

### Source Generator Debugging Protocol (Production Standard)

**When source generator issues occur:**

**Step 1: Build Output Investigation**
- Check `obj/Debug/net8.0/generated/` for actual generated files
- Verify `obj/Debug/net9.0/generated/` for multi-targeting scenarios
- Look for `KurrentOperationErrorGenerator` and `VariantGenerator` output folders

**Step 2: Attribute Verification**
- Confirm `[GenerateVariant]` on partial record struct
- Verify `[Variant]` on implicit operator methods
- Check Protobuf `ErrorDetails` annotations in .proto files

**Step 3: Protocol Synchronization Check**
- Compare Protobuf definition with expected C# generated type
- Verify error_code and error_message annotations match
- Confirm field mappings between proto and C# properties

**Step 4: Communication Pattern for Claude**
Always report source generator issues with this format:
```
Issue: [KurrentOperationErrorGenerator/VariantGenerator] not generating [specific type]
Source: proto/v2/streams/shared.proto:45 (or specific .cs file)
Expected: C# type [TypeName] implementing IVariantResultError
Actual: Missing from obj/generated/ output
Build Context: [Debug/Release], [net8.0/net9.0]
```

**Step 5: Build vs Runtime Distinction**
- **Compile-time issues**: Missing generated files, attribute recognition failures
- **Runtime issues**: Result pattern usage, error handling, Match/Switch operations

**Example Debugging Session:**
```
Problem: AppendStreamFailure missing StreamNotFoundError variant
Analysis:
1. Checked obj/Debug/net8.0/generated/KurrentOperationErrorGenerator/
2. Found partial AppendStreamFailure but missing StreamNotFoundError
3. Verified proto/v2/streams/shared.proto has correct ErrorDetails annotation
4. Issue: Source generator not processing nested error messages
Solution: Rebuild with clean obj/ folder, verify MSBuild integration
```

**Claude Communication Examples:**
- **Good**: "KurrentOperationErrorGenerator creating incomplete AppendStreamFailure from proto/v2/streams/shared.proto - missing StreamNotFoundError variant in generated C#"
- **Poor**: "Error generation not working" (lacks specificity and context)
