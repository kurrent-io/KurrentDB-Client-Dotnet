using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry;
using Kurrent.Client.Testing.Sample;

namespace Kurrent.Client.Tests.Streams;

[Category("Streams"), Category("Read")]
public class ReadTests : KurrentClientTestFixture {
    [Test]
    public async Task reads_stream(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        var now = TimeProvider.GetUtcNow();

        await using var messages = await AutomaticClient.Streams
            .ReadStream(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync();

        var records = await messages
            .Where(msg => msg.IsRecord)
            .Select(msg => msg.AsRecord)
            .ToListAsync(ct);

        records.Count.ShouldBe(simulation.Game.GameEvents.Count);

        for (var i = 0; i < records.Count; i++) {
            var record = records[i];
            var gameEvent = simulation.Game.GameEvents[i];

            record.Stream.ShouldBe(simulation.Game.Stream);
            record.StreamRevision.ShouldBeEquivalentTo(StreamRevision.From(i));
            record.Position.ShouldBeLessThanOrEqualTo(simulation.Position);

            record.Schema.SchemaName.Value.ShouldNotBeEmpty();
            record.Schema.DataFormat.ShouldBe(gameEvent.DataFormat);
            record.Schema.SchemaVersionId.ShouldBe(SchemaVersionId.None);

            record.Value.ShouldBeEquivalentTo(gameEvent.Value);
            record.ValueType.ShouldBe(gameEvent.Value.GetType());

            record.Timestamp.ShouldNotBeInRange(now.DateTime, TimeProvider.GetUtcNow().DateTime);

            record.Metadata.Stringify().ShouldBeEquivalentTo(gameEvent.Metadata.Stringify());
        }
    }

    [Test]
    public async Task gracefully_stops_reading_and_flushes_queue_when_read_token_is_cancelled(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        using var cancellator = CancellationTokenSource.CreateLinkedTokenSource(ct);

        await using var messages = await AutomaticClient.Streams
            .ReadStream(simulation.Game.Stream, cancellator.Token)
            .ShouldNotThrowOrFailAsync();

        var cancelOn = simulation.Game.GameEvents.Count / 2;

        await foreach (var _ in messages) {
            if (--cancelOn == 0 && !cancellator.IsCancellationRequested)
                cancellator.Cancel();
        }

        messages.QueuedMessages.ShouldBe(0, "Should have flushed all messages on cancellation");
        cancellator.IsCancellationRequested.ShouldBeTrue();
    }

    [Test, Skip("Needs investigation")]
    public async Task gracefully_stops_reading_without_flushing_queue_when_enumerable_token_is_cancelled(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await using Messages messages = await AutomaticClient.Streams
            .ReadStream(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync();

        var cancelOn = simulation.Game.GameEvents.Count / 2;


        using var cancellator = CancellationTokenSource.CreateLinkedTokenSource(ct);

        await foreach (var _ in messages.WithCancellation(cancellator.Token)) {
            if (--cancelOn == 0)
                cancellator.Cancel();
        }

        messages.QueuedMessages.ShouldBeGreaterThanOrEqualTo(simulation.Game.GameEvents.Count / 2, "Should not queue more than one message before cancellation");
        cancellator.IsCancellationRequested.ShouldBeTrue();
    }

    [Test]
    public async Task fails_with_stream_not_found_when_stream_was_deleted(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Delete(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Streams
            .ReadStream(simulation.Game.Stream, ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(ReadError.ReadErrorCase.StreamNotFound));
    }

    [Test]
    public async Task fails_with_stream_deleted_when_stream_was_tombstoned(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Tombstone(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Streams
            .ReadStream(simulation.Game.Stream, ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(ReadError.ReadErrorCase.StreamDeleted));
    }

    [Test]
    public async Task fails_with_stream_not_found_when_stream_does_not_exist(CancellationToken ct) {
        var game = TrySimulateGame(GamesAvailable.TicTacToe);

        await AutomaticClient.Streams
            .ReadStream(game.Stream, ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(ReadError.ReadErrorCase.StreamNotFound));
    }

    [Test]
    public async Task reads_first_stream_record(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        var firstMessage = simulation.Game.GameEvents.First();

        await AutomaticClient.Streams
            .ReadFirstStreamRecord(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync(record =>
                record.Value.ShouldBeEquivalentTo(firstMessage.Value));
    }

    [Test]
    public async Task reads_last_stream_record(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        var lastMessage = simulation.Game.GameEvents.Last();

        await AutomaticClient.Streams
            .ReadLastStreamRecord(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync(record =>
                record.Value.ShouldBeEquivalentTo(lastMessage.Value));
    }

    [Test, Skip("Needs investigation")]
    public async Task inspects_log_record(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        var firstMessage = simulation.Game.GameEvents.First();

        var linkedRecord = await AutomaticClient.Streams
            .ReadFirstStreamRecord("$ce-TicTacToe", ct)
            .ShouldNotThrowOrFailAsync(record => {
                record.IndexRevision.ShouldBeGreaterThan(0);
            });

        await AutomaticClient.Streams
            .InspectRecord(simulation.Position, ct)
            .ShouldNotThrowOrFailAsync(record => {
                record.IsDecoded.ShouldBeFalse();
            });
    }
}
