#pragma warning disable CA1822 // Mark members as static
// ReSharper disable CheckNamespace

using Bogus;
using KurrentDB.Client;
using UserDetails = Kurrent.Client.Model.UserDetails;

namespace Kurrent.Client.Tests;

public partial class KurrentClientTestFixture {
	public async ValueTask<TestUser[]> CreateTestUsers(int count = 3, bool withoutGroups = true) {
		if (count < 1)
			throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");

		var users = Users
			.RuleFor(x => x.Groups, f => withoutGroups ? [] : f.Lorem.Words())
			.Generate(count);

		var results = await users
			.Select(async user => (user, result: await AutomaticClient.UserManagement
				.CreateUser(
					user.LoginName,
					user.FullName,
					user.Groups,
					user.Password
				).ConfigureAwait(false))
			)
			.WhenAll()
			.ConfigureAwait(false);

		foreach (var (user, result) in results)
			if (!result.IsSuccess)
				throw new InvalidOperationException($"Failed to create user '{user.LoginName}': {result.Error}");

		return results.Select(x => x.user).ToArray();
	}

	public async ValueTask<TestUser> CreateTestUser(bool withoutGroups = true) {
		var result = await CreateTestUsers(1, withoutGroups);
		return result.First();
	}
}

public class TestUser {
	public UserDetails      Details     { get; set; } = default!;
	public UserCredentials? Credentials { get; set; } = default!;

	public string   LoginName { get; set; } = null!;
	public string   FullName  { get; set; } = null!;
	public string[] Groups    { get; set; } = null!;
	public string   Password  { get; set; } = null!;

	public override string ToString() => $"{LoginName} Credentials({Credentials?.Username ?? "null"})";
}

public sealed class TestUserFaker : Faker<TestUser> {
	internal static TestUserFaker Instance => new();

	TestUserFaker() {
		RuleFor(x => x.LoginName, f => f.Person.UserName);
		RuleFor(x => x.FullName, f => f.Person.FullName);
		RuleFor(x => x.Groups, f => f.Lorem.Words());
		RuleFor(x => x.Password, f => f.Internet.Password());
		RuleFor(x => x.Credentials, (_, user) => new(user.LoginName, user.Password));
		RuleFor(
			x => x.Details, (_, user) => new UserDetails {
				LoginName = user.LoginName,
				FullName  = user.FullName,
				Groups    = user.Groups
			}
		);
	}

	public TestUser WithValidCredentials() => Generate();

	public TestUser WithNoCredentials() =>
		Instance
			.FinishWith((_, x) => x.Credentials = null)
			.Generate();

	public TestUser WithInvalidCredentials(bool wrongLoginName = true, bool wrongPassword = true) =>
		Instance
			.FinishWith((f, x) => x.Credentials = new(
				wrongLoginName ? "wrong-username" : x.LoginName,
				wrongPassword ? "wrong-password" : x.Password))
			.Generate();
}
