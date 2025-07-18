using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToStreamConnectToExistingWithoutPermissionObsoleteTests(
	ITestOutputHelper output,
	SubscribeToStreamConnectToExistingWithoutPermissionObsoleteTests.CustomFixture fixture
)
	: KurrentTemporaryTests<SubscribeToStreamConnectToExistingWithoutPermissionObsoleteTests.CustomFixture>(output, fixture) {
	[Fact(Skip = "We disable passing credentials to the operations by default, so this test is not applicable")]
	public async Task connect_to_existing_without_permissions() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		await Assert.ThrowsAsync<AccessDeniedException>(
			async () => {
				using var _ = await Fixture.Subscriptions.SubscribeToStreamAsync(
					stream,
					group,
					delegate { return Task.CompletedTask; }
				);
			}
		).WithTimeout();
	}

	public class CustomFixture() : KurrentDBTemporaryFixture(x => x.WithoutDefaultCredentials());
}
