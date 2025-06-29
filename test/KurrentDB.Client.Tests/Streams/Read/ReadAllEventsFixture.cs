using KurrentDB.Client.Tests.TestNode;

namespace KurrentDB.Client.Tests;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Target:All")]
[Trait("Category", "Operation:Read")]
[Trait("Category", "Database:Dedicated")]
public class ReadAllEventsFixture : KurrentDBTemporaryFixture {
	public ReadAllEventsFixture() {
		OnSetup = async () => {
			_ = await Streams.SetStreamMetadataAsync(
				SystemStreams.AllStream,
				StreamState.NoStream,
				new(acl: new(SystemRoles.All)),
				userCredentials: TestCredentials.Root
			);

			Events = CreateTestEvents(20)
				.Concat(CreateTestEvents(2, metadata: CreateMetadataOfSize(10_000)))
				.Concat(CreateTestEvents(2, AnotherTestEventType))
				.ToArray();

			ExpectedStreamName = GetStreamName();

			await Streams.AppendToStreamAsync(ExpectedStreamName, StreamState.NoStream, Events);

			ExpectedEvents         = Events.ToBinaryData();
			ExpectedEventsReversed = Enumerable.Reverse(ExpectedEvents).ToArray();

			ExpectedFirstEvent = ExpectedEvents.First();
			ExpectedLastEvent  = ExpectedEvents.Last();
		};
	}

	public string ExpectedStreamName { get; private set; } = null!;

	public EventData[] Events { get; private set; } = Array.Empty<EventData>();

	public EventBinaryData[] ExpectedEvents         { get; private set; } = Array.Empty<EventBinaryData>();
	public EventBinaryData[] ExpectedEventsReversed { get; private set; } = Array.Empty<EventBinaryData>();

	public EventBinaryData ExpectedFirstEvent { get; private set; }
	public EventBinaryData ExpectedLastEvent  { get; private set; }
}
