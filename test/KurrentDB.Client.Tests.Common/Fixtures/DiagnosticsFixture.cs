// ReSharper disable InconsistentNaming

using System.Collections.Concurrent;
using System.Diagnostics;
using KurrentDB.Client.Diagnostics;
using KurrentDB.Client.Tests.TestNode;
using KurrentDB.Diagnostics;
using KurrentDB.Diagnostics.Telemetry;
using KurrentDB.Diagnostics.Tracing;

namespace KurrentDB.Client.Tests.Fixtures;

public class DiagnosticsFixture : KurrentDBPermanentFixture {
	readonly ConcurrentDictionary<(string Operation, string Stream), List<Activity>> Activities = [];

	public DiagnosticsFixture() : base(x => x.RunProjections()) {
		var diagnosticActivityListener = new ActivityListener {
			ShouldListenTo = source => source.Name == KurrentDBClientDiagnostics.InstrumentationName,
			Sample         = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
			ActivityStopped = activity => {
				var operation = (string?)activity.GetTagItem(TelemetryTags.Database.Operation);
				var stream    = (string?)activity.GetTagItem(TelemetryTags.KurrentDB.Stream);

				if (operation is null || stream is null)
					return;

				Activities.AddOrUpdate(
					(operation, stream),
					_ => [activity],
					(_, activities) => {
						activities.Add(activity);
						return activities;
					}
				);
			}
		};

		OnSetup += () => {
			ActivitySource.AddActivityListener(diagnosticActivityListener);
			return Task.CompletedTask;
		};

		OnTearDown = () => {
			diagnosticActivityListener.Dispose();
			return Task.CompletedTask;
		};
	}

	public List<Activity> GetActivitiesForOperation(string operation, params string[] streams) =>
		streams.SelectMany(stream => Activities.TryGetValue((operation, stream), out var activities) ? activities : []).ToList();

	public void AssertAppendActivityHasExpectedTags(Activity activity, string stream) {
		var expectedTags = new Dictionary<string, string?> {
			{ TelemetryTags.Database.System, KurrentDBClientDiagnostics.InstrumentationName },
			{ TelemetryTags.Database.Operation, TracingConstants.Operations.Append },
			{ TelemetryTags.KurrentDB.Stream, stream },
			{ TelemetryTags.Database.User, TestCredentials.Root.Username },
			{ TelemetryTags.Otel.StatusCode, ActivityStatusCodeHelper.OkStatusCodeTagValue }
		};

		foreach (var tag in expectedTags)
			activity.Tags.ShouldContain(tag);
	}

	public void AssertErroneousAppendActivityHasExpectedTags(Activity activity, Exception actualException) {
		var expectedTags = new Dictionary<string, string?> {
			{ TelemetryTags.Otel.StatusCode, ActivityStatusCodeHelper.ErrorStatusCodeTagValue }
		};

		foreach (var tag in expectedTags)
			activity.Tags.ShouldContain(tag);

		var actualEvent = activity.Events.ShouldHaveSingleItem();

		actualEvent.Name.ShouldBe(TelemetryTags.Exception.EventName);
		actualEvent.Tags.ShouldContain(
			new KeyValuePair<string, object?>(TelemetryTags.Exception.Type, actualException.GetType().FullName)
		);

		actualEvent.Tags.ShouldContain(
			new KeyValuePair<string, object?>(TelemetryTags.Exception.Message, actualException.Message)
		);

		actualEvent.Tags.Any(x => x.Key == TelemetryTags.Exception.Stacktrace).ShouldBeTrue();
	}

	public void AssertSubscriptionActivityHasExpectedTags(
		Activity activity,
		string stream,
		string eventId,
		string? subscriptionId = null
	) {
		var expectedTags = new Dictionary<string, string?> {
			{ TelemetryTags.Database.System, KurrentDBClientDiagnostics.InstrumentationName },
			{ TelemetryTags.Database.Operation, TracingConstants.Operations.Subscribe },
			{ TelemetryTags.KurrentDB.Stream, stream },
			{ TelemetryTags.KurrentDB.EventId, eventId },
			{ TelemetryTags.KurrentDB.EventType, TestEventType },
			{ TelemetryTags.Database.User, TestCredentials.Root.Username }
		};

		if (subscriptionId != null)
			expectedTags[TelemetryTags.KurrentDB.SubscriptionId] = subscriptionId;

		foreach (var tag in expectedTags) {
			activity.Tags.ShouldContain(tag);
		}
	}
}
