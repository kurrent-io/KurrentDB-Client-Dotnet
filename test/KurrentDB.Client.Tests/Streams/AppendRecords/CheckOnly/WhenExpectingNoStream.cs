using static KurrentDB.Client.ConsistencyCheck;

namespace KurrentDB.Client.Tests.Streams.AppendRecords.CheckOnly;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:AppendRecords")]
public class WhenExpectingNoStream(ITestOutputHelper output, KurrentDBPermanentFixture fixture) : KurrentDBPermanentTests<KurrentDBPermanentFixture>(output, fixture) {
	[MinimumVersion.Fact(26, 1)]
	public async Task succeeds_when_stream_not_found() {
		var checkStream = Fixture.GetStreamName();
		var writeStream = Fixture.GetStreamName();

		var result = await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(writeStream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(checkStream, StreamState.NoStream)
			]
		);

		result.Responses!.Count().ShouldBe(1);
		result.Responses!.First().Stream.ShouldBe(writeStream);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task succeeds_when_stream_is_deleted() {
		var checkStream = Fixture.GetStreamName();
		var writeStream = Fixture.GetStreamName();
		await Fixture.Streams.AppendToStreamAsync(checkStream, StreamState.NoStream, Fixture.CreateTestEvents());
		await Fixture.Streams.DeleteAsync(checkStream, StreamState.StreamExists);

		var result = await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(writeStream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(checkStream, StreamState.NoStream)
			]
		);

		result.Responses!.Count().ShouldBe(1);
		result.Responses!.First().Stream.ShouldBe(writeStream);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task succeeds_when_stream_is_tombstoned() {
		var checkStream = Fixture.GetStreamName();
		var writeStream = Fixture.GetStreamName();
		await Fixture.Streams.AppendToStreamAsync(checkStream, StreamState.NoStream, Fixture.CreateTestEvents());
		await Fixture.Streams.TombstoneAsync(checkStream, StreamState.StreamExists);

		var result = await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(writeStream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(checkStream, StreamState.NoStream)
			]
		);

		result.Responses!.Count().ShouldBe(1);
		result.Responses!.First().Stream.ShouldBe(writeStream);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task fails_when_stream_has_revision() {
		var checkStream = Fixture.GetStreamName();
		var writeStream = Fixture.GetStreamName();
		await Fixture.Streams.AppendRecordsAsync(checkStream, Fixture.CreateTestEvents(3));

		var act = async () => await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(writeStream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(checkStream, StreamState.NoStream)
			]
		);

		var rex = await act.ShouldThrowAsync<AppendConsistencyViolationException>();
		rex.Violations.Count.ShouldBe(1);
		rex.Violations[0].Stream.ShouldBe(checkStream);
	}
}
