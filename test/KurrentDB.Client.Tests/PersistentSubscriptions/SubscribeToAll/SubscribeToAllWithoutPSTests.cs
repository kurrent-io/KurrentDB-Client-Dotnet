using KurrentDB.Client;
using KurrentDB.Client.Tests.TestNode;
using KurrentDB.Client.Tests;

namespace KurrentDB.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToAllWithoutPsTests(ITestOutputHelper output, KurrentTemporaryFixture fixture)
	: KurrentTemporaryTests<KurrentTemporaryFixture>(output, fixture) {
	[RetryFact]
	public async Task list_without_persistent_subscriptions() {
		await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
			async () =>
				await Fixture.Subscriptions.ListToAllAsync(userCredentials: TestCredentials.Root)
		);
	}
}
