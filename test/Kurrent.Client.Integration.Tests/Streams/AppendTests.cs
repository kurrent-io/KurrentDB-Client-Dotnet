using System.Runtime.CompilerServices;
using Kurrent.Client.Model;
using Kurrent.Client.Testing.Sample;
using TicTacToe;

namespace Kurrent.Client.Tests.Streams;

public static class ShouldlyThrowExtensions {
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async ValueTask<T> ShouldNotThrowAsync<T>(this ValueTask<T> operation, string? customMessage = null) {
        T taskResult = default!;

        await Should
            .NotThrowAsync(async () => taskResult = await operation.AsTask().ConfigureAwait(false), customMessage)
            .ConfigureAwait(false);

        return taskResult;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ShouldNotThrow<T>(this Func<T> operation, string? customMessage = null) {
        T taskResult = default!;
        Should.NotThrow(operation, customMessage);
        return taskResult;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Exception RecordException(this Exception exception, string? customMessage = null) {
        Should.NotThrow(() => exception, customMessage);
        return exception;
    }

    // [MethodImpl(MethodImplOptions.NoInlining)]
    // public static async ValueTask<T> ShouldNotThrow<T>(this Func<ValueTask<T>> execute,  string? customMessage = null) {
    //     T taskResult = default!;
    //
    //     await Should
    //         .NotThrowAsync(async () => taskResult = await execute().AsTask().ConfigureAwait(false), customMessage)
    //         .ConfigureAwait(false);
    //
    //     return taskResult;
    // }
}

public class AppendTests : KurrentClientTestFixture {
    [Test]
    [Timeout(5000)]
    public async Task appends_message_to_new_stream_with_no_stream_expected_state(CancellationToken ct) {
        var simulationResult = Result.Try(
            () => SimulateGame(GamesAvailable.TicTacToe),
            ex => ex.RecordException()
        );

        var result = await AutomaticClient.Streams
            .Append(simulationResult.AsSuccess.Stream, simulationResult.AsSuccess.GameEvents, ct)
            .ShouldNotThrowAsync()
            .ConfigureAwait(false);

        if (result.TryGetValue(out var success)) {
            success.Stream.ShouldBe(simulationResult.AsSuccess.Stream);
            success.StreamRevision.ShouldBe(StreamRevision.Min);
            success.Position.ShouldBeGreaterThanOrEqualTo(LogPosition.Earliest);
        } else {
            result.AsError.ToException().RecordException();
        }

        await AutomaticClient.Streams
            .Append(simulationResult.AsSuccess.Stream, simulationResult.AsSuccess.GameEvents, ct)
            .ShouldNotThrowAsync()
            .OnSuccessAsync(ok => {
                ok.Stream.ShouldBe(simulationResult.AsSuccess.Stream);
                ok.StreamRevision.ShouldBe(StreamRevision.Min);
                ok.Position.ShouldBeGreaterThanOrEqualTo(LogPosition.Earliest);
            })
            .OnErrorAsync(ko => ko.ToException().RecordException());
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
