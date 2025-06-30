using Kurrent.Client.Model;
using Kurrent.Client.Testing.Sample;
using Kurrent.Client.Testing.TUnit;

namespace Kurrent.Client.Tests.Streams;

public class DeleteTests : KurrentClientTestFixture {
    public class DeletesStreamTestCases : TestCaseGenerator<ExpectedStreamState, string> {
        protected override IEnumerable<(ExpectedStreamState, string)> Data() => [
            (ExpectedStreamState.StreamExists, nameof(ExpectedStreamState.StreamExists)),
            (ExpectedStreamState.Any, nameof(ExpectedStreamState.Any)),
        ];
    }

    [Test]
    [DeletesStreamTestCases]
    public async Task deletes_stream(ExpectedStreamState expectedState, string testCase, CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Delete(simulation.Game.Stream, expectedState, ct)
            .ShouldNotThrowOrFailAsync(
                position => position.ShouldBeGreaterThanOrEqualTo(simulation.Position));
    }

    [Test]
    public async Task deletes_stream_with_expected_revision(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Delete(simulation.Game.Stream, simulation.Revision, ct)
            .ShouldNotThrowOrFailAsync(position =>
                position.ShouldBeGreaterThanOrEqualTo(simulation.Position));
    }

    [Test]
    public async Task returns_revision_conflict_error_when_revision_is_not_matched(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Delete(simulation.Game.Stream, simulation.Revision + 1, ct)
            .ShouldFailAsync(deleteError =>
                deleteError.Value.ShouldBeOfType<ErrorDetails.StreamRevisionConflict>());
    }

    [Test]
    public async Task returns_stream_not_found_error_when_deleting_non_existing_stream(CancellationToken ct) {
        var game = TrySimulateGame(GamesAvailable.TicTacToe);

        // TODO: This is failing because we have a real error in the legacy client or the db
        //       Should return not found or already deleted errors but returns revision conflict error
        await AutomaticClient.Streams
            .Delete(game.Stream, ExpectedStreamState.StreamExists, ct)
            .ShouldFailAsync(deleteError =>
                deleteError.Value.ShouldBeOfType<ErrorDetails.StreamNotFound>());
    }
}
