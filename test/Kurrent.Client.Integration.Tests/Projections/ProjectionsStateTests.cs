// ReSharper disable ConvertToConstant.Local
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Local

using Kurrent.Client.Projections;
using Kurrent.Client.Testing.TUnit;

namespace Kurrent.Client.Tests.Projections;

[Category("Projections")]
public class ProjectionsStateTests : KurrentClientTestFixture {
	[Test, TimeoutAfter.TenSeconds]
	public async Task returns_projection_state(CancellationToken ct) {
        var stream = NewStreamName();

		var definition = $$"""
			fromStream('{{stream}}').when({
				"$init": function() { return { Count: 0 }; },
				"$any": function(s, e) { 
				    log(JSON.stringify(e));
				    s.Count++; 
				    return s; 
				}
			})
			.outputState();
			""";

        var projection = ProjectionName.From(stream);

		await AutomaticClient.Projections
			.Create(projection, definition, ct)
			.ShouldNotThrowOrFailAsync();

		await SeedTestMessages(stream, transformMetadata: _ => { }, cancellationToken: ct);

        await Task.Delay(1.Seconds(), ct); // Allow time for the projection to process messages

        await AutomaticClient.Projections
            .GetState<TestProjectionState>(projection, ct)
            .ShouldNotThrowOrFailAsync(
                state => state.Count.ShouldBeGreaterThan(0));
	}

    record TestProjectionState(int Count);
}
