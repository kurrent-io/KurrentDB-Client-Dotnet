using System.Text.Json;
using Kurrent.Client.Streams;
using Kurrent.Client.Testing.Shouldly;

namespace Kurrent.Client.Tests.Streams;

[Category("Streams"), Category("Integration")]
public class StreamInfoTests : KurrentClientTestFixture {
    [Test]
    public async Task returns_existing_stream_info(CancellationToken ct) {
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
            .SetStreamMetadata(simulation.Game.Stream, meta, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Streams
            .GetStreamInfo(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync(info => {
                info.Metadata.ShouldBeEquivalentTo(meta, x => x.Using<JsonDocument>((left, right) => left.ToString() == right.ToString()));

                // Other properties
                info.MetadataRevision.ShouldBe(0);
                info.LastStreamPosition.ShouldBeGreaterThan(LogPosition.Earliest);
                info.LastStreamRevision.ShouldBe(simulation.Revision);
                info.LastStreamAppendTime.ShouldBeGreaterThan(DateTime.MinValue);
                info.State.ShouldBe(StreamState.Active);
            });
    }

    [Test]
    public async Task returns_stream_info_with_empty_metadata_when_no_metadata_exists(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .GetStreamInfo(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync(info => {
                info.HasMetadata.ShouldBeFalse();
                info.MetadataRevision.ShouldBe(StreamRevision.Unset);
                info.LastStreamPosition.ShouldBeGreaterThan(LogPosition.Earliest);
                info.LastStreamRevision.ShouldBe(simulation.Revision);
                info.LastStreamAppendTime.ShouldBeGreaterThan(DateTime.MinValue);
                info.State.ShouldBe(StreamState.Active);
            });
    }

    [Test]
    public async Task returns_stream_info_when_stream_does_not_exist(CancellationToken ct) {
        await AutomaticClient.Streams
            .GetStreamInfo("does_not_exist", ct)
            .ShouldNotThrowOrFailAsync(info => {
                info.HasMetadata.ShouldBeFalse();
                info.State.ShouldBe(StreamState.Missing);
            });
    }

    [Test]
    public async Task returns_stream_info_when_stream_is_deleted(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Delete(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Streams
            .GetStreamInfo(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync(info => {
                info.HasMetadata.ShouldBeTrue();
                info.State.ShouldBe(StreamState.Deleted);
            });
    }

    [Test]
    public async Task returns_stream_info_when_stream_is_tombstoned(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Tombstone(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync();

        await AutomaticClient.Streams
            .GetStreamInfo(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync(info => {
                info.HasMetadata.ShouldBeFalse();
                info.State.ShouldBe(StreamState.Tombstoned);
            });
    }

    [Test, Skip("Must setup a client with invalid credentials, and for that I need user management wrapped")]
    public async Task fails_to_return_stream_info_with_access_denied_without_permissions(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .GetStreamInfo(simulation.Game.Stream, ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(GetStreamInfoError.GetStreamInfoErrorCase.AccessDenied));
    }

    [Test]
    public async Task sets_stream_metadata_when_stream_exists(CancellationToken ct) {
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
            .SetStreamMetadata(simulation.Game.Stream, meta, ct)
            .ShouldNotThrowOrFailAsync(revision => revision.ShouldBe(0));
    }

    [Test]
    public async Task sets_stream_metadata_when_stream_does_not_exist(CancellationToken ct) {
        var stream = NewStreamName();

        var meta = new StreamMetadata {
            MaxAge         = TimeSpan.FromDays(1),
            TruncateBefore = null,
            CacheControl   = TimeSpan.FromHours(1),
            MaxCount       = 100,
            CustomMetadata = JsonDocument.Parse("""{"key1":"value1"}""")
        };

        await AutomaticClient.Streams
            .SetStreamMetadata(stream, meta, ct)
            .ShouldNotThrowOrFailAsync(revision => revision.ShouldBe(0));

        await AutomaticClient.Streams
            .GetStreamInfo(stream, ct)
            .ShouldNotThrowOrFailAsync(info => {
                info.Metadata.ShouldBeEquivalentTo(meta, x => x.Using<JsonDocument>((l, r) => l.ToString() == r.ToString()));
                info.State.ShouldBe(StreamState.Missing);
            });
    }

    [Test]
    public async Task sets_stream_metadata_when_stream_is_deleted(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Delete(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync();

        var meta = new StreamMetadata {
            MaxAge = TimeSpan.FromDays(1),
        };

        await AutomaticClient.Streams
            .SetStreamMetadata(simulation.Game.Stream, meta, ct)
            .ShouldNotThrowOrFailAsync(revision =>
                revision.ShouldBe(1));
    }

    [Test]
    public async Task fails_to_set_stream_metadata_when_stream_is_tombstoned(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .Tombstone(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync();

        var meta = new StreamMetadata {
            MaxAge         = TimeSpan.FromDays(1),
            TruncateBefore = null,
            CacheControl   = TimeSpan.FromHours(1),
            MaxCount       = 100
        };

        await AutomaticClient.Streams
            .SetStreamMetadata(simulation.Game.Stream, meta, ct)
            .ShouldFailAsync(error =>
                error.Case.ShouldBe(SetStreamMetadataError.SetStreamMetadataErrorCase.StreamTombstoned));
    }
}
