using KurrentDB.Client;

namespace KurrentDB.Client.Tests;

[Trait("Category", "Target:Operations")]
public class MergeIndexTests(ITestOutputHelper output, MergeIndexTests.CustomFixture fixture)
	: KurrentDBPermanentTests<MergeIndexTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task merge_indexes_does_not_throw() =>
		await Fixture.DBOperations
			.MergeIndexesAsync(userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();

	[RetryFact]
	public async Task merge_indexes_without_credentials_throws() =>
		await Fixture.DBOperations
			.MergeIndexesAsync()
			.ShouldThrowAsync<AccessDeniedException>();

	public class CustomFixture() : KurrentDBPermanentFixture(x => x.WithoutDefaultCredentials());
}
