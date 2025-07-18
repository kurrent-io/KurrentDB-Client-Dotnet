namespace KurrentDB.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToAllReplayParkedObsoleteTests(ITestOutputHelper output, SubscribeToAllReplayParkedTests.CustomFixture fixture)
	: KurrentDBPermanentTests<SubscribeToAllReplayParkedTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task does_not_throw() {
		var group = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);

		await Fixture.Subscriptions.ReplayParkedMessagesToAllAsync(
			group,
			userCredentials: TestCredentials.Root
		);

		await Fixture.Subscriptions.ReplayParkedMessagesToAllAsync(
			group,
			100,
			userCredentials: TestCredentials.Root
		);
	}

	[RetryFact]
	public async Task throws_when_given_non_existing_subscription() {
		var group = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);

		var nonExistingGroup = Fixture.GetGroupName();
		await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(
			() =>
				Fixture.Subscriptions.ReplayParkedMessagesToAllAsync(
					nonExistingGroup,
					userCredentials: TestCredentials.Root
				)
		);
	}

	[RetryFact]
	public async Task throws_with_no_credentials() {
		var group = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);

		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				Fixture.Subscriptions.ReplayParkedMessagesToAllAsync(group)
		);
	}

	[RetryFact]
	public async Task throws_with_non_existing_user() {
		var group = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToAllAsync(group, new(), userCredentials: TestCredentials.Root);

		await Assert.ThrowsAsync<NotAuthenticatedException>(
			() =>
				Fixture.Subscriptions.ReplayParkedMessagesToAllAsync(
					group,
					userCredentials: TestCredentials.TestBadUser
				)
		);
	}

	[RetryFact]
	public async Task throws_with_normal_user_credentials() {
		var user = Fixture.GetUserCredentials();

		await Fixture.DBUsers
			.CreateUserWithRetry(user.Username!, user.Username!, [], user.Password!, TestCredentials.Root)
			.WithTimeout();

		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				Fixture.Subscriptions.ReplayParkedMessagesToAllAsync(
					Fixture.GetGroupName(),
					userCredentials: user
				)
		);
	}

	public class CustomFixture() : KurrentDBPermanentFixture(x => x.WithoutDefaultCredentials());
}
