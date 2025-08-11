// ReSharper disable ConvertToConstant.Local
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Local

using System.Text.Json;
using Kurrent.Client.Projections;
using Kurrent.Client.Testing.TUnit;

namespace Kurrent.Client.Tests.Projections;

[Category("Projections")]
public class ProjectionsStateAndResultTests : KurrentClientTestFixture {
	[Test, TestTimeouts.FiveSeconds]
	public async Task returns_projection_state(CancellationToken ct) {
        var stream = NewStreamName();

		var query = $$"""
			fromStream('{{stream}}').when({
				"$init": function() { return { Count: 0 }; },
				"$any": function(s, e) { s.Count++; return s; }
			});
			""";

        var projection = ProjectionName.From(stream);

		await AutomaticClient.Projections
			.CreateProjection(projection, query, ProjectionSettings.Default, ct)
			.ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .EnableProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

		await SeedTestMessages(stream, transformMetadata: _ => { }, cancellationToken: ct);

        await Task.Delay(1.Seconds(), ct); // Allow time for the projection to process messages

        var state = await AutomaticClient.Projections
            .GetProjectionState<TestProjectionState>(projection, ProjectionPartition.None, JsonSerializerOptions.Default, ct)
            .ShouldNotThrowOrFailAsync();

        state.ShouldNotBeNull();
        state.Count.ShouldBeGreaterThan(0);
	}

    [Test, TestTimeouts.FiveSeconds]
    public async Task returns_projection_result(CancellationToken ct) {
        var stream = NewStreamName();

        var query = $$"""
            fromStream('{{stream}}').when({
            	"$init": function() { return { Count: 0 }; },
            	"$any": function(s, e) { s.Count++; return s; }
            });
            """;

        var projection = ProjectionName.From(stream);

        await AutomaticClient.Projections
            .CreateProjection(projection, query, ProjectionSettings.Default, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Projections
            .EnableProjection(projection, ct)
            .ShouldNotThrowOrFailAsync();

        await SeedTestMessages(stream, transformMetadata: _ => { }, cancellationToken: ct);

        await Task.Delay(1.Seconds(), ct); // Allow time for the projection to process messages

        var result = await AutomaticClient.Projections
            .GetProjectionResult<TestProjectionResult>(projection, ProjectionPartition.None, JsonSerializerOptions.Default, ct)
            .ShouldNotThrowOrFailAsync();

        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThan(0);
    }

    record RecordTracker(int Count);

    record TestProjectionState(int Count) : RecordTracker(Count);

    record TestProjectionResult(int Count) : RecordTracker(Count);
}
