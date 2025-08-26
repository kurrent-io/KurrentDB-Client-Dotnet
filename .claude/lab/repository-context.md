# 🧠 Repository Context for AI Agents
*Generated: 2025-07-07 19:03:00 UTC | Updated: 2025-07-07 19:18:00 UTC | Complexity: High | Update Mode: incremental*

## 🎯 Executive Summary for Agents

KurrentDB .NET Client is a production-ready, high-performance client library for KurrentDB - an event-native database engineered for modern software applications and event-driven architectures. The library implements a sophisticated dual API architecture transitioning from legacy (`KurrentDB.Client`) to modern patterns (`Kurrent.Client`) with advanced Result<T,E> error handling, source generators, and comprehensive gRPC streaming support. Recent changes show a maturing codebase with enhanced error semantics, import cleanup, defensive programming improvements, and sample reorganization - indicating focus on code quality and maintainability while advancing schema registry features and TUnit testing migration.

**Quick Facts:**
- **Type**: Client Library | **Complexity**: High | **Scale**: 686 files, 553 C# files
- **Primary Stack**: .NET 8.0/9.0 + gRPC + Protocol Buffers + Source Generators
- **Architecture**: Dual API (Legacy V1 + Modern V2) with Result patterns
- **Domain**: Event Sourcing Database Client for event-native applications
- **Risk Level**: Medium with namespace migration, Result pattern adoption, testing framework migration

## 🏗 Architecture & Decision Context

### Architectural Pattern
**Dual API Evolution Architecture** - Sophisticated transition from legacy exception-based patterns to modern functional Result<T,E> patterns while maintaining backward compatibility. This represents a fundamental shift from implicit error flows (exceptions) to explicit error flows (Result types), creating compile-time safety, eliminating exception unwinding overhead, enabling functional composition, and making error paths testable. The system implements a bridge architecture where modern APIs (`Kurrent.Client`) coexist with legacy APIs (`KurrentDB.Client`), enabling gradual type system evolution.

### Technology Decisions
- **gRPC with HTTP/2**: High-performance streaming communication with connection multiplexing and flow control optimization
- **Source Generators**: Custom `KurrentOperationErrorGenerator` and `VariantGenerator` for compile-time error variant generation, creating discriminated union error types that enable Native AOT compatibility, zero reflection overhead, full IntelliSense support, and compile-time error validation
- **Result<T,E> Patterns**: Functional error handling replacing traditional exceptions in modern API
- **Schema Registry Integration**: Type-safe serialization with dynamic schema validation
- **Performance-First**: Memory<T>, Span<T>, ConfigureAwait(false), and allocation minimization patterns

### Component Boundaries
- **Modern API (`Kurrent.Client`)**: Result patterns, functional composition, source-generated errors
- **Legacy API (`KurrentDB.Client`)**: Exception-based, traditional async patterns  
- **Bridge Layer**: Translation between modern and legacy patterns for compatibility
- **Protocol Layer**: gRPC v1/v2 protocol definitions with shared infrastructure
- **Testing Infrastructure**: TUnit migration from xUnit with Docker-based integration testing

### Integration Points
- **KurrentDB Server**: gRPC connection supporting 20.6.1+ with TLS and clustering
- **Schema Registry**: Dynamic type registration and validation
- **Observability**: OpenTelemetry integration with distributed tracing
- **Container Ecosystem**: Docker-based testing and certificate management

## 🏢 Business Domain Model

### Core Entities
- **Streams**: Append-only, immutable event containers with optimistic concurrency control
- **Events**: Immutable facts representing state changes with metadata and payload
- **Persistent Subscriptions**: Durable, competing consumer groups with checkpoint management
- **Projections**: Real-time event processing for read model creation and maintenance
- **Schema Registry**: Type definitions and compatibility validation for event evolution
- **Global Event Log**: Consistent ordering mechanism across all streams

### Business Workflows
1. **Event Appending**: Stream identification → Optimistic concurrency check → Event serialization → Atomic append
2. **Event Reading**: Stream/position specification → Authorization → Deserialization → Metadata enrichment
3. **Subscription Management**: Consumer group creation → Checkpoint tracking → Message acknowledgment → Failure handling
4. **Schema Evolution**: Compatibility validation → Version registration → Migration strategy → Type mapping

