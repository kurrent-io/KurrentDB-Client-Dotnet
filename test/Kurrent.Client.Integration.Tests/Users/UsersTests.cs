using Kurrent.Client.Streams;
using Kurrent.Client.Testing.Shouldly;
using Kurrent.Client.Users;
using UserDetails = Kurrent.Client.Users.UserDetails;

namespace Kurrent.Client.Tests.Users;

[Category("UserManagement")]
public class UsersTests : KurrentClientTestFixture {
    [Test]
    public async Task creates_user() {
        var user = Users.Generate();

        await AutomaticClient.Users
            .Create(user.LoginName, user.FullName, user.Groups, user.Password)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Users
            .GetDetails(user.LoginName)
            .ShouldNotThrowOrFailAsync(details => details
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
            .GetDetails(user.LoginName)
            .ShouldNotThrowOrFailAsync(details => details.ShouldBeEquivalentTo(
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
            .Enable(user.LoginName)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Users
            .GetDetails(user.LoginName)
            .ShouldNotThrowOrFailAsync(details => details.Disabled.ShouldBeFalse());
    }

    [Test]
    public async Task disable_user() {
        var user = await CreateTestUser();

        await AutomaticClient.Users
            .Disable(user.LoginName)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Users
            .GetDetails(user.LoginName)
            .ShouldNotThrowOrFailAsync(details => details.Disabled.ShouldBeTrue());
    }

    // [Test]
    // public async Task list_users() {
    // 	var users = await CreateTestUsers();
    //
    // 	var admin = new UserDetails {
    // 		LoginName       = "admin",
    // 		FullName        = "KurrentDB Administrator",
    // 		Groups          = [],
    // 		Disabled        = false,
    // 		DateLastUpdated = null
    // 	};
    //
    // 	var ops = new UserDetails {
    // 		LoginName       = "ops",
    // 		FullName        = "KurrentDB Operations",
    // 		Groups          = [],
    // 		Disabled        = false,
    // 		DateLastUpdated = null
    // 	};
    //
    // 	var expected = new[] { admin, ops }
    // 		.Concat(users.Select(user => user.Details))
    // 		.ToArray();
    //
    // 	var actual = await AutomaticClient.Users
    // 		.ListAllUsers()
    // 		.Select(user => new UserDetails {
    // 				LoginName       = user.LoginName,
    // 				FullName        = user.FullName,
    // 				Groups          = user.Groups,
    // 				Disabled        = user.Disabled
    // 			}
    // 		)
    // 		.ToArrayAsync();
    //
    // 	expected.ShouldBeSubsetOf(actual, config => config
    // 		.Excluding<UserDetails>(d => d.Groups)
    // 		.Excluding<UserDetails>(d => d.HasGroups)
    // 		.Excluding<UserDetails>(d => d.HasBeenUpdated)
    // 	);
    // }

    [Test]
    public async Task reset_password() {
        var user = await CreateTestUser();

        await AutomaticClient.Users
            .ResetPassword(user.LoginName, "new-password")
            .ShouldNotThrowOrFailAsync();

        // create a new client with the new password and append?
    }

    [Test]
    public async Task delete_user_throws_user_not_found_when_user_does_not_exist() {
        var user = Users.Generate();

        await AutomaticClient.Users
            .Delete(user.LoginName)
            .ShouldFailAsync(failure =>
                failure.Case.ShouldBe(DeleteUserError.DeleteUserErrorCase.NotFound));
    }
}
