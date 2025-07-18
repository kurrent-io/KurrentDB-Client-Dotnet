namespace KurrentDB.Client.Tests.PersistentSubscriptions;

[Trait("Category", "Target:PersistentSubscriptions")]
public class SubscribeToStreamNoDefaultCredentialsTests(ITestOutputHelper output, SubscribeToStreamNoDefaultCredentialsTests.CustomFixture fixture)
	: KurrentDBPermanentTests<SubscribeToStreamNoDefaultCredentialsTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task connect_to_existing_without_permissions() {
		var group  = Fixture.GetGroupName();
		var stream = Fixture.GetStreamName();

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		await Assert.ThrowsAsync<AccessDeniedException>(
			async () => {
				await using var subscription = Fixture.Subscriptions.SubscribeToStream(stream, group);
				await subscription.Messages.AnyAsync();
			}
		).WithTimeout();
	}

	[RetryFact]
	public async Task create_without_permissions() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Assert.ThrowsAsync<AccessDeniedException>(
			() =>
				Fixture.Subscriptions.CreateToStreamAsync(
					stream,
					group,
					new()
				)
		);
	}

	[RetryFact]
	public async Task deleting_without_permissions() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Assert.ThrowsAsync<AccessDeniedException>(() => Fixture.Subscriptions.DeleteToStreamAsync(stream, group));
	}

	[RetryFact]
	public async Task update_existing_without_permissions() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(),
			userCredentials: TestCredentials.Root
		);

		await Fixture.Subscriptions.CreateToStreamAsync(
			stream,
			group,
			new(),
			userCredentials: TestCredentials.Root
		);

		await Assert.ThrowsAsync<AccessDeniedException>(
			() => Fixture.Subscriptions.UpdateToStreamAsync(
				stream,
				group,
				new()
			)
		);
	}

	public class CustomFixture() : KurrentDBPermanentFixture(x => x.WithoutDefaultCredentials());
}
