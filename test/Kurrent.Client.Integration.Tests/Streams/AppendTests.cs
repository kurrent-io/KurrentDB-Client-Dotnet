using Kurrent.Client.Model;
using TicTacToe;

namespace Kurrent.Client.Tests;

public class AppendTests : KurrentClientTestFixture {
    [Test]
    public async Task appends_message_to_new_stream_with_no_stream_expected_state(CancellationToken ct) {
        // Arrange
        var stream = $"Game-{Guid.NewGuid().ToString().Substring(24, 12)}";

        var msg = new GameStarted(Guid.NewGuid(), Player.X);

        // var thing = new List<AppendStreamRequest>(
        //     [
        //         new AppendStreamRequest(
        //             stream, ExpectedStreamState.NoStream, [
        //                 new Message {
        //                     Value = msg
        //                 }
        //             ]
        //         )
        //     ]
        // ).ToAsyncEnumerable();
                   // Act
                   var appendTask = AutomaticClient.Streams
            .Append(stream, StreamRevision.From(1), msg, ct)
            .AsTask();

        await Should.NotThrowAsync(() => appendTask);

        var result = await appendTask;

        // Assert
        result
            .OnSuccess(success => {
                success.Stream.ShouldBe(stream);
                success.StreamRevision.ShouldBeGreaterThanOrEqualTo(StreamRevision.Min);
                success.Position.ShouldBeGreaterThanOrEqualTo(LogPosition.Earliest);
            })
            .OnError(failure => Assert.Fail(failure.Value.ToString()!));
    }

    [Test]
    public async Task deletes_existing_stream_with_expected_revision(CancellationToken ct) {
        // Arrange
        var stream = $"Game-{Guid.NewGuid().ToString().Substring(24, 12)}";

        var msg = new GameStarted(Guid.NewGuid(), Player.X);

        var appendResult = await AutomaticClient.Streams
            .Append(stream, msg, ct)
            .ConfigureAwait(false);

        if (appendResult.IsError)
            appendResult.AsError.Throw();

        // Act
        var deleteResult = await AutomaticClient.Streams
            .Delete(stream, appendResult.AsSuccess.StreamRevision, ct)
            .ConfigureAwait(false);

        // Assert
        deleteResult.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task tombstones_existing_stream_with_expected_revision(CancellationToken ct) {
        // Arrange
        var stream = $"Game-{Guid.NewGuid():N[24,12]}";

        var msg = new GameStarted(Guid.NewGuid(), Player.X);

        var appendResult = await AutomaticClient.Streams
            .Append(stream, msg, ct)
            .ConfigureAwait(false);

        if (appendResult.IsError)
            appendResult.AsError.Throw();

        // Act
        var deleteResult = await AutomaticClient.Streams
            .Tombstone(stream, appendResult.AsSuccess.StreamRevision, ct)
            .ConfigureAwait(false);

        // Assert
        deleteResult.IsSuccess.ShouldBeTrue();
    }
}
