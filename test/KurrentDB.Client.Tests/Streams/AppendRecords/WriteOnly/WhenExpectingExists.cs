using static KurrentDB.Client.ConsistencyCheck;

namespace KurrentDB.Client.Tests.Streams.AppendRecords.WriteOnly;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:AppendRecords")]
public class WhenExpectingExists(ITestOutputHelper output, KurrentDBPermanentFixture fixture) : KurrentDBPermanentTests<KurrentDBPermanentFixture>(output, fixture) {
	[MinimumVersion.Fact(26, 1)]
	public async Task succeeds_when_stream_has_revision() {
		var stream = Fixture.GetStreamName();
		await Fixture.Streams.AppendRecordsAsync(stream, Fixture.CreateTestEvents(3));

		var result = await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(stream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(stream, StreamState.StreamExists)
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
				new StreamStateCheck(stream, StreamState.StreamExists)
			]
		);

		var rex = await act.ShouldThrowAsync<AppendConsistencyViolationException>();
		rex.Violations.Count.ShouldBe(1);
		rex.Violations[0].Stream.ShouldBe(stream);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task fails_when_stream_is_deleted() {
		var stream = Fixture.GetStreamName();
		await Fixture.Streams.AppendToStreamAsync(stream, StreamState.NoStream, Fixture.CreateTestEvents());
		await Fixture.Streams.DeleteAsync(stream, StreamState.StreamExists);

		var act = async () => await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(stream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(stream, StreamState.StreamExists)
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
				new StreamStateCheck(stream, StreamState.StreamExists)
			]
		);

		await act.ShouldThrowAsync<AppendConsistencyViolationException>();
	}
}
