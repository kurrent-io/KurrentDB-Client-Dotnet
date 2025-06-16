using KurrentDB.Client;
using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests;

[Trait("Category", "Target:Operations")]
public class ShutdownNodeAuthenticationTests(ITestOutputHelper output, ShutdownNodeAuthenticationTests.CustomFixture fixture)
	: KurrentTemporaryTests<ShutdownNodeAuthenticationTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task shutdown_without_credentials_throws() =>
		await Fixture.DBOperations.ShutdownAsync().ShouldThrowAsync<AccessDeniedException>();

	public class CustomFixture() : KurrentDBTemporaryFixture(x => x.WithoutDefaultCredentials());
}
