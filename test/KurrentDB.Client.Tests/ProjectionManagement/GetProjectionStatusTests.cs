using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests.Projections;

[Trait("Category", "Target:ProjectionManagement")]
public class GetProjectionStatusTests(ITestOutputHelper output, GetProjectionStatusTests.CustomFixture fixture)
	: KurrentTemporaryTests<GetProjectionStatusTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task get_status() {
		var name   = Names.First();
		var result = await Fixture.DBProjections.GetStatusAsync(name, userCredentials: TestCredentials.Root);

		Assert.NotNull(result);
		Assert.Equal(name, result.Name);
	}

	static readonly string[] Names = ["$streams", "$stream_by_category", "$by_category", "$by_event_type", "$by_correlation_id"];

	public class CustomFixture : KurrentDBTemporaryFixture {
		public CustomFixture() : base(x => x.RunProjections()) { }
	}
}
