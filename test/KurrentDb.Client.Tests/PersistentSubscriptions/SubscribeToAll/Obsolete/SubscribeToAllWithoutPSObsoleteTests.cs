using KurrentDb.Client;
using KurrentDb.Client.Tests.TestNode;
using KurrentDb.Client.Tests;

namespace KurrentDb.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToAllWithoutPSObsoleteTests(ITestOutputHelper output, KurrentTemporaryFixture fixture)
	: KurrentTemporaryTests<KurrentTemporaryFixture>(output, fixture) {
	[RetryFact]
	public async Task list_without_persistent_subscriptions() {
		await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
			async () =>
				await Fixture.Subscriptions.ListToAllAsync(userCredentials: TestCredentials.Root)
		);
	}
}
