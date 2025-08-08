// ReSharper disable ConvertToConstant.Local
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Local

using Kurrent.Client.Projections;

namespace Kurrent.Client.Tests.Projections;

[Category("ProjectionManagement")]
public class ProjectionsTests : KurrentClientTestFixture {
	[Test]
	[Arguments("$streams")]
	[Arguments("$stream_by_category")]
	[Arguments("$by_category")]
	[Arguments("$by_event_type")]
	[Arguments("$by_correlation_id")]
	public async Task get_status(string name, CancellationToken ct) {
		// Act
		await AutomaticClient.Projections
			.GetProjection(name, ct)
			.ShouldNotThrowAsync()
			.OnFailureAsync(error => Should.NotThrow(() => error.Error.Throw()))
			.OnSuccessAsync(details => {
				details.ShouldNotBeNull();
				details.Name.ShouldBe(name);
			});
	}

	[Test]
	public async Task get_state(CancellationToken ct) {
		// Arrange
		var name = NewProjectionName();

		var query = $$"""
			fromStream('{{name}}').when({
				"$init": function() {return { Count: 0 }; },
				"$any": function(s, e) { s.Count++; return s; }
			});
			""";

		await AutomaticClient.Projections
			.CreateProjection(name, query, ProjectionSettings.Default, cancellationToken: ct)
			.ShouldNotThrowOrFailAsync();

		await SeedTestMessages(name, _ => { }, cancellationToken: ct);

		// Act & Assert
		await AutomaticClient.Projections
			.GetState<Result>(name, cancellationToken: ct)
			.ShouldNotThrowAsync()
			.OnFailureAsync(error => Should.NotThrow(() => error.Error.Throw()))
			.OnSuccessAsync(result => {
				result.ShouldNotBeNull();
				result.Count.ShouldBeGreaterThan(0);
			});
	}
	//
	// [Test]
	// public async Task create_one_time(CancellationToken ct) {
	// 	// Arrange
	// 	var existingProjections = await AutomaticClient.Projections
	// 		.ListAll(ct).Value
	// 		.Where(p => p.Mode == "OneTime")
	// 		.Select(p => p.EffectiveName)
	// 		.ToArrayAsync(cancellationToken: ct);
	//
	// 	// Act
	// 	await AutomaticClient.Projections
	// 		.CreateOneTimeProjection(ProjectionQuery, ct)
	// 		.ShouldNotThrowOrFailAsync();
	//
	// 	// Assert
	// 	var currentProjections = await AutomaticClient.Projections
	// 		.ListAll(ct).Value
	// 		.Where(p => p.Mode == "OneTime")
	// 		.Select(p => p.EffectiveName)
	// 		.ToArrayAsync(cancellationToken: ct);
	//
	// 	currentProjections
	// 		.Except(existingProjections)
	// 		.ShouldHaveSingleItem();
	// }
	//
	// [Test]
	// public async Task create_continuous(CancellationToken ct) {
	// 	// Arrange
	// 	var existingProjections = await AutomaticClient.Projections
	// 		.ListAll(ct).Value
	// 		.Where(p => p.Mode == "Continuous")
	// 		.Select(p => p.EffectiveName)
	// 		.ToArrayAsync(cancellationToken: ct);
	//
	// 	var name = NewProjectionName();
	//
	// 	// Act
	// 	await AutomaticClient.Projections
	// 		.CreateContinuousProjection(name, ProjectionQuery, cancellationToken: ct)
	// 		.ShouldNotThrowOrFailAsync();
	//
	// 	// Assert
	// 	var currentProjections = await AutomaticClient.Projections
	// 		.ListAll(ct).Value
	// 		.Where(p => p.Mode == "Continuous")
	// 		.Where(p => p.Name == name)
	// 		.Select(p => p.EffectiveName)
	// 		.ToArrayAsync(cancellationToken: ct);
	//
	// 	currentProjections
	// 		.Except(existingProjections)
	// 		.ShouldHaveSingleItem()
	// 		.ShouldBe(name);
	// }
	//
	// [Test]
	// public async Task create_transient(CancellationToken ct) {
	// 	// Arrange
	// 	var existingProjections = await AutomaticClient.Projections
	// 		.ListAll(ct).Value
	// 		.Where(p => p.Mode == "Transient")
	// 		.Select(p => p.EffectiveName)
	// 		.ToArrayAsync(cancellationToken: ct);
	//
	// 	var query = "fromAll().when({$init: function (state, ev) {return {};}});";
	// 	var name  = NewProjectionName();
	//
	// 	// Act
	// 	await AutomaticClient.Projections
	// 		.CreateTransientProjection(name, query, cancellationToken: ct)
	// 		.ShouldNotThrowOrFailAsync();
	//
	// 	// Assert
	// 	var currentProjections = await AutomaticClient.Projections
	// 		.ListAll(ct).Value
	// 		.Where(p => p.Mode == "Transient")
	// 		.Where(p => p.Name == name)
	// 		.Select(p => p.EffectiveName)
	// 		.ToArrayAsync(cancellationToken: ct);
	//
	// 	currentProjections
	// 		.Except(existingProjections)
	// 		.ShouldHaveSingleItem()
	// 		.ShouldBe(name);
	// }

	// [Test]
	// public async Task delete_projection(CancellationToken ct) {
	// 	// Arrange
	// 	var name = NewProjectionName();
	//
	// 	// Act
	// 	await AutomaticClient.Projections
	// 		.CreateTransientProjection(name, ProjectionQuery, cancellationToken: ct)
	// 		.ShouldNotThrowOrFailAsync();
	//
	// 	await AutomaticClient.Projections
	// 		.DeleteProjection(name, cancellationToken: ct)
	// 		.ShouldNotThrowOrFailAsync();
	//
	// 	// Assert
	// 	var currentProjections = await AutomaticClient.Projections
	// 		.ListProjections(new ListProjectionsOptions()).Value
	// 		.Where(p => p.Mode == "Transient")
	// 		.Where(p => p.EffectiveName == name)
	// 		.Select(p => p.EffectiveName)
	// 		.ToArrayAsync(cancellationToken: ct);
	//
	// 	currentProjections.ShouldBeEmpty();
	// }

	#region helpers

	static string NewProjectionName() => Guid.NewGuid().ToString("N");

	static readonly string ProjectionQuery = "fromAll().when({$init: function (state, ev) {return {};}});";

	record Result(int Count);

	#endregion
}
