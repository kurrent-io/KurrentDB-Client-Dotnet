using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry;
using KurrentDB.Client;

namespace Kurrent.Client.Tests.Streams;

public class ReadTests : KurrentClientTestFixture {
    [Test]
    public async Task reads_stream(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        var now = TimeProvider.GetUtcNow();

        var messages = await AutomaticClient.Streams
            .ReadStream(simulation.Game.Stream)
            .ShouldNotThrowOrFailAsync()
            .ConfigureAwait(false);

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
}
