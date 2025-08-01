# KurrentDB .NET Client: A Comprehensive Project Overview

## 1. Executive Summary

**Project:** KurrentDB .NET Client
**Purpose:** A high-performance, enterprise-grade .NET client for the KurrentDB event-native database. This library is the official and primary way for .NET applications to interface with KurrentDB.
**Key Differentiator:** The client features a sophisticated **dual API architecture**, a masterclass in software evolution. It enables a seamless, incremental migration for development teams from traditional, imperative .NET code to modern, safer, and more expressive functional patterns without introducing breaking changes.
**Technology:** This project is at the forefront of the .NET ecosystem, utilizing .NET 8/9, C# 14 (Preview), gRPC, Protocol Buffers, and advanced metaprogramming with Source Generators.
**Status:** The client is mature and production-ready, while also being under active development. Current efforts are focused on significant feature enhancements like a re-architected Schema Registry, and continuous modernization of the codebase, such as the migration to the TUnit testing framework.

## 2. Introduction: The "Why"

To understand this project, one must first understand the paradigm it serves: **Event Sourcing**.

-   **What is Event Sourcing?** Instead of storing the current state of data, we store a sequence of immutable "events" that describe every change that has ever occurred. The current state is derived by replaying these events. This provides a full audit log, enables powerful temporal queries, and is a natural fit for distributed, event-driven systems.
-   **What is KurrentDB?** Formerly known as EventStoreDB and originally created by Greg Young (a key figure in the CQRS and Event Sourcing communities), KurrentDB is a database purpose-built for event sourcing. It's not a relational or document database retrofitted for the task; its entire design is optimized for appending and reading streams of events. KurrentDB uses a clustered model with a single leader and multiple followers, a gossip protocol for cluster management, and quorum-based replication to ensure high availability and fault tolerance.
-   **The Role of the .NET Client:** This client is the vital bridge between the .NET ecosystem and the KurrentDB server. Its goal is to provide a robust, highly performant, and developer-friendly API that makes it easy to build complex, event-sourced applications in .NET.

## 3. Architectural Philosophy: A Tale of Two APIs

The most brilliant and defining feature of this client is its **dual API architecture**. This is a deliberate and sophisticated strategy to solve the classic problem of API evolution.

### The Dual API Strategy

The client exposes two distinct, parallel APIs within the same library:

1.  **`KurrentDB.Client` (The Legacy API):**
    *   **Style:** Traditional, imperative C#.
    *   **Error Handling:** Relies on throwing and catching exceptions.
    *   **Audience:** Familiar to all .NET developers. It provides a stable and predictable experience, ensuring backward compatibility for a large existing user base.

2.  **`Kurrent.Client` (The Modern API):**
    *   **Style:** Functional, expressive, and composition-oriented.
    *   **Error Handling:** Employs a `Result<TSuccess, TError>` pattern. Expected outcomes (like a stream not being found) are returned as explicit types, not thrown as exceptions. This leads to safer, more predictable, and self-documenting code.
    *   **Audience:** For developers building new applications or incrementally migrating existing ones. It encourages better, more robust error handling and leverages modern C# features for cleaner code.

### The Bridge Pattern

These two APIs are not entirely separate codebases. They share a common underlying infrastructure for connection management, gRPC communication, and other core concerns. This is achieved through a classic **Bridge Pattern**, which decouples the public-facing API abstraction from its underlying implementation. This is a mark of exceptional software design, maximizing code reuse while allowing complete freedom in the programming models exposed to the developer.

### Source Generators: The Secret Sauce

The modern API's elegance and performance are supercharged by **C# Source Generators**. Instead of relying on slow, runtime-based reflection, the client uses compile-time code generation for:
*   **Error Types:** Creating the various error types used in the `Result` pattern directly from the gRPC protocol definitions.
*   **Discriminated Unions:** Generating the boilerplate for `OneOf`-based discriminated unions, which are used to represent the different possible outcomes of an operation in a type-safe way.

This advanced technique provides the type-safety and expressiveness of a functional language like F# while maintaining the performance and familiarity of C#.

## 4. Communication Layer: gRPC & Protocol Buffers

