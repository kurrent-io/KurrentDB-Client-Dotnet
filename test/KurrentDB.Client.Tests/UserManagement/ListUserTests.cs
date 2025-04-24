namespace KurrentDB.Client.Tests;

[Trait("Category", "Target:UserManagement")]
public class ListUserTests(ITestOutputHelper output, KurrentDBPermanentFixture fixture) : KurrentPermanentTests<KurrentDBPermanentFixture>(output, fixture) {
	readonly string _userFullNamePrefix = fixture.IsKdb ? "KurrentDB" : "Event Store";

	[Fact]
	public async Task returns_all_created_users() {
		var seed = await Fixture.CreateTestUsers();

		var admin = new UserDetails("admin", $"{_userFullNamePrefix} Administrator", new[] { "$admins" }, false, default);
		var ops   = new UserDetails("ops", $"{_userFullNamePrefix} Operations", new[] { "$ops" }, false, default);

		var expected = new[] { admin, ops }
			.Concat(seed.Select(user => user.Details))
			.ToArray();

		var actual = await Fixture.DbUsers
			.ListAllAsync(userCredentials: TestCredentials.Root)
			.Select(user => new UserDetails(user.LoginName, user.FullName, user.Groups, user.Disabled, default))
			.ToArrayAsync();

		expected.ShouldBeSubsetOf(actual);
	}

	[Fact]
	public async Task returns_all_system_users() {
		var admin = new UserDetails("admin", $"{_userFullNamePrefix} Administrator", new[] { "$admins" }, false, default);
		var ops   = new UserDetails("ops", $"{_userFullNamePrefix} Operations", new[] { "$ops" }, false, default);

		var expected = new[] { admin, ops };

		var actual = await Fixture.DbUsers
			.ListAllAsync(userCredentials: TestCredentials.Root)
			.Select(user => new UserDetails(user.LoginName, user.FullName, user.Groups, user.Disabled, default))
			.ToArrayAsync();

		expected.ShouldBeSubsetOf(actual);
	}
}
