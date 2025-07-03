using Kurrent.Client.Model;

namespace Kurrent.Client.Tests.Streams.UserManagement;

public class EnableUserTests : KurrentClientTestFixture {
	[Test]
	public async Task enable_user() {
		var user = await CreateTestUser();

		await AutomaticClient.UserManagement
			.EnableUser(user.LoginName)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(success => success.ShouldBe(Success.Instance));

		await AutomaticClient.UserManagement
			.GetUser(user.LoginName)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(details => details.Disabled.ShouldBeFalse());
	}
}
