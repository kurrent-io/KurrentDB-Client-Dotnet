# Project Instructions: KurrentDB .NET Client v1.0

Last updated: 2025-06-09

> **Template Usage**: This file has been customized for the KurrentDB .NET Client project. It incorporates insights from the repository analysis and integrates with core and specialized AI agent guides.

## Version Information

**Instructions Version**: v1.0.0
**Project Phase**: Production (with ongoing development for Kurrent.Client)
**Last Updated**: 2025-06-09

### **Changelog**
- **v1.0.0** (2025-06-09): Initial version generated from repository analysis.

### **Recommended Specialized Guides**
Based on project analysis, the following guides are highly relevant and have been loaded:
- **Performance**: `guides/performance-guide.md` for high-throughput event processing and gRPC client optimization.
- **Testing**: `guides/testing-guide.md` for TUnit/Shouldly/FakeItEasy patterns and Docker-based integration testing.
- **Documentation**: `guides/documentation-guide.md` for comprehensive XML documentation of public client APIs.
- **C# Details**: `guides/csharp-standards-guide.md` for advanced C# formatting and modern language patterns.

## Project Overview

**KurrentDB .NET Client** provides a suite of production-ready .NET libraries for interacting with KurrentDB, an event-native database engineered for event sourcing and modern event-driven architectures. This SDK enables .NET developers to efficiently store, retrieve, subscribe to, and manage event streams with robust, performant, and developer-friendly APIs. It supports multiple KurrentDB protocol versions and offers a migration path from older client versions to the latest patterns.

### Key Features

- **Dual Client Implementations**:
    - `KurrentDB.Client`: The established, feature-rich client supporting KurrentDB v20.6.1+ (gRPC V1 & V2 protocols).
    - `Kurrent.Client`: The newer, evolving client focusing on modern .NET patterns and future KurrentDB capabilities (gRPC V2 protocol).
- **Comprehensive Event Streaming**: Append, read (forwards/backwards), subscribe to persistent and catch-up subscriptions.
- **Optimistic Concurrency Control**: Built-in support for `StreamState` and `ExpectedRevision` to handle concurrent writes.
- **Advanced Stream Management**: Projections, persistent subscription management, access control lists (ACLs).
- **Robust Connection Handling**: Cluster discovery, connection pooling, automatic retries, and configurable timeouts.
- **Strongly-Typed Events**: Flexible event serialization (typically JSON) with `EventData` wrappers.

### Technical Foundation

- **Runtime/Framework**: Built for **.NET 8.0 and .NET 9.0** (multi-targeted), leveraging modern C# 12+ (preview) features, nullable reference types, and async/await throughout.
- **Protocol/Communication**: Primarily **gRPC** for high-performance, cross-platform communication with KurrentDB servers. Utilizes Protobuf for message serialization.
- **API Design**: **Async-first, fluent APIs** designed for ease of use and discoverability. Exposes `Task` and `IAsyncEnumerable` for non-blocking operations.
- **Compatibility**: Cross-platform (Windows, Linux, macOS). Compatible with KurrentDB server version `20.6.1` and newer.

### Ideal Use Cases

- **Event Sourcing**: Building systems where all changes are captured as a sequence of immutable events.
- **CQRS (Command Query Responsibility Segregation)**: Separating read and write models, using event streams as the source of truth.
- **Real-time Data Processing**: Subscribing to event streams to react to changes and build reactive applications.
- **Microservices Architectures**: Using KurrentDB as an event backbone for communication and data consistency between services.

### Implementation Goals

The KurrentDB .NET Client aims to provide a highly performant, reliable, and developer-friendly interface to KurrentDB. Key goals include API stability, comprehensive feature coverage, excellent documentation, and ease of integration into modern .NET applications. The client prioritizes minimizing allocations in hot paths and providing robust error handling and resiliency patterns.

---

## Architecture Overview

### Key Architectural Concepts

#### 1. Event Sourcing Primitives
*   **Events as Immutable Facts**: Events (`EventData`, `ResolvedEvent`) are the core data unit, representing historical facts that cannot be changed. They consist of an event type, data (payload), and metadata.
*   **Streams for Event Organization**: Streams are sequences of events, typically per aggregate or entity (e.g., `order-123`, `customer-456`). System streams (prefixed with `$`) are used for internal KurrentDB operations and metadata.
*   **Global Event Log & Positions**: KurrentDB maintains a global, ordered log of all events. Positions (`Position`, `StreamPosition`) are used to navigate and checkpoint within streams and the global log.
*   **Optimistic Concurrency**: Writes are conditional based on an expected stream state or revision (`StreamState.Any`, `StreamState.NoStream`, `StreamState.Exists`, or a specific `StreamRevision`) to prevent lost updates.

