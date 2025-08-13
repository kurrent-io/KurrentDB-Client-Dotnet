using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests.Projections;

[Trait("Category", "Target:ProjectionManagement")]
public class RestartSubsystemTests(ITestOutputHelper output, RestartSubsystemTests.CustomFixture fixture)
	: KurrentTemporaryTests<RestartSubsystemTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task restart_subsystem_does_not_throw() =>
		await Fixture.DBProjections.RestartSubsystemAsync(userCredentials: TestCredentials.Root);

	[Fact(Skip = "Temporary")]
	public async Task restart_subsystem_throws_when_given_no_credentials() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
			Fixture.DBProjections.RestartSubsystemAsync(userCredentials: TestCredentials.TestUser1)
		);

	[UsedImplicitly]
	public class CustomFixture() : KurrentDBTemporaryFixture(x => x.RunProjections());
}
