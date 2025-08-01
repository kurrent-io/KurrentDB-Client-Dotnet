using Kurrent.Client.Streams;
using Kurrent.Client.Testing.Shouldly;
using KurrentDB.Client;
using Microsoft.Extensions.Logging;
using PersistentSubscriptionInfo = Kurrent.Client.PersistentSubscriptions.PersistentSubscriptionInfo;
using PersistentSubscriptionMessage = Kurrent.Client.PersistentSubscriptions.PersistentSubscriptionMessage;

namespace Kurrent.Client.Tests.PersistentSubscriptions;

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
		await AutomaticClient.PersistentSubscriptions.CreateToAll(
			group, filter, settings,
			ct
		);

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
		await AutomaticClient.PersistentSubscriptions.CreateToStream(
			stream, group, settings,
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

		var expected = new PersistentSubscriptionInfo {
			GroupName   = NewGroupName(),
			EventSource = SystemStreams.AllStream,
			Settings    = settings
		};

		// Act
		await AutomaticClient.PersistentSubscriptions.CreateToAll(expected.GroupName, expected.Settings, ct);

		await Task.Delay(1.Seconds(), ct);

		var info = await AutomaticClient.PersistentSubscriptions
			.GetInfoToAll(expected.GroupName, ct)
			.ShouldNotThrowOrFailAsync();

		// Assert
		info.ShouldNotBeNull().ShouldBeEquivalentTo(
			expected, config => config
				.Excluding<PersistentSubscriptionInfo>(subscriptionInfo => subscriptionInfo.Connections)
				.Excluding<PersistentSubscriptionInfo>(subscriptionInfo => subscriptionInfo.Stats)
				.Excluding<PersistentSubscriptionInfo>(subscriptionInfo => subscriptionInfo.Status)
		);
	}

	[Test]
	public async Task get_info_to_stream(CancellationToken ct) {
		// Arrange
		var stream = Guid.NewGuid().ToString("N");

		var settings = new PersistentSubscriptionSettings { StartFrom = LogPosition.Earliest };

		var expected = new PersistentSubscriptionInfo {
			GroupName   = NewGroupName(),
			EventSource = stream,
			Settings    = settings
		};

		// Act
		await AutomaticClient.PersistentSubscriptions.CreateToStream(stream, expected.GroupName, expected.Settings, ct);

		await Task.Delay(1.Seconds(), ct);

		var info = await AutomaticClient.PersistentSubscriptions
			.GetInfoToStream(stream, expected.GroupName, ct)
			.ShouldNotThrowOrFailAsync();

		// Assert
		info.ShouldNotBeNull().ShouldBeEquivalentTo(
			expected, config => config
				.Excluding<PersistentSubscriptionInfo>(subscriptionInfo => subscriptionInfo.Connections)
				.Excluding<PersistentSubscriptionInfo>(subscriptionInfo => subscriptionInfo.Stats)
				.Excluding<PersistentSubscriptionInfo>(subscriptionInfo => subscriptionInfo.Status)
		);
	}

	#region helpers

	static string NewGroupName() => Guid.NewGuid().ToString("N");

	#endregion
}
