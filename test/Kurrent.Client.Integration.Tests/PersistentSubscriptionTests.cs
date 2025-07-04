using Kurrent.Client.Model;
using Microsoft.Extensions.Logging;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

public class PersistentSubscriptionTests : KurrentClientTestFixture {
	[Test]
	public async Task subscribe_to_all(CancellationToken ct) {
		// Arrange
		var simulations = await SeedGameSimulations(2, ct: ct).ToListAsync(cancellationToken: ct);

		var group = NewGroupName();

		var streams     = simulations.Select(x => x.Game.Stream.Value).ToArray();
		var expectedIds = simulations.SelectMany(x => x.Game.GameEvents).Select(x => x.RecordId).ToList();

		var gameEventsCount = simulations.Sum(x => x.Game.GameEvents.Count);

		var filter          = ReadFilter.FromPrefixes(ReadFilterScope.Stream, streams);
		var settings        = new PersistentSubscriptionSettings(LogPosition.Earliest);
		var recordsReceived = new List<Record>();

		// Act
		await AutomaticClient.PersistentSubscription.CreateToAll(group, filter, settings, ct);

		await using var subscription = AutomaticClient.PersistentSubscription.SubscribeToAll(group, cancellationToken: ct);

		await foreach (var msg in subscription.Messages.WithCancellation(ct)) {
			switch (msg) {
				case PersistentSubscriptionMessage.SubscriptionConfirmation(var subscriptionId):
					Logger.LogDebug("Subscription confirmed: {SubscriptionId}", subscriptionId);
					break;

				case PersistentSubscriptionMessage.Event(var record, _):
					Logger.LogDebug("Received event: {RecordId} from {StreamName}", record.Id, record.Stream);
					recordsReceived.Add(record);
					await subscription.Ack(record);
					break;
			}

			if (recordsReceived.Count == gameEventsCount)
				break;
		}

		// Assert
		recordsReceived.Select(x => x.Id).ShouldBe(expectedIds);
	}

	[Test]
	public async Task subscribes_to_stream(CancellationToken ct) {
		// Arrange
		var simulation = await SeedGame(ct);

		var stream = simulation.Game.Stream;
		var group  = simulation.Game.GameId.ToString();

		var recordsReceived = new List<Record>();
		var settings        = new PersistentSubscriptionSettings(LogPosition.Earliest);

		var expectedIds = simulation.Game.GameEvents.Select(x => x.RecordId).ToList();

		// Act
		await AutomaticClient.PersistentSubscription.CreateToStream(stream, group, settings, ct);

		await using var subscription = AutomaticClient.PersistentSubscription.SubscribeToStream(stream, group, cancellationToken: ct);

		await foreach (var msg in subscription.Messages.WithCancellation(ct)) {
			switch (msg) {
				case PersistentSubscriptionMessage.SubscriptionConfirmation(var subscriptionId):
					Logger.LogDebug("Subscription confirmed: {SubscriptionId}", subscriptionId);
					break;

				case PersistentSubscriptionMessage.Event(var record, _):
					Logger.LogDebug("Received event: {RecordId} from {StreamName}", record.Id, record.Stream);
					recordsReceived.Add(record);
					await subscription.Ack(record);
					break;
			}

			if (recordsReceived.Count == simulation.Game.GameEvents.Count)
				break;
		}

		// Assert
		recordsReceived.Select(x => x.Id).ShouldBe(expectedIds);
	}


	#region helpers

	static string NewGroupName() => Guid.NewGuid().ToString("N");

	#endregion
}