### Domain Terminology
- **Event Store**: The core KurrentDB database storing immutable events
- **Stream**: Logical grouping of related events (e.g., "user-123", "order-456")
- **Event**: Domain fact with type, data, metadata, and global position
- **Revision**: Stream-specific event position for optimistic concurrency
- **Global Position**: Database-wide ordering for event sequencing
- **Catch-up Subscription**: Real-time event processing from specified position
- **Persistent Subscription**: Durable consumer group with competing consumers
- **Projection**: Materialized view derived from event stream processing
- **Schema**: Type definition and compatibility rules for event evolution

### API/Interface Design
- **Fluent Configuration**: Builder patterns for client options and connection strings
- **Async-First**: ValueTask optimization for frequently synchronous operations
- **Result Composition**: Functional chaining with Map, Bind, and error propagation
- **Type Safety**: Generic constraints and schema validation for compile-time guarantees

## 🛠 Agent Guidelines for Changes

### Safe Change Patterns
- **Follow Dual API Consistency**: Changes in modern API should have legacy bridge compatibility
- **Result Pattern Usage**: New methods must return Result<T,E> in modern API, exceptions in legacy
- **Testing Strategy**: TUnit for new tests, Docker containers for integration testing
- **Performance Validation**: Use BenchmarkDotNet for performance-critical changes, maintain sub-millisecond latencies
- **Source Generator Integration**: Error variants must use KurrentOperationErrorGenerator patterns

### Architecture Constraints
- **Maintain Namespace Separation**: `Kurrent.Client` (modern) vs `KurrentDB.Client` (legacy)
- **Backward Compatibility**: Legacy API cannot break existing clients
- **gRPC Protocol Adherence**: Must follow v1/v2 protocol definitions exactly
- **Memory Allocation Limits**: Hot paths must minimize allocations, use Span<T>/Memory<T>
- **ConfigureAwait(false)**: Required in all library code for deadlock prevention

### Quality Gates
- **TUnit Test Coverage**: Comprehensive snake_case test naming with Docker integration
- **Performance Benchmarks**: Maintain allocation budgets and throughput targets
- **Schema Compatibility**: Schema registry validation must pass for type changes
- **gRPC Compliance**: Protocol buffer compatibility across versions
- **Documentation Standards**: XML docs with business context and examples

### Integration Requirements
- **KurrentDB Server Compatibility**: Support for 20.6.1+ server versions
- **TLS Security**: Certificate validation and secure communication patterns
- **Connection Resilience**: Automatic reconnection and failure recovery
- **Observability**: OpenTelemetry tracing for distributed system monitoring

## ⚠️ Risk Factors & Hotspots

### Technical Debt Areas
- **Namespace Migration**: Ongoing `KurrentDB.Client` → `Kurrent.Client` transition requires careful coordination
- **Testing Framework Migration**: xUnit → TUnit migration in progress, mixed testing patterns
- **Error Handling Inconsistency**: Dual exception/Result patterns create complexity
- **Source Generator Maturity**: Custom generators require careful debugging and maintenance

### High-Churn Files
- `KurrentStreamsClient.cs`: Core streaming operations with frequent performance optimizations
- `KurrentOperationErrorGenerator.cs`: Source generator undergoing active development
- Schema registry components: Rapid iteration on type safety features
- Integration test fixtures: Docker configuration and test data management

### Dependency Risks
- **gRPC Version Compatibility**: Tight coupling to specific gRPC-dotnet versions
- **Protocol Buffer Evolution**: Breaking changes in v1/v2 protocol definitions
- **Performance Library Dependencies**: Memory management and allocation optimization libraries
- **Testing Infrastructure**: FluentDocker and container orchestration dependencies

### Security Considerations
- **TLS Certificate Management**: Development certificates and production security patterns
- **Authentication Integration**: OAuth and custom authentication provider support
- **Connection String Security**: Credential management and secure configuration
- **gRPC Transport Security**: HTTP/2 security and certificate validation patterns

### Recent Code Quality Improvements (Incremental Analysis)
- **Enhanced Error Handling**: KurrentClientException now includes comprehensive Google.Rpc error detail support and semantic error code mapping, moving from technical to business-meaningful error semantics
- **Defensive Programming**: Metadata.WithIf methods now validate locking before condition checks, ensuring consistent behavior
- **Import Organization**: KurrentStreamsClient import cleanup removes unnecessary dependencies, improving build performance and reducing complexity
- **Documentation Enhancement**: Added clarifying comments for TypeMapper registrations, improving code maintainability
- **Sample Consolidation**: Removal of HomeAutomation test samples indicates focused sample strategy and better organization

