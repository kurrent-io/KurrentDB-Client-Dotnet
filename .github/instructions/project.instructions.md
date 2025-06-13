---
description: This file provides project instructions for the KurrentDB .NET Client SDK, focusing on the migration from legacy to modern client code, best practices, and architectural guidelines.
applyTo: '**'
---

# PROJECT INSTRUCTIONS: KurrentDB .NET Client SDK

---

## 1. Project Type Identification

**Type:** Library/SDK Project  
**Reasoning:**
- This repository is a .NET client SDK for KurrentDB, designed for integration into other .NET applications.
- Focus is on public API design, backward compatibility, and developer experience.
- The README and structure confirm this is not a standalone application, but a reusable library.

---

## 2. Migration Context: Legacy to Modern Client

- **Legacy Namespace:** `KurrentDB.Client` (and its subfolders) is the legacy client implementation. Most types and logic here are being phased out or refactored.
- **Modern Namespace:** `Kurrent.Client` (in `src/KurrentDB.Client/Kurrent.Client`) is the new, modern client. All new development, refactoring, and features should be focused here.
- **Transition:** Some models and logic are still shared or referenced from legacy code, but the goal is to migrate or reimplement all reusable models and APIs in the new namespace.
- **Guidance:**
  - Avoid introducing new dependencies on legacy code in `Kurrent.Client`.
  - Migrate or reimplement shared models in `Kurrent.Client.Model` as needed.
  - Mark legacy-only types as `[Obsolete]` or with clear comments.
  - Document any transitional dependencies and plan for their removal.

---

## 3. Best Practices for Kurrent.Client

- **Model Usage:**
  - Prefer new or migrated models in `Kurrent.Client.Model`.
  - Avoid importing legacy models unless absolutely necessary, and document any such usage as transitional.
- **API Design:**
  - Follow modern C# and project standards (`@guides/csharp-standards-guide.mdc`).
  - Use XML documentation and API reference quality (`@guides/documentation-guide.mdc`).
  - Apply performance best practices for hot paths, memory, and async patterns (`@guides/performance-guide.mdc`).
- **Documentation:**
  - Document all public APIs and models in `Kurrent.Client` with clear XML docs, usage examples, and migration notes if relevant.
- **Decoupling:**
  - Minimize legacy dependencies; mark any remaining with TODOs or comments for future removal.
- **Testing:**
  - Write and maintain tests only for `Kurrent.Client`.
  - Ensure tests do not depend on legacy behaviors.

---

## 4. Analysis Summary

- **Architecture:** Layered, with clear separation between client API, domain models, and service abstractions.
- **Domain:** Event sourcing, stream-based data storage, and event-driven patterns.
- **API:** Modern, async-first, with strong typing and explicit error handling.
- **Testing:** Comprehensive, with both unit and integration tests.
- **Patterns:** Consistent use of C# best practices, dependency injection, and extensibility points.

---

## 5. Completed Project Instructions Template

### Project Overview

**KurrentDB .NET Client** is a high-performance, event-native database client SDK for .NET, enabling applications to interact with KurrentDB servers. It provides robust APIs for appending, reading, and subscribing to event streams, with a focus on data integrity, streaming, and distributed messaging.

**Key Features:**
- Asynchronous, streaming-first API surface
- Strongly-typed domain models (EventData, StreamState, ResolvedEvent, etc.)
- Support for stream metadata and system streams
- Advanced filtering and subscription options
- Backward compatibility with KurrentDB server v20.6.1+
- Integrated diagnostics and logging

