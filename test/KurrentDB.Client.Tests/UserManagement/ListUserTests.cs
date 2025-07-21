// ReSharper disable InconsistentNaming

namespace KurrentDB.Client.Tests;

[Trait("Category", "Target:UserManagement")]
public class ListUserTests(ITestOutputHelper output, KurrentDBPermanentFixture fixture) : KurrentDBPermanentTests<KurrentDBPermanentFixture>(output, fixture) {
	[Fact(Skip = "Temporary")]
	public async Task returns_all_created_users() {
		var seed = await Fixture.CreateTestUsers();

		var admin = new UserDetails("admin", "KurrentDB Administrator", ["$admins"], false, null);
		var ops   = new UserDetails("ops", "KurrentDB Operations", ["$ops"], false, null);

		var expected = new[] { admin, ops }
			.Concat(seed.Select(user => user.Details))
			.ToArray();

		var actual = await Fixture.DBUsers
			.ListAllAsync(userCredentials: TestCredentials.Root)
			.Select(user => new UserDetails(user.LoginName, user.FullName, user.Groups, user.Disabled, null))
			.ToArrayAsync();

		foreach (var user in expected) actual.ShouldContain(user);
	}

	[Fact(Skip = "Temporary")]
	public async Task returns_all_system_users() {
		var admin = new UserDetails("admin", "KurrentDB Administrator", ["$admins"], false, null);
		var ops = new UserDetails("ops", "KurrentDB Operations", ["$ops"], false, null);

		var expected = new[] { admin, ops };

		var actual = await Fixture.DBUsers
			.ListAllAsync(userCredentials: TestCredentials.Root)
			.Select(user => new UserDetails(user.LoginName, user.FullName, user.Groups, user.Disabled, null))
			.ToArrayAsync();

		foreach (var user in expected) actual.ShouldContain(user);
	}
}
