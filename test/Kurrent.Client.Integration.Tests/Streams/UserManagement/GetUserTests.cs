namespace Kurrent.Client.Tests.Streams.UserManagement;

public class GetUserTests : KurrentClientTestFixture {
	[Test]
	public async Task get_current_user() {
		await AutomaticClient.UserManagement
			.GetUser("admin")
			.OnSuccessAsync(details => details.LoginName.ShouldBe("admin"));
	}
}
