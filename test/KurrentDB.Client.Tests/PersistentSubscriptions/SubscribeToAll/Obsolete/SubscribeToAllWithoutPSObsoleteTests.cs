using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToAllWithoutPSObsoleteTests(ITestOutputHelper output, KurrentDBTemporaryFixture fixture)
	: KurrentTemporaryTests<KurrentDBTemporaryFixture>(output, fixture) {
	[RetryFact]
	public async Task list_without_persistent_subscriptions() {
		await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(async () =>
			await Fixture.Subscriptions.ListToAllAsync(userCredentials: TestCredentials.Root)
		);
	}
}
