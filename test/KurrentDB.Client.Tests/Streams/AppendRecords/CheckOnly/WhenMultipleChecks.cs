using static KurrentDB.Client.ConsistencyCheck;

namespace KurrentDB.Client.Tests.Streams.AppendRecords.CheckOnly;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:AppendRecords")]
public class WhenMultipleChecks(ITestOutputHelper output, KurrentDBPermanentFixture fixture) : KurrentDBPermanentTests<KurrentDBPermanentFixture>(output, fixture) {
	[MinimumVersion.Fact(26, 1)]
	public async Task succeeds_when_all_checks_pass() {
		var writeStream = Fixture.GetStreamName();
		var checkStreamA = Fixture.GetStreamName();
		var checkStreamB = Fixture.GetStreamName();

		await Fixture.Streams.AppendRecordsAsync(checkStreamA, Fixture.CreateTestEvents(3));
		await Fixture.Streams.AppendRecordsAsync(checkStreamB, Fixture.CreateTestEvents(5));

		var result = await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(writeStream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(checkStreamA, StreamState.StreamRevision(2)),
				new StreamStateCheck(checkStreamB, StreamState.StreamRevision(4))
			]
		);

		result.Responses!.Count().ShouldBe(1);
		result.Responses!.First().Stream.ShouldBe(writeStream);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task succeeds_when_all_mixed_check_types_pass() {
		var writeStream = Fixture.GetStreamName();
		var revisionStream = Fixture.GetStreamName();
		var existsStream = Fixture.GetStreamName();
		var noStream = Fixture.GetStreamName();

		await Fixture.Streams.AppendRecordsAsync(revisionStream, Fixture.CreateTestEvents(3));
		await Fixture.Streams.AppendRecordsAsync(existsStream, Fixture.CreateTestEvents(1));

		var result = await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(writeStream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(revisionStream, StreamState.StreamRevision(2)),
				new StreamStateCheck(existsStream, StreamState.StreamExists),
				new StreamStateCheck(noStream, StreamState.NoStream)
			]
		);

		result.Responses!.Count().ShouldBe(1);
		result.Responses!.First().Stream.ShouldBe(writeStream);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task fails_when_one_of_multiple_checks_fails() {
		var writeStream = Fixture.GetStreamName();
		var checkStreamA = Fixture.GetStreamName();
		var checkStreamB = Fixture.GetStreamName();

		await Fixture.Streams.AppendRecordsAsync(checkStreamA, Fixture.CreateTestEvents(3));
		// checkStreamB not seeded — will fail the revision check

		var act = async () => await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(writeStream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(checkStreamA, StreamState.StreamRevision(2)),
				new StreamStateCheck(checkStreamB, StreamState.StreamRevision(5))
			]
		);

		var rex = await act.ShouldThrowAsync<AppendConsistencyViolationException>();
		rex.Violations.Count.ShouldBe(1);
		rex.Violations[0].CheckIndex.ShouldBe(1);
		rex.Violations[0].Stream.ShouldBe(checkStreamB);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task fails_when_all_checks_fail() {
		var writeStream = Fixture.GetStreamName();
		var checkStreamA = Fixture.GetStreamName();
		var checkStreamB = Fixture.GetStreamName();

		// Neither stream is seeded — both will fail

		var act = async () => await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(writeStream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(checkStreamA, StreamState.StreamRevision(3)),
				new StreamStateCheck(checkStreamB, StreamState.StreamExists)
			]
		);

		var rex = await act.ShouldThrowAsync<AppendConsistencyViolationException>();
		rex.Violations.Count.ShouldBe(2);

		var violationA = rex.Violations.First(v => v.Stream == checkStreamA);
		var violationB = rex.Violations.First(v => v.Stream == checkStreamB);

		violationA.CheckIndex.ShouldBe(0);
		violationB.CheckIndex.ShouldBe(1);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task fails_when_first_check_fails_and_second_passes() {
		var writeStream = Fixture.GetStreamName();
		var checkStreamA = Fixture.GetStreamName();
		var checkStreamB = Fixture.GetStreamName();

		// checkStreamA not seeded — will fail the revision check
		await Fixture.Streams.AppendRecordsAsync(checkStreamB, Fixture.CreateTestEvents(6));

		var act = async () => await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(writeStream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(checkStreamA, StreamState.StreamRevision(3)),
				new StreamStateCheck(checkStreamB, StreamState.StreamRevision(5))
			]
		);