## 📊 Development Intelligence

### File Importance & Dependencies
**Tier 1 - Critical Core (9-10 importance)**:
- `KurrentStreamsClient.cs`: Primary streaming API implementation
- `KurrentClient.cs`: Main client entry point and configuration
- `KurrentOperationErrorGenerator.cs`: Source generator for error variants
- Protocol buffer definitions (`*.proto`): API contracts and compatibility

**Tier 2 - High Impact (7-8 importance)**:
- Result pattern implementations (`Result<T,E>`, `IVariantResultError`)
- Schema registry components and serialization
- Connection management and configuration builders
- Testing infrastructure and Docker configurations

**Tier 3 - Supporting (5-6 importance)**:
- Legacy API bridge implementations
- Diagnostic and telemetry components
- Sample applications and demonstrations
- Documentation and build scripts

### Git Activity Patterns
- **Code Quality Enhancement**: Recent import cleanup, defensive programming improvements (ThrowIfLocked positioning), and enhanced error semantics with Google.Rpc support
- **Error Handling Evolution**: Active migration to Result patterns with source generator improvements and semantic error code mapping
- **Sample Reorganization**: Removal of HomeAutomation test samples indicating better organization and focus
- **Documentation Improvements**: Added clarifying comments for complex streaming relationships ("metastream", "link")
- **Testing Modernization**: TUnit adoption and Docker-based integration testing
- **Performance Optimization**: Ongoing improvements to memory allocation and async patterns
- **API Stabilization**: Modern API refinement while maintaining legacy compatibility

### Testing Strategy
- **Primary Framework**: TUnit with snake_case naming (`append_succeeds_when_stream_exists`)
- **Integration Testing**: Docker-based KurrentDB instances with TLS certificate management
- **Performance Testing**: BenchmarkDotNet for hot path optimization and allocation tracking
- **Assertion Library**: Shouldly for fluent, readable test expressions
- **Mocking Strategy**: FakeItEasy for dependency isolation and behavior verification

### Documentation Status
**Comprehensive Standards** - Enhanced with Context7 gRPC patterns and Microsoft MCP best practices:
- **Code Documentation**: XML docs with business context and functional examples
- **Architecture Guides**: Detailed patterns for dual API usage and Result composition
- **Performance Guidelines**: Memory management and high-throughput optimization patterns
- **Testing Standards**: TUnit migration guidance and Docker integration patterns
- **Schema Registry**: Type safety and evolution strategy documentation

### Technology Best Practices
**gRPC for .NET Performance Optimization** (Context7 Enhanced):
- Channel reuse and connection multiplexing for high-throughput scenarios
- Streaming pattern optimization (client, server, bi-directional)
- Flow control configuration for large message handling
- Keep-alive configuration for long-running connections
- Error handling with Google.Rpc.Status for rich error details

**Source Generator Patterns** (Microsoft MCP Enhanced):
- Incremental source generation for build performance
- Roslyn analyzer integration for compile-time validation
- EmitCompilerGeneratedFiles for debugging generated code
- Source generator testing and debugging strategies

### Operational Context
- **Distribution**: Published to NuGet as production-ready packages
- **Server Compatibility**: Supports KurrentDB server 20.6.1 and higher
- **Development Tools**: gencert.sh/ps1 for TLS certificate generation
- **Container Support**: Docker Compose for development and testing environments
- **Performance Monitoring**: OpenTelemetry integration for distributed tracing

## 🔄 Context Metadata
*For incremental updates and agent learning*

**Last Full Analysis**: 2025-07-07 19:03:00 UTC
**Last Incremental Update**: 2025-07-07 19:18:00 UTC
**Analysis Scope**: 686 total files, 553 C# source files, incremental change analysis with deep architectural insights
**MCP Tools Used**: Context7 (gRPC patterns), Microsoft MCP (.NET best practices), Sequential Thinking (deep analysis)
**Technology Documentation**: gRPC-dotnet, source generators, .NET performance patterns
**Key Metrics**: High complexity, dual API architecture, active schema registry development, maturing code quality
**Recent Changes**: Import cleanup, enhanced error semantics, defensive programming, sample reorganization
**Change Indicators**: Namespace migration progress, Result pattern adoption, TUnit migration status, code quality maturation, error handling sophistication