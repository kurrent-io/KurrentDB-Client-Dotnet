# Project Context

## Overview
This repository contains the .NET client for KurrentDB, a database engineered for modern software applications and event-driven architectures. Its event-native design simplifies data modeling and preserves data integrity while the integrated streaming engine solves distributed messaging challenges and ensures data consistency. The client is a sophisticated, enterprise-grade database client library designed specifically for event sourcing applications. It's not a simple database wrapper but a comprehensive platform that bridges the gap between traditional .NET development patterns and modern functional programming approaches while maintaining production-grade performance requirements.

## Technology & Architecture
The project implements a dual API architecture that simultaneously supports legacy exception-based patterns and modern Result<T,E> functional patterns, enabling gradual migration without breaking changes. This architectural sophistication, combined with event sourcing domain complexity, extensive source generator usage, and performance optimization requirements, places this firmly in the High Complexity category.

The technology stack represents cutting-edge .NET development with sophisticated functional programming patterns and enterprise-grade infrastructure. The choice of .NET 8.0/9.0 with C# 14 preview features demonstrates commitment to modern language capabilities, while the extensive gRPC integration provides high-performance communication suitable for event sourcing workloads.

- **Languages & Frameworks**: 
  - Primary: C# 14 (Preview) + .NET 8.0/9.0
  - Secondary: Protocol Buffers for gRPC communication

- **Dependency Management**: 
  - Tool: NuGet Package Manager with version pinning
  - Key dependencies: Grpc.Net.Client (2.71.0), Google.Protobuf (3.31.1), OneOf (3.0.271), NJsonSchema (11.3.2), OpenTelemetry.Api (1.12.0)

- **Communication Protocols**: 
  - API consumption: gRPC client for KurrentDB server communication
  - Streaming: Bidirectional gRPC streaming for real-time event subscriptions
  - Messaging: Event streaming with backpressure handling and heartbeat management
  - Security: Mutual TLS authentication with certificate-based user credentials

## Domain & Business Logic
The event sourcing domain model demonstrates deep understanding of event-driven architecture patterns and enterprise requirements. The immutable event model with append-only semantics provides the foundation for audit trails, temporal queries, and distributed system coordination. The sophisticated optimistic concurrency control through expected stream states enables high-throughput scenarios while maintaining data consistency.

- **Core Domain Models & Entities**: 
  - **Stream** - Named sequence of immutable events with monotonic revision numbers and global positioning
  - **Event/Record** - Immutable data unit with unique identification, timestamps, metadata, and typed payload
  - **StreamRevision** - Optimistic concurrency control mechanism for conflict-free multi-writer scenarios
  - **Subscription** - Live event consumption pattern with consumer group semantics and checkpoint management
  - **Schema** - Versioned data contracts with backward/forward compatibility checking and automatic type mapping

- **Main Workflows & Use Cases**: 
  - **Event Append Workflow** - Message construction, schema registration, serialization, optimistic concurrency checking, and batch transactional operations
  - **Stream Consumption** - Real-time subscriptions, historical reads, server-side filtering, and backpressure handling with heartbeat management
  - **Schema Evolution** - Compatibility validation, version management, migration support, and automatic type mapping for C# applications

## Development Patterns
The repository demonstrates exceptional organizational maturity with clear separation of concerns that directly supports the dual API architecture strategy. The structure reveals a team that understands both backward compatibility requirements and forward-looking modernization needs. The parallel namespace hierarchies (`KurrentDB.Client` vs `Kurrent.Client`) provide complete isolation for the dual API approach, while shared infrastructure in bridge components maximizes code reuse.

- **Testing Frameworks & Coverage**: 
  - Unit testing: TUnit 0.25.21 (modern, preferred) with snake_case naming for new development
  - Integration testing: Docker-based KurrentDB containers via FluentDocker with health checking and automated lifecycle management
  - Legacy testing: xUnit 2.9.3 maintained for backward compatibility during migration phase
  - Test coverage: 90% coverage with comprehensive Result pattern and error scenario validation

- **CI/CD Approaches & Tools**: 
  - CI Tool: GitHub Actions with matrix testing across KurrentDB versions (ci, lts, previous-lts)
  - Deployment: Automated NuGet package publishing with multi-framework targeting (.NET 8.0/9.0)
  - Environments: Development, testing, and production configurations with Docker Compose orchestration
  - Security: Automated TLS certificate generation for secure testing scenarios and mutual authentication

## Key Insights
The dual API architecture represents one of the most sophisticated migration strategies observed in enterprise software development. Rather than forcing breaking changes, the team created parallel namespace hierarchies that provide complete programming model isolation while sharing infrastructure. This enables organizations to adopt modern functional patterns incrementally while maintaining production stability.
