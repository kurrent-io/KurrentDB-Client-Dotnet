using KurrentDb.Client.Tests.TestNode;

namespace KurrentDb.Client.Tests.Projections;

[Trait("Category", "Target:ProjectionManagement")]
public class UpdateProjectionTests(ITestOutputHelper output, UpdateProjectionTests.CustomFixture fixture)
	: KurrentTemporaryTests<UpdateProjectionTests.CustomFixture>(output, fixture) {
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task update_projection(bool? emitEnabled) {
		var name = Fixture.GetProjectionName();
		await Fixture.DbProjections.CreateContinuousAsync(
			name,
			"fromAll().when({$init: function (state, ev) {return {};}});",
			userCredentials: TestCredentials.Root
		);

		await Fixture.DbProjections.UpdateAsync(
			name,
			"fromAll().when({$init: function (s, e) {return {};}});",
			emitEnabled,
			userCredentials: TestCredentials.Root
		);
	}

	public class CustomFixture : KurrentTemporaryFixture {
		public CustomFixture() : base(x => x.RunProjections()) { }
	}
}
