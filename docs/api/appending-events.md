---
order: 2
---

# Appending events

When you start working with KurrentDB, your application streams are empty. The
first meaningful operation is to add one or more events to the database using
this API.

## Append your first event

The simplest way to append an event to KurrentDB is to create an `EventData` object and call `AppendToStream` method.

```cs
var eventData = new EventData(
  Uuid.NewUuid(), "OrderPlaced", "{\"orderId\": \"123\"}"u8.ToArray()
);

await client.AppendToStreamAsync(
  "order-123",
  StreamState.NoStream,
  new List<EventData> {
    eventData
  }
);
```

`AppendToStream` takes a collection of `EventData`, which allows you to save more than one event in a single batch.
 
Outside the example above, other options exist for dealing with different scenarios. 

::: tip
If you are new to Event Sourcing, please study the [Handling concurrency](#handling-concurrency) section below.
:::

## Working with EventData

Events appended to KurrentDB must be wrapped in an `EventData` object. This allows you to specify the event's content, the type of event, and whether it's in JSON format. In its simplest form, you need three arguments:  **eventId**, **type**, and **data**.

### EventId

This takes the format of a `Uuid` and is used to uniquely identify the event you are trying to append. If two events with the same `Uuid` are appended to the same stream in quick succession, KurrentDB will only append one of the events to the stream. 

For example, the following code will only append a single event:

```cs
var orderPlaced = new EventData(
  Uuid.NewUuid(), "OrderPlaced", "{\"orderId\": \"123\"}"u8.ToArray()
);

await client.AppendToStreamAsync("order-123", StreamState.Any, [orderPlaced]);

// attempt to append the same event again
await client.AppendToStreamAsync("order-123", StreamState.Any, [orderPlaced]);
```

### Type

Each event should be supplied with an event type. This unique string is used to identify the type of event you are saving. 

It is common to see the explicit event code type name used as the type as it makes serialising and de-serialising of the event easy. However, we recommend against this as it couples the storage to the type and will make it more difficult if you need to version the event at a later date.

### Data

Representation of your event data. It is recommended that you store your events as JSON objects.  This allows you to take advantage of all of KurrentDB's functionality, such as projections. That said, you can save events using whatever format suits your workflow. Eventually, the data will be stored as encoded bytes.

### Metadata

Storing additional information alongside your event that is part of the event itself is standard practice. This can be correlation IDs, timestamps, access information, etc. KurrentDB allows you to store a separate byte array containing this information to keep it separate.

## Handling concurrency

When appending events to a stream, you can supply a *stream state* or *stream revision*. Your client uses this to inform KurrentDB of the state or version you expect the stream to be in when appending an event. If the stream isn't in that state, an exception will be thrown. 

For example, if you try to append the same record twice, expecting both times that the stream doesn't exist, you will get an exception on the second:

```cs
var orderPlaced = new EventData(
  Uuid.NewUuid(), "OrderPlaced", "{\"orderId\": \"123\"}"u8.ToArray());

var orderShipped = new EventData(
  Uuid.NewUuid(), "OrderShipped", "{\"orderId\": \"123\"}"u8.ToArray()
);

await client.AppendToStreamAsync("order-123", StreamState.NoStream, [orderPlaced]);

// attempt to append the second event expecting no stream
await client.AppendToStreamAsync("order-123", StreamState.NoStream, [orderShipped]);
```

There are three available stream states: 
- `Any`
- `NoStream`
- `StreamExists`

This check can be used to implement optimistic concurrency. When retrieving a
stream from KurrentDB, note the current version number. When you save it
back, you can determine if somebody else has modified the record in the
meantime.

```cs{1-3,11,21}
var lastEvent = client
  .ReadStreamAsync(Direction.Forwards, "order-123", StreamPosition.Start)
  .LastAsync();

var orderPaid = new EventData(
  Uuid.NewUuid(), "OrderPaid", "{\"orderId\": \"123\"}"u8.ToArray()
);

await client.AppendToStreamAsync(
  "order-123",
  lastEvent.OriginalEventNumber.ToUInt64(),
  [orderPaid]
);

var orderCancelled = new EventData(
  Uuid.NewUuid(), "OrderCancelled", "{\"orderId\": \"123\"}"u8.ToArray()
);

await client.AppendToStreamAsync(
  "order-123",
  lastEvent.OriginalEventNumber.ToUInt64(),
  [orderCancelled]
);
```

## User credentials

You can provide user credentials to append the data as follows. This will override the default credentials set on the connection.

```cs{5}
await client.AppendToStreamAsync(
  "order-123",
  StreamState.Any,
  new[] { eventData },
  userCredentials: new UserCredentials("admin", "changeit")
);
```

## Atomic appends

KurrentDB provides two operations for appending events to one or more streams in a single atomic transaction: `AppendRecords` and `MultiStreamAppend`. Both guarantee that either all writes succeed or the entire operation fails, but they differ in how records are organized, ordered, and validated.

|                        | `AppendRecords`                                                                                                 | `MultiStreamAppend`                                                                             |
|------------------------|-----------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------|
| **Available since**    | KurrentDB 26.1                                                                                                  | KurrentDB 25.1                                                                                  |
| **Record ordering**    | Interleaved. Records from different streams can be mixed, and their exact order is preserved in the global log. | Grouped. All records for a stream are sent together; ordering across streams is not guaranteed. |
| **Consistency checks** | Decoupled. Can validate the state of any stream, including streams not being written to.                        | Coupled. Expected state is specified per stream being written to.                               |

::: warning
Metadata must be a valid JSON object, using string keys and string values only.
Binary metadata is not supported in this version to maintain compatibility with
KurrentDB's metadata handling. This restriction will be lifted in the next major
release.
:::

### AppendRecords

::: note
This feature is only available in KurrentDB 26.1 and later.
:::

`AppendRecords` appends events to one or more streams atomically. Each record specifies which stream it targets, and the exact order of records is preserved in the global log across all streams.

#### Single stream

The simplest usage appends events to a single stream:

```cs
var eventOne = new EventData(
  Uuid.NewUuid(), "OrderPlaced", "{\"orderId\": \"123\"}"u8.ToArray()
);

var eventTwo = new EventData(
  Uuid.NewUuid(), "OrderShipped", "{\"orderId\": \"123\"}"u8.ToArray()
);

await client.AppendRecordsAsync("order-123", [eventOne, eventTwo]);
```

When no expected state is provided, no consistency check is performed, which is equivalent to `StreamState.Any`.

You can also pass an expected stream state for optimistic concurrency:

```cs
await client.AppendRecordsAsync(
  "order-123",
  StreamState.NoStream,
  [eventOne, eventTwo]
);
```

#### Multiple streams

Use `AppendRecord` to target different streams. Records can be interleaved freely, and the global log preserves the exact order you specify:

```cs
var records = new[] {
  new AppendRecord("order-stream", new EventData(
    Uuid.NewUuid(), "OrderCreated", "{\"orderId\": \"123\"}"u8.ToArray()
  )),
  new AppendRecord("inventory-stream", new EventData(
    Uuid.NewUuid(), "ItemReserved", "{\"itemId\": \"abc\", \"quantity\": 2}"u8.ToArray()
  )),
  new AppendRecord("order-stream", new EventData(
    Uuid.NewUuid(), "OrderConfirmed", "{\"orderId\": \"123\"}"u8.ToArray()
  )),
};

await client.AppendRecordsAsync(records);
```

#### Consistency checks

Consistency checks let you validate the state of any stream, including streams you are not writing to, before the append is committed. All checks are evaluated atomically: if any check fails, the entire operation is rejected and an `AppendConsistencyViolationException` is thrown with details about every failing check and the actual state observed.

```cs
var records = new[] {
  new AppendRecord("order-stream", new EventData(
    Uuid.NewUuid(), "OrderConfirmed", "{\"orderId\": \"123\"}"u8.ToArray()
  )),
};

var checks = new[] {
  // ensure the inventory stream exists before confirming the order,
  // even though we are not writing to it
  new ConsistencyCheck.StreamStateCheck("inventory-stream", StreamState.StreamExists),
};

await client.AppendRecordsAsync(records, checks);
```

Because checks are decoupled from writes, you can validate the state of streams you are not writing to, enabling patterns where a business decision depends on the state of multiple streams but the resulting event is written to only one of them.

### MultiStreamAppend

::: note
This feature is only available in KurrentDB 25.1 and later.
:::

`MultiStreamAppend` appends events to one or more streams atomically. Records are grouped per stream using `AppendStreamRequest`, where each request specifies a stream name, an expected state, and the events for that stream.

```cs
AppendStreamRequest[] requests = [
  new(
    "order-stream",
    StreamState.Any,
    [
      new EventData(Uuid.NewUuid(), "OrderCreated",
        Encoding.UTF8.GetBytes("{\"orderId\": \"21345\", \"amount\": 99.99}"))
    ]
  ),
  new(
    "inventory-stream",
    StreamState.Any,
    [
      new EventData(Uuid.NewUuid(), "ItemReserved",
        Encoding.UTF8.GetBytes("{\"itemId\": \"abc123\", \"quantity\": 2}"))
    ]
  )
];

await client.MultiStreamAppendAsync(requests);
```

Each stream can only appear once in the request. The expected state is validated per stream before the transaction is committed.

The result returns the position of the last appended record in the transaction and a collection of responses for each stream.
