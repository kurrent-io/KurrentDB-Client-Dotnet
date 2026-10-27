#if !NET48
using System.Reflection;
using KurrentDB.Client;

namespace KurrentDB.Client.Tests.Streams;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:Read")]
public class ReadResultReleasesCallerTokenTests(ITestOutputHelper output, KurrentDBPermanentFixture fixture)
	: KurrentDBPermanentTests<KurrentDBPermanentFixture>(output, fixture) {
	const int Reads = 50;

	[Fact]
	public async Task read_stream_unregisters_from_caller_token() {
		var stream = Fixture.GetStreamName();
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents(5));

		using var parent = new CancellationTokenSource();

		for (var i = 0; i < Reads; i++) {
			await foreach (var _ in Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, cancellationToken: parent.Token)) { }

			await using var unread = Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, cancellationToken: parent.Token);
			Assert.Equal(ReadState.Ok, await unread.ReadState);

			await foreach (var _ in Fixture.Streams.ReadStreamAsync(Direction.Forwards, Fixture.GetStreamName(), StreamPosition.Start, cancellationToken: parent.Token).Messages) { }
		}

		await AssertNoRegistrations(parent);
	}

	[Fact]
	public async Task read_state_only_without_dispose_unregisters_from_caller_token_once_pump_completes() {
		using var parent = new CancellationTokenSource();

		for (var i = 0; i < Reads; i++) {
			var missing = Fixture.Streams.ReadStreamAsync(Direction.Forwards, Fixture.GetStreamName(), StreamPosition.Start, cancellationToken: parent.Token);
			Assert.Equal(ReadState.StreamNotFound, await missing.ReadState);
		}

		await AssertNoRegistrations(parent);
	}

	[Fact]
	public async Task read_all_unregisters_from_caller_token() {
		using var parent = new CancellationTokenSource();

		for (var i = 0; i < Reads; i++)
			await foreach (var _ in Fixture.Streams.ReadAllAsync(Direction.Backwards, Position.End, 5, cancellationToken: parent.Token)) { }

		await AssertNoRegistrations(parent);
	}

	[Fact]
	public async Task subscription_unregisters_from_caller_token() {
		var stream = Fixture.GetStreamName();
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents(1));

		using var parent = new CancellationTokenSource();

		for (var i = 0; i < Reads; i++) {
			await foreach (var _ in Fixture.Streams.SubscribeToStream(stream, FromStream.Start, cancellationToken: parent.Token))
				break;

			await using var unread = Fixture.Streams.SubscribeToStream(stream, FromStream.Start, cancellationToken: parent.Token);
			await unread.Features;
		}

		await AssertNoRegistrations(parent);
	}

	[Fact]
	public async Task persistent_subscription_unregisters_from_caller_token() {
		var stream = Fixture.GetStreamName();
		var group  = Fixture.GetGroupName();
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents(1));
		await Fixture.Subscriptions.CreateToStreamAsync(stream, group, new PersistentSubscriptionSettings());

		using var parent = new CancellationTokenSource();

		for (var i = 0; i < Reads; i++) {
			await foreach (var _ in Fixture.Subscriptions.SubscribeToStream(stream, group, cancellationToken: parent.Token).Messages)
				break;

			await using var unread = Fixture.Subscriptions.SubscribeToStream(stream, group, cancellationToken: parent.Token);
		}

		await AssertNoRegistrations(parent);
	}

	static async Task AssertNoRegistrations(CancellationTokenSource parent) {
		for (var i = 0; i < 50 && CountRegistrations(parent) > 0; i++)
			await Task.Delay(100);

		Assert.Equal(0, CountRegistrations(parent));
	}

	static int CountRegistrations(CancellationTokenSource cts) {
		const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

		var registrations = typeof(CancellationTokenSource).GetField("_registrations", Flags)!.GetValue(cts);
		var node          = registrations?.GetType().GetField("Callbacks", Flags)!.GetValue(registrations);
		var count         = 0;

		while (node is not null) {
			count++;
			node = node.GetType().GetField("Next", Flags)!.GetValue(node);
		}

		return count;
	}
}
#endif
