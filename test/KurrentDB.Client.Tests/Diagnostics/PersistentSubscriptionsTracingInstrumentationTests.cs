using KurrentDB.Client.Tests.Fixtures;
using KurrentDB.Client.Tests.TestNode;
using KurrentDB.Diagnostics.Tracing;

namespace KurrentDB.Client.Tests.Diagnostics;

[Trait("Category", "Target:Diagnostics")]
public class PersistentSubscriptionsTracingInstrumentationTests(ITestOutputHelper output, DiagnosticsFixture fixture)
	: KurrentDBPermanentTests<DiagnosticsFixture>(output, fixture) {
	[RetryFact]
	public async Task persistent_subscription_restores_remote_append_context() {
		var stream = Fixture.GetStreamName();
		var events = Fixture.CreateTestEvents(2, metadata: Fixture.CreateTestJsonMetadata()).ToArray();

		var groupName = $"{stream}-group";
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			groupName,
			new()
		);

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			events
		);

		string? subscriptionId = null;
		await Subscribe().WithTimeout();

		var appendActivity = Fixture
			.GetActivitiesForOperation(TracingConstants.Operations.Append, stream)
			.SingleOrDefault()
			.ShouldNotBeNull();

		var subscribeActivities = Fixture
			.GetActivitiesForOperation(TracingConstants.Operations.Subscribe, stream)
			.ToArray();

		subscriptionId.ShouldNotBeNull();
		subscribeActivities.Length.ShouldBe(events.Length);

		for (var i = 0; i < subscribeActivities.Length; i++) {
			subscribeActivities[i].TraceId.ShouldBe(appendActivity.Context.TraceId);
			subscribeActivities[i].ParentSpanId.ShouldBe(appendActivity.Context.SpanId);
			subscribeActivities[i].HasRemoteParent.ShouldBeTrue();

			Fixture.AssertSubscriptionActivityHasExpectedTags(
				subscribeActivities[i],
				stream,
				events[i].EventId.ToString(),
				subscriptionId
			);
		}

		return;

		async Task Subscribe() {
			await using var subscription = Fixture.Subscriptions.SubscribeToStream(stream, groupName);
			await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

			int eventsAppeared = 0;
			while (await enumerator.MoveNextAsync()) {
				if (enumerator.Current is PersistentSubscriptionMessage.SubscriptionConfirmation(var sid))
					subscriptionId = sid;

				if (enumerator.Current is not PersistentSubscriptionMessage.Event(_, _))
					continue;

				eventsAppeared++;
				if (eventsAppeared >= events.Length)
					return;
			}
		}
	}

	[RetryFact]
	public async Task persistent_subscription_handles_non_json_events() {
		var stream = Fixture.GetStreamName();
		var events = Fixture.CreateTestEvents(
			2,
			metadata: Fixture.CreateTestJsonMetadata(),
			contentType: Constants.Metadata.ContentTypes.ApplicationOctetStream
		).ToArray();

		var groupName = $"{stream}-group";
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			groupName,
			new()
		);

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			events
		);

		await Subscribe().WithTimeout();

		return;

		async Task Subscribe() {
			await using var subscription = Fixture.Subscriptions.SubscribeToStream(stream, groupName);
			await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

			var eventsAppeared = 0;
			while (await enumerator.MoveNextAsync()) {
				if (enumerator.Current is PersistentSubscriptionMessage.Event(_, _))
					eventsAppeared++;

				if (eventsAppeared >= events.Length)
					return;
			}
		}
	}

	[RetryFact]
	public async Task persistent_subscription_handles_invalid_json_metadata() {
		var stream = Fixture.GetStreamName();
		var events = Fixture.CreateTestEvents(
			2,
			metadata: "clearlynotavalidjsonobject"u8.ToArray()
		).ToArray();

		var groupName = $"{stream}-group";
		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			groupName,
			new()
		);

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			events
		);

		await Subscribe().WithTimeout();

		return;

		async Task Subscribe() {
			await using var subscription = Fixture.Subscriptions.SubscribeToStream(stream, groupName);
			await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

			var eventsAppeared = 0;
			while (await enumerator.MoveNextAsync()) {
				if (enumerator.Current is PersistentSubscriptionMessage.Event(_, _))
					eventsAppeared++;

				if (eventsAppeared >= events.Length)
					return;
			}
		}
	}
}
