// ReSharper disable InconsistentNaming

namespace Kurrent.Client.Tests.Operations;

[Skip("Ignore this because it interferes with other tests")]
public class OperationTests : KurrentClientTestFixture {
	[Test]
	public async Task merge_indexes() =>
		await AutomaticClient.Operations.MergeIndexes()
			.ShouldNotThrowOrFailAsync();

	[Test]
	public async Task resign_node() =>
		await AutomaticClient.Operations.ResignNode()
			.ShouldNotThrowOrFailAsync();

	[Test]
	public async Task shutdown() =>
		await AutomaticClient.Operations.Shutdown()
			.ShouldNotThrowOrFailAsync();

	[Test]
	public async Task set_node_priority() =>
		await AutomaticClient.Operations.SetNodePriority(1)
			.ShouldNotThrowOrFailAsync();

	[Test]
	public async Task restart_persistent_subscriptions() =>
		await AutomaticClient.Operations.RestartPersistentSubscriptions()
			.ShouldNotThrowOrFailAsync();

	[Test]
	public async Task start_scavenge() =>
		await AutomaticClient.Operations.StartScavenge()
			.ShouldNotThrowOrFailAsync();

	[Test]
	public async Task stop_scavenge() =>
		await AutomaticClient.Operations.StopScavenge("wwd")
			.ShouldNotThrowAsync()
			.ShouldFailAsync(error => {
					error.AsScavengeNotFound.ErrorMessage.ShouldContain("The specified scavenge was not found.");
				}
			);
}