#### 2. Client-Server Communication (gRPC)
*   **Protobuf Definitions**: Communication contracts are defined in `.proto` files (located in `proto/kurrentdb/protocol/v1` and `v2`), generating C# client stubs.
*   **Multiple Protocol Versions**: The `KurrentDB.Client` supports both V1 (older KurrentDB versions) and V2 gRPC protocols, while `Kurrent.Client` focuses on V2.
*   **Channel Management**: `Grpc.Net.ClientFactory` and custom channel management logic handle gRPC connections, including TLS, load balancing (cluster mode), and retries.
*   **Streaming RPCs**: Leverages gRPC client-streaming, server-streaming, and bidirectional-streaming for efficient data transfer (e.g., appending multiple events, subscribing to streams).

#### 3. Dual Client Strategy & Evolution
*   **`KurrentDB.Client` (Legacy/Stable)**: The mature client with extensive features, including operations, projections, persistent subscriptions, and user management. It acts as a foundation and provides a `KurrentDBLegacyCallInvoker` for the newer client.
*   **`Kurrent.Client` (Modern/Evolving)**: A newer client aiming for a more modern API surface, potentially with different abstractions and leveraging newer .NET features more directly. It uses the legacy client's call invoker as a bridge initially.
*   **Shared Internals**: Some internal utilities, connection string parsing, and core types are shared or have similar implementations across both clients.

### Layered Architecture (Conceptual for `KurrentDB.Client`)

- **Public API Layer (`KurrentDBClient`, `KurrentDBClientSettings`)**: Provides the main entry points for developers. Handles settings parsing, client creation, and delegates operations.
- **Operations Layer (e.g., `KurrentDBClientOperations`, `KurrentDBClientStreams`)**: Groups related functionalities like stream operations, persistent subscriptions, projections, etc. These often map to specific gRPC service categories.
- **gRPC Service Client Layer (Generated Protobuf Clients)**: Auto-generated C# clients from `.proto` definitions (e.g., `Streams.StreamsClient`, `Gossip.GossipClient`). These make the actual RPC calls.
- **Channel & Connection Management Layer (`LegacyClusterClient`, `KurrentDBLegacyCallInvoker`)**: Manages gRPC channels, cluster discovery via gossip, node selection, connection pooling, and retry logic.
- **Model & Serialization Layer (`EventData`, `UserCredentials`, Protobuf Messages)**: Defines data structures for events, credentials, and other concepts. Handles serialization/deserialization to/from Protobuf messages.

## Core Components

### `KurrentDB.Client` (Legacy/Stable Client)
- **`KurrentDBClient`**: Main entry point, aggregates various sub-clients for different KurrentDB features.
- **`KurrentDBClientSettings` / `KurrentDBConnectionString`**: Handles client configuration, including connection details, security, and behavior.
- **`KurrentDBClientOperations`**: Manages scavenger operations.
- **`KurrentDBClientPersistentSubscriptions`**: Manages persistent subscriptions.
- **`KurrentDBClientProjections`**: Manages projections.
- **`KurrentDBClientStreams`**: Handles reading from and appending to streams, managing transactions.
- **`KurrentDBClientUsers`**: Manages users and ACLs.
- **`Internal/` & `V1/Client/Gossip/`**: Contains logic for cluster discovery, node selection, and channel management.

### `Kurrent.Client` (Modern/Evolving Client)
- **`KurrentClient`**: Main entry point for the newer client.
- **`KurrentClientOptions` / `KurrentClientOptionsBuilder`**: Modern configuration API.
- **`KurrentStreamsClient`**: Focused client for stream operations.
- **`KurrentRegistryClient`**: Client for schema registry interactions.
- **`KurrentFeaturesClient`**: Client for querying server features.
- **`KurrentDBLegacyCallInvoker`**: Acts as a bridge, using the `KurrentDB.Client`'s underlying connection and gossip logic.

### Shared/Common Components
- **`proto/`**: Contains Protobuf definitions for gRPC services and messages.
- **`UserCredentials` / `SystemStreams`**: Basic shared types.
- **Testing Infrastructure (`KurrentDB.Client.Tests.Common`, `Kurrent.Client.Testing`)**: Provides fixtures, test nodes (Docker), and utilities for writing unit and integration tests.

