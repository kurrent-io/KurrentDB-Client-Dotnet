// ReSharper disable AccessToDisposedClosure

using System.Diagnostics;
using KurrentDB.Client.Diagnostics;
using KurrentDB.Client.Tests.Fixtures;
using KurrentDB.Diagnostics.Telemetry;
using KurrentDB.Diagnostics.Tracing;

namespace KurrentDB.Client.Tests.Diagnostics;

[Trait("Category", "Target:Diagnostics")]
public class StreamsTracingInstrumentationTests(ITestOutputHelper output, DiagnosticsFixture fixture) : KurrentDBPermanentTests<DiagnosticsFixture>(output, fixture) {
	[Fact]
	public async Task append_to_stream() {
		var traceId = Fixture.CreateTraceId();

		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		var activity = Fixture
			.GetActivities(TracingConstants.Operations.Append, traceId)
			.SingleOrDefault()
			.ShouldNotBeNull();

		Fixture.AssertAppendActivityHasExpectedTags(activity, stream);
	}

	[MinimumVersion.Fact(25, 1)]
	public async Task multi_stream_append() {
		// Arrange
		var traceId = Fixture.CreateTraceId();

		var seedEvents = Fixture.CreateTestEvents(10).ToList();

		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

		var stream1 = Fixture.GetStreamName();
		var stream2 = Fixture.GetStreamName();

		AppendStreamRequest[] requests = [new(stream1, StreamState.NoStream, seedEvents.Take(5)), new(stream2, StreamState.NoStream, seedEvents.Skip(5))];

		// Act
		var appendResult = await Fixture.Streams.MultiStreamAppendAsync(requests.ToAsyncEnumerable());

		await using var subscription = Fixture.Streams.SubscribeToAll(
			FromAll.Start,
			filterOptions: new SubscriptionFilterOptions(StreamFilter.Prefix(stream1, stream2))
		);

		await using var enumerator = subscription.Messages.GetAsyncEnumerator();

		await Subscribe().WithTimeout();

		// Assert
		appendResult.Position.ShouldBePositive();

		var appendActivities    = Fixture.GetActivities(TracingConstants.Operations.MultiAppend, traceId);
		var subscribeActivities = Fixture.GetActivities(TracingConstants.Operations.Subscribe, traceId);

		appendActivities.ShouldNotBeEmpty();
		subscribeActivities.ShouldNotBeEmpty();

		appendActivities.Count.ShouldBe(1);
		subscribeActivities.Count.ShouldBe(10);

		// They also have the same duration
		appendActivities.Select(x => x.Duration).Distinct().Count().ShouldBe(1);

		// Check that subscribe activities have the correct parent IDs inherited from append activities
		subscribeActivities
			.FirstOrDefault(x => x.ParentId == appendActivities.First().Id)?.ParentSpanId
			.ShouldBe(appendActivities.First().SpanId);

		subscribeActivities
			.FirstOrDefault(x => x.ParentId == appendActivities.Last().Id)?.ParentSpanId
			.ShouldBe(appendActivities.Last().SpanId);

		subscribeActivities
			.All(x => x.StartTimeUtc > appendActivities.First().StartTimeUtc)
			.ShouldBeTrue();

		Fixture.AssertMultiAppendActivityHasExpectedTags(appendActivities.First());
		Fixture.AssertSubscriptionActivityHasExpectedTags(subscribeActivities.First(), stream1, seedEvents.First().EventId.ToString());

		return;

		async Task Subscribe() {
			while (await enumerator.MoveNextAsync()) {
				if (enumerator.Current is not StreamMessage.Event(var resolvedEvent))
					continue;

				availableEvents.Remove(resolvedEvent.Event.EventId);

				if (availableEvents.Count is 0)
					return;
			}
		}
	}

	[MinimumVersion.Fact(25, 1)]
	public async Task multi_stream_append_with_exceptions() {
		var traceId = Fixture.CreateTraceId();

		// Arrange
		var stream1 = Fixture.GetStreamName();
		var stream2 = Fixture.GetStreamName();

		AppendStreamRequest[] requests = [
			new(stream1, StreamState.StreamExists, Fixture.CreateTestEvents()),
			new(stream2, StreamState.StreamExists, Fixture.CreateTestEvents())
		];

		// Act
		var appendTask = async () => await Fixture.Streams.MultiStreamAppendAsync(requests);
		var rex = await appendTask.ShouldThrowAsync<WrongExpectedVersionException>();

		// Assert
		var appendActivities = Fixture.GetActivities(TracingConstants.Operations.MultiAppend, traceId);

		appendActivities.ShouldNotBeEmpty();

		appendActivities.Count.ShouldBe(1);

		var activity = appendActivities.FirstOrDefault().ShouldNotBeNull();
		activity.Status.ShouldBe(ActivityStatusCode.Error);
		activity.Events.ShouldHaveSingleItem();

		var activityEvent = activity.Events.First();

		activityEvent.Name.ShouldBe(TelemetryTags.Exception.EventName);
		activityEvent.Tags.Any(tag => tag.Key == TelemetryTags.Exception.Message).ShouldBeTrue();
		activityEvent.Tags.Any(tag => tag.Key == TelemetryTags.Exception.Stacktrace).ShouldBeTrue();
		activityEvent.Tags.Any(tag => tag.Key == TelemetryTags.Exception.Type && (string?)tag.Value == rex.GetType().FullName).ShouldBeTrue();
	}

	[Fact]
	public async Task append_trace_tagged_with_error_on_exception() {
		var traceId = Fixture.CreateTraceId();
		var stream = Fixture.GetStreamName();

		var actualException = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEventsThatThrowsException()
		).ShouldThrowAsync<Exception>();

