using System.Text.Json;
using Humanizer;
using static KurrentDB.Client.Constants;

namespace KurrentDB.Client.Tests.Streams;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:MultiStreamAppend")]
public class MultiStreamAppendTests(ITestOutputHelper output, KurrentDBPermanentFixture fixture)
	: KurrentDBPermanentTests<KurrentDBPermanentFixture>(output, fixture) {
	[MinimumVersion.Fact(25, 1)]
	public async Task append_events_with_invalid_metadata_format_throws_exceptions() {
		// Arrange
		var stream = Fixture.GetStreamName();

		var invalidMetadata = "invalid"u8.ToArray();

		AppendStreamRequest[] requests = [new(stream, StreamState.NoStream, Fixture.CreateTestEvents(3, metadata: invalidMetadata)),];

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
			StringValue    = "Foo",
			BooleanValue   = true,
			DoubleValue    = 2.718281828,
			DateTimeValue  = DateTime.UtcNow,
			TimeSpanValue  = 2.5.Hours(),
			ByteArrayValue = "Bar"u8.ToArray()
		};

		var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(expectedMetadata);

		AppendStreamRequest[] requests = [
			new(stream1, StreamState.NoStream, Fixture.CreateTestEvents(3, metadata: metadataBytes).ToArray()),
			new(stream2, StreamState.NoStream, Fixture.CreateTestEvents(2, metadata: metadataBytes).ToArray())
		];

		// Act
		var result = await Fixture.Streams.MultiStreamAppendAsync(requests);

		// Assert
		result.IsSuccess.ShouldBeTrue();
		var success = result.ShouldBeOfType<MultiAppendSuccess>();
		success.Successes.Count.ShouldBe(2);

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
		metadata[Metadata.SchemaName].ShouldBe("test-event-type");
		metadata[Metadata.SchemaDataFormat].ShouldBe(SchemaDataFormat.Json);
		metadata["StringValue"].ShouldBe(expectedMetadata.StringValue);
		metadata["BooleanValue"].ShouldBe(expectedMetadata.BooleanValue);
		metadata["DoubleValue"].ShouldBe(expectedMetadata.DoubleValue);
		metadata["DateTimeValue"].ShouldBe(expectedMetadata.DateTimeValue);
		metadata["TimeSpanValue"].ShouldBe(expectedMetadata.TimeSpanValue);
		metadata["ByteArrayValue"].ShouldBe(expectedMetadata.ByteArrayValue);
	}

	[MinimumVersion.Fact(25, 1)]
	public async Task appending_events_with_failures() {
		// Arrange
		var stream1 = Fixture.GetStreamName();
		var stream2 = Fixture.GetStreamName();
		var stream3 = Fixture.GetStreamName();

		AppendStreamRequest[] requests = [
			new(stream1, StreamState.StreamExists, Fixture.CreateTestEvents(3).ToArray()), // does not exist
			new(stream2, StreamState.NoStream, Fixture.CreateTestEvents(2).ToArray()),
			new(stream3, StreamState.StreamExists, Fixture.CreateTestEvents(3).ToArray()), // does not exist
		];

		// Act
		var result = await Fixture.Streams.MultiStreamAppendAsync(requests);

		// Assert
		result.IsFailure.ShouldBeTrue();
		var failure = result.ShouldBeOfType<MultiAppendFailure>();
		failure.Failures.Count.ShouldBe(2);

		failure.Failures
			.Select(f => f.ShouldBeOfType<WrongExpectedVersionException>())
			.Count().ShouldBe(2);
	}
}

public class TestMetadata {
	public string?   StringValue    { get; init; }
	public bool?     BooleanValue   { get; init; }
	public double?   DoubleValue    { get; init; }
	public DateTime? DateTimeValue  { get; init; }
	public TimeSpan? TimeSpanValue  { get; init; }
	public byte[]?   ByteArrayValue { get; init; }
}
