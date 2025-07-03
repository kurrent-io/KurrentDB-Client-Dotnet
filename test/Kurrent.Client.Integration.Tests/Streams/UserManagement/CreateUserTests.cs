using Kurrent.Client.Model;
using Kurrent.Client.Testing.TUnit;

namespace Kurrent.Client.Tests.Streams.UserManagement;

public class CreateUserTests : KurrentClientTestFixture {
	[Test]
	public async Task creating_user_with_password() {
		var user = Users.Generate();

		await AutomaticClient.UserManagement
			.CreateUser(user.LoginName, user.FullName, user.Groups, user.Password)
			.ShouldNotThrowAsync();
	}

	[Test]
	[CreateUserTestCases]
	public async Task creating_user_with_insufficient_credentials_throws(TestUser user, Type type) =>
		await AutomaticClient.UserManagement
			.CreateUser(user.LoginName, user.FullName, user.Groups, user.Password)
            .ShouldFailAsync(error => error.Value.ShouldBeOfType<ErrorDetails.StreamRevisionConflict>());


	public class CreateUserTestCases : TestCaseGenerator<TestUser, Type> {
		protected override IEnumerable<(TestUser, Type)> Data() => [
			(Users.WithNoCredentials(), typeof(ErrorDetails.AccessDenied)),
			(Users.WithInvalidCredentials(), typeof(ErrorDetails.NotAuthenticated)),
			(Users.WithInvalidCredentials(wrongPassword: false), typeof(ErrorDetails.NotAuthenticated)),
		];
	}
}
