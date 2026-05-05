namespace KurrentDB.Client.Tests.Streams.AppendRecords;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:AppendRecords")]
public class AppendRecordsTests(ITestOutputHelper output, KurrentDBPermanentFixture fixture) : KurrentDBPermanentTests<KurrentDBPermanentFixture>(output, fixture) {
	[MinimumVersion.Fact(26, 1)]
	public async Task append_records_to_single_stream() {
		// Arrange
		var stream = Fixture.GetStreamName();

		var events = Fixture.CreateTestEvents(3);
		var records = events.Select(e => new AppendRecord(stream, e));

		// Act
		var result = await Fixture.Streams.AppendRecordsAsync(records);

		// Assert
		result.Position.ShouldBePositive();
		result.Responses.ShouldNotBeEmpty();
		result.Responses!.Count().ShouldBe(1);
		result.Responses!.First().Stream.ShouldBe(stream);
		result.Responses!.First().StreamRevision.ShouldBePositive();

		var readEvents = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 10)
			.ToArrayAsync();

		readEvents.Length.ShouldBe(3);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task append_records_to_multiple_streams() {
		// Arrange
		var stream1 = Fixture.GetStreamName();
		var stream2 = Fixture.GetStreamName();

		var expectedMetadata = new Dictionary<string, string> {
			["Name"] = Fixture.Faker.Person.FullName
		};

		var records = new[] {
			new AppendRecord(stream1, Fixture.CreateTestEvent(metadata: expectedMetadata.Encode())),
			new AppendRecord(stream1, Fixture.CreateTestEvent(metadata: expectedMetadata.Encode())),
			new AppendRecord(stream1, Fixture.CreateTestEvent(metadata: expectedMetadata.Encode())),
			new AppendRecord(stream2, Fixture.CreateTestEvent(metadata: expectedMetadata.Encode())),
			new AppendRecord(stream2, Fixture.CreateTestEvent(metadata: expectedMetadata.Encode())),
		};

		// Act
		var result = await Fixture.Streams.AppendRecordsAsync(records);

		// Assert
		result.Position.ShouldBePositive();
		result.Responses.ShouldNotBeEmpty();
		result.Responses!.Count().ShouldBe(2);

		var stream1Events = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream1, StreamPosition.Start, 10)
			.ToArrayAsync();

		var stream2Events = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream2, StreamPosition.Start, 10)
			.ToArrayAsync();

		stream1Events.Length.ShouldBe(3);
		stream2Events.Length.ShouldBe(2);

		var metadata = stream1Events.First().Decode();
		metadata.ShouldNotBeNull();
		metadata[Constants.Metadata.SchemaName].ShouldBe("test-event-type");
		metadata[Constants.Metadata.SchemaFormat].ShouldBe(nameof(SchemaDataFormat.Json));
		metadata["Name"].ShouldBe(expectedMetadata["Name"]);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task interleaved_tracks_revisions() {
		// Arrange
		var stream1 = Fixture.GetStreamName();
		var stream2 = Fixture.GetStreamName();

		var records = new[] {
			new AppendRecord(stream1, Fixture.CreateTestEvent()),
			new AppendRecord(stream2, Fixture.CreateTestEvent()),
			new AppendRecord(stream1, Fixture.CreateTestEvent()),
			new AppendRecord(stream2, Fixture.CreateTestEvent()),
			new AppendRecord(stream1, Fixture.CreateTestEvent()),
		};

		// Act
		var result = await Fixture.Streams.AppendRecordsAsync(records);

		// Assert
		result.Position.ShouldBePositive();
		result.Responses!.Count().ShouldBe(2);

		var revStream1 = result.Responses!.First(r => r.Stream == stream1);
		var revStream2 = result.Responses!.First(r => r.Stream == stream2);

		revStream1.StreamRevision.ShouldBe(2);
		revStream2.StreamRevision.ShouldBe(1);

		var stream1Events = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream1, StreamPosition.Start, 10)
			.ToArrayAsync();

		var stream2Events = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream2, StreamPosition.Start, 10)
			.ToArrayAsync();

		stream1Events.Length.ShouldBe(3);
		stream2Events.Length.ShouldBe(2);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task fails_with_invalid_metadata_format() {
		// Arrange
		var stream = Fixture.GetStreamName();

		var invalidMetadata = "invalid"u8.ToArray();
		var records = Fixture.CreateTestEvents(1, metadata: invalidMetadata).Select(e => new AppendRecord(stream, e));

		// Act & Assert
		var exception = await Fixture.Streams
			.AppendRecordsAsync(records).AsTask()
			.ShouldThrowAsync<ArgumentException>();

		exception.Message.ShouldContain("Failed to decode event metadata");
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task extension_with_stream_and_events() {
		// Arrange
		var stream = Fixture.GetStreamName();
		var events = Fixture.CreateTestEvents(3);

		// Act
		var result = await Fixture.Streams.AppendRecordsAsync(stream, events);

		// Assert
		result.Position.ShouldBePositive();
		result.Responses.ShouldNotBeEmpty();
		result.Responses!.First().Stream.ShouldBe(stream);

		var readEvents = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 10)
			.ToArrayAsync();

		readEvents.Length.ShouldBe(3);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task extension_with_expected_state() {
		// Arrange
		var stream = Fixture.GetStreamName();

		// First write
		await Fixture.Streams.AppendRecordsAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents(2));

		// Second write with expected state
		var result = await Fixture.Streams.AppendRecordsAsync(stream, StreamState.StreamRevision(1), Fixture.CreateTestEvents(1));

		// Assert
		result.Position.ShouldBePositive();

		var readEvents = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start, 10)
			.ToArrayAsync();

		readEvents.Length.ShouldBe(3);
	}
}
