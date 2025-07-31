using Kurrent.Client.Model;
using Kurrent.Client.Testing.Shouldly;
using UserDetails = Kurrent.Client.Model.UserDetails;

namespace Kurrent.Client.Tests.Users;

[Category("UserManagement")]
public class UsersTests : KurrentClientTestFixture {
	[Test]
	public async Task creates_user() {
		var user = Users.Generate();

		await AutomaticClient.Users
			.CreateUser(
				user.LoginName, user.FullName, user.Groups,
				user.Password
			)
			.ShouldNotThrowAsync()
			.ShouldNotFailAsync();

		await AutomaticClient.Users
			.GetUser(user.LoginName)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(details => details
				.ShouldBeEquivalentTo(
					user.Details, config => config
						.Excluding<UserDetails>(d => d.DateLastUpdated!)
						.Excluding<UserDetails>(d => d.HasBeenUpdated)
				)
			);
	}

	[Test]
	public async Task get_user() {
		var user = await CreateTestUser();

		await AutomaticClient.Users
			.GetUser(user.LoginName)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(details => details
				.ShouldBeEquivalentTo(
					user.Details, config => config
						.Excluding<UserDetails>(d => d.DateLastUpdated!)
						.Excluding<UserDetails>(d => d.HasBeenUpdated)
				)
			);
	}

	[Test]
	public async Task enable_user() {
		var user = await CreateTestUser();

		await AutomaticClient.Users
			.EnableUser(user.LoginName)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(success => success.ShouldBe(Success.Instance));

		await AutomaticClient.Users
			.GetUser(user.LoginName)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(details => details.Disabled.ShouldBeFalse());
	}

	[Test]
	public async Task disable_user() {
		var user = await CreateTestUser();

		await AutomaticClient.Users
			.DisableUser(user.LoginName)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(success => success.ShouldBe(Success.Instance));

		await AutomaticClient.Users
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
			Groups          = [],
			Disabled        = false,
			DateLastUpdated = null
		};

		var ops = new UserDetails {
			LoginName       = "ops",
			FullName        = "KurrentDB Operations",
			Groups          = [],
			Disabled        = false,
			DateLastUpdated = null
		};

		var expected = new[] { admin, ops }
			.Concat(users.Select(user => user.Details))
			.ToArray();

		var actual = await AutomaticClient.Users
			.ListAllAsync()
			.Select(user => new UserDetails {
					LoginName       = user.LoginName,
					FullName        = user.FullName,
					Groups          = user.Groups,
					Disabled        = user.Disabled
				}
			)
			.ToArrayAsync();

		expected.ShouldBeSubsetOf(actual, config => config
			.Excluding<UserDetails>(d => d.Groups)
			.Excluding<UserDetails>(d => d.HasGroups)
			.Excluding<UserDetails>(d => d.HasBeenUpdated)
		);
	}

	[Test]
	public async Task reset_password() {
		var user = await CreateTestUser();

		await AutomaticClient.Users
			.ResetPassword(user.LoginName, "new-password")
			.ShouldNotThrowAsync()
			.OnSuccessAsync(success => success.ShouldBe(Success.Instance));

		// create a new client with the new password and append?
	}

	[Test]
	public async Task delete_user_throws_user_not_found_when_user_does_not_exist() {
		var user = Users.Generate();

		await AutomaticClient.Users
			.DeleteUser(user.LoginName)
			.ShouldNotThrowAsync()
			.OnFailureAsync(failure => failure.Value.ShouldBeOfType<ErrorDetails.UserNotFound>());
	}
}
