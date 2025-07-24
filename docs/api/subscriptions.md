---
order: 4
---

# Catch-up Subscriptions

Subscriptions allow you to subscribe to a stream and receive notifications about new events added to the stream. You provide an event handler and an optional starting point to the subscription. The handler is called for each event from the starting point onward.

If events already exist, the handler will be called for each event one by one until it reaches the end of the stream. The server will then notify the handler whenever a new event appears.

## Basic Subscriptions

You can subscribe to a single stream or to `$all` to process all events in the database.

**Stream subscription:**

```cs
await using var subscription = client.SubscribeToStream("order-123", FromStream.Start);

await foreach (var message in subscription.Messages.WithCancellation(ct)) {
  switch (message) {
    case StreamMessage.Event(var evnt):
      Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
      break;
  }
}
```

**`$all` subscription:**

```cs
await using var subscription = client.SubscribeToAll(FromAll.Start);

await foreach (var message in subscription.Messages) {
  switch (message) {
    case StreamMessage.Event(var evnt):
      Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
      break;
  }
}
```

When you subscribe to a stream with link events (e.g., `$ce` category stream), set `resolveLinkTos` to `true`.

## Subscribing from a Position

Both stream and `$all` subscriptions accept a starting position if you want to read from a specific point onward. If events already exist at the position you subscribe to, they will be read on the server side and sent to the subscription.

Once caught up, the server will push any new events received on the streams to the client. There is no difference between catching up and live on the client side.

::: warning
The positions provided to the subscriptions are exclusive. You will only receive the next event after the subscribed position.
:::

**Stream from specific position:**

To subscribe to a stream from a specific position, provide a stream position (`Start`, `End` or a 64-bit unsigned integer representing the stream revision):

```cs{3}
await using var subscription = client.SubscribeToStream(
  "order-123",
  FromStream.After(StreamPosition.FromInt64(20))
);

await foreach (var message in subscription.Messages) {
  switch (message) {
    case StreamMessage.Event(var evnt):
      Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
      break;
  }
}
```

**`$all` from specific position:**

For the `$all` stream, provide a `Position` structure with prepare and commit positions:

```cs{10}
var result = await client.AppendToStreamAsync(
  "order-123",
  StreamState.NoStream,
  [
    new EventData(Uuid.NewUuid(), "-", ReadOnlyMemory<byte>.Empty)
  ]
);

await using var subscription = client.SubscribeToAll(
  FromAll.After(result.LogPosition)
);

await foreach (var message in subscription.Messages) {
  switch (message) {
    case StreamMessage.Event(var evnt):
      Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
      break;
  }
}
```

**Live updates only:**

Subscribe to the end of a stream to get only new events:

```cs
// Stream
await using var subscription = client.SubscribeToStream("order-123", FromStream.End);

// $all
await using var subscription = client.SubscribeToAll(FromAll.End);
```

## Resolving link-to events

Link-to events point to events in other streams in EventStoreDB. These are generally created by projections such as the `$by_event_type` projection which links events of the same event type into the same stream. This makes it easier to look up all events of a specific type.

