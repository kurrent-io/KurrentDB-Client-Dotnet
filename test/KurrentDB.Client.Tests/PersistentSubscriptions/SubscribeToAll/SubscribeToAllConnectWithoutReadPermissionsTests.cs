using KurrentDB.Client;
using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToAllConnectWithoutReadPermissionsTests(ITestOutputHelper output, KurrentDBTemporaryFixture fixture)
	: KurrentTemporaryTests<KurrentDBTemporaryFixture>(output, fixture) {
	[RetryFact]
	public async Task connect_to_existing_without_read_all_permissions() {
		var group = Fixture.GetGroupName();
		var user  = Fixture.GetUserCredentials();

		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);

		await Fixture.DbUsers.CreateUserWithRetry(
			user.Username!,
			user.Username!,
			[],
			user.Password!,
			TestCredentials.Root
		);

		await Assert.ThrowsAsync<AccessDeniedException>(
			async () => {
				await using var subscription = Fixture.Subscriptions.SubscribeToAll(group, userCredentials: user);
				await subscription.Messages.AnyAsync().AsTask().WithTimeout();
			}
		);
	}
}
