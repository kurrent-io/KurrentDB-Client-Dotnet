using Kurrent.Client.Model;

namespace Kurrent.Client.Tests.Streams.UserManagement;

public class ListUserTests : KurrentClientTestFixture {
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
}
