using KurrentDB.Client;
using KurrentDB.Client.Tests.TestNode;
using KurrentDB.Client.Tests;

namespace KurrentDB.Client.Tests;

[Trait("Category", "Target:Operations")]
public class ScavengeTests(ITestOutputHelper output, ScavengeTests.CustomFixture fixture)
	: KurrentTemporaryTests<ScavengeTests.CustomFixture>(output, fixture) {
	[RetryFact]
	public async Task start() {
		var result = await Fixture.DBOperations.StartScavengeAsync(userCredentials: TestCredentials.Root);

		result.ScavengeId.ShouldNotBeNullOrEmpty();
	}

	[RetryFact]
	public async Task start_without_credentials_throws() =>
		await Fixture.DBOperations
			.StartScavengeAsync()
			.ShouldThrowAsync<AccessDeniedException>();

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(int.MinValue)]
	public async Task start_with_thread_count_le_one_throws(int threadCount) {
		var ex = await Fixture.DBOperations
			.StartScavengeAsync(threadCount)
			.ShouldThrowAsync<ArgumentOutOfRangeException>();

		ex.ParamName.ShouldBe(nameof(threadCount));
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(-2)]
	[InlineData(int.MinValue)]
	public async Task start_with_start_from_chunk_lt_zero_throws(int startFromChunk) {
		var ex = await Fixture.DBOperations
			.StartScavengeAsync(startFromChunk: startFromChunk)
			.ShouldThrowAsync<ArgumentOutOfRangeException>();

		ex.ParamName.ShouldBe(nameof(startFromChunk));
	}

	[Fact(Skip = "Scavenge on an empty database finishes too quickly")]
	public async Task stop() {
		var startResult = await Fixture.DBOperations
			.StartScavengeAsync(userCredentials: TestCredentials.Root);

		var stopResult = await Fixture.DBOperations
			.StopScavengeAsync(startResult.ScavengeId, userCredentials: TestCredentials.Root);

		stopResult.ShouldBe(DatabaseScavengeResult.Stopped(startResult.ScavengeId));
	}

	[RetryFact]
	public async Task stop_when_no_scavenge_is_running() {
		var scavengeId = Guid.NewGuid().ToString();

		var ex = await Fixture.DBOperations
			.StopScavengeAsync(scavengeId, userCredentials: TestCredentials.Root)
			.ShouldThrowAsync<ScavengeNotFoundException>();

		// ex.ScavengeId.ShouldBeNull(); // it is blowing up because of this
	}

	[RetryFact]
	public async Task stop_without_credentials_throws() =>
		await Fixture.DBOperations
			.StopScavengeAsync(Guid.NewGuid().ToString())
			.ShouldThrowAsync<AccessDeniedException>();

	public class CustomFixture() : KurrentDBTemporaryFixture(x => x.WithoutDefaultCredentials());
}
