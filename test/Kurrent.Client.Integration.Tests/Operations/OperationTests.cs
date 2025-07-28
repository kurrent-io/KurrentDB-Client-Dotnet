// ReSharper disable InconsistentNaming

namespace Kurrent.Client.Tests.Operations;

public class OperationTests : KurrentClientTestFixture {
	[Test]
	public async Task merge_indexes() =>
		await AutomaticClient.Operations.MergeIndexes()
			.ShouldNotThrowAsync()
			.ShouldNotFailAsync();

	[Test]
	public async Task resign_node() =>
		await AutomaticClient.Operations.ResignNode()
			.ShouldNotThrowAsync()
			.ShouldNotFailAsync();

	[Test]
	public async Task shutdown() =>
		await AutomaticClient.Operations.Shutdown()
			.ShouldNotThrowAsync()
			.ShouldNotFailAsync();

	[Test]
	public async Task set_node_priori() =>
		await AutomaticClient.Operations.SetNodePriority(1)
			.ShouldNotThrowAsync()
			.ShouldNotFailAsync();

	[Test]
	public async Task restart_persistent_subscriptions() =>
		await AutomaticClient.Operations.RestartPersistentSubscriptions()
			.ShouldNotThrowAsync()
			.ShouldNotFailAsync();

	[Test]
	public async Task start_scavenge() =>
		await AutomaticClient.Operations.StartScavenge()
			.ShouldNotThrowAsync()
			.ShouldNotFailAsync();

	[Test]
	public async Task stop_scavenge() =>
		await AutomaticClient.Operations.StopScavenge("wwd")
			.ShouldNotThrowAsync()
			.ShouldFailAsync(error => {
					error.AsScavengeNotFound.ErrorMessage.ShouldContain("The specified scavenge was not found.");
				}
			);
}
