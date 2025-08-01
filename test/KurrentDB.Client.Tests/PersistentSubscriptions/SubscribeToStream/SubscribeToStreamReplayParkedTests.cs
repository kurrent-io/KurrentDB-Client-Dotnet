namespace KurrentDB.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToStreamReplayParkedTests(ITestOutputHelper output, SubscribeToStreamReplayParkedTests.CustomFixture fixture)
	: KurrentDBPermanentTests<SubscribeToStreamReplayParkedTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task does_not_throw() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToStreamAsync(stream, group, new(), userCredentials: TestCredentials.Root);

		await Fixture.Subscriptions.ReplayParkedMessagesToStreamAsync(
			stream,
			group,
			userCredentials: TestCredentials.Root
		);

		await Fixture.Subscriptions.ReplayParkedMessagesToStreamAsync(
			stream,
			group,
			100,
			userCredentials: TestCredentials.Root
		);
	}

	[RetryFact]
	public async Task throws_with_no_credentials() {
		var group  = Fixture.GetGroupName();
		var stream = Fixture.GetStreamName();

		await Fixture.Subscriptions.CreateToStreamAsync(stream, group, new(), userCredentials: TestCredentials.Root);

		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				Fixture.Subscriptions.ReplayParkedMessagesToStreamAsync(stream, group)
		);
	}

	[Fact(Skip = "Unable to produce same behavior with HTTP fallback!")]
	public async Task throws_with_non_existing_user() {
		var group  = Fixture.GetGroupName();
		var stream = Fixture.GetStreamName();

		await Fixture.Subscriptions.CreateToStreamAsync(stream, group, new(), userCredentials: TestCredentials.Root);

		await Assert.ThrowsAsync<NotAuthenticatedException>(
			() => Fixture.Subscriptions.ReplayParkedMessagesToStreamAsync(stream, group, userCredentials: TestCredentials.TestBadUser)
		);
	}

	[RetryFact]
	public async Task throws_with_normal_user_credentials() {
		var group  = Fixture.GetGroupName();
		var stream = Fixture.GetStreamName();
		var user   = Fixture.GetUserCredentials();

		await Fixture.DBUsers
			.CreateUserWithRetry(user.Username!, user.Username!, [], user.Password!, TestCredentials.Root)
			.WithTimeout();

		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				Fixture.Subscriptions.ReplayParkedMessagesToStreamAsync(stream, group, userCredentials: user)
		);
	}

	public class CustomFixture() : KurrentDBPermanentFixture(x => x.WithoutDefaultCredentials());
}
