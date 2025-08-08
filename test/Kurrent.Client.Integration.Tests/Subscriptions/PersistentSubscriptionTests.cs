using Kurrent.Client.PersistentSubscriptions;
using Kurrent.Client.Streams;
using Kurrent.Client.Testing.Shouldly;
using KurrentDB.Client;
using Microsoft.Extensions.Logging;
using PersistentSubscriptionMessage = Kurrent.Client.PersistentSubscriptions.PersistentSubscriptionMessage;
using PersistentSubscriptionSettings = Kurrent.Client.PersistentSubscriptions.PersistentSubscriptionSettings;

namespace Kurrent.Client.Tests.Subscriptions;

[Category("PersistentSubscriptions")]
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
		var settings        = new PersistentSubscriptionSettings { StartFrom = LogPosition.Earliest };
		var recordsReceived = new List<Record>();

		// Act
		await AutomaticClient.PersistentSubscriptions
            .CreateSubscription(group, StreamName.AllStream, filter, settings, ct);

		await using var subscription = await AutomaticClient.PersistentSubscriptions
			.SubscribeToAll(group, cancellationToken: ct)
			.ShouldNotThrowOrFailAsync();

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
		var group  = NewGroupName();

		var recordsReceived = new List<Record>();
		var settings        = new PersistentSubscriptionSettings { StartFrom = LogPosition.Earliest };

		var expectedIds = simulation.Game.GameEvents.Select(x => x.RecordId).ToList();

		// Act
		await AutomaticClient.PersistentSubscriptions.CreateSubscription(
			group, stream, ReadFilter.None, settings,
			ct
		);

		await using var subscription = await AutomaticClient.PersistentSubscriptions
			.SubscribeToStream(stream, group, cancellationToken: ct)
			.ShouldNotThrowOrFailAsync();

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

	[Test]
	public async Task get_info_to_all(CancellationToken ct) {
		// Arrange
		var settings = new PersistentSubscriptionSettings {
			StartFrom = LogPosition.Earliest
		};

		var expected = new PersistentSubscriptionDetails {
			Group   = NewGroupName(),
			Source = SystemStreams.AllStream,
			Settings    = settings
		};

		// Act
		await AutomaticClient.PersistentSubscriptions.CreateAllStreamSubscription(expected.Group, ReadFilter.None, expected.Settings, ct);

		await Task.Delay(1.Seconds(), ct);

		var info = await AutomaticClient.PersistentSubscriptions
			.GetPersistentAllStreamSubscription(expected.Group, ct)
			.ShouldNotThrowOrFailAsync();

		// Assert
		info.ShouldNotBeNull().ShouldBeEquivalentTo(
			expected, config => config
				.Excluding<PersistentSubscriptionDetails>(subscriptionInfo => subscriptionInfo.Connections)
				.Excluding<PersistentSubscriptionDetails>(subscriptionInfo => subscriptionInfo.Stats)
				.Excluding<PersistentSubscriptionDetails>(subscriptionInfo => subscriptionInfo.Status)
		);
	}

	[Test]
	public async Task get_info_to_stream(CancellationToken ct) {
		// Arrange
		var stream = Guid.NewGuid().ToString("N");

		var settings = new PersistentSubscriptionSettings { StartFrom = LogPosition.Earliest };

		var expected = new PersistentSubscriptionDetails {
			Group   = NewGroupName(),
			Source = stream,
			Settings    = settings
		};

		// Act
		await AutomaticClient.PersistentSubscriptions.CreateStreamSubscription(stream, expected.Group, expected.Settings, ct);

		await Task.Delay(1.Seconds(), ct);

		var info = await AutomaticClient.PersistentSubscriptions
			.GetPersistentStreamSubscription(stream, expected.Group, ct)
			.ShouldNotThrowOrFailAsync();

		// Assert
		info.ShouldNotBeNull().ShouldBeEquivalentTo(
			expected, config => config
				.Excluding<PersistentSubscriptionDetails>(subscriptionInfo => subscriptionInfo.Connections)
				.Excluding<PersistentSubscriptionDetails>(subscriptionInfo => subscriptionInfo.Stats)
				.Excluding<PersistentSubscriptionDetails>(subscriptionInfo => subscriptionInfo.Status)
		);
	}

	#region helpers

	static string NewGroupName() => Guid.NewGuid().ToString("N");

	#endregion
}
