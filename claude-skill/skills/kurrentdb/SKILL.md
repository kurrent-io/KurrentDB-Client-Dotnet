---
name: kurrentdb
description: |
  Guidance for the KurrentDB .NET Client SDK (NuGet: KurrentDB.Client).
  Triggers on: KurrentDB, EventStoreDB, event sourcing with .NET, esdb://, kdb://, kurrentdb:// connection strings,
  KurrentDBClient, EventData, StreamState, persistent subscriptions, catch-up subscriptions, projections.
user-invocable: true
---

# KurrentDB .NET Client SDK

The `KurrentDB.Client` NuGet package provides a gRPC-based .NET client for KurrentDB (formerly EventStoreDB). Compatible with server v20.6.1+. Targets `net48`, `net8.0`, `net9.0`, `net10.0`.

For detailed API signatures and complete parameter lists, see `reference.md` in this skill directory.

## 1. Client Setup

### Connection Strings

All these schemes are equivalent and interchangeable:

```csharp
// Single node
var settings = KurrentDBClientSettings.Create("kurrentdb://admin:changeit@localhost:2113?tls=true");

// Cluster with discovery
var settings = KurrentDBClientSettings.Create(
    "kurrentdb+discover://admin:changeit@node1:2113,node2:2113,node3:2113"
);
```

**Supported schemes:** `esdb://`, `kdb://`, `kurrent://`, `kurrentdb://` — each with an optional `+discover` suffix for cluster gossip discovery.

**Format:** `scheme://[user:pass@]host[:port][,host[:port]...][?params]`

Default port is `2113`. Parameters are case-insensitive.

### Connection String Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tls` | bool | `true` | Enable TLS. Set to `false` for insecure connections. |
| `tlsVerifyCert` | bool | `true` | Verify the server's TLS certificate. Set to `false` only for development. |
| `tlsCaFile` | string | — | Path to a CA certificate file for self-signed or private CAs. |
| `userCertFile` | string | — | Path to client certificate PEM file (certificate-based auth). Must be paired with `userKeyFile`. |
| `userKeyFile` | string | — | Path to client private key PEM file. Must be paired with `userCertFile`. |
| `connectionName` | string | — | Identifies this client in server logs. |
| `defaultDeadline` | int | `10000` | Default timeout for operations, in milliseconds. |
| `keepAliveInterval` | int | `10000` | gRPC keep-alive ping interval in ms. Use `-1` for infinite. |
| `keepAliveTimeout` | int | `10000` | gRPC keep-alive ping timeout in ms. Use `-1` for infinite. |
| `nodePreference` | string | `leader` | Preferred node type: `leader`, `follower`, `random`, or `readonlyreplica`. |
| `maxDiscoverAttempts` | int | `10` | Max gossip discovery attempts before giving up. |
| `discoveryInterval` | int | `100` | Interval between gossip discovery attempts, in ms. |
| `gossipTimeout` | int | `5000` | Timeout for a single gossip request, in ms. |
| `throwOnAppendFailure` | bool | `true` | When `false`, `AppendToStreamAsync` returns `WrongExpectedVersionResult` instead of throwing `WrongExpectedVersionException`. |

**Examples:**

```
kurrentdb://localhost:2113?tls=false
kurrentdb://admin:changeit@localhost:2113?tls=true&tlsVerifyCert=false
kurrentdb+discover://node1:2113,node2:2113,node3:2113?nodePreference=follower&maxDiscoverAttempts=5
kurrentdb://localhost:2113?tls=true&tlsCaFile=/path/to/ca.crt
kurrentdb://localhost:2113?tls=true&userCertFile=/path/to/user.crt&userKeyFile=/path/to/user.key
```

### Programmatic Configuration

```csharp
var settings = new KurrentDBClientSettings {
    ConnectivitySettings = { Address = new Uri("https://localhost:2113") },
    DefaultCredentials = new UserCredentials("admin", "changeit"),
    DefaultDeadline = TimeSpan.FromSeconds(30),
    ConnectionName = "my-app"
};
var client = new KurrentDBClient(settings);
```

