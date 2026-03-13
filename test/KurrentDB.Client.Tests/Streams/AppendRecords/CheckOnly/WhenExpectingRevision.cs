using static KurrentDB.Client.ConsistencyCheck;

namespace KurrentDB.Client.Tests.Streams.AppendRecords.CheckOnly;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:AppendRecords")]
public class WhenExpectingRevision(ITestOutputHelper output, KurrentDBPermanentFixture fixture) : KurrentDBPermanentTests<KurrentDBPermanentFixture>(output, fixture) {
	const ulong ExpectedRevision = 10;

	[MinimumVersion.Fact(26, 1)]
	public async Task succeeds_when_stream_has_revision() {
		var checkStream = Fixture.GetStreamName();
		var writeStream = Fixture.GetStreamName();
		await Fixture.Streams.AppendRecordsAsync(checkStream, Fixture.CreateTestEvents(11));

		var result = await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(writeStream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(checkStream, StreamState.StreamRevision(ExpectedRevision))
			]
		);

		result.Responses!.Count().ShouldBe(1);
		result.Responses!.First().Stream.ShouldBe(writeStream);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task fails_when_stream_not_found() {
		var checkStream = Fixture.GetStreamName();
		var writeStream = Fixture.GetStreamName();

		var act = async () => await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(writeStream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(checkStream, StreamState.StreamRevision(ExpectedRevision))
			]
		);

		var rex = await act.ShouldThrowAsync<AppendConsistencyViolationException>();
		rex.Violations.Count.ShouldBe(1);
		rex.Violations[0].Stream.ShouldBe(checkStream);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task fails_when_stream_has_wrong_revision() {
		var checkStream = Fixture.GetStreamName();
		var writeStream = Fixture.GetStreamName();
		await Fixture.Streams.AppendRecordsAsync(checkStream, Fixture.CreateTestEvents(5));

		var act = async () => await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(writeStream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(checkStream, StreamState.StreamRevision(ExpectedRevision))
			]
		);

		var rex = await act.ShouldThrowAsync<AppendConsistencyViolationException>();
		rex.Violations.Count.ShouldBe(1);
		rex.Violations[0].Stream.ShouldBe(checkStream);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task fails_when_stream_is_deleted() {
		var checkStream = Fixture.GetStreamName();
		var writeStream = Fixture.GetStreamName();
		await Fixture.Streams.AppendToStreamAsync(checkStream, StreamState.NoStream, Fixture.CreateTestEvents(3));
		await Fixture.Streams.DeleteAsync(checkStream, StreamState.StreamExists);

		var act = async () => await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(writeStream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(checkStream, StreamState.StreamRevision(ExpectedRevision))
			]
		);

		var rex = await act.ShouldThrowAsync<AppendConsistencyViolationException>();
		rex.Violations.Count.ShouldBe(1);
		rex.Violations[0].Stream.ShouldBe(checkStream);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task fails_when_stream_is_tombstoned() {
		var checkStream = Fixture.GetStreamName();
		var writeStream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(checkStream, StreamState.NoStream, Fixture.CreateTestEvents());
		await Fixture.Streams.TombstoneAsync(checkStream, StreamState.StreamExists);

		var act = async () => await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(writeStream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(checkStream, StreamState.StreamRevision(ExpectedRevision))
			]
		);

		var rex = await act.ShouldThrowAsync<AppendConsistencyViolationException>();
		rex.Violations.Count.ShouldBe(1);
		rex.Violations[0].Stream.ShouldBe(checkStream);
	}
}
