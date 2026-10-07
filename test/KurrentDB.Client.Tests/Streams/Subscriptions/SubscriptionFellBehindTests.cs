using Grpc.Core;
using Grpc.Core.Interceptors;

namespace KurrentDB.Client.Tests.Streams;

[Trait("Category", "Subscriptions")]
[Trait("Category", "Target:Streams")]
public class SubscriptionFellBehindTests(ITestOutputHelper output, KurrentDBPermanentFixture fixture)
	: KurrentDBPermanentTests<KurrentDBPermanentFixture>(output, fixture) {
	const string FinishEventType = "finish";

	static readonly Version FellBehindVersion = new(26, 2, 2);

	// CaughtUp carries a timestamp and checkpoint from this version
	static readonly Version RichCaughtUpVersion = new(25, 1);

	// enough to overflow the live buffer of the subscription
	const int NumBatchesToFallBehind = 2;
	const int NumEventsPerBatch      = 500;

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task works_with_any_server(bool richLiveness, bool subscribeToAll) {
		var requests = new RequestRecorder();
		var settings = Fixture.DBClientSettings;
		settings.EnableRichSubscriptionLiveness = richLiveness;
		settings.Interceptors      = [requests];
		await using var client = new KurrentDBClient(settings);

		// stays within the live buffer: servers before 24.10 drop a subscription that overflows it
		var (messages, subscriptionFeatures) = await GoLiveAndReceive(client, subscribeToAll, numBatches: 1, numEventsPerBatch: 10, untilCaughtUpAgain: false);

		var events     = messages.OfType<StreamMessage.Event>().Select(x => x.ResolvedEvent).ToArray();
		var finish     = events.Last(x => x.OriginalEvent.EventType == FinishEventType);
		var streamName = finish.OriginalStreamId;
		var last       = finish.OriginalEventNumber;

		var first = subscribeToAll ? 1UL : 0UL;
		Assert.Equal(
			Enumerable.Range(0, (int)(last.ToUInt64() - first) + 1).Select(x => new StreamPosition(first + (ulong)x)),
			events.Where(x => x.OriginalStreamId == streamName).Select(x => x.OriginalEventNumber)
		);

		Assert.Contains(messages, x => x is StreamMessage.CaughtUp);

		var caughtUps = messages.OfType<StreamMessage.CaughtUp>().ToArray();
		if (!richLiveness) {
			Assert.DoesNotContain(messages, x => x is StreamMessage.FellBehind);
			AssertWithoutData(caughtUps);
		} else {
			Assert.All(caughtUps, x => Assert.True(subscribeToAll ? x is StreamMessage.AllStreamCaughtUp : x is StreamMessage.StreamCaughtUp, x.ToString()));

			// servers send these with CaughtUp before they support FellBehind
			if (Fixture.DatabaseVersion >= RichCaughtUpVersion)
				Assert.All(
					caughtUps,
					x => {
						Assert.NotNull((x as StreamMessage.StreamCaughtUp)?.Timestamp ?? (x as StreamMessage.AllStreamCaughtUp)?.Timestamp);
						Assert.True(
							subscribeToAll ? x is StreamMessage.AllStreamCaughtUp { Position: not null } : x is StreamMessage.StreamCaughtUp { StreamPosition: not null },
							x.ToString()
						);
					}
				);
		}

		Assert.Equal(richLiveness && Fixture.DatabaseVersion >= FellBehindVersion, subscriptionFeatures.RichLiveness);

		// without the setting the request is as before it existed
		var subscribeRequest = Assert.Single(requests.ServerStreaming);
		Assert.Equal(richLiveness, subscribeRequest.Contains("\"controlOption\""));
		if (richLiveness)
			Assert.Contains("\"controlOption\": { \"compatibility\": 2 }", subscribeRequest);

		var read = await client
			.ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, 1)
			.Messages.ToArrayAsync();

		Assert.Contains(read, x => x is StreamMessage.Event);
		if (Fixture.HasLastStreamPosition)
			Assert.Contains(read, x => x == new StreamMessage.LastStreamPosition(last));
	}

	// before 24.10 a subscription that overflowed its live buffer was dropped instead of falling behind
	[MinimumVersion.Theory(24, 10)]
	[InlineData(false)]
	[InlineData(true)]
	public async Task default_settings_never_receive_fell_behind(bool subscribeToAll) {
		Assert.False(Fixture.DBClientSettings.EnableRichSubscriptionLiveness);

		var (messages, _) = await FallBehind(Fixture.Streams, subscribeToAll);

		Assert.DoesNotContain(messages, x => x is StreamMessage.FellBehind);

		var caughtUps = messages.OfType<StreamMessage.CaughtUp>().ToArray();
		Assert.True(caughtUps.Length >= 2, $"caught up {caughtUps.Length} time(s)");
		AssertWithoutData(caughtUps);
	}

	[MinimumVersion.Theory(26, 2, 2)]
	[InlineData(false)]
	[InlineData(true)]
	public async Task subscription_fell_behind_level_receives_fell_behind_then_caught_up(bool subscribeToAll) {
		var settings = Fixture.DBClientSettings;
		settings.EnableRichSubscriptionLiveness = true;
		await using var client = new KurrentDBClient(settings);

		var (messages, subscriptionFeatures) = await FallBehind(client, subscribeToAll);

		Assert.True(subscriptionFeatures.RichLiveness);

		var numCaughtUp   = messages.Count(x => x is StreamMessage.CaughtUp);
		var numFellBehind = messages.Count(x => x is StreamMessage.FellBehind);
		Assert.True(numFellBehind >= 1, "did not fall behind");
		Assert.Equal(numCaughtUp - 1, numFellBehind);

		var             live                 = false;
		StreamPosition? lastStreamPosition   = null;
		Position?       lastPosition         = null;

		foreach (var message in messages) {
			switch (message) {
				case StreamMessage.Event(var resolvedEvent):
					lastStreamPosition = resolvedEvent.OriginalEventNumber;
					lastPosition       = resolvedEvent.OriginalPosition;
					break;

				case StreamMessage.CaughtUp caughtUp:
					Assert.False(live, "caught up when already live");
					live = true;
					AssertCheckpoint(
						(caughtUp as StreamMessage.StreamCaughtUp)?.Timestamp ?? (caughtUp as StreamMessage.AllStreamCaughtUp)?.Timestamp,
						(caughtUp as StreamMessage.StreamCaughtUp)?.StreamPosition,
						(caughtUp as StreamMessage.AllStreamCaughtUp)?.Position
					);
					break;

				case StreamMessage.FellBehind fellBehind:
					Assert.True(live, "fell behind when not live");
					live = false;
					AssertCheckpoint(
						(fellBehind as StreamMessage.StreamFellBehind)?.Timestamp ?? (fellBehind as StreamMessage.AllStreamFellBehind)?.Timestamp,
						(fellBehind as StreamMessage.StreamFellBehind)?.StreamPosition,
						(fellBehind as StreamMessage.AllStreamFellBehind)?.Position
					);
					break;
			}
		}

		Assert.True(live);

		Assert.All(
			messages.Where(x => x is StreamMessage.CaughtUp or StreamMessage.FellBehind),
			x => Assert.True(
				subscribeToAll
					? x is StreamMessage.AllStreamCaughtUp or StreamMessage.AllStreamFellBehind
					: x is StreamMessage.StreamCaughtUp or StreamMessage.StreamFellBehind,
				x.ToString()
			)
		);

		return;

		void AssertCheckpoint(DateTime? timestamp, StreamPosition? streamPosition, Position? position) {
			Assert.NotNull(timestamp);
			Assert.Equal(DateTimeKind.Utc, timestamp.Value.Kind);
			Assert.InRange(timestamp.Value, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(5));

			if (subscribeToAll) {
				Assert.Null(streamPosition);
				Assert.NotNull(lastPosition);
				Assert.Equal(lastPosition, position);
			} else {
				Assert.Null(position);
				Assert.NotNull(lastStreamPosition);
				Assert.Equal(lastStreamPosition, streamPosition);
			}
		}
	}

	// the data-less singleton, as yielded before the derived types
	static void AssertWithoutData(StreamMessage.CaughtUp[] caughtUps) =>
		Assert.All(
			caughtUps,
			x => {
				Assert.Same(caughtUps[0], x);
				Assert.Equal(new StreamMessage.CaughtUp(), x);
				Assert.Equal("CaughtUp { }", x.ToString());
			}
		);

	class RequestRecorder : Interceptor {
		public List<string> ServerStreaming { get; } = [];

		public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
			TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
			AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation
		) {
			lock (ServerStreaming)
				ServerStreaming.Add(request.ToString()!);

			return continuation(request, context);
		}
	}

	// goes live, falls behind and catches up again
	Task<(List<StreamMessage> Messages, StreamSubscriptionFeatures Features)> FallBehind(KurrentDBClient client, bool subscribeToAll) =>
		GoLiveAndReceive(client, subscribeToAll, NumBatchesToFallBehind, NumEventsPerBatch, untilCaughtUpAgain: true);

	// goes live, then appends the batches and a final event; returns all messages received
	async Task<(List<StreamMessage> Messages, StreamSubscriptionFeatures Features)> GoLiveAndReceive(
		KurrentDBClient client, bool subscribeToAll, int numBatches, int numEventsPerBatch, bool untilCaughtUpAgain
	) {
		var streamName = Fixture.GetStreamName();
		var messages   = new List<StreamMessage>();


		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

		var start = await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, Fixture.CreateTestEvents());
		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.StreamExists, Fixture.CreateTestEvents(10));

		await using var subscription = subscribeToAll
			? client.SubscribeToAll(FromAll.After(start.LogPosition), userCredentials: TestCredentials.Root, cancellationToken: cts.Token)
			: client.SubscribeToStream(streamName, FromStream.Start, cancellationToken: cts.Token);

		var features = await subscription.Features.WithTimeout();

		await using var enumerator = subscription.Messages.GetAsyncEnumerator();

		while (await enumerator.MoveNextAsync()) {
			messages.Add(enumerator.Current);
			if (enumerator.Current is StreamMessage.CaughtUp)
				break;
		}

		for (var i = 0; i < numBatches; i++)
			await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.StreamExists, Fixture.CreateTestEvents(numEventsPerBatch));

		var finish = Fixture.CreateTestEvent(FinishEventType);
		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.StreamExists, [finish]);

		// the last event arrives either while catching up or once live again, and it can fall behind more than once
		var finished      = false;
		var caughtUpAgain = !untilCaughtUpAgain;
		var live          = true;
		while (!(finished && caughtUpAgain && live) && await enumerator.MoveNextAsync()) {
			messages.Add(enumerator.Current);

			switch (enumerator.Current) {
				case StreamMessage.Event(var resolvedEvent) when resolvedEvent.OriginalEvent.EventId == finish.EventId:
					finished = true;
					break;

				case StreamMessage.CaughtUp:
					caughtUpAgain = true;
					live          = true;
					break;

				case StreamMessage.FellBehind:
					live = false;
					break;
			}
		}

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(messages[0]);
		Assert.Same(features, await subscription.Features);

		return (messages, features);
	}
}
