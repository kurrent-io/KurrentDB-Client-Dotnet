using static KurrentDB.Client.ConsistencyCheck;

namespace KurrentDB.Client.Tests.Streams.AppendRecords.WriteOnly;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:AppendRecords")]
public class WhenExpectingRevision(ITestOutputHelper output, KurrentDBPermanentFixture fixture) : KurrentDBPermanentTests<KurrentDBPermanentFixture>(output, fixture) {
	const ulong ExpectedRevision = 10;

	[MinimumVersion.Fact(26, 1)]
	public async Task succeeds_when_stream_has_revision() {
		var stream = Fixture.GetStreamName();
		await Fixture.Streams.AppendRecordsAsync(stream, Fixture.CreateTestEvents(11));

		var result = await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(stream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(stream, StreamState.StreamRevision(ExpectedRevision))
			]
		);

		result.Responses!.Count().ShouldBe(1);
		result.Responses!.First().Stream.ShouldBe(stream);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task fails_when_stream_not_found() {
		var stream = Fixture.GetStreamName();

		var act = async () => await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(stream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(stream, StreamState.StreamRevision(ExpectedRevision))
			]
		);

		var rex = await act.ShouldThrowAsync<AppendConsistencyViolationException>();
		rex.Violations.Count.ShouldBe(1);
		rex.Violations[0].Stream.ShouldBe(stream);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task fails_when_stream_has_wrong_revision() {
		var stream = Fixture.GetStreamName();
		await Fixture.Streams.AppendRecordsAsync(stream, Fixture.CreateTestEvents(5));

		var act = async () => await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(stream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(stream, StreamState.StreamRevision(ExpectedRevision))
			]
		);

		var rex = await act.ShouldThrowAsync<AppendConsistencyViolationException>();
		rex.Violations.Count.ShouldBe(1);
		rex.Violations[0].Stream.ShouldBe(stream);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task fails_when_stream_is_deleted() {
		var stream = Fixture.GetStreamName();
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents(3));
		await Fixture.Streams.DeleteAsync(stream, StreamState.StreamExists);

		var act = async () => await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(stream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(stream, StreamState.StreamRevision(ExpectedRevision))
			]
		);

		await act.ShouldThrowAsync<AppendConsistencyViolationException>();
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task fails_when_stream_is_tombstoned() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());
		await Fixture.Streams.TombstoneAsync(stream, StreamState.StreamExists);

		var act = async () => await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(stream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(stream, StreamState.StreamRevision(ExpectedRevision))
			]
		);

		await act.ShouldThrowAsync<AppendConsistencyViolationException>();
	}
}