### Dependency Injection

```csharp
// From connection string
services.AddKurrentDBClient("kurrentdb://admin:changeit@localhost:2113?tls=false");

// From configuration callback
services.AddKurrentDBClient(settings => {
    settings.ConnectivitySettings.Address = new Uri("https://localhost:2113");
    settings.DefaultCredentials = new UserCredentials("admin", "changeit");
});
```

This registers `KurrentDBClient` as a singleton. Inject it via constructor injection.

## 2. Appending Events

### Creating Events

```csharp
var eventData = new EventData(
    Uuid.NewUuid(),
    "OrderPlaced",
    JsonSerializer.SerializeToUtf8Bytes(new { OrderId = "123", Amount = 99.99 }),
    metadata: JsonSerializer.SerializeToUtf8Bytes(new { UserId = "user-1" }),
    contentType: "application/json" // default
);
```

### Appending with Concurrency Control

```csharp
// Append to a new stream (fails if stream already exists)
var result = await client.AppendToStreamAsync("order-123", StreamState.NoStream, [eventData]);

// Append to any stream (no concurrency check)
var result = await client.AppendToStreamAsync("order-123", StreamState.Any, [eventData]);

// Optimistic concurrency — append only if stream is at expected revision
var result = await client.AppendToStreamAsync("order-123", StreamState.StreamRevision(5), [eventData]);
```

`AppendToStreamAsync` returns `IWriteResult`. On success, cast to `SuccessResult` to get `NextExpectedStreamState` and `LogPosition`. When `ThrowOnAppendFailure` is `false`, check for `WrongExpectedVersionResult`.

### Multi-Stream Append (Server 25.1+)

Atomic writes across multiple streams in a single transaction. `AppendStreamRequest` is a positional record — use constructor syntax with `IEnumerable<EventData>`:

```csharp
AppendStreamRequest[] requests = [
    new("order-123", StreamState.Any, [orderEvent]),
    new("inventory-abc", StreamState.Any, [inventoryEvent])
];

var response = await client.MultiStreamAppendAsync(requests);
// response.Responses contains per-stream results
// response.Position contains the log position
```

## 3. Reading Streams

### Reading a Stream

```csharp
// Read forwards from the beginning
var result = client.ReadStreamAsync(Direction.Forwards, "order-123", StreamPosition.Start);

// Check if stream exists
if (await result.ReadState == ReadState.StreamNotFound) {
    // stream does not exist
    return;
}

// Iterate events
await foreach (var @event in result) {
    Console.WriteLine($"{@event.Event.EventType}: {Encoding.UTF8.GetString(@event.Event.Data.Span)}");
}
```

### Reading Backwards

```csharp
// Read last 10 events
var result = client.ReadStreamAsync(Direction.Backwards, "order-123", StreamPosition.End, maxCount: 10);
```

### Message-Based Reading

For richer control, use the `Messages` property:

```csharp
await foreach (var message in result.Messages) {
    switch (message) {
        case StreamMessage.Event(var resolvedEvent):
            // process event
            break;
        case StreamMessage.NotFound:
            // stream doesn't exist
            break;
        case StreamMessage.FirstStreamPosition(var pos):
        case StreamMessage.LastStreamPosition(var pos):
            // stream position metadata
            break;
    }
}
```

### Reading $all

```csharp
var result = client.ReadAllAsync(Direction.Forwards, Position.Start, maxCount: 100);

await foreach (var @event in result) {
    if (!@event.Event.EventType.StartsWith("$")) // skip system events
        Console.WriteLine(@event.Event.EventType);
}
```

### Resolving Link Events

KurrentDB uses **link events** (type `$>`) to reference events in other streams. System projections like `$by_category` and `$by_event_type` create streams composed entirely of links. By default, reads and subscriptions return the raw link event. Set `resolveLinkTos: true` to automatically follow links and return the original event instead.

