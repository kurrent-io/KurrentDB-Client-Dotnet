# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

KurrentDB .NET Client SDK — a gRPC-based .NET client library for KurrentDB (formerly EventStoreDB). Published as the `KurrentDB.Client` NuGet package. Compatible with KurrentDB server v20.6.1+.

## Build Commands

```bash
# Build the solution
dotnet build KurrentDB.Client.sln

# Build in release mode
dotnet build KurrentDB.Client.sln --configuration release

# Pack NuGet package (version derived from git tags via MinVer, tag prefix "v")
dotnet pack --configuration Release
```

## Testing

Tests are integration tests that run against a KurrentDB instance via Docker (using FluentDocker/TestContainers). Certificates must be generated before running tests.

```bash
# Generate test certificates (requires Docker, run with sudo on Linux)
sudo ./gencert.sh

# Run all tests
dotnet test --configuration release test/KurrentDB.Client.Tests

# Run tests for a specific category
dotnet test --configuration release --framework net9.0 \
  --filter "Category=Target:Streams" \
  test/KurrentDB.Client.Tests

# Run a single test class
dotnet test --configuration release --framework net9.0 \
  --filter "FullyQualifiedName~AppendToStreamTests" \
  test/KurrentDB.Client.Tests
```

**Test categories** (used in CI matrix): `Streams`, `PersistentSubscriptions`, `Operations`, `UserManagement`, `ProjectionManagement`, `Security`, `Diagnostics`, `Misc`

**Target frameworks for tests**: `net8.0`, `net9.0`, `net10.0`

**Test infrastructure**: xUnit with Shouldly assertions, Bogus for fake data, Polly for resilience. Tests use `KurrentDBTemporaryFixture` (per-test instance) and `KurrentDBPermanentFixture` (shared across tests).

### Test Conventions

**Class structure** — Tests use primary constructors with dependency injection into a base class:

```csharp
[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:Read")]
public class ReadStreamForwardTests(ITestOutputHelper output, ReadStreamForwardTests.CustomFixture fixture)
    : KurrentDBPermanentTests<ReadStreamForwardTests.CustomFixture>(output, fixture) {

    [Fact]
    public async Task my_test() { ... }

    public class CustomFixture : KurrentDBPermanentFixture {
        public CustomFixture() {
            OnSetup = async () => { /* custom setup */ };
        }
    }
}
```

**Naming** — Test classes: `<Feature>Tests`. Test methods: `snake_case` describing expected behavior (e.g., `throws_when_stream_does_not_exist`).

