using Kurrent.Client.Model;
using TicTacToe;

namespace Kurrent.Client.Tests;

public class StreamsAppendTests : KurrentClientTestFixture {
    [Test]
    public async Task appends_message_to_new_stream_with_expected_revision(CancellationToken ct) {
        // Arrange
        var stream = $"Game-{Guid.NewGuid().ToString().Substring(24, 12)}";

        var msg = new GameStarted(Guid.NewGuid(), Player.X);

        // Act
        var appendResult = await Client.Streams
            .Append(stream, ExpectedStreamState.NoStream, msg, ct)
            .ConfigureAwait(false);

        // Assert
        appendResult.Switch(
            success => {
                success.Stream.ShouldBe(stream);
                success.StreamRevision.ShouldBeGreaterThanOrEqualTo(StreamRevision.Min);
                success.Position.ShouldBeGreaterThanOrEqualTo(LogPosition.Earliest);
            },
            failure => Assert.Fail(failure.Value.ToString()!)
        );
    }

    [Test]
    public async Task appends_message_to_existing_stream_with_expected_revision(CancellationToken ct) {
        // Arrange
        var stream = $"Game-{Guid.NewGuid().ToString().Substring(24, 12)}";

        var msg = new GameStarted(Guid.NewGuid(), Player.X);

        // Act
        var appendResult = await Client.Streams
            .Append(stream, ExpectedStreamState.NoStream, msg, ct)
            .ConfigureAwait(false);

        // Assert
        appendResult.Switch(
            success => {
                success.Stream.ShouldBe(stream);
                success.StreamRevision.ShouldBeGreaterThanOrEqualTo(StreamRevision.Min);
                success.Position.ShouldBeGreaterThanOrEqualTo(LogPosition.Earliest);
            },
            failure => Assert.Fail(failure.Value.ToString()!)
        );
    }

    [Test]
    public async Task deletes_existing_stream_with_expected_revision(CancellationToken ct) {
        // Arrange
        var stream = $"Game-{Guid.NewGuid().ToString().Substring(24, 12)}";

        var msg = new GameStarted(Guid.NewGuid(), Player.X);

        var appendResult = await Client.Streams
            .Append(stream, msg, ct)
            .ConfigureAwait(false);

        if (appendResult.IsError)
            appendResult.AsError.Throw();

        // Act
        var deleteResult = await Client.Streams
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

        var appendResult = await Client.Streams
            .Append(stream, msg, ct)
            .ConfigureAwait(false);

        if (appendResult.IsError)
            appendResult.AsError.Throw();

        // Act
        var deleteResult = await Client.Streams
            .Tombstone(stream, appendResult.AsSuccess.StreamRevision, ct)
            .ConfigureAwait(false);

        // Assert
        deleteResult.IsSuccess.ShouldBeTrue();
    }
}
