using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests.Projections;

[Trait("Category", "Target:ProjectionManagement")]
public class DisableProjectionTests(ITestOutputHelper output, DisableProjectionTests.CustomFixture fixture)
	: KurrentTemporaryTests<DisableProjectionTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task disable_projection() {
		var name = Names.First();
		await Fixture.DbProjections.DisableAsync(name, userCredentials: TestCredentials.Root);
		var result = await Fixture.DbProjections.GetStatusAsync(name, userCredentials: TestCredentials.Root);
		Assert.NotNull(result);
		Assert.Contains(["Aborted/Stopped", "Stopped"], x => x == result!.Status);
	}

	static readonly string[] Names = ["$streams", "$stream_by_category", "$by_category", "$by_event_type", "$by_correlation_id"];

	public class CustomFixture : KurrentDBTemporaryFixture {
		public CustomFixture() : base(x => x.RunProjections()) { }
	}
}
