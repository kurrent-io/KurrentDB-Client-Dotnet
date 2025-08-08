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
                info.LastStreamUpdate.ShouldBeGreaterThan(DateTimeOffset.MinValue);
                info.IsDeleted.ShouldBeFalse();
            });
    }

    [Test]
    public async Task returns_stream_info_with_empty_metadata_when_not_set(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .GetStreamInfo(simulation.Game.Stream, ct)
            .ShouldNotThrowOrFailAsync(info => {
                info.HasMetadata.ShouldBeFalse();
                info.MetadataRevision.ShouldBe(StreamRevision.Unset);
                info.LastStreamPosition.ShouldBeGreaterThan(LogPosition.Earliest);
                info.LastStreamRevision.ShouldBe(simulation.Revision);
                info.LastStreamUpdate.ShouldBeGreaterThan(DateTimeOffset.MinValue);
                info.IsDeleted.ShouldBeFalse();
            });
    }

    [Test]
    public async Task returns_stream_info_when_stream_does_not_exist(CancellationToken ct) {
        await AutomaticClient.Streams
            .GetStreamInfo("does_not_exist", ct)
            .ShouldNotThrowOrFailAsync(info => {
                info.HasMetadata.ShouldBeFalse();
                info.IsDeleted.ShouldBeFalse();
                info.IsTombstoned.ShouldBeFalse();
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
                info.IsDeleted.ShouldBeTrue();
                info.IsTombstoned.ShouldBeFalse();
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
                info.HasMetadata.ShouldBeFalse(); // because the server does not let this happen... absurd...
                info.IsDeleted.ShouldBeTrue();
                info.IsTombstoned.ShouldBeTrue();
            });
    }

    [Test, Skip("Must setup a client with invalid credentials, and for that I need user management wrapped")]
    public async Task fails_with_access_denied_without_permissions(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        await AutomaticClient.Streams
            .GetStreamInfo(simulation.Game.Stream, ct)
            .ShouldFailAsync(error =>
                error.Value.ShouldBeOfType<ErrorDetails.AccessDenied>());
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
            .SetStreamMetadata(simulation.Game.Stream, meta, ct)
            .ShouldNotThrowOrFailAsync(revision => revision.ShouldBe(0));
    }

    [Test]
    public async Task sets_stream_metadata_even_when_stream_is_deleted(CancellationToken ct) {
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
                error.Value.ShouldBeOfType<ErrorDetails.StreamDeleted>());
    }


    // [Test]
    // public async Task gets_stream_metadata(CancellationToken ct) {
    //     var simulation = await SeedGame(ct);
    //
    //     var customMetadata = JsonDocument.Parse("""{"key1":"value1"}""");
    //
    //     var meta = new StreamMetadata {
    //         MaxAge         = TimeSpan.FromDays(7),
    //         TruncateBefore = null,
    //         CacheControl   = TimeSpan.FromHours(3),
    //         MaxCount       = 200,
    //         CustomMetadata = customMetadata
    //     };
    //
    //     await AutomaticClient.Streams
    //         .SetStreamMetadata(simulation.Game.Stream, meta, ExpectedStreamState.Any, ct)
    //         .ShouldNotThrowOrFailAsync();
    //
    //     await AutomaticClient.Streams
    //         .GetStreamMetadata(simulation.Game.Stream, ct)
    //         .ShouldNotThrowOrFailAsync(streamMeta =>
    //             streamMeta.ShouldBeEquivalentTo(meta, x => x.Using<JsonDocument>((l, r) => l.ToString() == r.ToString())));
    // }
}
