using Kurrent.Client.Testing.Containers.KurrentDB;

namespace Kurrent.Client.Tests;

public sealed class KurrentDBContainerAutoWireUp {
    public static KurrentDBTestContainer Container { get; private set; } = null!;

    [Before(Assembly)]
    public static async Task AssemblySetUp(AssemblyHookContext context, CancellationToken cancellationToken) {
        Container = new KurrentDBTestContainer();
        await Container.Start().ConfigureAwait(false);
    }

    [After(Assembly)]
    public static async Task AssemblyCleanUp(AssemblyHookContext context, CancellationToken cancellationToken) =>
        await Container.DisposeAsync().ConfigureAwait(false);

    [BeforeEvery(Test)]
    public static void TestSetUp(TestContext context) => Container.ReportStatus();
}
