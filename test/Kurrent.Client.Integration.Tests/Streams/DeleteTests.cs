using Kurrent.Client.Model;
using Kurrent.Client.Testing.Sample;
using KurrentDB.Client;

namespace Kurrent.Client.Tests.Streams;

public class DeleteTests : KurrentClientTestFixture {
    [Test]
    [Timeout(60000)]
    public async Task appends_message_to_new_stream_with_no_stream_expected_state(CancellationToken ct) {
        var simulatedGame = Result.Try(() => SimulateGame(GamesAvailable.TicTacToe))
            .ThrowOnError(Should.RecordException);

        var expectedRevision = StreamRevision.From(simulatedGame.GameEvents.Count - 1);

        await AutomaticClient.Streams
            .Append(simulatedGame.Stream, simulatedGame.GameEvents, ct)
            .ShouldNotThrowAsync()
            .OnErrorAsync(err => Should.RecordException(err.ToException()))
            .OnSuccessAsync(val => {
                val.Stream.ShouldBe(simulatedGame.Stream);
                val.StreamRevision.ShouldBe(expectedRevision);
                val.Position.ShouldBeGreaterThanOrEqualTo(LogPosition.Earliest);
            });
    }

    // [Test]
    // [Timeout(60000)]
    // public async Task deletes_existing_stream_with_expected_revision(CancellationToken ct) {
    //     // Arrange
    //     var stream = $"Game-{Guid.NewGuid().ToString().Substring(24, 12)}";
    //
    //     var msg = new GameStarted(Guid.NewGuid(), Player.X);
    //
    //     var appendResult = await AutomaticClient.Streams
    //         .Append(stream, [msg], ct)
    //         .ConfigureAwait(false);
    //
    //     if (appendResult.IsError)
    //         appendResult.AsError.Throw();
    //
    //     // Act
    //     var deleteResult = await AutomaticClient.Streams
    //         .Delete(stream, appendResult.AsSuccess.StreamRevision, ct)
    //         .ConfigureAwait(false);
    //
    //     // Assert
    //     deleteResult.IsSuccess.ShouldBeTrue();
    // }
    //
    // [Test]
    // [Timeout(60000)]
    // public async Task tombstones_existing_stream_with_expected_revision(CancellationToken ct) {
    //     // Arrange
    //     var stream = $"Game-{Guid.NewGuid():N[24,12]}";
    //
    //     var msg = new GameStarted(Guid.NewGuid(), Player.X);
    //
    //     var appendResult = await AutomaticClient.Streams
    //         .Append(stream, msg, ct)
    //         .ConfigureAwait(false);
    //
    //     if (appendResult.IsError)
    //         appendResult.AsError.Throw();
    //
    //     // Act
    //     var deleteResult = await AutomaticClient.Streams
    //         .Tombstone(stream, appendResult.AsSuccess.StreamRevision, ct)
    //         .ConfigureAwait(false);
    //
    //     // Assert
    //     deleteResult.IsSuccess.ShouldBeTrue();
    // }
}
