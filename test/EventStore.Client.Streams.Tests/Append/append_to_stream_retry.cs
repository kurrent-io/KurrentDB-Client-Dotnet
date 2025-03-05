using Polly;
using Polly.Contrib.WaitAndRetry;

namespace EventStore.Client.Streams.Tests.Append;

[Trait("Category", "Target:Stream")]
[Trait("Category", "Operation:Append")]
public class append_to_stream_retry(ITestOutputHelper output, StreamRetryFixture fixture) : EventStoreTests<StreamRetryFixture>(output, fixture) {
	[Fact]
	public async Task can_retry() {
		var stream = Fixture.GetStreamName();

		// can definitely write without throwing
		var result = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());

		result.NextExpectedStreamState.ShouldBe(new(0));

		await Fixture.Service.Restart();

		// write can be retried
		var writeResult = await Policy
			.Handle<Exception>()
			.WaitAndRetryAsync(
				Backoff.LinearBackoff(TimeSpan.FromMilliseconds(250), 10),
				(ex, ts) => Fixture.Log.Debug("Error writing events to stream. Retrying. Reason: {Message}.", ex.Message)
			)
			.ExecuteAsync(() => Fixture.Streams.AppendToStreamAsync(stream, result.NextExpectedStreamState, Fixture.CreateTestEvents()));

		Fixture.Log.Information("Successfully wrote events to stream {Stream}.", stream);
		
		writeResult.NextExpectedStreamState.ShouldBe(StreamState.StreamRevision(1));
	}
}

public class StreamRetryFixture() : EventStoreFixture(
	x => x.RunInMemory(false).With(o => o.ClientSettings.ConnectivitySettings.MaxDiscoverAttempts = 2)
);