> **Performance Note**: For performance-critical components like stream reading/writing and subscription handling, reference `guides/performance-guide.md` for advanced optimization techniques such as `Span<T>`, `Memory<T>`, and minimizing allocations.

## Integration Patterns

### Client Initialization & Configuration
*   **Connection String Parsing**: `KurrentDBConnectionString.Parse(string)` or `KurrentClientOptionsBuilder` for flexible client setup.
*   **`KurrentDBClientSettings` / `KurrentClientOptions`**: Objects holding all configuration for timeouts, security, cluster discovery, etc.
*   **Dependency Injection**: Samples show setup with `Microsoft.Extensions.DependencyInjection`.

### Event Operations
*   **Appending Events**: `AppendToStreamAsync` with `EventData` array, `StreamState` or `StreamRevision` for optimistic concurrency.
*   **Reading Events**: `ReadStreamAsync` (forwards/backwards), `ReadAllAsync` with `Position` and filters. Results are `ResolvedEvent` which include event and link event data.
*   **Subscribing to Streams**:
    *   `SubscribeToStreamAsync` (Catch-up): For live updates and replaying historical events.
    *   `SubscribeToAllAsync` (Catch-up): For subscribing to the global log.
    *   Persistent Subscriptions: Create, update, delete, and connect to persistent subscriptions for durable, competing consumer patterns.

### Cluster Interaction
*   **Gossip Protocol**: Client discovers cluster topology and node capabilities using KurrentDB's gossip protocol.
*   **Node Preference**: `NodePreference` (Leader, Follower, ReadOnlyReplica) can guide where operations are directed.
*   **Resiliency**: Handles node failures and leader changes by re-gossiping and selecting appropriate nodes.

## Design Principles

### Async Everywhere
*   All I/O-bound operations are asynchronous, returning `Task`, `ValueTask`, or `IAsyncEnumerable<T>`.
*   Consistent use of `ConfigureAwait(false)` in library code to prevent deadlocks in consuming applications.

### Immutability & Fluent APIs
*   Configuration objects (`KurrentDBClientSettings`, `KurrentClientOptions`) often use builder patterns or are effectively immutable after creation.
*   Event data itself is treated as immutable once created.

### Strong Typing & Explicit Contracts
*   Use of specific types for IDs (`Uuid`), stream positions (`StreamPosition`, `Position`), and revisions (`StreamRevision`).
*   Clear separation of event payload (user-defined, often byte array) and metadata (structured).

### Resilience and Error Handling
*   Custom exceptions (e.g., `StreamNotFoundException`, `WrongExpectedVersionException`) provide specific error context.
*   Internal retry mechanisms for transient network issues or during cluster changes.

---

## Project Structure

All paths and files in the project's .gitignore file must not be included in the agent's context or memory.
The agent should only focus on the files and directories that are relevant to the KurrentDB .NET Client development.

### Root Level Organization

```
[src/]                         # Main source code for client libraries (KurrentDB.Client, Kurrent.Client)
[test/]                        # Unit and integration tests for the client libraries
[samples/]                     # Example applications demonstrating client usage
[proto/]                       # Protobuf definitions for KurrentDB gRPC services
[docs/]                        # Project documentation (often links to external docs site)
[ai-agent-system/]             # AI agent instructions and guides
KurrentDB.Client.sln           # Main solution file
Directory.Build.props          # Common MSBuild properties for all projects
README.md                      # Project overview and entry point
```

## KurrentDB .NET Client-Specific Principles

### Event-Native Design
*   The client is fundamentally designed around KurrentDB's event-native model. Operations align with concepts of appending, reading, and subscribing to immutable event streams.
*   State is derived from events; the client facilitates this by providing efficient ways to read and process event history.

### Optimistic Concurrency by Default
*   Most write operations (e.g., `AppendToStreamAsync`) require or encourage specifying an expected stream state/revision. This is crucial for data integrity in distributed systems.
*   Developers must understand and correctly use `StreamState.Any`, `StreamState.NoStream`, `StreamState.Exists`, or specific `StreamRevision` values.

### Protocol Abstraction & Evolution
*   The client abstracts the underlying gRPC protocol details, providing higher-level .NET APIs.
*   The presence of V1 and V2 protocol support within `KurrentDB.Client` and the separate `Kurrent.Client` indicates a strategy for evolving the client while maintaining compatibility.

