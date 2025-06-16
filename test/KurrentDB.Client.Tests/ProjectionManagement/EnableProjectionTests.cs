using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests.Projections;

[Trait("Category", "Target:ProjectionManagement")]
public class EnableProjectionTests(ITestOutputHelper output, EnableProjectionTests.CustomFixture fixture)
	: KurrentTemporaryTests<EnableProjectionTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task enable_projection() {
		var name = Names.First();
		await Fixture.DBProjections.EnableAsync(name, userCredentials: TestCredentials.Root);
		var result = await Fixture.DBProjections.GetStatusAsync(name, userCredentials: TestCredentials.Root);
		Assert.NotNull(result);
		Assert.Equal("Running", result.Status);
	}

	static readonly string[] Names = ["$streams", "$stream_by_category", "$by_category", "$by_event_type", "$by_correlation_id"];

	public class CustomFixture : KurrentDBTemporaryFixture {
		public CustomFixture() : base(x => x.RunProjections()) { }
	}
}
