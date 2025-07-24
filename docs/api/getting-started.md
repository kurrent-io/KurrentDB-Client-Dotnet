---
order: 1
---

# Getting started

Get started by connecting your application to EventStoreDB. 

## Connecting to EventStoreDB

To connect your application to EventStoreDB, instantiate and configure the client.

::: tip Insecure clusters
The recommended way to connect to KurrentDB is using secure mode (which is
the default). However, if your KurrentDB instance is running in insecure
mode, you must explicitly set `tls=false` in your connection
string or client configuration.
:::

### Required packages

Add the .NET `EventStore.Client.Grpc` and `EventStore.Client.Grpc.Streams` package to your project:

```bash
dotnet add package EventStoreDB.Client
dotnet add package EventStore.Client.Grpc.Streams 
```

### Connection string

Each SDK has its own way of configuring the client, but the connection string can always be used. 
The EventStoreDB connection string supports two schemas: `esdb://` for connecting to a single-node server, and `esdb+discover://` for connecting to a multi-node cluster. The difference between the two schemas is that when using `esdb://`, the client will connect directly to the node; with `esdb+discover://` schema the client will use the gossip protocol to retrieve the cluster information and choose the right node to connect to.
Since version 22.10, ESDB supports gossip on single-node deployments, so `esdb+discover://` schema can be used for connecting to any topology.

The connection string has the following format:

```
esdb+discover://admin:changeit@cluster.dns.name:2113
```

There, `cluster.dns.name` is the name of a DNS `A` record that points to all the cluster nodes. Alternatively, you can list cluster nodes separated by comma instead of the cluster DNS name:

```
esdb+discover://admin:changeit@node1.dns.name:2113,node2.dns.name:2113,node3.dns.name:2113
```

There are a number of query parameters that can be used in the connection string to instruct the cluster how and where the connection should be established. All query parameters are optional.

| Parameter             | Accepted values                                   | Default  | Description                                                                                                                                    |
|-----------------------|---------------------------------------------------|----------|------------------------------------------------------------------------------------------------------------------------------------------------|
| `tls`                 | `true`, `false`                                   | `true`   | Use secure connection, set to `false` when connecting to a non-secure server or cluster.                                                       |
| `connectionName`      | Any string                                        | None     | Connection name                                                                                                                                |
| `maxDiscoverAttempts` | Number                                            | `10`     | Number of attempts to discover the cluster.                                                                                                    |
| `discoveryInterval`   | Number                                            | `100`    | Cluster discovery polling interval in milliseconds.                                                                                            |
| `gossipTimeout`       | Number                                            | `5`      | Gossip timeout in seconds, when the gossip call times out, it will be retried.                                                                 |
| `nodePreference`      | `leader`, `follower`, `random`, `readOnlyReplica` | `leader` | Preferred node role. When creating a client for write operations, always use `leader`.                                                         |
| `tlsVerifyCert`       | `true`, `false`                                   | `true`   | In secure mode, set to `true` when using an untrusted connection to the node if you don't have the CA file available. Don't use in production. |
| `tlsCaFile`           | String, file path                                 | None     | Path to the CA file when connecting to a secure cluster with a certificate that's not signed by a trusted CA.                                  |
| `defaultDeadline`     | Number                                            | None     | Default timeout for client operations, in milliseconds. Most clients allow overriding the deadline per operation.                              |
| `keepAliveInterval`   | Number                                            | `10`     | Interval between keep-alive ping calls, in seconds.                                                                                            |
| `keepAliveTimeout`    | Number                                            | `10`     | Keep-alive ping call timeout, in seconds.                                                                                                      |
| `userCertFile`        | String, file path                                 | None     | User certificate file for X.509 authentication.                                                                                                |
| `userKeyFile`         | String, file path                                 | None     | Key file for the user certificate used for X.509 authentication.                                                                               |

When connecting to an insecure instance, specify `tls=false` parameter. For example, for a node running locally use `esdb://localhost:2113?tls=false`. Note that usernames and passwords aren't provided there because insecure deployments don't support authentication and authorisation.

### Creating a client

First, create a client and get it connected to the database.

```cs
var client = new EventStoreClient(EventStoreClientSettings.Create("esdb://admin:changeit@localhost:2113?tls=false&tlsVerifyCert=false"));
```

The client instance can be used as a singleton across the whole application. It doesn't need to open or close the connection.

### Creating an event

You can write anything to EventStoreDB as events. The client needs a byte array as the event payload. Normally, you'd use a serialized object, and it's up to you to choose the serialization method.

::: tip Server-side projections
User-defined server-side projections require events to be serialized in JSON format.

We use JSON for serialization in the documentation examples.
:::

The code snippet below creates an event object instance, serializes it, and adds it as a payload to the `EventData` structure, which the client can then write to the database.

```cs
using System.Text.Json;

public class OrderCreated {
    public string? OrderId { get; set; }
}

var evt = new OrderCreated {
    OrderId = Guid.NewGuid().ToString("N"),
};

var orderCreated = new EventData(
    Uuid.NewUuid(),
    "OrderCreated",
    JsonSerializer.SerializeToUtf8Bytes(evt)
);
```

### Appending events

Each event in the database has its own unique identifier (UUID). The database
uses it to ensure idempotent writes, but it only works if you specify the stream
revision when appending events to the stream.

In the snippet below, we append the event to the stream `order-123`.

```cs
await client.AppendToStreamAsync("order-123", StreamState.Any, [orderCreated]);
```

Here we are appending events without checking if the stream exists or if the
stream version matches the expected event version. See more advanced scenarios
in [appending events documentation](./appending-events.md).

### Reading events

Finally, we can read events back from the `order-123` stream.

```cs
var result = client.ReadStreamAsync(
	Direction.Forwards, "order-123", StreamPosition.Start
);

var events = await result.ToListAsync();
```

When you read events from the stream, you get a collection of `ResolvedEvent`
structures. The event payload is returned as a byte array and needs to be
deserialized. See more advanced scenarios in
[reading events documentation](./reading-events.md).
