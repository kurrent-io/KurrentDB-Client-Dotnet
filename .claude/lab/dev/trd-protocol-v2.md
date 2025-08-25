Here is a Technical Requirements Document (TRD) for the proposed `StreamsReadService` and `ReadSession` RPC, designed for iteration and improvement.

***

## Technical Requirements Document: `StreamsReadService` and `ReadSession` RPC

**Version:** 1.0
**Date:** 2024-07-30

### 1. Executive Summary

This document outlines the technical requirements for a new `StreamsReadService` and `ReadSession` Remote Procedure Call (RPC) using Protocol Buffers. The primary goal is to simplify KurrentDB's read protocol, accelerate the implementation of other database clients, and enhance overall performance. This new API will consolidate existing read functionalities, particularly replacing the concept of "catch-up subscriptions", and introduce advanced server-side filtering.

### 2. Problem Statement

The current KurrentDB protocol for reading events, including direct reads and catch-up subscriptions, presents complexities that can impede client implementation speed and overall performance. The existing API may require clients to manage more state and filter data on their side, leading to increased network traffic and client-side processing overhead. Different positioning semantics for individual streams (`StreamPosition`) and the `$all` stream (`Position` with commit/prepare pairs) also add to client-side complexity.

### 3. Proposed Solution – `StreamsReadService` & `ReadSession` RPC

The solution involves introducing a new `StreamsReadService` with a single, highly flexible `ReadSession` streaming RPC. This RPC is designed to continuously retrieve batches of records.

**Key Components Introduced:**
*   **`ReadRequest`**: Defines how clients initiate and control a read session, including filtering, starting position, limits, direction, and heartbeat options.
*   **`ReadResponse`**: The streamed response from the server, containing either successful records, a failure, or a heartbeat message.
*   **`Record`**: A simplified representation of an event, designed for consistency across all read operations.
*   **`ReadFilter`**: Provides powerful server-side filtering capabilities based on various scopes and expressions.
*   **`HeartbeatOptions` & `Heartbeat`**: Formalizes the mechanism for monitoring the end-to-end health of a read session.
*   **`ReadFailure`**: Standardized error reporting for read operations.

Protocol Buffers (Protobuf) will be used for message definitions, leveraging their compact binary format and high-performance serialization for efficiency.

### 4. Detailed Technical Requirements

#### 4.1 Record Structure (`Record` Message)
*   **`record_id` (string)**: A unique identifier for the record in the database. This replaces the current `UUID` format used in existing `RecordedEvent` structures.
*   **`position` (int64)**: Represents the *commit position* of the record in the database. This single `int64` field will be used by *all* read operations (individual streams and `$all` stream) to denote the event's location and can be used to start reading from a given position.
*   **`data` (bytes)**: The actual payload of the record. KurrentDB generally recommends storing event data as JSON objects to leverage features like projections.
*   **`properties` (map)**: Additional key-value metadata associated with the record. This aligns with the existing `metadata` concept.
*   **`timestamp` (google.protobuf.Timestamp)**: The creation timestamp of the record.
*   **`stream` (optional string)**: The name of the stream to which the record belongs.
*   **`stream_revision` (optional int64)**: The revision of the stream when the record was appended. This maps to `OriginalEventNumber` in existing read responses.

#### 4.2 Filtering (`ReadFilter` Message)
*   **Scopes (`ReadFilterScope`)**: The filter can be applied to:
    *   `READ_FILTER_SCOPE_STREAM`: Record stream name.
    *   `READ_FILTER_SCOPE_SCHEMA_NAME`: Record schema name.
    *   `READ_FILTER_SCOPE_PROPERTIES`: Specific properties within the record's data.
    *   `READ_FILTER_SCOPE_RECORD`: All record properties, including stream and schema name.
*   **Expression (`expression`)**: Can be a regular expression (if prefixed with "~") or a literal value.
*   **Property Names (`property_names`)**: An optional list of specific property names to filter on, particularly relevant for `READ_FILTER_SCOPE_PROPERTIES`.
*   **Server-Side Filtering**: This new filter is designed to offload filtering logic to the server, reducing network traffic and client-side processing. Existing server-side filtering is limited to `$all` stream and by event type or stream name using regex/prefix.

#### 4.3 Read Direction (`ReadDirection` Enum)
*   Supports `READ_DIRECTION_FORWARDS` and `READ_DIRECTION_BACKWARDS`. This maintains the existing capability to read events in both directions.

#### 4.4 Starting Position (`start_position` in `ReadRequest`)
*   **`start_position` (optional int64)**: Allows clients to specify the exact `int64` commit position from which to begin reading. This unifies the "start from specific position" concept previously handled differently for individual streams (`stream_position` or `revision`) and the `$all` stream (`Position` with commit/prepare).
*   **Constants**: `READ_POSITION_CONSTANTS_EARLIEST` and `READ_POSITION_CONSTANTS_LATEST` provide symbolic start/end points.

#### 4.5 Batching (`batch_size` in `ReadRequest`)
*   **`batch_size` (int32)**: Allows clients to explicitly define the maximum number of records to be returned in a single batch. This enables efficient data transfer by reducing the number of RPC calls. The default limit will be capped at 1000 records.

#### 4.6 Heartbeats (`HeartbeatOptions`, `Heartbeat` Messages)
*   **Purpose**: To monitor end-to-end session health.
*   **Configuration (`HeartbeatOptions`)**:
    *   `enable` (bool): Activates heartbeats.
    *   `period` (optional google.protobuf.Duration): The maximum time to wait before sending a heartbeat if no records are found (default: 30 seconds).
    *   `records_threshold` (optional int32): Heartbeat sent if this number of records are processed (default: 500).
