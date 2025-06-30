using Kurrent.Client.Model;
using Kurrent.Client.Testing.Sample;
using Kurrent.Client.Testing.TUnit;

namespace Kurrent.Client.Tests.Streams;

public class TombstoneTests : KurrentClientTestFixture {
    public class TombstonesStreamTestCases : TestCaseGenerator<ExpectedStreamState, string> {
        protected override IEnumerable<(ExpectedStreamState, string)> Data() => [
            (ExpectedStreamState.StreamExists, nameof(ExpectedStreamState.StreamExists)),
            (ExpectedStreamState.Any, nameof(ExpectedStreamState.Any)),
        ];
    }

    [Test]
    [TombstonesStreamTestCases]
    public async Task tombstones_stream(ExpectedStreamState expectedState, string testCase, CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Tombstone(simulation.Game.Stream, expectedState, ct)
            .ShouldNotThrowOrFailAsync(
                position => position.ShouldBeGreaterThanOrEqualTo(simulation.Position));
    }

    [Test]
    public async Task tombstones_stream_with_expected_revision(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Tombstone(simulation.Game.Stream, simulation.Revision, ct)
            .ShouldNotThrowOrFailAsync(position =>
                position.ShouldBeGreaterThanOrEqualTo(simulation.Position));
    }

    [Test]
    public async Task returns_revision_conflict_error_when_revision_is_not_matched(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Tombstone(simulation.Game.Stream, simulation.Revision + 1, ct)
            .ShouldFailAsync(tombstoneError =>
                tombstoneError.Value.ShouldBeOfType<ErrorDetails.StreamRevisionConflict>());
    }
}
