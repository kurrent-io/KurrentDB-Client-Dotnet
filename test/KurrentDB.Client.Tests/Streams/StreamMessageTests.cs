namespace KurrentDB.Client.Tests.Streams;

[Trait("Category", "Target:Misc")]
public class StreamMessageTests {
	static readonly DateTime Timestamp = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

	[Fact]
	public void caught_up_and_fell_behind_print_no_members() {
		Assert.Equal("CaughtUp { }", new StreamMessage.CaughtUp().ToString());
		Assert.Equal("FellBehind { }", new StreamMessage.FellBehind().ToString());
	}

	[Fact]
	public void caught_up_and_fell_behind_of_a_subscription_print_their_members() {
		Assert.Equal(
			$"StreamCaughtUp {{ Timestamp = {Timestamp}, StreamPosition = 5 }}",
			new StreamMessage.StreamCaughtUp { Timestamp = Timestamp, StreamPosition = new StreamPosition(5) }.ToString()
		);

		Assert.Equal(
			$"AllStreamFellBehind {{ Timestamp = , Position = {new Position(7, 6)} }}",
			new StreamMessage.AllStreamFellBehind { Position = new Position(7, 6) }.ToString()
		);
	}

	[Fact]
	public void caught_up_and_fell_behind_compare_by_their_type_and_data() {
		Assert.Equal(new StreamMessage.CaughtUp(), new StreamMessage.CaughtUp());
		Assert.Equal(new StreamMessage.FellBehind(), new StreamMessage.FellBehind());
		Assert.NotEqual(new StreamMessage.CaughtUp(), new StreamMessage.StreamCaughtUp());
		Assert.NotEqual<StreamMessage>(new StreamMessage.StreamCaughtUp(), new StreamMessage.AllStreamCaughtUp());
		Assert.NotEqual(new StreamMessage.StreamCaughtUp(), new StreamMessage.StreamCaughtUp { StreamPosition = new StreamPosition(5) });
		Assert.Equal(
			new StreamMessage.AllStreamFellBehind { Timestamp = Timestamp, Position = new Position(7, 6) },
			new StreamMessage.AllStreamFellBehind { Timestamp = Timestamp, Position = new Position(7, 6) }
		);
	}

	[Fact]
	public void caught_up_and_fell_behind_of_a_subscription_match_the_base_message() {
		StreamMessage[] messages = [
			new StreamMessage.StreamCaughtUp(), new StreamMessage.AllStreamCaughtUp(),
			new StreamMessage.StreamFellBehind(), new StreamMessage.AllStreamFellBehind()
		];

		Assert.Equal(2, messages.Count(x => x is StreamMessage.CaughtUp));
		Assert.Equal(2, messages.Count(x => x is StreamMessage.FellBehind));
	}
}
