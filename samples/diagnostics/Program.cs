// ReSharper disable InconsistentNaming

// 1. Install OpenTelemetry NuGet Packages
// dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
// dotnet add package OpenTelemetry.Extensions.Hosting
// dotnet add package OpenTelemetry.Instrumentation.Runtime

// 2. Optional Hosting Extensions
// dotnet add package Microsoft.Extensions.Hosting

// Configure dashboard: https://aspiredashboard.com/

using Kurrent;
using Kurrent.Client;
using Kurrent.Client.Extensions;
using Kurrent.Client.Streams;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

ConfigureOpenTelemetry(builder);

builder.Services.AddHostedService<KurrentDiagnosticsService>();

var host = builder.Build();

await host.RunAsync();

return;

static IHostApplicationBuilder ConfigureOpenTelemetry(IHostApplicationBuilder builder) {
	builder.Logging.AddOpenTelemetry(logging => {
			logging.IncludeFormattedMessage = true;
			logging.IncludeScopes           = true;
		}
	);

	builder.Services.AddOpenTelemetry()
		.ConfigureResource(c => c.AddService("diagnostics-sample"))
		.WithTracing(tracing => {
				tracing
					.AddKurrentClientInstrumentation()
					.AddConsoleExporter()
					.AddOtlpExporter();
			}
		);

	return builder;
}

class KurrentDiagnosticsService(ILogger<KurrentDiagnosticsService> Logger) : IHostedService {
	public async Task StartAsync(CancellationToken cancellationToken) {
		Logger.LogInformation("KurrentDiagnosticsService starting");

		var orderPlacedEvent = new OrderPlaced("customer-123");

		var client = KurrentClientOptions.Build
			.WithConnectionString("kurrentdb://admin:changeit@localhost:2113?tls=false&tlsVerifyCert=false")
			.WithSchema(KurrentClientSchemaOptions.Disabled)
			.WithResilience(KurrentClientResilienceOptions.FailFast)
			.WithMessages(map => map.Map<OrderPlaced>())
			.CreateClient();

		var metadata = new Metadata()
			.With("browser", "chrome")
			.With("sessionId", "session-789");

		var streamName = $"order-{Guid.NewGuid():N}";
		var appendRequest = new AppendStreamRequestBuilder()
			.ForStream(streamName)
			.ExpectingState(ExpectedStreamState.NoStream)
			.WithMessage(Message.New.WithValue(orderPlacedEvent).WithMetadata(metadata))
			.Build();

		Logger.LogInformation("Appending to '{Stream}'", streamName);
		await client.Streams.Append(appendRequest, cancellationToken);

		Logger.LogInformation("Subscribing to '{Stream}'", streamName);
		await using var subscription = await client.Streams
			.Subscribe(
				new StreamSubscriptionOptions {
					Stream    = appendRequest.Stream,
					Start     = StreamRevision.Min,
					Direction = ReadDirection.Forwards
				}
			)
			.ThrowOnFailureAsync();

		await foreach (var msg in subscription.WithCancellation(cancellationToken)) {
			if (msg.IsRecord) {
				var record = msg.AsRecord;
				switch (record.Value) {
					case OrderPlaced orderPlaced:
						Logger.LogInformation("Received OrderPlaced record for CustomerId {CustomerId}", orderPlaced.CustomerId);
						record.CompleteActivity(subscription);
						break;
				}
			} else if (msg is { IsHeartbeat: true, AsHeartbeat.Type: HeartbeatType.CaughtUp })
				Logger.LogInformation("Caught Up on stream {Stream}", streamName);
		}
	}

	public Task StopAsync(CancellationToken cancellationToken) {
		Logger.LogInformation("KurrentDiagnosticsService stopping");
		return Task.CompletedTask;
	}
}

record OrderPlaced(string CustomerId);
