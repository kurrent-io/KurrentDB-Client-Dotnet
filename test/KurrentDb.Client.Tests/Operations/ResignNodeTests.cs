using KurrentDb.Client.Tests.TestNode;
using KurrentDb.Client;

namespace KurrentDb.Client.Tests.Operations;

[Trait("Category", "Target:DbOperations")]
public class ResignNodeTests(ITestOutputHelper output, ResignNodeTests.CustomFixture fixture)
	: KurrentTemporaryTests<ResignNodeTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task resign_node_does_not_throw() =>
		await Fixture.DbOperations
			.ResignNodeAsync(userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();

	[RetryFact]
	public async Task resign_node_without_credentials_throws() =>
		await Fixture.DbOperations
			.ResignNodeAsync()
			.ShouldThrowAsync<AccessDeniedException>();

	public class CustomFixture() : KurrentTemporaryFixture(x => x.WithoutDefaultCredentials());
}
