using KurrentDB.Client;

namespace KurrentDB.Client.Tests;

[Trait("Category", "Target:DbOperations")]
public class MergeIndexTests(ITestOutputHelper output, MergeIndexTests.CustomFixture fixture)
	: KurrentPermanentTests<MergeIndexTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task merge_indexes_does_not_throw() =>
		await Fixture.DbOperations
			.MergeIndexesAsync(userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();

	[RetryFact]
	public async Task merge_indexes_without_credentials_throws() =>
		await Fixture.DbOperations
			.MergeIndexesAsync()
			.ShouldThrowAsync<AccessDeniedException>();

	public class CustomFixture() : KurrentPermanentFixture(x => x.WithoutDefaultCredentials());
}
