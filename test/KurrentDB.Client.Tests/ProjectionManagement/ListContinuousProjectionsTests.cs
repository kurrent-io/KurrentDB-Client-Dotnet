// ReSharper disable InconsistentNaming

using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests;

[Trait("Category", "Target:ProjectionManagement")]
public class ListContinuousProjectionsTests(ITestOutputHelper output, ListContinuousProjectionsTests.CustomFixture fixture)
	: KurrentTemporaryTests<ListContinuousProjectionsTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task list_continuous_projections() {
		var name = Fixture.GetProjectionName();

		await Fixture.DBProjections.CreateContinuousAsync(
			name,
			"fromAll().when({$init: function (state, ev) {return {};}});",
			userCredentials: TestCredentials.Root
		);

		var result = await Fixture.DBProjections.ListContinuousAsync(userCredentials: TestCredentials.Root)
			.ToArrayAsync();

		Assert.Equal(
			result.Select(x => x.Name).OrderBy(x => x),
			Names.Concat([name]).OrderBy(x => x)
		);

		Assert.True(result.All(x => x.Mode == "Continuous"));
	}

	static readonly string[] Names = ["$streams", "$stream_by_category", "$by_category", "$by_event_type", "$by_correlation_id"];

	public class CustomFixture : KurrentDBTemporaryFixture {
		public CustomFixture() : base(x => x.RunProjections()) { }
	}
}
