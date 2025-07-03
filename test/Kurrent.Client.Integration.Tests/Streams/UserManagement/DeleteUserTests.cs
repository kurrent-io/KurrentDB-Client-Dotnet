using Kurrent.Client.Model;

namespace Kurrent.Client.Tests.Streams.UserManagement;

public class DeleteUserTests : KurrentClientTestFixture {
	[Test]
	public async Task creates_user() {
		var user = await CreateTestUser();

		await AutomaticClient.UserManagement
			.GetUser(user.LoginName)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(details => details.ShouldBeEquivalentTo(user.Details));
	}

	[Test]
	public async Task throws_user_not_found_when_user_does_not_exist() {
		var user = Users.Generate();

		await AutomaticClient.UserManagement
			.DeleteUser(user.LoginName)
			.ShouldNotThrowAsync()
			.OnFailureAsync(failure => failure.Value.ShouldBeOfType<ErrorDetails.UserNotFound>());
	}
}
