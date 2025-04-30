using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests.Projections;

[Trait("Category", "Target:ProjectionManagement")]
public class ListOneTimeProjectionsTests(ITestOutputHelper output, ListOneTimeProjectionsTests.CustomFixture fixture)
	: KurrentTemporaryTests<ListOneTimeProjectionsTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task list_one_time_projections() {
		await Fixture.DBProjections.CreateOneTimeAsync("fromAll().when({$init: function (state, ev) {return {};}});", userCredentials: TestCredentials.Root);

		var result = await Fixture.DBProjections.ListOneTimeAsync(userCredentials: TestCredentials.Root)
			.ToArrayAsync();

		var details = Assert.Single(result);
		Assert.Equal("OneTime", details.Mode);
	}

	public class CustomFixture : KurrentDBTemporaryFixture {
		public CustomFixture() : base(x => x.RunProjections()) { }
	}
}
