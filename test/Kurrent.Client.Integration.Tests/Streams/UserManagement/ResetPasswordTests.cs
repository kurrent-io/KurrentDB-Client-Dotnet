using Kurrent.Client.Model;

namespace Kurrent.Client.Tests.Streams.UserManagement;

public class ResetPasswordTests  : KurrentClientTestFixture {
	[Test]
	public async Task reset_password() {
		var user = await CreateTestUser();

		await AutomaticClient.UserManagement
			.ResetPassword(user.LoginName, "new-password")
			.ShouldNotThrowAsync()
			.OnSuccessAsync(success => success.ShouldBe(Success.Instance));

		// create a new client with the new password and append?
	}
}