*   **Types (`HeartbeatType`)**:
    *   `HEARTBEAT_TYPE_CHECKPOINT`: Indicates a checkpoint has been reached. This formalizes the checkpointing concept from existing catch-up subscriptions.
    *   `HEARTBEAT_TYPE_CAUGHT_UP`: Indicates the subscription has caught up to live events. This replaces the `CaughtUp` message in the old `ReadResp`.
    *   `HEARTBEAT_TYPE_FELL_BEHIND`: Indicates the subscription has fallen back into catch-up mode. This replaces the `FellBehind` message in the old `ReadResp`.
*   **Content (`Heartbeat`)**: Includes `HeartbeatType`, the current `position` (int64 commit position), and `timestamp`.

#### 4.7 Error Handling (`ReadFailure` Message)
*   **Specific Error Types**: Provides granular error details within `ReadFailure`, leveraging the `ErrorDetails` defined in `core.proto` (implicitly, given `streams/shared.proto` is imported).
    *   `AccessDenied`: Client lacks permissions. Matches existing `AccessDenied`.
    *   `StreamDeleted`: Target stream has been soft-deleted. Matches existing `StreamDeleted` and soft-delete functionality.
    *   `StreamTombstoned`: Stream has been hard-deleted (and cannot be reused). Relates to hard delete functionality.
    *   `StreamNotFound`: Stream does not exist. Matches existing `StreamNotFound` and `ReadState.StreamNotFound`.

#### 4.8 Authentication and Authorization
*   The new `StreamsReadService` will enforce authentication requirements. Reading from the `$all` stream or creating filtered subscriptions currently requires admin user credentials. This granular permission control is also extended to the new `ReadFilter`'s capabilities.
*   KurrentDB supports various authentication methods, including user credentials, X.509 certificates, LDAP, and OAuth.
*   User credentials can be provided to override default connection credentials for read operations.

### 5. Compatibility and Evolution
*   **New Client Implementation**: The `StreamsReadService` is designed for new client implementations and is intended to eventually supersede existing read and catch-up subscription RPCs, simplifying the protocol for new integrations.
*   **Core Concepts Maintained**: The main ideas of reading forward/backward and filters are retained.
*   **Schema Evolution**: The `ReadFilter`'s ability to filter by `schema_name` and `properties` is designed to integrate with KurrentDB's Schema Registry. The Schema Registry enables schema evolution while maintaining compatibility between different versions, supporting formats like JSONSchema and Protobuf. Compatibility modes (BACKWARD, FORWARD, FULL, etc.) ensure that schema changes can be managed without disrupting existing consumers or producers.

### 6. Performance Considerations
*   **Protobuf Efficiency**: Utilizes Protobuf's compact binary format and high-performance serialization for efficient data transfer.
*   **Server-Side Filtering**: By performing advanced filtering on the server (e.g., by `schema_name` or `properties`), the amount of data transmitted over the network is significantly reduced, alleviating client-side processing load.
*   **Explicit Batching**: The `batch_size` parameter allows clients to optimize network calls and throughput by fetching multiple records per RPC.
*   **Consolidated `position`**: Simplifies data handling and potentially indexing given a single `int64` position.
*   **Metrics**: KurrentDB provides metrics like `kurrentdb_io_bytes_total` and `kurrentdb_io_events_total` to track read activity and `kurrentdb_grpc_method_duration_seconds_bucket` for gRPC method performance, which will be crucial for monitoring the impact of this new service.

### 7. Outstanding Questions & Next Steps

To move forward with critical strategic recommendations, the following areas require further technical specification and analysis:

*   **Detailing `int64 position` Mapping**:
    *   **Action**: Provide a precise technical specification on how the single `int64 position` will comprehensively and accurately handle the nuances of the current `commit_position` and `prepare_position` for `$all` stream reads. This includes ensuring transactional integrity and allowing for exact replay from specific transaction boundaries, given KurrentDB's event-sourcing nature.
    *   **Framework Analogy**: Consider the "position" as a universal timestamp across all events. While individual stream events have a "local sequence number" (stream revision), the `int64 position` would be the authoritative global ordering, like a global clock tick for every event ever committed. How do we ensure this global clock tick maps perfectly to transaction boundaries, which might involve multiple events and their prepare/commit positions?

*   **Schema Registry Integration for Filtering**:
    *   **Action**: Elaborate on the detailed integration plan for how the KurrentDB Schema Registry will specifically support the new `READ_FILTER_SCOPE_SCHEMA_NAME` and `READ_FILTER_SCOPE_PROPERTIES`. This should cover how schemas are retrieved and utilized for efficient and accurate server-side filtering without requiring clients to explicitly manage schema metadata.
    *   **Framework Analogy**: Think of the Schema Registry as a central "language dictionary" for your event data. The new `ReadFilter` will allow the database (the "librarian") to understand complex queries like "give me all books written in 'English' about 'Quantum Physics' that mention 'entanglement'". The integration plan needs to detail how the "librarian" efficiently consults the "dictionary" to fulfill these advanced requests without needing to read every single book.

*   **Deprecation Strategy for Existing Read RPCs**:
    *   **Action**: Develop a plan for the eventual deprecation and removal of the existing `Streams.Read` and `Catch-up subscriptions` RPCs, given the new `ReadSession` is intended to replace them. This plan should include a timeline, communication strategy, and migration guidance for existing clients.
    *   **Counter-point**: What if some legacy clients cannot immediately migrate to the new `ReadSession` due to technical constraints or resource limitations?
    *   **Address Counter-point**: A phased deprecation strategy would be necessary. This could involve an extended period where both the old and new APIs are supported, potentially with new features exclusively in the `ReadSession`. Comprehensive migration tools and documentation would be provided, along with clear communication on end-of-life for the older APIs. This balances the desire for simplification with practical concerns of existing deployments.
