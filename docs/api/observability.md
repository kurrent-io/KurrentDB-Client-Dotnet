---
order: 8
---

# Observability

The .NET client provides observability capabilities through OpenTelemetry
integration. This enables you to monitor, trace, and troubleshoot your event
store operations with distributed tracing support.

## Prerequisites

You'll need to install exporters for your chosen observability platform:

```bash
# For console output
dotnet add package OpenTelemetry.Exporter.Console

# For Jaeger
dotnet add package OpenTelemetry.Exporter.Jaeger

# For OTLP (OpenTelemetry Protocol)
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol

# For Seq
dotnet add package Seq.Extensions.Logging
```

## Basic Configuration

Configure instrumentation using the `AddKurentDBClientInstrumentation()`
extension method. Here's a minimal setup:

```cs {15}
using KurrentDB.Client.Extensions.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

const string serviceName = "my-eventstore-app";

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddOpenTelemetry()
            .ConfigureResource(builder => builder.AddService(serviceName))
            .WithTracing(tracerBuilder => tracerBuilder
                .AddKurrentDBClientInstrumentation()
                .AddConsoleExporter()
            );
    })
    .Build();

await host.RunAsync();
```

## Trace Exporters

OpenTelemetry supports various exporters to send trace data to different
observability platforms. You can find a list of available exporters in the
[OpenTelemetry Registry](https://opentelemetry.io/ecosystem/registry/?component=exporter&language=dotnet).

You can configure multiple exporters simultaneously:

```cs {10-18}
using OpenTelemetry.Exporter;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddOpenTelemetry()
            .ConfigureResource(builder => builder.AddService("my-eventstore-app"))
            .WithTracing(tracerBuilder => tracerBuilder
                .AddKurrentDBClientInstrumentation()
                .AddConsoleExporter()
                .AddJaegerExporter(options =>
                {
                    options.Endpoint = new Uri("http://localhost:14268/api/traces");
                })
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://localhost:4318/v1/traces");
                })
            );
    })
    .Build();
```

For detailed configuration options, refer to the
[OpenTelemetry .NET documentation](https://opentelemetry.io/docs/languages/dotnet/).

## Understanding Traces

### What Gets Traced

The .NET client currently creates traces for append, catch-up and persistent
subscription operations.

### Trace Attributes

Each trace includes metadata to help with debugging and monitoring:

| Attribute                         | Description                            | Example                               |
| --------------------------------- | -------------------------------------- | ------------------------------------- |
| `db.user`                         | Database user name                     | `admin`                               |
| `db.system`                       | Database system identifier             | `eventstoredb`                        |
| `db.operation`                    | Type of operation performed            | `streams.append`, `streams.subscribe` |
| `db.eventstoredb.stream`          | Stream name or identifier              | `user-events-123`                     |
| `db.eventstoredb.subscription.id` | Subscription identifier                | `user-events-123-sub`                 |
| `db.eventstoredb.event.id`        | Event identifier                       | `event-456`                           |
| `db.eventstoredb.event.type`      | Event type identifier                  | `user.created`                        |
| `server.address`                  | KurrentDB server address            | `localhost`                           |
| `server.port`                     | KurrentDB server port               | `2113`                                |
| `otel.status_code`                | Status code for the operation          | `UNSET`, `OK`, `ERROR`                |
| `otel.status_description`         | Status of a span                       |                                       |
| `exception.type`                  | Exception type if an error occurred    |                                       |
| `exception.message`               | Exception message if an error occurred |                                       |
| `exception.stacktrace`            | Stack trace of the exception           |                                       |

### Sample Trace Output

Here's an example trace from a stream append operation:

```bash
Activity.TraceId:            8da04787239dbb85c1f9c6fba1b1f0d6
Activity.SpanId:             4352ec4a66a20b95
Activity.TraceFlags:         Recorded
Activity.ActivitySourceName: kurrentdb
Activity.DisplayName:        streams.append
Activity.Kind:               Client
Activity.StartTime:          2024-05-29T06:50:41.2519016Z
Activity.Duration:           00:00:00.1500707
Activity.Tags:
    db.kurrentdb.stream: example-stream
    server.address: localhost
    server.port: 2113
    db.system: kurrentdb
    db.operation: streams.append
    event.count: 3
StatusCode: Ok
Resource associated with Activity:
    service.name: my-eventstore-app
    service.instance.id: 7316ef20-c354-4e64-97da-c1b99c2c28b0
    service.version: 1.0.0
    deployment.environment: production
    telemetry.sdk.name: opentelemetry
    telemetry.sdk.language: dotnet
    telemetry.sdk.version: 1.9.0
```
