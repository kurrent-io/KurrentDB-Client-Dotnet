using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToAllConnectToExistingWithStartFromNotSetTests(ITestOutputHelper output, KurrentDBTemporaryFixture fixture)
	: KurrentTemporaryTests<KurrentDBTemporaryFixture>(output, fixture) {
	[RetryFact]
	public async Task connect_to_existing_with_start_from_not_set() {
		var group  = Fixture.GetGroupName();
		var stream = Fixture.GetStreamName();

		foreach (var @event in Fixture.CreateTestEvents(10))
			await Fixture.Streams.AppendToStreamAsync(
				stream,
				StreamState.Any,
				[@event]
			);

		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);
		await using var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: TestCredentials.Root);

		await Assert.ThrowsAsync<TimeoutException>(
			() => subscription.Messages
				.OfType<PersistentSubscriptionMessage.Event>()
				.Where(e => !SystemStreams.IsSystemStream(e.ResolvedEvent.OriginalStreamId))
				.AnyAsync()
				.AsTask()
				.WithTimeout()
		);
	}
}
