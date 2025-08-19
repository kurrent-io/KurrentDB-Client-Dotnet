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

## Append to multiple streams

::: note
This feature is only available in KurrentDB 25.1 and later. 
:::

You can append events to multiple streams in a single atomic operation. Either all streams are updated, or the entire operation fails.

The `MultiStreamAppendAsync` method accepts a collection of `AppendStreamRequest` objects and returns a `MultiAppendWriteResult`. Each `AppendStreamRequest` contains:

- **Stream** - The name of the stream
- **ExpectedState** - The expected state of the stream for optimistic concurrency control
- **Messages** - A collection of `EventData` objects to append

The operation returns either:
- `MultiAppendSuccess` - Successful append results for all streams
- `MultiAppendFailure` - Specific exceptions for any failed operations

::: warning
Event metadata in `EventData` must be valid JSON deserializable to
`Dictionary<string, object?>`. This requirement will be removed in a future
major release.
:::

Here's a basic example of appending events to multiple streams:

```cs
using System.Text.Json;

var metadata = JsonSerializer.SerializeToUtf8Bytes(
	new {
		Timestamp = DateTime.UtcNow,
		Source    = "OrderProcessingSystem",
		Version   = 1.0
	}
);

AppendStreamRequest[] requests = [
	new(
		"order-stream-1",
		StreamState.Any,
		[
			new EventData(
				Uuid.NewUuid(),
				"OrderCreated",
				JsonSerializer.SerializeToUtf8Bytes(
					new {
						OrderId = "12345",
						Amount  = 99.99
					}
				),
				metadata
			)
		]
	),
	new(
		"inventory-stream-1",
		StreamState.Any,
		[
			new EventData(
				Uuid.NewUuid(),
				"ItemReserved",
				JsonSerializer.SerializeToUtf8Bytes(
					new {
						ItemId   = "ABC123",
						Quantity = 2
					}
				),
				metadata
			)
		]
	)
];

var result = await client.MultiStreamAppendAsync(requests);

if (result is MultiAppendSuccess { Successes: var successes })
	foreach (var item in successes)
		Console.WriteLine($"Stream '{item.Stream}' updated at position {item.Position}");
```

If the operation doesn't succeed, it can fail with the following exceptions:

```cs
var result = await client.MultiStreamAppendAsync(requests.ToAsyncEnumerable());

if (result is MultiAppendFailure { Failures: var failures }) {
	foreach (var error in failures) {
		switch (error) {
			case WrongExpectedVersionException ex:
				Console.WriteLine($"Version conflict in stream: {ex.Message}");
				break;

			case AccessDeniedException:
				Console.WriteLine("Access denied to one or more streams");
				break;

			case StreamDeletedException ex:
				Console.WriteLine($"Stream was deleted: {ex.Message}");
				break;

			case TransactionMaxSizeExceededException ex:
				Console.WriteLine($"Transaction too large: {ex.Message}");
				break;

			default:
				Console.WriteLine($"Unexpected error: {error.Message}");
				break;
		}
	}
}
```