**Fixtures** — `KurrentDBPermanentFixture` is shared across tests (use when tests don't need isolation). `KurrentDBTemporaryFixture` creates a fresh KurrentDB instance per test class (use for projections or when isolation is needed). Custom fixtures are nested classes inside the test class.

**Fixture helpers** — Fixtures expose client properties (`Streams`, `Subscriptions`, `DBProjections`, `DBUsers`, `DBOperations`) and helper methods:
- `GetStreamName()` / `GetGroupName()` / `GetProjectionName()` — generate unique names using `[CallerMemberName]` + GUID
- `CreateTestEvents(count)` / `CreateTestEvent()` — create test `EventData` instances
- `CreateTestUser()` — create a test user with credentials
- `Faker` — Bogus faker instance for random data

**Traits** — Every test class must have `[Trait("Category", "Target:<Category>")]` matching one of the CI categories. Additional operation-level traits are optional (e.g., `Operation:Read:Forwards`).

**Special attributes**:
- `[RetryFact]` — Use instead of `[Fact]` for flaky/timing-sensitive tests (xRetry)
- `[MinimumVersion.Fact(major, minor?)]` — Skip test if KurrentDB version is too old
- `[Regression.Fact(version, reason)]` — Mark regression tests
- `[Theory]` with `[InlineData]`, `[MemberData]`, or custom `TestCaseGenerator` subclasses for data-driven tests

**Assertions** — Mix of Shouldly (`result.ShouldBe(...)`) and xUnit Assert (`Assert.Equal(...)`). Custom Shouldly extension `ShouldThrowAsync` exists for `ReadStreamResult`.

**Auth in tests** — `TestCredentials.Root` provides admin credentials (admin/changeit). Use `CreateTestUser()` for non-admin users. Fixtures can be configured with `.WithoutDefaultCredentials()`.

### Test Infrastructure

Tests spin up KurrentDB via Docker containers managed by FluentDocker. Docker compose files live in `test/KurrentDB.Client.Tests.Common/`:
- `docker-compose.yml` / `docker-compose.node.yml` — Single node
- `docker-compose.cluster.yml` — 4-node cluster (3 nodes + 1 read-only replica)
- `docker-compose.certs.yml` — Certificate generation

The `TESTCONTAINER_KURRENTDB_IMAGE` environment variable controls which Docker image is used.

## Architecture

### Solution Structure

- `src/KurrentDB.Client/` — The main client library (single NuGet package)
- `test/KurrentDB.Client.Tests/` — Integration tests
- `test/KurrentDB.Client.Tests.Common/` — Shared test fixtures, Docker compose files, certificates
- `samples/` — Sample projects (separate `Samples.sln`)

### Client Classes

All clients inherit from `KurrentDBClientBase` and communicate via gRPC:

| Client | Purpose |
|--------|---------|
| `KurrentDBClient` | Stream operations (append, read, subscribe, delete, metadata) |
| `KurrentDBPersistentSubscriptionsClient` | Persistent subscription management |
| `KurrentDBProjectionManagementClient` | Projection CRUD and control |
| `KurrentDBUserManagementClient` | User and role management |
| `KurrentDBOperationsClient` | Admin operations (scavenge, etc.) |

`KurrentDBClient` is split into partial classes by operation: `.Append`, `.Read`, `.Subscriptions`, `.Delete`, `.Metadata`, `.Tombstone`, `.MultiAppend`.

### Source Layout (src/KurrentDB.Client/)

- `Core/` — Base classes, settings, exceptions, gRPC interceptors, proto definitions
  - `proto/` — Protocol Buffer definitions (v1, v2, and kurrent protocol)
  - `Interceptors/` — gRPC interceptor chain (exception mapping, leader routing, connection naming)
  - `Exceptions/` — Domain-specific exception types
  - `Common/Diagnostics/` — OpenTelemetry tracing
- `Streams/` — Stream operations and types (the primary API surface)
- `PersistentSubscriptions/` — Persistent subscription operations
- `ProjectionManagement/` — Projection operations
- `UserManagement/` — User management operations
- `Operations/` — Administrative operations
- `OpenTelemetry/` — OTel extension methods

### Connection Configuration

Clients are configured via `KurrentDBClientSettings`, which supports:
- **Connection strings**: `esdb://`, `kdb://`, `kurrent://`, `kurrentdb://` schemes (all with `+discover` variants)
- **Programmatic configuration**: Direct property assignment
- **Dependency injection**: `IServiceCollection.AddKurrentDBClient()` extensions

### Key Types

- `EventData` — Event payload (EventId, Type, Data bytes, Metadata bytes, ContentType)
- `ResolvedEvent` / `EventRecord` — Read-side event representation
- `StreamState` — Expected stream state for optimistic concurrency (Any, NoStream, StreamExists, or specific revision)
- `Uuid` — Cross-language compatible RFC-4122 v4 UUID with bit reordering

### gRPC & Protocol Buffers

Proto files live in `Core/proto/` with code generated via `Grpc.Tools`. The library supports:
- **v1 protocol**: Standard operations (streams, persistent subscriptions, projections, etc.)
- **v2 protocol**: Newer operations like `MultiStreamAppend` (server 25.1+)

## Samples

13 sample projects in `samples/` (built via `samples/Samples.sln`), targeting `net8.0` and `net9.0`. Each is a standalone console app demonstrating a specific feature (e.g., `appending-events`, `reading-events`, `persistent-subscriptions`).

**Key conventions**:
- Shared build config in `samples/Directory.Build.props` (sets OutputType, TargetFrameworks, global usings for `KurrentDB.Client` and `System.Text`)
- Each sample has a `Program.cs` with all demo code
- **`#region` markers** are used to delimit code snippets that get extracted into documentation (e.g., `#region append-to-stream` ... `#endregion append-to-stream`). Preserve these markers when editing samples.
- Connection strings support environment variable overrides with fallback defaults

```bash
# Build all samples
dotnet build samples/Samples.sln
```

## Documentation

VuePress 2.0 site in `docs/`, deployed via Cloudflare. API docs are markdown files in `docs/api/` (getting-started, appending-events, reading-events, subscriptions, etc.).

- Page ordering is controlled by `order` frontmatter
- A custom `xode` markdown plugin imports code snippets from sample files using `#region` markers — this is why region markers in samples matter
- Documentation builds are triggered automatically on markdown changes to `release/**` branches

```bash
# Local docs development (requires pnpm)
cd docs && pnpm install && pnpm dev
```

## C# Code Style

- **Indentation**: Tabs (size 4), enforced by `.editorconfig`
- **Braces**: Same-line (K&R style) — `csharp_new_line_before_open_brace = none`
- **Private fields**: `_camelCase` prefix
- **Constants**: PascalCase
- **Nullable reference types**: Enabled globally
- **Warnings as errors**: Enabled (`TreatWarningsAsErrors: true`)
- **Target frameworks**: `net48`, `net8.0`, `net9.0`, `net10.0`
