using KurrentDb.Client.Tests.TestNode;
using KurrentDb.Client.Tests;

namespace KurrentDb.Client.Tests.Operations;

[Trait("Category", "Target:DbOperations")]
public class ShutdownNodeTests(ITestOutputHelper output, ShutdownNodeTests.NoDefaultCredentialsFixture fixture)
	: KurrentTemporaryTests<ShutdownNodeTests.NoDefaultCredentialsFixture>(output, fixture) {
	[RetryFact]
	public async Task shutdown_does_not_throw() =>
		await Fixture.DbOperations.ShutdownAsync(userCredentials: TestCredentials.Root).ShouldNotThrowAsync();

	public class NoDefaultCredentialsFixture() : KurrentTemporaryFixture(x => x.WithoutDefaultCredentials());
}
