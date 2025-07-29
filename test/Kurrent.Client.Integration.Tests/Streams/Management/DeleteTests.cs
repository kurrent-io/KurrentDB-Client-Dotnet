using Kurrent.Client.Model;
using Kurrent.Client.Testing.Sample;

namespace Kurrent.Client.Tests.Streams;

[Category("Streams"), Category("Management"), Category("Delete")]
public class DeleteTests : KurrentClientTestFixture {
    [Test]
    public async Task deletes_existing_stream_without_specifing_revision(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Delete(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync(
                position => position.ShouldBeGreaterThanOrEqualTo(simulation.Position));
    }

    [Test]
    public async Task deletes_stream_when_expected_revision_matches(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Delete(simulation.Game.Stream, simulation.Revision, ct)
            .ShouldNotThrowOrFailAsync(position =>
                position.ShouldBeGreaterThanOrEqualTo(simulation.Position));
    }

    [Test]
    public async Task fails_to_delete_stream_with_revision_conflict_error_when_revision_is_not_matched(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Delete(simulation.Game.Stream, simulation.Revision + 99, ct)
            .ShouldFailAsync(error =>
                error.Value.ShouldBeOfType<ErrorDetails.StreamRevisionConflict>());
    }

    [Test]
    public async Task fails_to_delete_stream_with_not_found_error_when_stream_does_not_exist(CancellationToken ct) {
        var game = TrySimulateGame(GamesAvailable.TicTacToe);

        await AutomaticClient.Streams
            .Delete(game.Stream, ct)
            .ShouldFailAsync(error =>
                error.Value.ShouldBeOfType<ErrorDetails.StreamNotFound>());
    }

    [Test]
    public async Task fails_to_delete_stream_with_stream_deleted_error_when_stream_was_deleted(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Delete(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Streams
            .Delete(simulation.Game.Stream, ct)
            .ShouldFailAsync(error =>
                error.Value.ShouldBeOfType<ErrorDetails.StreamDeleted>());
    }

    [Test]
    public async Task fails_to_delete_stream_with_stream_tombstoned_error_when_stream_was_tombstoned(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Tombstone(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Streams
            .Delete(simulation.Game.Stream, ct)
            .ShouldFailAsync(error =>
                error.Value.ShouldBeOfType<ErrorDetails.StreamTombstoned>());
    }
}
