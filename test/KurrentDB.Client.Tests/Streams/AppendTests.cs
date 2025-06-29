using Grpc.Core;
using Humanizer;

namespace KurrentDB.Client.Tests.Streams;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:Append")]
public class AppendTests(ITestOutputHelper output, KurrentDBPermanentFixture fixture) : KurrentDBPermanentTests<KurrentDBPermanentFixture>(output, fixture) {
	[Theory, ExpectedVersionCreateStreamTestCases]
	public async Task appending_zero_events(StreamState expectedStreamState) {
		var stream = $"{Fixture.GetStreamName()}_{expectedStreamState}";

		const int iterations = 2;
		for (var i = 0; i < iterations; i++) {
			var writeResult = await Fixture.Streams.AppendToStreamAsync(
				stream,
				expectedStreamState,
				Enumerable.Empty<EventData>()
			);

			writeResult.NextExpectedStreamState.ShouldBe(StreamState.NoStream);
		}

		await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, iterations)
			.ShouldThrowAsync<StreamNotFoundException>(ex => ex.Stream.ShouldBe(stream));
	}

	[Theory, ExpectedVersionCreateStreamTestCases]
	public async Task appending_zero_events_again(StreamState expectedStreamState) {
		var stream = $"{Fixture.GetStreamName()}_{expectedStreamState}";

		const int iterations = 2;
		for (var i = 0; i < iterations; i++) {
			var writeResult = await Fixture.Streams.AppendToStreamAsync(
				stream,
				expectedStreamState,
				Enumerable.Empty<EventData>()
			);

			Assert.Equal(StreamState.NoStream, writeResult.NextExpectedStreamState);
		}

		await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, iterations)
			.ShouldThrowAsync<StreamNotFoundException>(ex => ex.Stream.ShouldBe(stream));
	}

	[Theory, ExpectedVersionCreateStreamTestCases]
	public async Task create_stream_expected_version_on_first_write_if_does_not_exist(StreamState expectedStreamState) {
		var stream = $"{Fixture.GetStreamName()}_{expectedStreamState}";

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			expectedStreamState,
			Fixture.CreateTestEvents(1)
		);

		Assert.Equal(new(0), writeResult.NextExpectedStreamState);

		var count = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 2)
			.CountAsync();

		Assert.Equal(1, count);
	}

	[RetryFact]
	public async Task multiple_idempotent_writes() {
		var stream = Fixture.GetStreamName();
		var events = Fixture.CreateTestEvents(4).ToArray();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events);
		Assert.Equal(new(3), writeResult.NextExpectedStreamState);

		writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events);
		Assert.Equal(new(3), writeResult.NextExpectedStreamState);
	}

	[RetryFact]
	public async Task multiple_idempotent_writes_with_same_id_bug_case() {
		var stream = Fixture.GetStreamName();

		var evnt   = Fixture.CreateTestEvents().First();
		var events = new[] { evnt, evnt, evnt, evnt, evnt, evnt };

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events);

		Assert.Equal(new(5), writeResult.NextExpectedStreamState);
	}

	[RetryFact]
	public async Task
		in_case_where_multiple_writes_of_multiple_events_with_the_same_ids_using_expected_version_any_then_next_expected_version_is_unreliable() {
		var stream = Fixture.GetStreamName();

		var evnt   = Fixture.CreateTestEvents().First();
		var events = new[] { evnt, evnt, evnt, evnt, evnt, evnt };

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events);

		Assert.Equal(new(5), writeResult.NextExpectedStreamState);

		writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events);

		Assert.Equal(new(0), writeResult.NextExpectedStreamState);
	}

	[RetryFact]
	public async Task
		in_case_where_multiple_writes_of_multiple_events_with_the_same_ids_using_expected_version_nostream_then_next_expected_version_is_correct() {
		var stream = Fixture.GetStreamName();

		var evnt           = Fixture.CreateTestEvents().First();
		var events         = new[] { evnt, evnt, evnt, evnt, evnt, evnt };
		var streamRevision = StreamState.StreamRevision((ulong)events.Length - 1);

		var writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		Assert.Equal(streamRevision, writeResult.NextExpectedStreamState);

		writeResult = await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		Assert.Equal(streamRevision, writeResult.NextExpectedStreamState);
	}

	[RetryFact]
	public async Task writing_with_correct_expected_version_to_deleted_stream_throws_stream_deleted() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);

		await Fixture.Streams
			.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents(1))
			.ShouldThrowAsync<StreamDeletedException>();
	}

	[RetryFact]
	public async Task returns_log_position_when_writing() {
		var stream = Fixture.GetStreamName();

		var result = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(1)
		);

		Assert.True(0 < result.LogPosition.PreparePosition);
		Assert.True(0 < result.LogPosition.CommitPosition);
	}

	[RetryFact]
	public async Task writing_with_any_expected_version_to_deleted_stream_throws_stream_deleted() {
		var stream = Fixture.GetStreamName();
		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);

		await Fixture.Streams
			.AppendToStreamAsync(stream, StreamState.Any, Fixture.CreateTestEvents(1))
			.ShouldThrowAsync<StreamDeletedException>();
	}

	[RetryFact]
	public async Task writing_with_invalid_expected_version_to_deleted_stream_throws_stream_deleted() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);

		await Fixture.Streams
			.AppendToStreamAsync(stream, StreamState.StreamRevision(5), Fixture.CreateTestEvents())
			.ShouldThrowAsync<StreamDeletedException>();
	}

	[RetryFact]
	public async Task append_with_correct_expected_version_to_existing_stream() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(1)
		);

		writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			writeResult.NextExpectedStreamState,
			Fixture.CreateTestEvents()
		);

		Assert.Equal(new(1), writeResult.NextExpectedStreamState);
	}

	[RetryFact]
	public async Task append_with_any_expected_version_to_existing_stream() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(1)
		);

		Assert.Equal(new(0), writeResult.NextExpectedStreamState);

		writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.Any,
			Fixture.CreateTestEvents(1)
		);

		Assert.Equal(new(1), writeResult.NextExpectedStreamState);
	}

	[RetryFact]
	public async Task appending_with_wrong_expected_version_to_existing_stream_throws_wrong_expected_version() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());

		var ex = await Fixture.Streams
			.AppendToStreamAsync(stream, StreamState.StreamRevision(999), Fixture.CreateTestEvents())
			.ShouldThrowAsync<WrongExpectedVersionException>();

		ex.ActualStreamState.ShouldBe(new(0));
		ex.ExpectedStreamState.ShouldBe(new(999));
	}

	[RetryFact]
	public async Task appending_with_wrong_expected_version_to_existing_stream_returns_wrong_expected_version() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.StreamRevision(1),
			Fixture.CreateTestEvents(),
			options => { options.ThrowOnAppendFailure = false; }
		);

		var wrongExpectedVersionResult = (WrongExpectedVersionResult)writeResult;

		Assert.Equal(new(1), wrongExpectedVersionResult.NextExpectedStreamState);
	}

	[RetryFact]
	public async Task append_with_stream_exists_expected_version_to_existing_stream() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.StreamExists,
			Fixture.CreateTestEvents()
		);
	}

	[RetryFact]
	public async Task append_with_stream_exists_expected_version_to_stream_with_multiple_events() {
		var stream = Fixture.GetStreamName();

		for (var i = 0; i < 5; i++)
			await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, Fixture.CreateTestEvents(1));

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.StreamExists,
			Fixture.CreateTestEvents()
		);
	}

	[RetryFact]
	public async Task append_with_stream_exists_expected_version_if_metadata_stream_exists() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.SetStreamMetadataAsync(
			stream,
			StreamState.Any,
			new(10, default)
		);

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.StreamExists,
			Fixture.CreateTestEvents()
		);
	}

	[RetryFact]
	public async Task
		appending_with_stream_exists_expected_version_and_stream_does_not_exist_throws_wrong_expected_version() {
		var stream = Fixture.GetStreamName();

		var ex = await Fixture.Streams
			.AppendToStreamAsync(stream, StreamState.StreamExists, Fixture.CreateTestEvents())
			.ShouldThrowAsync<WrongExpectedVersionException>();

		ex.ActualStreamState.ShouldBe(StreamState.NoStream);
	}

	[RetryFact]
	public async Task
		appending_with_stream_exists_expected_version_and_stream_does_not_exist_returns_wrong_expected_version() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.StreamExists,
			Fixture.CreateTestEvents(),
			options => { options.ThrowOnAppendFailure = false; }
		);

		var wrongExpectedVersionResult = Assert.IsType<WrongExpectedVersionResult>(writeResult);

		Assert.Equal(StreamState.Any, wrongExpectedVersionResult.NextExpectedStreamState);
	}

	[RetryFact]
	public async Task appending_with_stream_exists_expected_version_to_hard_deleted_stream_throws_stream_deleted() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.TombstoneAsync(stream, StreamState.NoStream);

		await Fixture.Streams
			.AppendToStreamAsync(stream, StreamState.StreamExists, Fixture.CreateTestEvents())
			.ShouldThrowAsync<StreamDeletedException>();
	}

	[RetryFact]
	public async Task appending_with_stream_exists_expected_version_to_deleted_stream_throws_stream_deleted() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());

		await Fixture.Streams.DeleteAsync(stream, StreamState.Any);

		await Fixture.Streams
			.AppendToStreamAsync(stream, StreamState.StreamExists, Fixture.CreateTestEvents())
			.ShouldThrowAsync<StreamDeletedException>();
	}

	[RetryFact]
	public async Task can_append_multiple_events_at_once() {
		var stream = Fixture.GetStreamName();

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(100)
		);

		Assert.Equal(new(99), writeResult.NextExpectedStreamState);
	}

	[RetryFact]
	public async Task returns_failure_status_when_conditionally_appending_with_version_mismatch() {
		var stream = Fixture.GetStreamName();

		var result = await Fixture.Streams.ConditionalAppendToStreamAsync(
			stream,
			StreamState.StreamRevision(7),
			Fixture.CreateTestEvents()
		);

		Assert.Equal(
			ConditionalWriteResult.FromWrongExpectedVersion(new(stream, StreamState.StreamRevision(7), StreamState.NoStream)),
			result
		);
	}

	[RetryFact]
	public async Task returns_success_status_when_conditionally_appending_with_matching_version() {
		var stream = Fixture.GetStreamName();

		var result = await Fixture.Streams.ConditionalAppendToStreamAsync(
			stream,
			StreamState.Any,
			Fixture.CreateTestEvents()
		);

		Assert.Equal(
			ConditionalWriteResult.FromWriteResult(new SuccessResult(0, result.LogPosition)),
			result
		);
	}

	[RetryFact]
	public async Task returns_failure_status_when_conditionally_appending_to_a_deleted_stream() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());

		await Fixture.Streams.TombstoneAsync(stream, StreamState.Any);

		var result = await Fixture.Streams.ConditionalAppendToStreamAsync(
			stream,
			StreamState.Any,
			Fixture.CreateTestEvents()
		);

		Assert.Equal(ConditionalWriteResult.StreamDeleted, result);
	}

	[RetryFact]
	public async Task expected_version_no_stream() {
		var result = await Fixture.Streams.AppendToStreamAsync(
			Fixture.GetStreamName(),
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		Assert.Equal(new(0), result!.NextExpectedStreamState);
	}

	[RetryFact]
	public async Task expected_version_no_stream_returns_position() {
		var result = await Fixture.Streams.AppendToStreamAsync(
			Fixture.GetStreamName(),
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		Assert.True(result.LogPosition > Position.Start);
	}

	[RetryFact]
	public async Task with_timeout_any_stream_revision_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();

		var ex = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.Any,
			Fixture.CreateTestEvents(100),
			deadline: TimeSpan.FromTicks(1)
		).ShouldThrowAsync<RpcException>();

		ex.StatusCode.ShouldBe(StatusCode.DeadlineExceeded);
	}

	[RetryFact]
	public async Task with_timeout_stream_revision_fails_when_operation_expired() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, Fixture.CreateTestEvents());

		var ex = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.StreamRevision(0),
			Fixture.CreateTestEvents(10),
			deadline: TimeSpan.Zero
		).ShouldThrowAsync<RpcException>();

		ex.StatusCode.ShouldBe(StatusCode.DeadlineExceeded);
	}

	[RetryFact]
	public async Task when_events_enumerator_throws_the_write_does_not_succeed() {
		var streamName = Fixture.GetStreamName();

		await Fixture.Streams
			.AppendToStreamAsync(
				streamName,
				StreamState.Any,
				Fixture.CreateTestEventsThatThrowsException(),
				userCredentials: new UserCredentials(TestCredentials.Root.Username!, TestCredentials.Root.Password!)
			)
			.ShouldThrowAsync<Exception>();

		var state = await Fixture.Streams.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start)
			.ReadState;

		state.ShouldBe(ReadState.StreamNotFound);
	}

	[RetryFact]
	public async Task succeeds_when_size_is_less_than_max_append_size() {
		// Arrange
		var maxAppendSize = (uint)100.Kilobytes().Bytes;
		var stream        = Fixture.GetStreamName();

		// Act
		var (events, size) = Fixture.CreateTestEventsUpToMaxSize(maxAppendSize);

		// Assert
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
	}

	[RetryFact]
	public async Task fails_when_size_exceeds_max_append_size() {
		// Arrange
		var maxAppendSize    = (uint)100.Kilobytes().Bytes;
		var stream           = Fixture.GetStreamName();
		var eventsAppendSize = (uint)10.Megabytes().Bytes;

		// Act
		var (events, size) = Fixture.CreateTestEventsUpToMaxSize(eventsAppendSize);

		// Assert
		size.ShouldBeGreaterThan(maxAppendSize);

		var ex = await Fixture.Streams
			.AppendToStreamAsync(stream, StreamState.NoStream, events)
			.ShouldThrowAsync<MaximumAppendSizeExceededException>();

		ex.MaxAppendSize.ShouldBe(maxAppendSize);
	}

	[RetryFact]
	public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0em1_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(6).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[RetryFact]
	public async Task sequence_0em1_1e0_2e1_3e2_4e3_4e4_0any_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(6).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events.Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[RetryFact]
	public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0e5_non_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(6).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.StreamRevision(5), events.Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 2).CountAsync();

		Assert.Equal(events.Length + 1, count);
	}

	[RetryFact]
	public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0e6_throws_wev() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(6).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		await Assert.ThrowsAsync<WrongExpectedVersionException>(() => Fixture.Streams.AppendToStreamAsync(stream, StreamState.StreamRevision(6), events.Take(1)));
	}

	[RetryFact]
	public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0e6_returns_wev() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(6).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.StreamRevision(6),
			events.Take(1),
			options => options.ThrowOnAppendFailure = false
		);

		Assert.IsType<WrongExpectedVersionResult>(writeResult);
	}

	[RetryFact]
	public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0e4_throws_wev() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(6).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		await Assert.ThrowsAsync<WrongExpectedVersionException>(() => Fixture.Streams.AppendToStreamAsync(stream, StreamState.StreamRevision(4), events.Take(1)));
	}

	[RetryFact]
	public async Task sequence_0em1_1e0_2e1_3e2_4e3_5e4_0e4_returns_wev() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(6).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.StreamRevision(4),
			events.Take(1),
			options => options.ThrowOnAppendFailure = false
		);

		Assert.IsType<WrongExpectedVersionResult>(writeResult);
	}

	[RetryFact]
	public async Task sequence_0em1_0e0_non_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents().ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.StreamRevision(0), events.Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 2).CountAsync();

		Assert.Equal(events.Length + 1, count);
	}

	[RetryFact]
	public async Task sequence_0em1_0any_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents().ToArray();

		await Task.Delay(TimeSpan.FromSeconds(30));
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events.Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[RetryFact]
	public async Task sequence_0em1_0em1_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents().ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[RetryFact]
	public async Task sequence_0em1_1e0_2e1_1any_1any_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(3).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events.Skip(1).Take(1));
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events.Skip(1).Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[RetryFact]
	public async Task sequence_S_0em1_1em1_E_S_0em1_E_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(2).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[RetryFact]
	public async Task sequence_S_0em1_1em1_E_S_0any_E_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(2).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events.Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[RetryFact]
	public async Task sequence_S_0em1_1em1_E_S_1e0_E_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(2).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.StreamRevision(0), events.Skip(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[RetryFact]
	public async Task sequence_S_0em1_1em1_E_S_1any_E_idempotent() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(2).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events);
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.Any, events.Skip(1).Take(1));

		var count = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, events.Length + 1).CountAsync();

		Assert.Equal(events.Length, count);
	}

	[RetryFact]
	public async Task sequence_S_0em1_1em1_E_S_0em1_1em1_2em1_E_idempotancy_fail_throws() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(3).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(2));

		await Assert.ThrowsAsync<WrongExpectedVersionException>(
			() => Fixture.Streams.AppendToStreamAsync(
				stream,
				StreamState.NoStream,
				events
			)
		);
	}

	[RetryFact]
	public async Task sequence_S_0em1_1em1_E_S_0em1_1em1_2em1_E_idempotancy_fail_returns() {
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(3).ToArray();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, events.Take(2));

		var writeResult = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			events,
			options => options.ThrowOnAppendFailure = false
		);

		Assert.IsType<WrongExpectedVersionResult>(writeResult);
	}

	// [Fact]
	// public async Task sending_and_receiving_large_messages_over_the_hard_limit() {
	// 	uint maxAppendSize = 16 * 1024 * 1024 - 10000;
	// 	var  streamName    = Fixture.GetStreamName();
	// 	var largeEvent = Fixture.CreateTestEvents()
	// 		.Select(e => new EventData(e.EventId, "-", new byte[maxAppendSize + 1]));
	//
	// 	var ex = await Assert.ThrowsAsync<RpcException>(() => Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, largeEvent));
	//
	// 	Assert.Equal(StatusCode.ResourceExhausted, ex.StatusCode);
	// }

	class ExpectedVersionCreateStreamTestCases : TestCaseGenerator<ExpectedVersionCreateStreamTestCases> {
		protected override IEnumerable<object[]> Data() {
			yield return [StreamState.Any];
			yield return [StreamState.NoStream];
		}
	}
}
