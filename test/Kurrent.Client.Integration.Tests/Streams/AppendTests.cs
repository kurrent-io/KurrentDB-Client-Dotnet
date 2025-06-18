using Kurrent.Client.Model;
using Kurrent.Client.Testing.Sample;

namespace Kurrent.Client.Tests.Streams;

public class AppendTests : KurrentClientTestFixture {
    [Test]
    [Timeout(60000)]
    public async Task appends_message_to_create_stream(CancellationToken ct) {
        var simulatedGame = Result.Try(() => SimulateGame(GamesAvailable.TicTacToe))
            .ThrowOnError(Should.RecordException);

        var expectedRevision = StreamRevision.From(simulatedGame.GameEvents.Count - 1);

        await AutomaticClient.Streams
            .Append(simulatedGame.Stream, ExpectedStreamState.NoStream, simulatedGame.GameEvents, ct) // this could be a Create Stream operation
            .ShouldNotThrowAsync()
            .OnSuccessAsync(success => {
                success.Stream.ShouldBe(simulatedGame.Stream);
                success.StreamRevision.ShouldBe(expectedRevision);
                success.Position.ShouldBeGreaterThanOrEqualTo(LogPosition.Earliest);
            });
    }

    [Test]
    [Timeout(60000)]
    public async Task appends_message(CancellationToken ct) {
        var simulatedGame = Result.Try(() => SimulateGame(GamesAvailable.TicTacToe))
            .ThrowOnError(Should.RecordException);

        //var expectedRevision = StreamRevision.From(simulatedGame.GameEvents.Count - 1);

        var expectedRevision = await AutomaticClient.Streams
            .Append(simulatedGame.Stream, ExpectedStreamState.NoStream, simulatedGame.GameEvents, ct)
            //.ShouldNotThrowAsync()
            .MatchAsync(
                success => success.StreamRevision,
                failure => throw failure.CreateException()
            );

        await AutomaticClient.Streams
            .Append(simulatedGame.Stream, simulatedGame.GameEvents, ct)
            .ShouldNotThrowAsync()
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
