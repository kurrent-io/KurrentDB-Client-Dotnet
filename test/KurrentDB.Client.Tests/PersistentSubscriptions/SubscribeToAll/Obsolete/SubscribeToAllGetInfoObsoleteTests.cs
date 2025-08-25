// ReSharper disable InconsistentNaming

using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToAllGetInfoObsoleteTests(SubscribeToAllGetInfoObsoleteTests.CustomFixture fixture)
	: IClassFixture<SubscribeToAllGetInfoObsoleteTests.CustomFixture> {
	static readonly PersistentSubscriptionSettings Settings = new(
		true,
		Position.Start,
		true,
		TimeSpan.FromSeconds(9),
		11,
		303,
		30,
		909,
		TimeSpan.FromSeconds(1),
		1,
		1,
		500,
		SystemConsumerStrategies.Pinned
	);

	[RetryFact]
	public async Task throws_with_non_existing_subscription() {
		var group = $"NonExisting-{fixture.GetGroupName()}";

		await Assert.ThrowsAsync<PersistentSubscriptionNotFoundException>(async () => await fixture.Subscriptions.GetInfoToAllAsync(group, userCredentials: TestCredentials.Root));
	}

	[Fact(Skip = "We disable passing credentials to the operations by default, so this test is not applicable")]
	public async Task throws_with_no_credentials() {
		var group = $"NonExisting-{fixture.GetGroupName()}";

		await Assert.ThrowsAsync<AccessDeniedException>(async () =>
			await fixture.Subscriptions.GetInfoToAllAsync(group)
		);
	}

	[RetryFact]
	public async Task throws_with_non_existing_user() {
		var group = $"NonExisting-{fixture.GetGroupName()}";

		await Assert.ThrowsAsync<NotAuthenticatedException>(async () =>
			await fixture.Subscriptions.GetInfoToAllAsync(group, userCredentials: TestCredentials.TestBadUser)
		);
	}

	[RetryFact]
	public async Task returns_result_with_normal_user_credentials() {
		var result = await fixture.Subscriptions.GetInfoToAllAsync(fixture.Group, userCredentials: TestCredentials.Root);

		Assert.Equal("$all", result.EventSource);
	}

	public class CustomFixture : KurrentDBTemporaryFixture {
		public CustomFixture() : base(x => x.WithoutDefaultCredentials()) {
			Group = GetGroupName();

			OnSetup += async () => {
				await Subscriptions.CreateToAllAsync(Group, Settings, userCredentials: TestCredentials.Root);

				var counter = 0;
				var tcs     = new TaskCompletionSource();

				await Subscriptions.SubscribeToAllAsync(
					Group,
					(s, e, r, ct) => {
						counter++;

						switch (counter) {
							case 1:
								s.Nack(PersistentSubscriptionNakEventAction.Park, "Test", e);
								break;

							case > 10:
								tcs.TrySetResult();
								break;
						}

						return Task.CompletedTask;
					},
					userCredentials: TestCredentials.Root
				);
			};
		}

		public string Group { get; }
	};
}
