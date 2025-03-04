using KurrentDb.Client.Tests.TestNode;
using KurrentDb.Client;

namespace KurrentDb.Client.Tests;

[Trait("Category", "Target:DbOperations")]
public class ShutdownNodeAuthenticationTests(ITestOutputHelper output, ShutdownNodeAuthenticationTests.CustomFixture fixture)
	: KurrentTemporaryTests<ShutdownNodeAuthenticationTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task shutdown_without_credentials_throws() =>
		await Fixture.DbOperations.ShutdownAsync().ShouldThrowAsync<AccessDeniedException>();

	public class CustomFixture() : KurrentTemporaryFixture(x => x.WithoutDefaultCredentials());
}
