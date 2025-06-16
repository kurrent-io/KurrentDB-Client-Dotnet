using KurrentDB.Client;
using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests.Operations;

[Trait("Category", "Target:Operations")]
public class RestartPersistentSubscriptionsTests(ITestOutputHelper output, RestartPersistentSubscriptionsTests.CustomFixture fixture)
	: KurrentTemporaryTests<RestartPersistentSubscriptionsTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task restart_persistent_subscriptions_does_not_throw() =>
		await Fixture.DBOperations
			.RestartPersistentSubscriptions(userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();

	[RetryFact]
	public async Task restart_persistent_subscriptions_without_credentials_throws() =>
		await Fixture.DBOperations
			.RestartPersistentSubscriptions()
			.ShouldThrowAsync<AccessDeniedException>();

	public class CustomFixture() : KurrentDBTemporaryFixture(x => x.WithoutDefaultCredentials());
}
