using Kurrent.Client.Model;
using Kurrent.Client.Testing.Sample;
using Kurrent.Client.Testing.TUnit;

namespace Kurrent.Client.Tests.Streams;

public class TruncateTests : KurrentClientTestFixture {
    [Test]
    public async Task truncates_stream(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Truncate(simulation.Game.Stream, simulation.Revision, ct)
            .ShouldNotThrowOrFailAsync();
    }

    [Test]
    public async Task returns_revision_conflict_error_when_revision_is_not_matched(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Truncate(simulation.Game.Stream, simulation.Revision + 1, ct)
            .ShouldFailAsync(deleteError =>
                deleteError.Value.ShouldBeOfType<ErrorDetails.StreamRevisionConflict>());
    }
}