		var rex = await act.ShouldThrowAsync<AppendConsistencyViolationException>();
		rex.Violations.Count.ShouldBe(1);
		rex.Violations[0].CheckIndex.ShouldBe(0);
		rex.Violations[0].Stream.ShouldBe(checkStreamA);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task fails_when_two_of_three_checks_fail() {
		var writeStream = Fixture.GetStreamName();
		var checkStreamA = Fixture.GetStreamName();
		var checkStreamB = Fixture.GetStreamName();
		var checkStreamC = Fixture.GetStreamName();

		await Fixture.Streams.AppendRecordsAsync(checkStreamA, Fixture.CreateTestEvents(3));
		// checkStreamB not seeded — will fail
		await Fixture.Streams.AppendRecordsAsync(checkStreamC, Fixture.CreateTestEvents(2));

		var act = async () => await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(writeStream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(checkStreamA, StreamState.StreamRevision(2)),
				new StreamStateCheck(checkStreamB, StreamState.StreamExists),
				new StreamStateCheck(checkStreamC, StreamState.StreamRevision(10))
			]
		);

		var rex = await act.ShouldThrowAsync<AppendConsistencyViolationException>();
		rex.Violations.Count.ShouldBe(2);

		var violationB = rex.Violations.First(v => v.Stream == checkStreamB);
		var violationC = rex.Violations.First(v => v.Stream == checkStreamC);

		violationB.CheckIndex.ShouldBe(1);
		violationC.CheckIndex.ShouldBe(2);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task fails_with_mixed_violation_states() {
		var writeStream = Fixture.GetStreamName();
		var deletedStream = Fixture.GetStreamName();
		var tombstonedStream = Fixture.GetStreamName();
		var missingStream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(deletedStream, StreamState.NoStream, Fixture.CreateTestEvents(3));
		await Fixture.Streams.DeleteAsync(deletedStream, StreamState.StreamExists);

		await Fixture.Streams.AppendToStreamAsync(tombstonedStream, StreamState.NoStream, Fixture.CreateTestEvents());
		await Fixture.Streams.TombstoneAsync(tombstonedStream, StreamState.StreamExists);
		// missingStream not seeded

		var act = async () => await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(writeStream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(deletedStream, StreamState.StreamExists),
				new StreamStateCheck(tombstonedStream, StreamState.StreamExists),
				new StreamStateCheck(missingStream, StreamState.StreamRevision(10))
			]
		);

		var rex = await act.ShouldThrowAsync<AppendConsistencyViolationException>();
		rex.Violations.Count.ShouldBe(3);

		var deletedViolation = rex.Violations.First(v => v.Stream == deletedStream);
		var tombstonedViolation = rex.Violations.First(v => v.Stream == tombstonedStream);
		var missingViolation = rex.Violations.First(v => v.Stream == missingStream);

		deletedViolation.CheckIndex.ShouldBe(0);
		tombstonedViolation.CheckIndex.ShouldBe(1);
		missingViolation.CheckIndex.ShouldBe(2);
	}

	[MinimumVersion.Fact(26, 1)]
	public async Task fails_when_check_on_write_target_and_separate_check_fails() {
		var checkStream = Fixture.GetStreamName();
		var writeStream = Fixture.GetStreamName();

		// checkStream not seeded — will fail
		await Fixture.Streams.AppendRecordsAsync(writeStream, Fixture.CreateTestEvents(4));

		var act = async () => await Fixture.Streams.AppendRecordsAsync(
			records: [
				new AppendRecord(writeStream, Fixture.CreateTestEvent())
			],
			checks: [
				new StreamStateCheck(checkStream, StreamState.StreamRevision(5)),
				new StreamStateCheck(writeStream, StreamState.StreamRevision(3))
			]
		);

		var rex = await act.ShouldThrowAsync<AppendConsistencyViolationException>();
		rex.Violations.Count.ShouldBe(1);
		rex.Violations[0].CheckIndex.ShouldBe(0);
		rex.Violations[0].Stream.ShouldBe(checkStream);
	}
}
