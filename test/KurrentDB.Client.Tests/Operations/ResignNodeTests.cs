using KurrentDB.Client;
using KurrentDB.Client.Tests.TestNode;
using KurrentDB.Client.Tests;

namespace KurrentDB.Client.Tests.Operations;

[Trait("Category", "Target:Operations")]
public class ResignNodeTests(ITestOutputHelper output, ResignNodeTests.CustomFixture fixture)
	: KurrentTemporaryTests<ResignNodeTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task resign_node_does_not_throw() =>
		await Fixture.DBOperations
			.ResignNodeAsync(userCredentials: TestCredentials.Root)
			.ShouldNotThrowAsync();

	[RetryFact]
	public async Task resign_node_without_credentials_throws() =>
		await Fixture.DBOperations
			.ResignNodeAsync()
			.ShouldThrowAsync<AccessDeniedException>();

	public class CustomFixture() : KurrentDBTemporaryFixture(x => x.WithoutDefaultCredentials());
}
