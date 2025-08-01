﻿---
order: 6
title: Projections
---

# Projection management

The client provides a way to manage projections in KurrentDB. 

For a detailed explanation of projections, see the [server documentation](@server/features/projections/README.md).

## Creating a client

Projection management operations are exposed through the dedicated client.

```cs
var client = new KurrentDBProjectionManagementClient(
  KurrentDBClientSettings.Create("kurrentdb://localhost:2113?tls=false&tlsVerifyCert=false")
);
```

## Create a projection

Creates a projection that runs until the last event in the store, and then continues processing new events as they are appended to the store. The query parameter contains the JavaScript you want created as a projection.
Projections have explicit names, and you can enable or disable them via this name.

```cs
const string js = """
  fromAll()
    .when({
      $init: function() {
        return {
          count: 0
        };
      },
      $any: function(s, e) {
        s.count += 1;
      }
    })
    .outputState();
""";

await client.CreateContinuousAsync("count-events", js);
```

Trying to create projections with the same name will result in an error:

```cs
var name = "count-events";

await client.CreateContinuousAsync(name, js);
try {
  await client.CreateContinuousAsync(name, js);
}
catch (RpcException e) when (e.StatusCode is StatusCode.AlreadyExists) {
  Console.WriteLine(e.Message);
}
catch (RpcException e) when (e.Message.Contains("Conflict")) { // will be removed in a future release
  var format = $"{name} already exists";
  Console.WriteLine(format);
}
```

## Restart the subsystem

It is possible to restart the entire projection subsystem using the projections management client API. The user must be in the `$ops` or `$admin` group to perform this operation.

```cs
await client.RestartSubsystemAsync();
```

## Enable a projection

Enables an existing projection by name.
Once enabled, the projection will start to process events even after restarting the server or the projection subsystem.
You must have access to a projection to enable it, see the [ACL documentation](@server/security/user-authorization.md).

```cs
await client.EnableAsync("$by_category");
```

You can only enable an existing projection. When you try to enable a non-existing projection, you'll get an error:

 ```cs
try {
  await client.EnableAsync("projection that does not exists");
}
catch (RpcException e) when (e.StatusCode is StatusCode.NotFound) {
  Console.WriteLine(e.Message);
}
catch (RpcException e) when (e.Message.Contains("NotFound")) { // will be removed in a future release
  Console.WriteLine(e.Message);
}
```

## Disable a projection

Disables a projection, this will save the projection checkpoint.
Once disabled, the projection will not process events even after restarting the server or the projection subsystem.
You must have access to a projection to disable it, see the [ACL documentation](@server/security/user-authorization.md).

```cs
await client.DisableAsync("$by_category");
```

You can only disable an existing projection. When you try to disable a non-existing projection, you'll get an error:

```cs
try {
  await client.DisableAsync("projection that does not exists");
}
catch (RpcException e) when (e.StatusCode is StatusCode.NotFound) {
  Console.WriteLine(e.Message);
}
catch (RpcException e) when (e.Message.Contains("NotFound")) { // will be removed in a future release
  Console.WriteLine(e.Message);
}
```

## Delete a projection

This feature is currently not supported by the client.  

## Abort a projection

Aborts a projection, this will not save the projection's checkpoint.

```cs
await client.AbortAsync("countEvents_Abort");
```

You can only abort an existing projection. When you try to abort a non-existing projection, you'll get an error:

```cs
try {
  await client.AbortAsync("projection that does not exists");
}
catch (RpcException e) when (e.StatusCode is StatusCode.NotFound) {
  Console.WriteLine(e.Message);
}
catch (RpcException e) when (e.Message.Contains("NotFound")) { // will be removed in a future release
  Console.WriteLine(e.Message);
}
```

## Reset a projection

Resets a projection, which causes deleting the projection checkpoint. This will force the projection to start afresh and re-emit events. Streams that are written to from the projection will also be soft-deleted.

```cs
await client.ResetAsync("countEvents_Reset");
```

Resetting a projection that does not exist will result in an error.

```cs
try {
  await client.ResetAsync("projection that does not exists");
}
catch (RpcException e) when (e.StatusCode is StatusCode.NotFound) {
  Console.WriteLine(e.Message);
}
catch (RpcException e) when (e.Message.Contains("NotFound")) { // will be removed in a future release
  Console.WriteLine(e.Message);
}
```

## Update a projection

Updates a projection with a given name. The query parameter contains the new JavaScript. Updating system projections using this operation is not supported at the moment.

```cs
const string js = """
fromAll()
  .when({
      $init: function() {
          return {
              count: 0
          };
      },
      $any: function(s, e) {
          s.count += 1;
      }
  })
  .outputState();
""";

var name = "count-events";

await client.CreateContinuousAsync(name, "fromAll().when()");
await client.UpdateAsync(name, js);
```

You can only update an existing projection. When you try to update a non-existing projection, you'll get an error:

```cs
try {
  await client.UpdateAsync("Update Not existing projection", "fromAll().when()");
}
catch (RpcException e) when (e.StatusCode is StatusCode.NotFound) {
  Console.WriteLine(e.Message);
}
catch (RpcException e) when (e.Message.Contains("NotFound")) { // will be removed in a future release
  Console.WriteLine("'Update Not existing projection' does not exists and can not be updated");
}
```

## List all projections

