using Kurrent.Client.Streams;

namespace Kurrent.Client.Tests.Streams;

public class TruncateTests : KurrentClientTestFixture {
    [Test]
    public async Task truncates_stream_at_revision(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Truncate(simulation.Game.Stream, simulation.Revision, ct)
            .ShouldNotThrowOrFailAsync();

        var messages = await AutomaticClient.Streams
            .ReadStream(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync();

        var count = await messages.CountAsync(ct);
        count.ShouldBe(1, "Stream should be empty after truncation");
    }

    [Test]
    public async Task truncates_full_stream(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        var revision = simulation.Revision + 1; // all messages

        await AutomaticClient.Streams
            .Truncate(simulation.Game.Stream, revision, ct)
            .ShouldNotThrowOrFailAsync();

        var messages = await AutomaticClient.Streams
            .ReadStream(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync();

        var count = await messages.CountAsync(ct);
        count.ShouldBe(0, "Stream should be empty after truncation");
    }

    [Test]
    public async Task returns_revision_conflict_error_when_revision_is_not_matched(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        var badRevision = simulation.Revision + 99;

        await AutomaticClient.Streams
            .Truncate(simulation.Game.Stream, badRevision, ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(TruncateStreamError.TruncateStreamErrorCase.StreamRevisionConflict));
    }
}
