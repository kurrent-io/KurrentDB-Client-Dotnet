using KurrentDB.Client.Tests.TestNode;
using KurrentDB.Client;

namespace KurrentDB.Client.Tests.Operations;

[Trait("Category", "Target:DbOperations")]
public class RestartPersistentSubscriptionsTests(ITestOutputHelper output, RestartPersistentSubscriptionsTests.CustomFixture fixture)
	: KurrentTemporaryTests<RestartPersistentSubscriptionsTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task restart_persistent_subscriptions_does_not_throw() =>
		await Fixture.DbOperations
			.RestartPersistentSubscriptions(userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();

	[RetryFact]
	public async Task restart_persistent_subscriptions_without_credentials_throws() =>
		await Fixture.DbOperations
			.RestartPersistentSubscriptions()
			.ShouldThrowAsync<AccessDeniedException>();

	public class CustomFixture() : KurrentDBTemporaryFixture(x => x.WithoutDefaultCredentials());
}
