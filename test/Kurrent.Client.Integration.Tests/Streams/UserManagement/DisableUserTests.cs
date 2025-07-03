using Kurrent.Client.Model;

namespace Kurrent.Client.Tests.Streams.UserManagement;

public class DisableUserTests : KurrentClientTestFixture {
	[Test]
	public async Task disable_user() {
		var user = await CreateTestUser();

		await AutomaticClient.UserManagement
			.DisableUser(user.LoginName)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(success => success.ShouldBe(Success.Instance));

		await AutomaticClient.UserManagement
			.GetUser(user.LoginName)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(details => details.Disabled.ShouldBeTrue());
	}
}
