namespace KurrentDB.Client.Tests.Streams;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:MultiStreamAppend")]
public class MultiStreamAppendTests(ITestOutputHelper output, KurrentDBPermanentFixture fixture) : KurrentDBPermanentTests<KurrentDBPermanentFixture>(output, fixture) {
	[MinimumVersion.Fact(25, 1)]
	public async Task append_events_with_invalid_metadata_format_throws_exceptions() {
		// Arrange
		var stream = Fixture.GetStreamName();

		var invalidMetadata = "invalid"u8.ToArray();

		AppendStreamRequest[] requests = [
			new(stream, StreamState.NoStream, Fixture.CreateTestEvents(3, metadata: invalidMetadata))
		];

		// Act & Assert
		var exception = await Fixture.Streams
			.MultiStreamAppendAsync(requests).AsTask()
			.ShouldThrowAsync<ArgumentException>();

		exception.Message.ShouldContain("Failed to decode event metadata");
	}

	[MinimumVersion.Fact(25, 1)]
	public async Task append_events_to_multiple_streams() {
		// Arrange
		var stream1 = Fixture.GetStreamName();
		var stream2 = Fixture.GetStreamName();

		var expectedMetadata = new Dictionary<string, string> {
			["Name"] = Fixture.Faker.Person.FullName
		};

		AppendStreamRequest[] requests = [
			new(stream1, StreamState.NoStream, Fixture.CreateTestEvents(3, metadata: expectedMetadata.Encode()).ToArray()),
			new(stream2, StreamState.NoStream, Fixture.CreateTestEvents(2, metadata: expectedMetadata.Encode()).ToArray())
		];

		// Act
		var result = await Fixture.Streams.MultiStreamAppendAsync(requests);

		// Assert
		result.Position.ShouldBePositive();
		result.Responses.ShouldNotBeEmpty();

		result.Responses.First().Stream.ShouldBe(stream1);
		result.Responses.First().StreamRevision.ShouldBePositive();

		result.Responses.Last().Stream.ShouldBe(stream2);
		result.Responses.Last().StreamRevision.ShouldBePositive();

		var stream1Events = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream1, StreamPosition.Start, 10)
			.ToArrayAsync();

		var stream2Events = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream2, StreamPosition.Start, 10)
			.ToArrayAsync();

		var metadata = stream1Events.First().Decode();

		stream1Events.Length.ShouldBe(3);
		stream2Events.Length.ShouldBe(2);

		metadata.ShouldNotBeNull();
		metadata[Constants.Metadata.SchemaName].ShouldBe("test-event-type");
		metadata[Constants.Metadata.SchemaFormat].ShouldBe(nameof(SchemaDataFormat.Json));
		metadata["Name"].ShouldBe(expectedMetadata["Name"]);
	}

	[MinimumVersion.Fact(25, 1)]
	public async Task appending_events_with_stream_revision_conflicts() {
		// Arrange
		var stream1 = Fixture.GetStreamName();
		var stream2 = Fixture.GetStreamName();

		AppendStreamRequest[] requests = [
			new(stream1, StreamState.StreamExists, Fixture.CreateTestEvents(3).ToArray()),
			new(stream2, StreamState.StreamExists, Fixture.CreateTestEvents(3).ToArray()),
		];

		// Act
		var appendTask = async () => await Fixture.Streams.MultiStreamAppendAsync(requests);

		// Assert
		var rex = await appendTask.ShouldThrowAsync<WrongExpectedVersionException>();
		rex.ExpectedStreamState.ShouldBe(StreamState.StreamExists);
		rex.ActualStreamState.ShouldBe(StreamState.NoStream);
	}

	[MinimumVersion.Fact(25, 1)]
	public async Task appending_events_throws_deleted_exception_when_tombstoned() {
		// Arrange
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());

		await Fixture.Streams.TombstoneAsync(stream, StreamState.StreamExists);

		AppendStreamRequest[] requests = [
			new(stream, StreamState.NoStream, Fixture.CreateTestEvents(3).ToArray())
		];

		// Act
		var appendTask = async () => await Fixture.Streams.MultiStreamAppendAsync(requests);

		// Assert
		var rex = await appendTask.ShouldThrowAsync<StreamTombstonedException>();
		rex.Stream.ShouldBe(stream);
	}
}
