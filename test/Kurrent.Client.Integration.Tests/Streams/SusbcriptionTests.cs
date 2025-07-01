using Kurrent.Client.Model;
using Microsoft.Extensions.Logging;

namespace Kurrent.Client.Tests.Streams;

public class SubscriptionTests : KurrentClientTestFixture {
    [Test]
    public async Task subscribes_to_all_streams(CancellationToken ct) {
        var simulations = await SeedGameSimulations(50, ct: ct).ToListAsync(cancellationToken: ct);

        var gameEventsCount = simulations.Sum(x => x.Game.GameEvents.Count);

        var streams = simulations.Select(x => x.Game.Stream.Value).ToArray();

        var options = new AllSubscriptionOptions {
            Start             = LogPosition.Earliest,
            Filter            = ReadFilter.FromPrefixes(ReadFilterScope.Stream, streams),
            CancellationToken = ct
        };

        await using var subscription = await AutomaticClient.Streams
            .Subscribe(options)
            .ShouldNotThrowOrFailAsync()
            .ConfigureAwait(false);

        var recordsReceived = new List<Record>();

        Heartbeat caughtUp = default;

        await foreach (var msg in subscription) {
            if (msg.IsRecord) {
                recordsReceived.Add(msg.AsRecord);
                continue;
            }

            Logger.LogDebug("Received: {Message}", msg.AsHeartbeat.ToString());

            if (msg.AsHeartbeat.Type == HeartbeatType.CaughtUp) {
                caughtUp = msg.AsHeartbeat;
                break;
            }
        }

        caughtUp.Position.ShouldBeGreaterThan(0);

        recordsReceived.Count.ShouldBe(gameEventsCount);
    }

    [Test]
    public async Task subscribes_to_stream(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        var options = new StreamSubscriptionOptions {
            Stream            = simulation.Game.Stream,
            Start             = StreamRevision.Min,
            CancellationToken = ct
        };

        await using var subscription = await AutomaticClient.Streams
            .Subscribe(options)
            .ShouldNotThrowOrFailAsync()
            .ConfigureAwait(false);

        var recordsReceived = new List<Record>();

        Heartbeat caughtUp = default;

        await foreach (var msg in subscription) {
            if (msg.IsRecord) {
                recordsReceived.Add(msg.AsRecord);
                Logger.LogDebug("Received: {Message}", msg.AsRecord.ToDebugString());

                continue;
            }

            Logger.LogDebug("Received: {Message}", msg.AsHeartbeat.ToString());

            if (msg.AsHeartbeat.Type == HeartbeatType.CaughtUp) {
                caughtUp = msg.AsHeartbeat;
                break;
            }
        }

        caughtUp.StreamRevision.ShouldBeEquivalentTo(simulation.Revision);

        recordsReceived.Count.ShouldBe(simulation.Game.GameEvents.Count);
    }

    [Test]
    public async Task stops_gracefully(CancellationToken ct) {
        var simulation = await SeedGame(ct);

        var options = new StreamSubscriptionOptions {
            Stream            = simulation.Game.Stream,
            Start             = StreamRevision.Min,
            CancellationToken = ct
        };

        var subscription = await AutomaticClient.Streams
            .Subscribe(options)
            .ShouldNotThrowOrFailAsync()
            .ConfigureAwait(false);

        var recordsReceived = new List<Record>();

        await foreach (var msg in subscription) {
            if (!msg.IsRecord) continue;

            recordsReceived.Add(msg.AsRecord);
            Logger.LogDebug("Received: {Message}", msg.AsRecord.ToDebugString());

            break;
        }

        recordsReceived.Count.ShouldBe(1);

        await Should.NotThrowAsync(async () => await subscription.DisposeAsync());
    }
}
