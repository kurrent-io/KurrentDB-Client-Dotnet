// ReSharper disable InconsistentNaming

namespace Kurrent.Client.Tests.Admin;

[Skip("Ignore this because it interferes with other tests")]
public class AdminTests : KurrentClientTestFixture {
	[Test]
	public async Task merge_indexes() =>
		await AutomaticClient.Admin.MergeIndexes()
			.ShouldNotThrowOrFailAsync();

	[Test]
	public async Task resign_node() =>
		await AutomaticClient.Admin.ResignNode()
			.ShouldNotThrowOrFailAsync();

	[Test]
	public async Task shutdown() =>
		await AutomaticClient.Admin.Shutdown()
			.ShouldNotThrowOrFailAsync();

	[Test]
	public async Task set_node_priority() =>
		await AutomaticClient.Admin.SetNodePriority(1)
			.ShouldNotThrowOrFailAsync();

	[Test]
	public async Task restart_persistent_subscriptions() =>
		await AutomaticClient.Admin.RestartPersistentSubscriptions()
			.ShouldNotThrowOrFailAsync();

	[Test]
	public async Task start_scavenge() =>
		await AutomaticClient.Admin.StartScavenge()
			.ShouldNotThrowOrFailAsync();

	[Test]
	public async Task stop_scavenge() =>
        await AutomaticClient.Admin.StopScavenge("wwd")
            .ShouldNotThrowAsync()
            .ShouldFailAsync(failure => failure.Value.ShouldBeOfType<ErrorDetails.NotFound>());
}
