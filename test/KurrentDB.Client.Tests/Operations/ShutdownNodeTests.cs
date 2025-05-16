using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests.Operations;

[Trait("Category", "Target:Operations")]
public class ShutdownNodeTests(ITestOutputHelper output, ShutdownNodeTests.NoDefaultCredentialsFixture fixture)
	: KurrentTemporaryTests<ShutdownNodeTests.NoDefaultCredentialsFixture>(output, fixture) {
	[RetryFact]
	public async Task shutdown_does_not_throw() =>
		await Fixture.DBOperations.ShutdownAsync(userCredentials: TestCredentials.Root).ShouldNotThrowAsync();

	public class NoDefaultCredentialsFixture() : KurrentDBTemporaryFixture(x => x.WithoutDefaultCredentials());
}