### API Design Principles
*   **Discoverability**: Group related operations into specific sub-clients (e.g., `KurrentDBClientStreams`, `KurrentDBClientPersistentSubscriptions`).
*   **Clarity**: Use specific types for KurrentDB concepts (e.g., `Uuid`, `StreamRevision`, `Position`) rather than primitive types where possible.
*   **Flexibility**: Provide overloads and optional parameters (e.g., `UserCredentials`, `CancellationToken`, operation-specific settings) to cater to various scenarios.
*   **Performance**: Offer batch operations and streaming APIs to handle large volumes of data efficiently.

## Common Pitfalls to Avoid

### Anti-Patterns
*   **Ignoring Optimistic Concurrency**: Using `StreamState.Any` indiscriminately can lead to lost writes or inconsistent state in multi-writer scenarios.
*   **Treating KurrentDB like a CRUD Store**: Attempting to "update" or "delete" events. Events are immutable; new events should be written to reflect changes.
*   **Large Event Payloads**: Storing excessively large payloads in single events can impact performance and network traffic. Consider splitting large data or storing references.
*   **Synchronous Blocking on Async APIs**: Calling `.Result` or `.Wait()` on `Task`-returning methods can lead to deadlocks, especially in UI or ASP.NET Core contexts. Always `await`.

### Performance Traps
*   **Reading Entire Streams Unnecessarily**: For large streams, always read in batches or from a specific position. Avoid loading millions of events into memory if not needed.
*   **Frequent Small Appends without Batching**: While KurrentDB is fast, appending events one-by-one in a tight loop can be less efficient than batching them into a single `AppendToStreamAsync` call.
*   **Inefficient Subscriptions**: Subscribing to `$all` without server-side filtering when only interested in specific event types can lead to high client-side processing load.
*   **Misconfigured Connection Pooling or Timeouts**: Default settings might not be optimal for all workloads. Tune connection timeouts, keep-alive settings, and operation deadlines.

> **Performance Alert**: For detailed optimization strategies specific to these patterns, such as efficient batching, `Span<T>`/`Memory<T>` usage for payloads, and optimizing gRPC communication, reference `guides/performance-guide.md`.

## Implementation Priorities

1.  **Correctness & Reliability**: Ensuring data integrity and that the client behaves as expected according to KurrentDB semantics (e.g., optimistic concurrency, stream guarantees).
2.  **Performance**: High throughput for reads and writes, low latency, and minimal resource (CPU, memory, network) consumption. This is critical for an KurrentDB client.
3.  **Developer Experience**: Providing clear, intuitive, and well-documented APIs that are easy to use correctly. Comprehensive samples and clear error messages are part_of this.
4.  **Backward Compatibility & API Stability**: Especially for `KurrentDB.Client`, maintaining stable public APIs and ensuring compatibility with supported KurrentDB server versions.
5.  **Feature Completeness**: Covering the full range of KurrentDB features relevant to .NET client applications.

## Development Workflow Integration

### **With Core Standards**
These project instructions work with `core-prompt.md` which provides:
- C# coding standards and formatting rules (K&R braces, 4-space indent).
- Adaptive pattern analysis protocol (analyze existing code before implementing).
- General performance guidelines (minimize allocations, `ConfigureAwait(false)`).
- Documentation requirements (XML docs for all public members).
- Workflow protocols (communication, decision framework).

### **Specialized Resource Integration**
- **C# Formatting**: Reference `guides/csharp-standards-guide.md` for comprehensive formatting, member ordering, and advanced C# patterns.
- **Testing**: Reference `guides/testing-guide.md` for TUnit/Shouldly/FakeItEasy patterns, Docker-based integration tests, and test organization.
- **Performance**: Reference `guides/performance-guide.md` for advanced gRPC client optimization, `Span<T>`/`Memory<T>` usage, batching strategies, and minimizing GC pressure in event processing.
- **Documentation**: Reference `guides/documentation-guide.md` for comprehensive XML documentation examples, especially for public client APIs and options classes.

### **Context Loading Examples**
```bash
# General development on KurrentDB .NET Client
core-prompt.md + project-instructions-kurrentdb.md + guides/csharp-standards-guide.md

# Performance optimization work for stream subscriptions
core-prompt.md + project-instructions-kurrentdb.md + guides/performance-guide.md + guides/csharp-standards-guide.md

# Adding new integration tests for a client feature
core-prompt.md + project-instructions-kurrentdb.md + guides/testing-guide.md + guides/csharp-standards-guide.md

# Improving XML documentation for public APIs
core-prompt.md + project-instructions-kurrentdb.md + guides/documentation-guide.md