```csharp
// Reading — resolve links to get the original events
var result = client.ReadStreamAsync(
    Direction.Forwards, "$ce-order", StreamPosition.Start,
    resolveLinkTos: true);

// Subscribing — same parameter
await using var subscription = client.SubscribeToStream(
    "$ce-order", FromStream.Start, resolveLinkTos: true);
```

When resolved, `ResolvedEvent.Event` is the original event, `ResolvedEvent.Link` is the link event, and `IsResolved` is `true`. Without resolution, `Event` is the link itself and `Link` is `null`.

**When to use:** Always set `resolveLinkTos: true` when reading projected/system streams (prefixed with `$`). Not needed when reading your own application streams directly.

Persistent subscriptions set this via `PersistentSubscriptionSettings(resolveLinkTos: true)` at creation time.

## 4. Catch-Up Subscriptions

### Subscribe to a Stream

```csharp
// From the beginning
await using var subscription = client.SubscribeToStream("order-123", FromStream.Start);

// From a specific position (for recovery after restart)
await using var subscription = client.SubscribeToStream("order-123", FromStream.After(lastCheckpoint));

// Live only (new events)
await using var subscription = client.SubscribeToStream("order-123", FromStream.End);

await foreach (var message in subscription.Messages) {
    switch (message) {
        case StreamMessage.Event(var evnt):
            await ProcessEvent(evnt);
            SaveCheckpoint(evnt.OriginalEventNumber);
            break;
    }
}
```

### Subscribe to $all

```csharp
await using var subscription = client.SubscribeToAll(FromAll.Start);

await foreach (var message in subscription.Messages) {
    switch (message) {
        case StreamMessage.Event(var evnt):
            await ProcessEvent(evnt);
            break;
        case StreamMessage.AllStreamCheckpointReached(var position):
            SaveCheckpoint(position);
            break;
    }
}
```

### Server-Side Filtering

```csharp
// Filter by event type prefix
var filter = new SubscriptionFilterOptions(EventTypeFilter.Prefix("Order"));

// Filter by stream name regex
var filter = new SubscriptionFilterOptions(StreamFilter.RegularExpression("^order-"));

// Exclude system events
var filter = new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents());

await using var subscription = client.SubscribeToAll(FromAll.Start, filterOptions: filter);
```

### Reconnection Pattern

```csharp
Subscribe:
try {
    await using var subscription = client.SubscribeToStream("my-stream", FromStream.After(lastCheckpoint));
    await foreach (var message in subscription.Messages) {
        if (message is StreamMessage.Event(var evnt)) {
            await ProcessEvent(evnt);
            lastCheckpoint = evnt.OriginalEventNumber;
        }
    }
} catch (OperationCanceledException) {
    return;
} catch (ObjectDisposedException) {
    return;
} catch {
    // reconnect after transient failure
    goto Subscribe;
}
```

## 5. Persistent Subscriptions

Persistent subscriptions are server-managed competing consumer groups with manual acknowledgment.

```csharp
var psClient = new KurrentDBPersistentSubscriptionsClient(settings);

// Create subscription
await psClient.CreateAsync("order-stream", "order-processor",
    new PersistentSubscriptionSettings(startFrom: StreamPosition.Start));

// Subscribe and process
await using var subscription = psClient.SubscribeToStream("order-stream", "order-processor");

await foreach (var message in subscription.Messages) {
    switch (message) {
        case PersistentSubscriptionMessage.Event(var evnt, var retryCount):
            try {
                await ProcessEvent(evnt);
                await subscription.Ack(evnt);
            } catch {
                await subscription.Nack(PersistentSubscriptionNakEventAction.Park, "Processing failed", evnt);
            }
            break;
    }
}
```

### Persistent Subscription to $all

```csharp
// Requires server support (check ServerCapabilities.SupportsPersistentSubscriptionsToAll)
await psClient.CreateToAllAsync("all-processor",
    new PersistentSubscriptionSettings(startFrom: Position.Start),
    filter: StreamFilter.Prefix("order-"));
```

### Consumer Strategies