### Technology Stack
- **Runtime/Framework:** .NET (C# 10+), targeting modern .NET runtimes
- **Dependencies:** gRPC, Microsoft.Extensions.Logging, System.Text.Json
- **Protocols:** gRPC for client-server communication
- **Data Storage:** Event streams, with support for metadata and projections

### Primary Use Cases
- Append events to streams with concurrency control
- Read events (by stream or globally) with filtering and paging
- Subscribe to event streams for real-time processing
- Manage stream metadata and system settings

### Core Domain Concepts
- **EventData:** Represents an immutable event to be written to a stream.
- **StreamState:** Encodes expected concurrency state for optimistic writes.
- **ResolvedEvent:** Represents an event or a resolved link event from a stream.
- **Position/StreamPosition:** Used for paging and checkpointing in reads.

### Architecture Layers
- **Client API Layer:** (`KurrentClient`, sub-clients) – Public entry points for all operations.
- **Domain Model Layer:** (`Model/`) – Strongly-typed representations of events, streams, positions, etc.
- **Service Layer:** (`Streams/`, `Registry/`, `Features/`) – Implements core operations and business logic.
- **Internal/Infrastructure Layer:** (`Internal/`, etc.) – Handles low-level concerns, connection management, and protocol details.

### Component Categories
- **Client API:** Main user-facing classes and methods.
- **Domain Models:** Event, stream, and metadata representations.
- **Service Abstractions:** Stream operations, subscriptions, registry, features.

### Integration Patterns
- **gRPC-based communication** for all client-server interactions.
- **Dependency injection** for configuration and extensibility.
- **Async/await** for all I/O operations, with `ConfigureAwait(false)` for library code.

### Design Principles
- Immutability for all domain models.
- Explicit error handling and exception mapping.
- Minimal allocations in hot paths (e.g., batch appenders, streaming).
- Consistent, modern C# style (K&R braces, 4-space indent, file-scoped namespaces).

### Directory Structure
- `src/KurrentDB.Client/Kurrent.Client/` – New client code, models, and services (focus here)
- `src/KurrentDB.Client/KurrentDB.Client/` – Legacy client code (being phased out)
- `test/` – Unit and integration tests, organized by feature
- `docs/` – Documentation and guides
- `samples/` – Example usage and sample projects
- `.clinerules/` – Coding standards and analysis rules

---

## 6. Unique Aspects

- **Event-native API:** The SDK is designed around event sourcing and stream processing, not just CRUD.
- **Batch append optimization:** Uses a `StreamAppender` for efficient, batched writes.
- **Extensive filtering:** Supports advanced filtering for reads and subscriptions.
- **Strong focus on diagnostics:** Integrated logging, tracing, and telemetry hooks.

---

## 7. Assumptions

- Some internal implementation details (e.g., custom source generators) are inferred from directory names and may require deeper inspection for full documentation.
- The SDK is assumed to be used in modern .NET environments, as legacy .NET Framework support is not indicated.

---

## 8. Adaptation Notes

- Analysis focused on SDK/library patterns, public API design, and extensibility.
- Emphasis on developer experience, API stability, and performance, as is standard for client libraries.
- All new work should be in `Kurrent.Client` and its subfolders.

---

## 9. Anti-Patterns to Avoid

- Direct manipulation of internal types or bypassing the client API.
- Synchronous/blocking I/O in application code.
- Ignoring stream state/concurrency controls when appending events.
- Overusing dynamic or weakly-typed APIs.
- Introducing new dependencies on legacy code in `Kurrent.Client`.

---

## 10. Implementation Priorities

- **Performance:** Minimize allocations, optimize hot paths, and use async throughout.
- **API Stability:** Avoid breaking changes, use semantic versioning.
- **Documentation:** Provide XML docs and samples for all public APIs.
- **Testing:** Maintain high coverage with both unit and integration tests.
- **Migration:** Move or reimplement shared models in `Kurrent.Client` as legacy is phased out.

---

## 11. Cross-References

- For advanced performance: see `guides/performance-guide.md`
- For documentation standards: see `guides/documentation-guide.md`
- For testing patterns: see `guides/testing-guide.md`

---

**This file was updated to reflect the migration focus on `Kurrent.Client` and the phased deprecation of legacy code.** 
 