::: tip
[Filtered subscriptions](subscriptions.md#server-side-filtering) make it easier and faster to subscribe to all events of a specific type or matching a prefix.
:::

When reading a stream you can specify whether to resolve link-to's. By default, link-to events are not resolved. You can change this behaviour by setting the `resolveLinkTos` parameter to `true`:

```cs{4}
await using var subscription = client.SubscribeToStream(
  "$et-order",
  FromStream.Start,
  resolveLinkTos: true
);

await foreach (var message in subscription.Messages) {
  switch (message) {
    case StreamMessage.Event(var evnt):
      Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
      break;
  }
}
```

## Subscription Drops and Recovery

When a subscription stops or experiences an error, it will be dropped. The subscription provides a `subscriptionDropped` callback, which will get called when the subscription breaks.

The `subscriptionDropped` callback allows you to inspect the reason why the subscription dropped, as well as any exceptions that occurred.

The possible reasons for a subscription to drop are:

| Reason            | Why it might happen                                                                                                  |
|:------------------|:---------------------------------------------------------------------------------------------------------------------|
| `Disposed`        | The client canceled or disposed of the subscription.                                                            |
| `SubscriberError` | An error occurred while handling an event in the subscription handler.                                               |
| `ServerError`     | An error occurred on the server, and the server closed the subscription. Check the server logs for more information. |

Bear in mind that a subscription can also drop because it is slow. The server tried to push all the live events to the subscription when it is in the live processing mode. If the subscription gets the reading buffer overflow and won't be able to acknowledge the buffer, it will break.

### Handling Dropped Subscriptions

An application, which hosts the subscription, can go offline for some time for different reasons. It could be a crash, infrastructure failure, or a new version deployment. As you rarely would want to reprocess all the events again, you'd need to store the current position of the subscription somewhere, and then use it to restore the subscription from the point where it dropped off:

```cs{1,10}
var checkpoint = FromStream.Start; // or read from a persistent store

await using var subscription = client.SubscribeToStream("order-123", checkpoint);

await foreach (var message in subscription.Messages) {
  switch (message) {
    case StreamMessage.Event(var evnt):
      Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");

      checkpoint = FromStream.After(evnt.OriginalEventNumber);

      break;
  }
}
```

When subscribed to `$all` you want to keep the event's position in the `$all` stream. As mentioned previously, the `$all` stream position consists of two big integers (prepare and commit positions), not one:

```cs{1,13}
var checkpoint = FromAll.Start; // or read from a persistent store

await using var subscription = client.SubscribeToAll(checkpoint);

await foreach (var message in subscription.Messages) {
  switch (message) {
    case StreamMessage.Event(var evnt):
      Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");

      if (evnt.OriginalPosition is not null)
        checkpoint = FromAll.After(evnt.OriginalPosition.Value);

      break;
  }
}
```

## Handling Subscription State Changes

::: info EventStoreDB 23.10.0+
This feature requires EventStoreDB version 23.10.0 or later.
:::

When a subscription processes historical events and reaches the end of the stream, it transitions from "catching up" to "live" mode. You can detect this transition using the `CaughtUp` message on the subscription.

```cs{8-10}
await using var subscription = client.SubscribeToStream("order-123", FromStream.Start);

await foreach (var message in subscription.Messages) {
  switch (message) {
    case StreamMessage.Event(var evnt):
      Console.WriteLine($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
      break;
    case StreamMessage.CaughtUp:
      Console.WriteLine("Caught up to live mode");
      break;
  }
}
```

::: tip
The `CaughtUp` message is only emitted when transitioning from catching up to live mode. If you subscribe from the end of a stream, you'll immediately be in live mode and this message will be emitted right away.
:::

## User credentials

The user creating a subscription must have read access to the stream it's subscribing to, and only admin users may subscribe to `$all` or create filtered subscriptions.

The code below shows how you can provide user credentials for a subscription. When you specify subscription credentials explicitly, it will override the default credentials set for the client. If you don't specify any credentials, the client will use the credentials specified for the client, if you specified those.

```cs{3}
await using var subscription = client.SubscribeToAll(
  FromAll.Start,
  userCredentials: new UserCredentials("admin", "changeit")
);
```

## Server-side Filtering

EventStoreDB allows you to filter events while subscribing to the `$all` stream to only receive the events you care about. You can filter by event type or stream name using a regular expression or a prefix. Server-side filtering is currently only available on the `$all` stream.

::: tip
Server-side filtering was introduced as a simpler alternative to projections. You should consider filtering before creating a projection to include the events you care about.
:::

**Basic filtering:**

```cs
await using var subscription = client.SubscribeToAll(
  FromAll.Start,
  filterOptions: new SubscriptionFilterOptions(StreamFilter.Prefix("test-", "other-"))
);
```

### Filtering out system events

System events are prefixed with `$` and can be filtered out when subscribing to `$all`:

```cs
await using var subscription = client.SubscribeToAll(
  FromAll.Start,
  filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents())
);
```

### Filtering by event type

**By prefix:**

```cs
var filterOptions = new SubscriptionFilterOptions(EventTypeFilter.Prefix("customer-"));
```

**By regular expression:**

```cs
var filterOptions = new SubscriptionFilterOptions(
  EventTypeFilter.RegularExpression("^user|^company")
);
```

### Filtering by stream name

**By prefix:**

```cs
var filterOptions = new SubscriptionFilterOptions(StreamFilter.Prefix("user-"));
```

**By regular expression:**

```cs
var filterOptions = new SubscriptionFilterOptions(
  StreamFilter.RegularExpression("^account|^savings")
);
```

## Checkpointing

When a catch-up subscription is used to process an `$all` stream containing many events, the last thing you want is for your application to crash midway, forcing you to restart from the beginning.

### What is a checkpoint?

A checkpoint is the position of an event in the `$all` stream to which your application has processed. By saving this position to a persistent store (e.g., a database), it allows your catch-up subscription to:
- Recover from crashes by reading the checkpoint and resuming from that position
- Avoid reprocessing all events from the start

To create a checkpoint, store the event's commit or prepare position.

::: warning
If your database contains events created by the legacy TCP client using the [transaction feature](https://docs.kurrent.io/clients/tcp/dotnet/21.2/appending.html#transactions), you should store both the commit and prepare positions together as your checkpoint.
:::

### Updating checkpoints at regular intervals

The client SDK provides a way to notify your application after processing a configurable number of events. This allows you to periodically save a checkpoint at regular intervals.

```cs{10-13}
var filterOptions = new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents());

await using var subscription = client.SubscribeToAll(FromAll.Start, filterOptions: filterOptions);

await foreach (var message in subscription.Messages) {
  switch (message) {
    case StreamMessage.Event(var e):
      Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.CommitPosition}");
      break;
    case StreamMessage.AllStreamCheckpointReached(var p):
      // Save commit position to a persistent store as a checkpoint
      Console.WriteLine($"checkpoint taken at {p.CommitPosition}");
      break;
  }
}
```

By default, the checkpoint notification is sent after every 32 non-system events processed from $all.

### Configuring the checkpoint interval

You can adjust the checkpoint interval to change how often the client is notified.

```cs{3}
var filterOptions = new SubscriptionFilterOptions(
  filter: EventTypeFilter.ExcludeSystemEvents(), 
  checkpointInterval: 1000
);
```

By configuring this parameter, you can balance between reducing checkpoint overhead and ensuring quick recovery in case of a failure.

::: info
The checkpoint interval parameter configures the database to notify the client after `n` * 32 number of events where `n` is defined by the parameter.

For example:
- If `n` = 1, a checkpoint notification is sent every 32 events.
- If `n` = 2, the notification is sent every 64 events.
- If `n` = 3, it is sent every 96 events, and so on.
:::
