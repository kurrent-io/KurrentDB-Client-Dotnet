using Kurrent.Client.Model;
using Kurrent.Client.Testing.TUnit;

namespace Kurrent.Client.Tests.Streams.UserManagement;

public class CreateUserTests : KurrentClientTestFixture {
	[Test]
	public async Task creates_user() {
		var user = Users.Generate();

		await AutomaticClient.UserManagement
			.CreateUser(user.LoginName, user.FullName, user.Groups, user.Password)
			.ShouldNotThrowAsync();

		await AutomaticClient.UserManagement
			.GetUser(user.LoginName)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(details => details.ShouldBeEquivalentTo(user.Details));
	}

	[Test]
	[CreateUserNullOrEmptyInputTestCases]
	public async Task throws_null_exception_when_null_input_is_provided(TestUser user) {
		var response = async () => await AutomaticClient.UserManagement
			.CreateUser(user.LoginName, user.FullName, user.Groups, user.Password);

		await response.ShouldThrowAsync<ArgumentNullException>();
	}

	[Test]
	[InvalidCredentialsTestCases]
	public async Task throws_access_denied_when_credentials_are_insufficient(TestUser user, Type type) =>
		await AutomaticClient.UserManagement
			.CreateUser(user.LoginName, user.FullName, user.Groups, user.Password)
            .ShouldFailAsync(error => error.Value.ShouldBeOfType(type));


	public class InvalidCredentialsTestCases : TestCaseGenerator<TestUser, Type> {
		protected override IEnumerable<(TestUser, Type)> Data() => [
			(Users.WithNoCredentials(), typeof(ErrorDetails.AccessDenied)),
			(Users.WithInvalidCredentials(), typeof(ErrorDetails.NotAuthenticated)),
			(Users.WithInvalidCredentials(wrongPassword: false), typeof(ErrorDetails.NotAuthenticated)),
		];
	}

	public class CreateUserNullOrEmptyInputTestCases : TestCaseGenerator<TestUser> {
		protected override IEnumerable<TestUser> Data() => [
			Users.FinishWith((_, x) => x.LoginName = null!).Generate(),
			Users.FinishWith((_, x) => x.FullName = null!).Generate(),
			Users.FinishWith((_, x) => x.Groups = null!).Generate(),
			Users.FinishWith((_, x) => x.Password = null!).Generate(),
			Users.FinishWith((_, x) => x.LoginName = string.Empty).Generate(),
			Users.FinishWith((_, x) => x.FullName = string.Empty).Generate(),
			Users.FinishWith((_, x) => x.Password = string.Empty).Generate()
		];
	}
}
