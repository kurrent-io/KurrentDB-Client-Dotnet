using KurrentDB.Client;
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
				new SetStreamMetadataOptions { UserCredentials = TestCredentials.Root }
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

	public MessageData[] Events { get; private set; } = [];

	public MessageBinaryData[] ExpectedEvents         { get; private set; } = [];
	public MessageBinaryData[] ExpectedEventsReversed { get; private set; } = [];

	public MessageBinaryData ExpectedFirstEvent { get; private set; }
	public MessageBinaryData ExpectedLastEvent  { get; private set; }
}
