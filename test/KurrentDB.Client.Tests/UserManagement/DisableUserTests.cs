// namespace KurrentDB.Client.Tests;
//
// [Trait("Category", "Target:UserManagement")]
// public class DisableUserTests(ITestOutputHelper output, DisableUserTests.CustomFixture fixture)
// 	: KurrentDBPermanentTests<DisableUserTests.CustomFixture>(output, fixture) {
// 	[Fact]
// 	public async Task with_null_input_throws() {
// 		var ex = await Fixture.DBUsers
// 			.DisableUserAsync(null!, userCredentials: TestCredentials.Root)
// 			.ShouldThrowAsync<ArgumentNullException>();
//
// 		// must fix since it is returning value instead of param name
// 		//ex.ParamName.ShouldBe("loginName");
// 	}
//
// 	[Fact]
// 	public async Task with_empty_input_throws() {
// 		var ex = await Fixture.DBUsers
// 			.DisableUserAsync(string.Empty, userCredentials: TestCredentials.Root)
// 			.ShouldThrowAsync<ArgumentOutOfRangeException>();
//
// 		ex.ParamName.ShouldBe("loginName");
// 	}
//
// 	[Theory]
// 	[ClassData(typeof(InvalidCredentialsTestCases))]
// 	public async Task with_user_with_insufficient_credentials_throws(InvalidCredentialsTestCase testCase) {
// 		await Fixture.DBUsers.CreateUserAsync(
// 			testCase.User.LoginName,
// 			testCase.User.FullName,
// 			testCase.User.Groups,
// 			testCase.User.Password,
// 			userCredentials: TestCredentials.Root
// 		);
//
// 		await Fixture.DBUsers
// 			.DisableUserAsync(testCase.User.LoginName, userCredentials: testCase.User.Credentials)
// 			.ShouldThrowAsync(testCase.ExpectedException);
// 	}
//
// 	[Fact]
// 	public async Task that_was_disabled() {
// 		var user = await Fixture.CreateTestUser();
//
// 		await Fixture.DBUsers
// 			.DisableUserAsync(user.LoginName, userCredentials: TestCredentials.Root)
// 			.ShouldNotThrowAsync();
//
// 		await Fixture.DBUsers
// 			.DisableUserAsync(user.LoginName, userCredentials: TestCredentials.Root)
// 			.ShouldNotThrowAsync();
// 	}
//
// 	[Fact]
// 	public async Task that_is_enabled() {
// 		var user = await Fixture.CreateTestUser();
//
// 		await Fixture.DBUsers
// 			.DisableUserAsync(user.LoginName, userCredentials: TestCredentials.Root)
// 			.ShouldNotThrowAsync();
// 	}
//
// 	public class CustomFixture() : KurrentDBPermanentFixture(x => x.WithoutDefaultCredentials());
// }
