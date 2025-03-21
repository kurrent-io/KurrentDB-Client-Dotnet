using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToAllConnectToExistingWithStartFromNotSetObsoleteTests(ITestOutputHelper output, KurrentDBTemporaryFixture fixture)
	: KurrentTemporaryTests<KurrentDBTemporaryFixture>(output, fixture) {
	[RetryFact]
	public async Task connect_to_existing_with_start_from_not_set() {
		var group  = Fixture.GetGroupName();
		var stream = Fixture.GetStreamName();

		TaskCompletionSource<ResolvedEvent> firstNonSystemEventSource = new();

		foreach (var @event in Fixture.CreateTestEvents(10))
			await Fixture.Streams.AppendToStreamAsync(
				stream,
				StreamState.Any,
				new[] {
					@event
				}
			);

		await Fixture.Subscriptions.CreateToAllAsync(
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		using var subscription = await Fixture.Subscriptions.SubscribeToAllAsync(
			group,
			new PersistentSubscriptionListener{
			 EventAppeared = async (subscription, e, r, ct) => {
				if (SystemStreams.IsSystemStream(e.OriginalStreamId)) {
					await subscription.Ack(e);
					return;
				}

				firstNonSystemEventSource.TrySetResult(e);
				await subscription.Ack(e);
			},
			SubscriptionDropped = (subscription, reason, ex) => {
				if (reason != SubscriptionDroppedReason.Disposed)
					firstNonSystemEventSource.TrySetException(ex!);
			}},
			new SubscribeToPersistentSubscriptionOptions{ UserCredentials = TestCredentials.Root }
		);

		await Assert.ThrowsAsync<TimeoutException>(() => firstNonSystemEventSource.Task.WithTimeout());
	}
}
