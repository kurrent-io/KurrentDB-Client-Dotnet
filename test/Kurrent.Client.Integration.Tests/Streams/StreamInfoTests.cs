using System.Text.Json;
using Kurrent.Client.Model;
using Kurrent.Client.Testing.Shouldly;

namespace Kurrent.Client.Tests.Streams;

public class StreamInfoTests : KurrentClientTestFixture {
    [Test]
    public async Task returns_empty_stream_info_when_metadata_is_not_set(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .GetStreamInfo(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync(info =>
                info.MetadataRevision.ShouldBe(StreamRevision.Unset));
    }

    [Test]
    public async Task sets_stream_metadata(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        var customMetadata = JsonDocument.Parse("""{ "key1": "value1" }""");

        var meta = new StreamMetadata {
            MaxAge         = TimeSpan.FromDays(1),
            TruncateBefore = null,
            CacheControl   = TimeSpan.FromHours(1),
            MaxCount       = 100,
            CustomMetadata = customMetadata
        };

        await AutomaticClient.Streams
            .SetStreamMetadata(simulation.Game.Stream, meta, ExpectedStreamState.Any, ct)
            .ShouldNotThrowOrFailAsync(revision => revision.ShouldBe(0));
    }

    [Test]
    public async Task returns_stream_metadata(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        var customMetadata = JsonDocument.Parse("""{"key1":"value1"}""");

        var meta = new StreamMetadata {
            MaxAge         = TimeSpan.FromDays(1),
            TruncateBefore = null,
            CacheControl   = TimeSpan.FromHours(1),
            MaxCount       = 100,
            CustomMetadata = customMetadata
        };

        await AutomaticClient.Streams
            .SetStreamMetadata(simulation.Game.Stream, meta, ExpectedStreamState.Any, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Streams
            .GetStreamMetadata(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync(streamMeta => {
                streamMeta.ShouldBeEquivalentTo(meta, x => x.Using<JsonDocument>((l, r) => l.ToString() == r.ToString()));
            });
    }

    [Test]
    public async Task returns_stream_info(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        var customMetadata = JsonDocument.Parse("""{"key1":"value1"}""");

        var meta = new StreamMetadata {
            MaxAge         = TimeSpan.FromDays(1),
            TruncateBefore = null,
            CacheControl   = TimeSpan.FromHours(1),
            MaxCount       = 100,
            CustomMetadata = customMetadata
        };

        await AutomaticClient.Streams
            .SetStreamMetadata(simulation.Game.Stream, meta, ExpectedStreamState.Any, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Streams
            .GetStreamInfo(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync(streamInfo => {
                streamInfo.Metadata.ShouldBeEquivalentTo(meta, x => x.Using<JsonDocument>((left, right) => left.ToString() == right.ToString()));

                // Other properties
                streamInfo.MetadataRevision.ShouldBe(0);
                streamInfo.LastStreamPosition.ShouldBeGreaterThan(LogPosition.Earliest);
                streamInfo.LastStreamRevision.ShouldBe(simulation.Revision);
                streamInfo.IsDeleted.ShouldBeFalse();
            });
    }
}
