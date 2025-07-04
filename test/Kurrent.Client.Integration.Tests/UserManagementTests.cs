using Kurrent.Client.Model;
using Kurrent.Client.Testing.TUnit;

namespace Kurrent.Client.Tests.Streams;

public class UserManagementTests : KurrentClientTestFixture {
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
	public async Task get_user() {
		var user = await CreateTestUser();

		await AutomaticClient.UserManagement
			.GetUser(user.LoginName)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(details => details.ShouldBeEquivalentTo(user.Details));
	}

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

	[Test]
	public async Task list_users() {
		var users = await CreateTestUsers();

		var admin = new UserDetails {
			LoginName       = "admin",
			FullName        = "KurrentDB Administrator",
			Groups          = ["$admins"],
			Disabled        = false,
			DateLastUpdated = null
		};

		var ops = new UserDetails {
			LoginName       = "ops",
			FullName        = "KurrentDB Operations",
			Groups          = ["$ops"],
			Disabled        = false,
			DateLastUpdated = null
		};

		var expected = new[] { admin, ops }
			.Concat(users.Select(user => user.Details))
			.ToArray();

		var actual = await AutomaticClient.UserManagement
			.ListAllAsync()
			.Select(user => new UserDetails {
					LoginName       = user.LoginName,
					FullName        = user.FullName,
					Groups          = user.Groups,
					Disabled        = user.Disabled,
					DateLastUpdated = user.DateLastUpdated
				}
			)
			.ToArrayAsync();

		expected.ShouldBeSubsetOf(actual);
	}

	[Test]
	public async Task reset_password() {
		var user = await CreateTestUser();

		await AutomaticClient.UserManagement
			.ResetPassword(user.LoginName, "new-password")
			.ShouldNotThrowAsync()
			.OnSuccessAsync(success => success.ShouldBe(Success.Instance));

		// create a new client with the new password and append?
	}

	[Test]
	[InvalidCredentialsTestCases]
	public async Task create_user_throws_access_denied_when_credentials_are_insufficient(TestUser user, Type type) =>
		await AutomaticClient.UserManagement
			.CreateUser(user.LoginName, user.FullName, user.Groups, user.Password)
			.ShouldFailAsync(error => error.Value.ShouldBeOfType(type));

	[Test]
	public async Task delete_user_throws_user_not_found_when_user_does_not_exist() {
		var user = Users.Generate();

		await AutomaticClient.UserManagement
			.DeleteUser(user.LoginName)
			.ShouldNotThrowAsync()
			.OnFailureAsync(failure => failure.Value.ShouldBeOfType<ErrorDetails.UserNotFound>());
	}

	public class InvalidCredentialsTestCases : TestCaseGenerator<TestUser, Type> {
		protected override IEnumerable<(TestUser, Type)> Data() => [
			(Users.WithNoCredentials(), typeof(ErrorDetails.AccessDenied)),
			(Users.WithInvalidCredentials(), typeof(ErrorDetails.NotAuthenticated)),
			(Users.WithInvalidCredentials(wrongPassword: false), typeof(ErrorDetails.NotAuthenticated)),
		];
	}
}