- `SystemConsumerStrategies.RoundRobin` — distribute evenly (default)
- `SystemConsumerStrategies.DispatchToSingle` — all to one consumer
- `SystemConsumerStrategies.Pinned` — hash-based pinning to a consumer

## 6. Projections

```csharp
var projClient = new KurrentDBProjectionManagementClient(settings);

// Create a continuous projection
await projClient.CreateContinuousAsync("order-totals", @"
    fromStream('orders')
    .when({
        $init: function() { return { total: 0 }; },
        OrderPlaced: function(state, event) {
            state.total += event.body.Amount;
            return state;
        }
    })
    .outputState()
");

// Get projection state
var state = await projClient.GetStateAsync("order-totals");

// Enable/disable
await projClient.EnableAsync("order-totals");
await projClient.DisableAsync("order-totals");

// Reset
await projClient.ResetAsync("order-totals");
```

## 7. Stream Metadata & ACLs

```csharp
// Set metadata
await client.SetStreamMetadataAsync("order-123", StreamState.Any,
    new StreamMetadata(
        maxCount: 1000,
        maxAge: TimeSpan.FromDays(30),
        acl: new StreamAcl(readRole: "$admins", writeRole: "$admins")
    ));

// Read metadata
var meta = await client.GetStreamMetadataAsync("order-123");
Console.WriteLine($"Max count: {meta.Metadata.MaxCount}");
```

### Soft Delete vs Tombstone

```csharp
// Soft delete — stream can be recreated
await client.DeleteAsync("order-123", StreamState.Any);

// Tombstone — permanent, stream name cannot be reused
await client.TombstoneAsync("order-123", StreamState.Any);
```

## 8. Observability

```csharp
using KurrentDB.Client.Extensions.OpenTelemetry;

services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddKurrentDBClientInstrumentation()
        .AddConsoleExporter()
    );
```

This traces all gRPC operations with span attributes including stream name, event type, and operation.

## 9. Error Handling

| Exception | Cause |
|-----------|-------|
| `WrongExpectedVersionException` | Optimistic concurrency conflict — stream not at expected revision |
| `StreamNotFoundException` | Stream does not exist (reading/subscribing) |
| `StreamDeletedException` | Stream was soft-deleted |
| `StreamTombstonedException` | Stream was tombstoned (permanently deleted) |
| `AccessDeniedException` | Insufficient permissions |
| `NotAuthenticatedException` | Invalid or missing credentials |
| `NotLeaderException` | Operation requires leader node; client will auto-redirect |
| `DiscoveryException` | Failed to discover cluster nodes via gossip |
| `ConnectionStringParseException` | Invalid connection string syntax |
| `PersistentSubscriptionNotFoundException` | Subscription group does not exist |
| `MaximumSubscribersReachedException` | Consumer group at capacity |

## 10. Migration from EventStoreDB

The KurrentDB client is a direct successor to the EventStore.Client.Grpc packages.

**Package change:** Replace `EventStore.Client.Grpc.*` NuGet packages with `KurrentDB.Client`.

**Namespace change:** `EventStore.Client` -> `KurrentDB.Client`

**Connection strings:** All old `esdb://` schemes continue to work. You can optionally migrate:
- `esdb://` -> `kurrentdb://`
- `esdb+discover://` -> `kurrentdb+discover://`

**Class renames:**
- `EventStoreClient` -> `KurrentDBClient`
- `EventStoreClientSettings` -> `KurrentDBClientSettings`
- `EventStorePersistentSubscriptionsClient` -> `KurrentDBPersistentSubscriptionsClient`
- `EventStoreProjectionManagementClient` -> `KurrentDBProjectionManagementClient`
- `EventStoreUserManagementClient` -> `KurrentDBUserManagementClient`
- `EventStoreOperationsClient` -> `KurrentDBOperationsClient`

**DI:** `AddEventStoreClient()` -> `AddKurrentDBClient()`

All APIs, types, and behavior remain identical. The migration is purely mechanical namespace/type renaming.