All communication between the client and the KurrentDB server happens over **gRPC**.
*   **Why gRPC?** It's a high-performance RPC framework that uses HTTP/2 for multiplexing, streaming, and low-latency communication, making it a perfect fit for the demands of event streaming. The adoption of gRPC is a move away from the previous legacy TCP client.
*   **Protocol Buffers (`.proto`):** The API contract is defined in `.proto` files. This contract-first approach ensures strong typing and clear versioning. The repository contains both `v1` and `v2` protocol definitions, demonstrating a well-planned evolution of the server-side API.

## 5. Repository Deep Dive

The repository is exceptionally well-organized, with a structure that clearly reflects its architectural philosophy.

```
/
├── proto/              # gRPC v1 and v2 protocol definitions. The API contract.
├── src/
│   ├── KurrentDB.Client/   # The main client library project.
│   │   ├── KurrentDB.Client/ # Legacy API implementation.
│   │   └── Kurrent.Client/   # Modern API implementation.
│   ├── Kurrent.Client.SourceGenerators/ # Source generator for error types.
│   └── Variant/            # The core Result<T,E> and IVariant implementation.
├── test/
│   ├── KurrentDB.Client.Tests/ # Tests for the legacy API (using xUnit).
│   ├── Kurrent.Client.Tests/   # Tests for the modern API (using TUnit).
│   └── Kurrent.Client.Integration.Tests/ # Docker-based integration tests.
├── samples/              # A rich collection of sample applications.
├── certs/                # Scripts and assets for TLS certificate management.
└── .claude/              # AI-powered documentation and context.
```

## 6. Technology Stack

*   **Languages:** C# 14 (Preview), Protocol Buffers
*   **Frameworks:** .NET 8.0 & .NET 9.0
*   **Key Dependencies:**
    *   `Grpc.Net.Client`: The core gRPC client.
    *   `Google.Protobuf`: For handling protocol buffer messages.
    *   `OneOf`: For creating type-safe discriminated unions in the modern API.
    *   `TUnit`: The preferred modern testing framework.
    *   `xUnit`: For legacy tests.
    *   `FluentDocker`: For managing Docker containers in integration tests.
    *   `OpenTelemetry.Api`: For diagnostics and observability.
*   **Tooling:** .NET SDK, Docker, GitHub Actions

## 7. Quality and Automation

*   **Testing Strategy:** The project has a multi-layered testing strategy:
    *   **Unit Tests:** The ongoing migration from xUnit to TUnit shows a commitment to modern, high-performance testing.
    *   **Integration Tests:** These are a key strength. They use `FluentDocker` to automatically spin up real KurrentDB instances, ensuring that the client is tested against the actual database, not mocks.
    *   **Test Coverage:** The project maintains a high level of test coverage (aiming for 90%).
*   **CI/CD:** A mature CI/CD pipeline is implemented using GitHub Actions. It runs a matrix of tests against different KurrentDB versions, ensuring compatibility. Releases are automated, publishing packages directly to NuGet.

## 8. Core Features Explained

*   **Event Streaming:** The primary function. Includes appending events to a stream (with optimistic concurrency control via `ExpectedState`) and reading events from a stream.
*   **Persistent Subscriptions:** A powerful feature for building resilient, distributed systems. It allows multiple consumers to work through a stream of events in a competing consumer pattern, with the server tracking their progress.
*   **Projections:** KurrentDB can run server-side projections, which are essentially event handlers that run inside the database to create new, derived streams or materialized views.
*   **Schema Registry:** A major feature currently under active development. This will provide a mechanism for managing the evolution of event schemas over time, a critical concern in long-lived, event-sourced systems.

## 9. Security & Performance

*   **Security:** Security is a first-class concern. The client supports mutual TLS for encrypted communication and certificate-based authentication. The `certs` directory contains scripts to facilitate setting up secure development environments.
*   **Performance:** Performance is a core design goal. The client is engineered for sub-millisecond latencies and high throughput. This is achieved through the use of gRPC and careful, performance-oriented coding practices throughout the codebase (`Span<T>`, `Memory<T>`, `ValueTask`, `ConfigureAwait(false)`).

## 10. For Developers

The best place for a new developer to start is the `samples` directory. It contains numerous small, focused applications that demonstrate how to use the various features of the client. For instructions on how to build the project and run the samples, see the [Build and Run Reference](./reference.build-and-run.md).