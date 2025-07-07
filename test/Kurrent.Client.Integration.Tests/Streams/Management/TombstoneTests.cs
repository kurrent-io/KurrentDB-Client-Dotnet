using Kurrent.Client.Model;
using Kurrent.Client.Testing.Sample;
using Kurrent.Client.Testing.TUnit;

namespace Kurrent.Client.Tests.Streams;

[Category("Streams"), Category("Management"), Category("Tombstone")]
public class TombstoneTests : KurrentClientTestFixture {
    [Test]
    public async Task tombstones_existing_stream_without_specifing_revision(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Tombstone(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync(
                position => position.ShouldBeGreaterThanOrEqualTo(simulation.Position));
    }

    [Test]
    public async Task tombstones_stream_when_expected_revision_matches(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Tombstone(simulation.Game.Stream, simulation.Revision, ct)
            .ShouldNotThrowOrFailAsync(position =>
                position.ShouldBeGreaterThanOrEqualTo(simulation.Position));
    }

    [Test]
    public async Task fails_to_tombstone_stream_with_revision_conflict_error_when_revision_is_not_matched(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Tombstone(simulation.Game.Stream, simulation.Revision + 99, ct)
            .ShouldFailAsync(error =>
                error.Value.ShouldBeOfType<ErrorDetails.StreamRevisionConflict>());
    }

    [Test]
    public async Task fails_to_tombstone_stream_with_not_found_error_when_stream_does_not_exist(CancellationToken ct) {
        var game = TrySimulateGame(GamesAvailable.TicTacToe);

        await AutomaticClient.Streams
            .Tombstone(game.Stream, ct)
            .ShouldFailAsync(error =>
                error.Value.ShouldBeOfType<ErrorDetails.StreamNotFound>());
    }
}
