using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests.Projections;

[Trait("Category", "Target:ProjectionManagement")]
public class UpdateProjectionTests(ITestOutputHelper output, UpdateProjectionTests.CustomFixture fixture)
	: KurrentTemporaryTests<UpdateProjectionTests.CustomFixture>(output, fixture) {
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task update_projection(bool? emitEnabled) {
		var name = Fixture.GetProjectionName();
		await Fixture.DBProjections.CreateContinuousAsync(
			name,
			"fromAll().when({$init: function (state, ev) {return {};}});",
			userCredentials: TestCredentials.Root
		);

		await Fixture.DBProjections.UpdateAsync(
			name,
			"fromAll().when({$init: function (s, e) {return {};}});",
			emitEnabled,
			userCredentials: TestCredentials.Root
		);
	}

	public class CustomFixture : KurrentDBTemporaryFixture {
		public CustomFixture() : base(x => x.RunProjections()) { }
	}
}
