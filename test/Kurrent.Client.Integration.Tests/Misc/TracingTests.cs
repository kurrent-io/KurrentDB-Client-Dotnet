// ReSharper disable InvertIf

using System.Diagnostics;
using Kurrent.Client.Exceptions;
using Kurrent.Client.Registry;
using Kurrent.Client.Streams;
using Kurrent.Client.Testing.Sample;
using KurrentDB.Diagnostics.Tracing;

namespace Kurrent.Client.Tests.Misc;

[Category("Misc"), Category("Tracing")]
public class TracingTests : KurrentClientTestFixture {
	[Test]
	public async Task append_should_record_activity(CancellationToken ct) {
		var activityStarted = false;
		var activityStopped = false;

		using var listener = new ActivityListener();

		listener.ShouldListenTo = source => source.Name == AppVersionInfo.Current.ProductName;
		listener.Sample         = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
		listener.ActivityStarted = activity => {
			activityStarted = true;
			activity.ShouldNotBeNull();
		};

		listener.ActivityStopped = activity => {
			activityStopped = true;
			activity.ShouldNotBeNull();
			activity.Status.ShouldBe(ActivityStatusCode.Ok);
			activity.DisplayName.ShouldBe(TraceConstants.Operations.Append);
			activity.OperationName.ShouldBe(TraceConstants.Operations.Append);
			activity.Tags.ShouldContainKeyValue(TraceConstants.Tags.ServerAddress, "localhost");
			activity.Tags.ShouldContainKeyValue(TraceConstants.Tags.DatabaseUser, "admin");
			activity.Tags.ShouldContainKeyValue(TraceConstants.Tags.DatabaseSystemName, "kurrentdb");
			activity.Tags.ShouldContainKeyValue(TraceConstants.Tags.DatabaseOperationName, TraceConstants.Operations.Append);
		};

		ActivitySource.AddActivityListener(listener);

		var simulatedGame = TrySimulateGame(GamesAvailable.TicTacToe);

		await AutomaticClient.Streams
			.Append(
				simulatedGame.Stream, ExpectedStreamState.NoStream, simulatedGame.GameEvents,
				ct
			)
			.ShouldNotThrowOrFailAsync();

		activityStopped.ShouldBeTrue();
		activityStarted.ShouldBeTrue();
		Activity.Current.ShouldBeNull();
	}

	[Test]
	public async Task append_with_failures_should_record_exceptions(CancellationToken ct) {
		var activityStarted = false;
		var activityStopped = false;

		using var listener = new ActivityListener();

		listener.ShouldListenTo = source => source.Name == AppVersionInfo.Current.ProductName;
		listener.Sample         = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
		listener.ActivityStarted = activity => {
			activityStarted = true;
			activity.ShouldNotBeNull();
		};

		listener.ActivityStopped = activity => {
			activityStopped = true;
			activity.ShouldNotBeNull();
			activity.Status.ShouldBe(ActivityStatusCode.Error);
			activity.DisplayName.ShouldBe(TraceConstants.Operations.Append);
			activity.OperationName.ShouldBe(TraceConstants.Operations.Append);
			activity.Events.ShouldHaveSingleItem().Tags.ShouldContainKeyValue("exception.type", typeof(StreamRevisionConflictException).FullName);
		};

		ActivitySource.AddActivityListener(listener);

		var simulatedGame = TrySimulateGame(GamesAvailable.TicTacToe);

		await AutomaticClient.Streams
			.Append(
				simulatedGame.Stream, ExpectedStreamState.StreamExists, simulatedGame.GameEvents.Skip(3),
				ct
			)
			.ShouldFailAsync();

		activityStarted.ShouldBeTrue();
		activityStopped.ShouldBeTrue();
		Activity.Current.ShouldBeNull();
	}

	[Test]
	public async Task subscribe_should_record_activity(CancellationToken ct) {
		var activityCount   = 0;
		var activityStarted = false;
		var activityStopped = false;

		var simulation = await SeedGame(ct);

		using var listener = new ActivityListener();

		listener.ShouldListenTo = source => source.Name == AppVersionInfo.Current.ProductName;
		listener.Sample         = (ref _) => ActivitySamplingResult.AllDataAndRecorded;

		listener.ActivityStarted = activity => {
			activityStarted = true;
			activity.ShouldNotBeNull();
		};

		listener.ActivityStopped = activity => {
			activityStopped = true;
			activity.ShouldNotBeNull();

			if (activity.OperationName != TraceConstants.Operations.Subscribe) return;

			var message = simulation.Game.GameEvents[activityCount];

			message.Metadata.TryGet<SchemaDataFormat>(SystemMetadataKeys.SchemaDataFormat, out var schemaDataFormat);
			message.Metadata.TryGet<SchemaName>(SystemMetadataKeys.SchemaName, out var schemaName);

			activity.Tags.ShouldContainKeyValue(TraceConstants.Tags.ServerAddress, "localhost");
			activity.Tags.ShouldContainKeyValue(TraceConstants.Tags.DatabaseUser, "admin");
			activity.Tags.ShouldContainKeyValue(TraceConstants.Tags.DatabaseSystemName, "kurrentdb");
			activity.Tags.ShouldContainKeyValue(TraceConstants.Tags.DatabaseOperationName, TraceConstants.Operations.Subscribe);
			activity.Tags.ShouldContainKeyValue(TraceConstants.Tags.DatabaseRecordId, message.RecordId.ToString());
			activity.Tags.ShouldContainKeyValue(TraceConstants.Tags.DatabaseSchemaFormat, schemaDataFormat.ToString());
			activity.Tags.ShouldContainKeyValue(TraceConstants.Tags.DatabaseSchemaName, schemaName.Value);

			activityCount++;
		};

		ActivitySource.AddActivityListener(listener);

		var options = new StreamSubscriptionOptions {
			Stream            = simulation.Game.Stream,
			Start             = StreamRevision.Min,
			CancellationToken = ct
		};

		await using var subscription = await AutomaticClient.Streams
			.Subscribe(options)
			.ShouldNotThrowOrFailAsync()
			.ConfigureAwait(false);

		await foreach (var msg in subscription) {
			if (msg.IsRecord) {
				msg.AsRecord.CompleteActivity(subscription);
				continue;
			}

			if (msg.AsHeartbeat.Type == HeartbeatType.CaughtUp) {
				break;
			}
		}

		activityStopped.ShouldBeTrue();
		activityStarted.ShouldBeTrue();

		activityCount.ShouldBe(simulation.Game.GameEvents.Count);
	}
}