		var activity = Fixture
			.GetActivities(TracingConstants.Operations.Append, traceId)
			.SingleOrDefault()
			.ShouldNotBeNull();

		Fixture.AssertErroneousAppendActivityHasExpectedTags(activity, actualException);
	}

	[Fact]
	public async Task tracing_context_injected_when_metadata_is_json() {
		var traceId = Fixture.CreateTraceId();
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(1, metadata: Fixture.CreateTestJsonMetadata())
		);

		var activity = Fixture
			.GetActivities(TracingConstants.Operations.Append, traceId)
			.SingleOrDefault()
			.ShouldNotBeNull();

		var readResult = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
			.ToListAsync();

		var tracingMetadata = readResult[0].OriginalEvent.Metadata.ExtractTracingMetadata();

		tracingMetadata.ShouldNotBe(TracingMetadata.None);
		tracingMetadata.TraceId.ShouldBe(activity.TraceId.ToString());
		tracingMetadata.SpanId.ShouldBe(activity.SpanId.ToString());
	}

	[Fact]
	public async Task tracing_context_not_injected_when_metadata_not_json() {
		var stream = Fixture.GetStreamName();

		var inputMetadata = "clearlynotavalidjsonobject"u8.ToArray();
		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(1, metadata: inputMetadata)
		);

		var readResult = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
			.ToListAsync();

		var outputMetadata = readResult[0].OriginalEvent.Metadata.ToArray();
		outputMetadata.ShouldBe(inputMetadata);
	}

	[Fact]
	public async Task tracing_context_injected_when_event_not_json_but_metadata_json() {
		var traceId = Fixture.CreateTraceId();
		var stream = Fixture.GetStreamName();

		var inputMetadata = Fixture.CreateTestJsonMetadata().ToArray();
		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents(
				metadata: inputMetadata,
				contentType: Constants.Metadata.ContentTypes.ApplicationOctetStream
			)
		);

		var readResult = await Fixture.Streams
			.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
			.ToListAsync();

		var outputMetadata = readResult[0].OriginalEvent.Metadata.ToArray();
		outputMetadata.ShouldNotBe(inputMetadata);

		var appendActivities = Fixture.GetActivities(TracingConstants.Operations.Append, traceId);

		appendActivities.ShouldNotBeEmpty();
	}

	[Fact]
	public async Task json_metadata_traced_non_json_metadata_not_traced() {
		var traceId = Fixture.CreateTraceId();
		var streamName = Fixture.GetStreamName();

		var seedEvents = new[] {
			Fixture.CreateTestEvent(metadata: Fixture.CreateTestJsonMetadata()),
			Fixture.CreateTestEvent(metadata: Fixture.CreateTestNonJsonMetadata())
		};

		var availableEvents = new HashSet<Uuid>(seedEvents.Select(x => x.EventId));

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents);

		await using var subscription = Fixture.Streams.SubscribeToStream(streamName, FromStream.Start);
		await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

		var appendActivities = Fixture
			.GetActivities(TracingConstants.Operations.Append, traceId)
			.ShouldNotBeNull();

		Assert.True(await enumerator.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator.Current);

		await Subscribe(enumerator).WithTimeout();

		var subscribeActivities = Fixture
			.GetActivities(TracingConstants.Operations.Subscribe, traceId)
			.ToArray();

		appendActivities.ShouldHaveSingleItem();

		subscribeActivities.ShouldHaveSingleItem();

		subscribeActivities.First().ParentId.ShouldBe(appendActivities.First().Id);

		var jsonMetadataEvent = seedEvents.First();

		Fixture.AssertSubscriptionActivityHasExpectedTags(
			subscribeActivities.First(),
			streamName,
			jsonMetadataEvent.EventId.ToString()
		);

		return;

		async Task Subscribe(IAsyncEnumerator<StreamMessage> internalEnumerator) {
			while (await internalEnumerator.MoveNextAsync()) {
				if (internalEnumerator.Current is not StreamMessage.Event(var resolvedEvent))
					continue;

				availableEvents.Remove(resolvedEvent.Event.EventId);

				if (availableEvents.Count == 0)
					return;
			}
		}
	}

	[RetryFact]
	[Trait("Category", "Special cases")]
	public async Task no_trace_when_event_is_null() {
		var traceId = Fixture.CreateTraceId();
		var category   = Guid.NewGuid().ToString("N");
		var streamName = category + "-123";

		var seedEvents = Fixture.CreateTestEvents(type: $"{category}-{Fixture.GetStreamName()}").ToArray();
		await Fixture.Streams.AppendToStreamAsync(streamName, StreamState.NoStream, seedEvents);

		await Fixture.Streams.DeleteAsync(streamName, StreamState.StreamExists);

		await using var subscription = Fixture.Streams.SubscribeToStream("$ce-" + category, FromStream.Start, resolveLinkTos: true);

		await using var enumerator = subscription.Messages.GetAsyncEnumerator();

		Assert.True(await enumerator.MoveNextAsync());

		Assert.IsType<StreamMessage.SubscriptionConfirmation>(enumerator.Current);

		await Subscribe().WithTimeout();

		var appendActivities = Fixture
			.GetActivities(TracingConstants.Operations.Append, traceId)
			.ShouldNotBeNull();

		var subscribeActivities = Fixture
			.GetActivities(TracingConstants.Operations.Subscribe, traceId)
			.ToArray();

		appendActivities.ShouldHaveSingleItem();
		subscribeActivities.ShouldBeEmpty();

		return;

		async Task Subscribe() {
			while (await enumerator.MoveNextAsync()) {
				if (enumerator.Current is not StreamMessage.Event(var resolvedEvent))
					continue;

				if (resolvedEvent.Event?.EventType is "$metadata")
					return;
			}
		}
	}
}
