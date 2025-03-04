// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Local

using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests;

[Trait("Category", "Target:ProjectionManagement")]
public class ProjectionManagementTests(ITestOutputHelper output, ProjectionManagementTests.CustomFixture fixture)
	: KurrentTemporaryTests<ProjectionManagementTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task status_is_aborted() {
		var name = Names.First();
		await Fixture.DbProjections.AbortAsync(name, userCredentials: TestCredentials.Root);
		var result = await Fixture.DbProjections.GetStatusAsync(name, userCredentials: TestCredentials.Root);
		Assert.NotNull(result);
		Assert.Contains(["Aborted/Stopped", "Stopped"], x => x == result.Status);
	}

	[Fact]
	public async Task one_time() =>
		await Fixture.DbProjections.CreateOneTimeAsync("fromAll().when({$init: function (state, ev) {return {};}});", userCredentials: TestCredentials.Root);

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task continuous(bool trackEmittedStreams) {
		var name = Fixture.GetProjectionName();

		await Fixture.DbProjections.CreateContinuousAsync(
			name,
			"fromAll().when({$init: function (state, ev) {return {};}});",
			trackEmittedStreams,
			userCredentials: TestCredentials.Root
		);
	}

	[Fact]
	public async Task transient() {
		var name = Fixture.GetProjectionName();

		await Fixture.DbProjections.CreateTransientAsync(
			name,
			"fromAll().when({$init: function (state, ev) {return {};}});",
			userCredentials: TestCredentials.Root
		);
	}

	static readonly string[] Names = ["$streams", "$stream_by_category", "$by_category", "$by_event_type", "$by_correlation_id"];

	public class CustomFixture : KurrentTemporaryFixture {
		public CustomFixture() : base(x => x.RunProjections()) { }
	}
}
