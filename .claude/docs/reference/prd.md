# KurrentDB .NET Client: Product Requirements Document (v3)

## 1. Introduction

### 1.1. Problem Statement

.NET developers building on KurrentDB require a cutting-edge client that fully leverages the v2 server protocol and modern .NET idioms. The existing legacy client, while functional, does not expose new server capabilities like the Schema Registry and Multi-Stream Appends, and its exception-based error handling is misaligned with modern, safer coding practices. A new, singular client is needed to provide a best-in-class developer experience and unlock the full potential of KurrentDB.

### 1.2. Vision

To create the single, definitive .NET client for KurrentDB, built from the ground up to be the premier tool for developing event-sourced applications. This client will be a complete replacement for the legacy client, offering a modern, functional, and highly-performant API. It will fully embrace the KurrentDB v2 protocol, providing an intuitive and type-safe interface for all server features, including the Schema Registry and advanced stream operations. The client will be the gold standard for database clients in the .NET ecosystem.

### 1.3. Target Audience

-   **.NET Developers:** The primary audience, who will use this client for all new and existing KurrentDB projects.
-   **Software Architects:** Who will leverage the client's advanced features to design robust, event-driven architectures.
-   **Teams migrating from the legacy client:** Who require a clear, compelling reason and a straightforward path to upgrade.

## 2. Core Principles

-   **V2 Protocol First:** The client will be designed around the capabilities of the KurrentDB v2 protocol.
-   **Functional API:** The client will use a `Result<TSuccess, TError>` pattern for all operations that can have expected failure states, eliminating exceptions for control flow.
-   **Performance by Design:** The client will be engineered for minimal latency and maximum throughput, using modern .NET performance idioms.
-   **Impeccable Developer Experience:** The API will be intuitive, discoverable, and well-documented, with a focus on ease of use.

## 3. Features & Functional Requirements

### 3.1. Core Client & Server Feature Discovery

-   **Description:** The client must establish and manage a connection to the KurrentDB server and be aware of the server's capabilities.
-   **Requirements:**
    -   Must connect to a single node or a cluster using a connection string or a dedicated options builder.
    -   Must support secure (TLS) connections by default.
    -   Must implement the `ServerInfoService` to discover and adapt to available server features and their policies (Optional, Required, Prohibited).

### 3.2. Stream Operations (V2 Protocol)

#### 3.2.1. Multi-Stream Append

-   **Description:** Atomically append events to multiple streams in a single transactional operation.
-   **Requirements:**
    -   Must implement the `MultiStreamAppend` and `MultiStreamAppendSession` gRPC methods.
    -   The operation must be fully atomic. If any single stream append fails, the entire transaction must be rolled back.
    -   Must support optimistic concurrency control (`expected_revision`) for each stream in the transaction.
    -   The `Result` returned must be a composite type that clearly indicates the success or failure for each stream in the request.

#### 3.2.2. Reading Streams

-   **Description:** A powerful, unified read API for consuming events from the database.
-   **Requirements:**
    -   Must implement the `ReadSession` gRPC method for continuous, streaming reads.
    -   Must support reading forwards and backwards.
    -   Must support reading from a specific log position (`start_position`).
    -   Must allow for server-side filtering using `ReadFilter`:
        -   Filtering by stream name (literal or regex).
        -   Filtering by schema name.
        -   Filtering by event properties.
    -   Must support heartbeats (`HeartbeatOptions`) to maintain session health and provide checkpoints.

### 3.3. Schema Registry

-   **Description:** A first-class client for interacting with the KurrentDB Schema Registry.
-   **Requirements:**
    -   **Schema Group Management:**
        -   Implement all `SchemaRegistryService` methods for creating, updating, deleting, and listing schema groups.
        -   Must support configuring all `SchemaGroupDetails`, including compatibility modes, data formats, and stream filters.
    -   **Schema Management:**
        -   Implement all `SchemaRegistryService` methods for the full lifecycle of schemas and their versions (create, update, delete, list, lookup, register).
    -   **Compatibility & Validation:**
        -   Implement `CheckSchemaCompatibility` to allow for pre-flight checks of new schema versions against existing ones.

### 3.4. User Management

-   **Description:** Provides a comprehensive API for managing KurrentDB users.
-   **Requirements:**
    -   **Create User:**
        -   Must take `loginName`, `fullName`, `password`, and `groups` as input.
        -   Returns a `Result<Success, UserCreationError>`.
    -   **Get User:**
        -   Must take `loginName` as input.
        -   Returns a `Result<UserDetails, UserNotFoundError>`.
    -   **List All Users:**
        -   Returns an `IAsyncEnumerable<UserDetails>`.
    -   **Update User:**
        -   Must take `loginName`, and optional `fullName` and `groups` as input.
        -   Returns a `Result<Success, UserNotFoundError>`.
    -   **Delete User:**
        -   Must take `loginName` as input.
        -   Returns a `Result<Success, UserNotFoundError>`.
    -   **Enable/Disable User:**
        -   Must take `loginName` as input.
        -   Returns a `Result<Success, UserNotFoundError>`.
    -   **Change Password:**
        -   Must take `loginName`, `currentPassword`, and `newPassword` as input.
        -   Returns a `Result<Success, UserNotFoundError | InvalidPasswordError>`.

### 3.5. Projection Management

-   **Description:** Provides a comprehensive API for managing server-side projections.
-   **Requirements:**
    -   **Create Projection:**
        -   Must support creating `OneTime`, `Continuous`, and `Transient` projections.
        -   Must take the projection `name` and `query` as input.
        -   Continuous projections must support the `trackEmittedStreams` option.
    -   **Get Projection Status, State, and Result:**
        -   Must take `name` as input.
        -   Returns a `Result<ProjectionDetails, ProjectionNotFoundError>` (for Status) or the specific state/result type.
    -   **List Projections:**
        -   Must support listing all, continuous, or one-time projections.
        -   Returns an `IAsyncEnumerable<ProjectionDetails>`.
    -   **Control Projections:**
        -   Must provide methods to `Abort`, `Enable`, `Disable`, and `Reset` a projection, taking the projection `name` as input.
        -   Each method returns a `Result<Success, ProjectionNotFoundError>`.
    -   **Restart Subsystem:**
        -   Must provide a method to restart the entire projection subsystem.

## 4. Non-Functional Requirements (NFRs)

-   **4.1. Performance:**
    -   The client must be benchmarked to ensure it meets the performance goals of sub-10ms p99 latency for appends.
-   **4.2. Security:**
    -   The client must use modern TLS practices and provide clear guidance on certificate management.
-   **4.3. Observability:**
    -   The client must be fully instrumented with OpenTelemetry, providing rich, contextual information for traces and metrics.
-   **4.4. Testing:**
    -   The client must have a comprehensive test suite written in TUnit, with a focus on integration tests that run against a real KurrentDB instance.
-   **4.5. Documentation:**
    -   The client must have excellent, comprehensive documentation, including API reference, conceptual guides, and practical samples.

## 5. Migration Path

-   A detailed migration guide will be created to help users of the legacy client upgrade to the new client. This guide will provide clear, step-by-step instructions and code examples for migrating from the exception-based pattern to the `Result`-based pattern.

## 6. Success Metrics

-   Successful deprecation and eventual removal of the legacy client from the codebase.
-   High adoption of the new client, measured by NuGet downloads.
-   Positive community feedback and a thriving ecosystem of users and contributors.