Returns a list of all projections, user defined & system projections.
See the [projection details](#projection-details) section for an explanation of the returned values.

```cs
var details = client.ListAllAsync();

await foreach (var detail in details)
  Console.WriteLine(
    $@"{detail.Name}, {detail.Status}, {detail.CheckpointStatus}, {detail.Mode}, {detail.Progress}"
  );
```

## List continuous projections

Returns a list of all continuous projections.
See the [projection details](#projection-details) section for an explanation of the returned values.

```cs
var details = client.ListContinuousAsync();
await foreach (var detail in details)
  Console.WriteLine(
    $@"{detail.Name}, {detail.Status}, {detail.CheckpointStatus}, {detail.Mode}, {detail.Progress}"
  );
```

## Get status

Gets the status of a named projection.
See the [projection details](#projection-details) section for an explanation of the returned values.

```cs{18}
const string js = """
fromAll()
  .when({
    $init: function() {
      return { count: 0 };
    },
    $any: function(state, event) {
      state.count += 1;
    }
  })
  .outputState();
""";

var name = "count-events";

await client.CreateContinuousAsync(name, js);

var status = await client.GetStatusAsync(name);

Console.WriteLine(
  $@"{status?.Name}, {status?.Status}, {status?.CheckpointStatus}, {status?.Mode}, {status?.Progress}"
);
```

## Get state

Retrieves the state of a projection.

```cs{20}
const string js = """
fromAll()
  .when({
    $init: function() {
      return { count: 0 };
    },
    $any: function(s, e) {
      s.count += 1;
    }
  })
  .outputState();
""";

var name = $"count-events";

await client.CreateContinuousAsync(name, js);

await Task.Delay(500); // give it some time to process and have a state.

var document = await client.GetStateAsync(name);

Console.WriteLine(document.RootElement.GetRawText())
```

or you can retrieve the state as a typed result:

```cs
public class Result {
	public int count { get; set; }

	public override string ToString() => $"count= {count}";
};

var result = await client.GetStateAsync<Result>(name);
```

## Get result

Retrieves the result of the named projection and partition.

```cs{20}
const string js = """
fromAll()
  .when({
    $init: function() {
      return { count: 0 };
    },
    $any: function(s, e) {
      s.count += 1;
    }
  })
  .outputState();
""";

var name = "count-events";

await client.CreateContinuousAsync(name, js);

await Task.Delay(500); //give it some time to have a result.

var document = await client.GetResultAsync(name);

Console.WriteLine(document.RootElement.GetRawText())
```

or it can be retrieved as a typed result:

```cs
public class Result {
	public int count { get; set; }

	public override string ToString() => $"count= {count}";
};

var result = await client.GetResultAsync<Result>(name);
```

## Projection Details

The `ListAllAsync`, `ListContinuousAsync`, and `GetStatusAsync` methods return detailed statistics and information about projections. Below is an explanation of the fields included in the projection details:

| Field                                | Description                                                                                                                                                                                           |
|--------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Name`, `EffectiveName`              | The name of the projection                                                                                                                                                                            |
| `Status`                             | A human readable string of the current statuses of the projection (see below)                                                                                                                         |
| `StateReason`                        | A human readable string explaining the reason of the current projection state                                                                                                                         |
| `CheckpointStatus`                   | A human readable string explaining the current operation performed on the checkpoint : `requested`, `writing`                                                                                         |
| `Mode`                               | `Continuous`, `OneTime` , `Transient`                                                                                                                                                                 |
| `CoreProcessingTime`                 | The total time, in ms, the projection took to handle events since the last restart                                                                                                                    |
| `Progress`                           | The progress, in %, indicates how far this projection has processed event, in case of a restart this could be -1% or some number. It will be updated as soon as a new event is appended and processed |
| `WritesInProgress`                   | The number of write requests to emitted streams currently in progress, these writes can be batches of events                                                                                          |
| `ReadsInProgress`                    | The number of read requests currently in progress                                                                                                                                                     |
| `PartitionsCached`                   | The number of cached projection partitions                                                                                                                                                            |
| `Position`                           | The Position of the last processed event                                                                                                                                                              |
| `LastCheckpoint`                     | The Position of the last checkpoint of this projection                                                                                                                                                |
| `EventsProcessedAfterRestart`        | The number of events processed since the last restart of this projection                                                                                                                              |
| `BufferedEvents`                     | The number of events in the projection read buffer                                                                                                                                                    |
| `WritePendingEventsBeforeCheckpoint` | The number of events waiting to be appended to emitted streams before the pending checkpoint can be written                                                                                           |
| `WritePendingEventsAfterCheckpoint`  | The number of events to be appended to emitted streams since the last checkpoint                                                                                                                      |
| `Version`                            | This is used internally, the version is increased when the projection is edited or reset                                                                                                              |
| `Epoch`                              | This is used internally, the epoch is increased when the projection is reset                                                                                                                          |

The `Status` string is a combination of the following values.
The first 3 are the most common one, as the other one are transient values while the projection is initialised or stopped

| Value              | Description                                                                                                             |
|--------------------|-------------------------------------------------------------------------------------------------------------------------|
| Running            | The projection is running and processing events                                                                         |
| Stopped            | The projection is stopped and is no longer processing new events                                                        |
| Faulted            | An error occurred in the projection, `StateReason` will give the fault details, the projection is not processing events |
| Initial            | This is the initial state, before the projection is fully initialised                                                   |
| Suspended          | The projection is suspended and will not process events, this happens while stopping the projection                     |
| LoadStateRequested | The state of the projection is being retrieved, this happens while the projection is starting                           |
| StateLoaded        | The state of the projection is loaded, this happens while the projection is starting                                    |
| Subscribed         | The projection has successfully subscribed to its readers, this happens while the projection is starting                |
| FaultedStopping    | This happens before the projection is stopped due to an error in the projection                                         |
| Stopping           | The projection is being stopped                                                                                         |
| CompletingPhase    | This happens while the projection is stopping                                                                           |
| PhaseCompleted     | This happens while the projection is stopping                                                                           |
