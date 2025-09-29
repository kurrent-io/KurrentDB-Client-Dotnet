using System.Text.Json;
using Humanizer;

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

		Assert.Contains("Deserialization failed:", exception.Message);
	}

	[MinimumVersion.Fact(25, 1)]
	public async Task append_events_to_multiple_streams() {
		// Arrange
		var stream1 = Fixture.GetStreamName();
		var stream2 = Fixture.GetStreamName();

		var expectedMetadata = new TestMetadata {
			StringValue       = "Foo",
			IntegerValue      = 0,
			BooleanValue      = true,
			DoubleValue       = 2.718281828,
			DateTimeValue     = DateTime.UtcNow,
			TimeSpanValue     = 2.5.Hours(),
			NullTimeSpanValue = null,
			ZeroTimeSpanValue = TimeSpan.Zero,
			ByteArrayValue    = "Bar"u8.ToArray()
		};

		var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(expectedMetadata);

		AppendStreamRequest[] requests = [
			new(stream1, StreamState.NoStream, Fixture.CreateTestEvents(3, metadata: metadataBytes).ToArray()),
			new(stream2, StreamState.NoStream, Fixture.CreateTestEvents(2, metadata: metadataBytes).ToArray())
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

		var metadata = MetadataDecoder.Decode(stream1Events.First().OriginalEvent.Metadata);

		stream1Events.Length.ShouldBe(3);
		stream2Events.Length.ShouldBe(2);

		metadata.ShouldNotBeNull();
		metadata[Constants.Metadata.SchemaName].ShouldBe("test-event-type");
		metadata[Constants.Metadata.SchemaFormat].ShouldBe(SchemaDataFormat.Json);
		metadata["StringValue"].ShouldBe(expectedMetadata.StringValue);
		metadata["BooleanValue"].ShouldBe(expectedMetadata.BooleanValue);

		metadata["IntegerValue"].ShouldBe(expectedMetadata.IntegerValue);
		metadata["DoubleValue"].ShouldBe(expectedMetadata.DoubleValue);

		metadata["DateTimeValue"].ShouldBe(expectedMetadata.DateTimeValue);
		metadata["TimeSpanValue"].ShouldBe(expectedMetadata.TimeSpanValue);
		metadata["NullTimeSpanValue"].ShouldBeNull();
		metadata["ZeroTimeSpanValue"].ShouldBe(expectedMetadata.ZeroTimeSpanValue);
		metadata["ByteArrayValue"].ShouldBe(expectedMetadata.ByteArrayValue);

		metadata["BooleanValue"]?.GetType().ShouldBe(typeof(bool));
		metadata["StringValue"]?.GetType().ShouldBe(typeof(string));
		metadata["IntegerValue"]?.GetType().ShouldBe(typeof(double));
		metadata["DoubleValue"]?.GetType().ShouldBe(typeof(double));
		metadata["DateTimeValue"]?.GetType().ShouldBe(typeof(DateTime));
		metadata["TimeSpanValue"]?.GetType().ShouldBe(typeof(TimeSpan));
		metadata["NullTimeSpanValue"]?.GetType().ShouldBe(typeof(TimeSpan));
		metadata["ZeroTimeSpanValue"]?.GetType().ShouldBe(typeof(TimeSpan));
		metadata["ByteArrayValue"]?.GetType().ShouldBe(typeof(ReadOnlyMemory<byte>));
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
		var rex = await appendTask.ShouldThrowAsync<StreamDeletedException>();
		rex.Stream.ShouldBe(stream);
	}
}

public class TestMetadata {
	public string?   StringValue       { get; init; }
	public int?      IntegerValue      { get; init; }
	public bool?     BooleanValue      { get; init; }
	public double?   DoubleValue       { get; init; }
	public DateTime? DateTimeValue     { get; init; }
	public TimeSpan? TimeSpanValue     { get; init; }
	public TimeSpan? NullTimeSpanValue { get; init; }
	public TimeSpan? ZeroTimeSpanValue { get; init; }
	public byte[]?   ByteArrayValue    { get; init; }
}
