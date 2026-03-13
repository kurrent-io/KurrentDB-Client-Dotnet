// ReSharper disable AccessToDisposedClosure

using System.Diagnostics;
using KurrentDB.Client.Diagnostics;
using KurrentDB.Client.Tests.Fixtures;
using KurrentDB.Diagnostics.Telemetry;
using KurrentDB.Diagnostics;
using KurrentDB.Diagnostics.Tracing;
using static KurrentDB.Diagnostics.Tracing.TracingConstants;

namespace KurrentDB.Client.Tests.Diagnostics;

[Trait("Category", "Target:Diagnostics")]
[Trait("Category", "Operation:AppendRecords")]
public class AppendRecordsTracingTests(ITestOutputHelper output, DiagnosticsFixture fixture) : KurrentDBPermanentTests<DiagnosticsFixture>(output, fixture) {
	[MinimumVersion.Fact(26, 1)]
	public async Task append_records_creates_trace_activity() {
		// Arrange
		var traceId = Fixture.CreateTraceId();

		var stream = Fixture.GetStreamName();
		var records = Fixture.CreateTestEvents(3).Select(e => new AppendRecord(stream, e));

		// Act
		var result = await Fixture.Streams.AppendRecordsAsync(records);

		// Assert
		result.Position.ShouldBePositive();

		var appendActivities = Fixture.GetActivities(TracingConstants.Operations.MultiAppend, traceId);

		appendActivities.ShouldNotBeEmpty();
		appendActivities.Count.ShouldBe(1);

		var activity = appendActivities.First();

		var expectedTags = new Dictionary<string, string?> {
			{ TelemetryTags.Database.System, KurrentDBClientDiagnostics.InstrumentationName },
			{ TelemetryTags.Database.Operation, TracingConstants.Operations.MultiAppend },
			{ TelemetryTags.Database.User, TestCredentials.Root.Username },
			{ TelemetryTags.Otel.StatusCode, ActivityStatusCodeHelper.OkStatusCodeTagValue }
		};

		foreach (var tag in expectedTags)
			activity.Tags.ShouldContain(tag);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task append_records_with_exceptions_traces_error() {
		// Arrange
		var traceId = Fixture.CreateTraceId();

		var stream = Fixture.GetStreamName();
		var records = Fixture.CreateTestEvents(1).Select(e => new AppendRecord(stream, e));
		var checks = new ConsistencyCheck[] {
			new ConsistencyCheck.StreamStateCheck(stream, StreamState.StreamExists)
		};

		// Act
		var appendTask = async () => await Fixture.Streams.AppendRecordsAsync(records, checks);
		var rex = await appendTask.ShouldThrowAsync<AppendConsistencyViolationException>();

		// Assert
		var appendActivities = Fixture.GetActivities(TracingConstants.Operations.MultiAppend, traceId);

		appendActivities.ShouldNotBeEmpty();
		appendActivities.Count.ShouldBe(1);

		var activity = appendActivities.First();
		activity.Status.ShouldBe(ActivityStatusCode.Error);
		activity.Events.ShouldHaveSingleItem();

		var activityEvent = activity.Events.First();
		activityEvent.Name.ShouldBe(TelemetryTags.Exception.EventName);
		activityEvent.Tags.Any(tag => tag.Key == TelemetryTags.Exception.Message).ShouldBeTrue();
		activityEvent.Tags.Any(tag => tag.Key == TelemetryTags.Exception.Stacktrace).ShouldBeTrue();
		activityEvent.Tags.Any(tag => tag.Key == TelemetryTags.Exception.Type && (string?)tag.Value == rex.GetType().FullName).ShouldBeTrue();
	}
}
