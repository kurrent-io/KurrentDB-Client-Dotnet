using Bogus;
using JetBrains.Annotations;
using KurrentDB.Client.Testing.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Serilog;
using TUnit.Core.Exceptions;
using TUnit.Core.Interfaces;

namespace KurrentDB.Client.Testing.Fixtures;
/// <summary>
/// Base class designed for creating robust and reusable test fixtures.<para />
/// This class extends testing capabilities by providing functionalities
/// such as dependency injection, logging, and time manipulation.<para />
/// It also supports asynchronous setup and clean-up processes tailored
/// for testing scenarios.
/// </summary>
[PublicAPI]
public class TestFixture : ITestStartEventReceiver, ITestEndEventReceiver {
    /// <summary>
    /// The Fixture name.
    /// </summary>
    protected string FixtureName { get; private set; } = null!;

    /// <summary>
    /// Pre-configured Faker instance for generating test data.
    /// </summary>
    protected Faker Faker => TestingToolkitAutoWireUp.Faker;

    /// <summary>
    /// The logger factory instance associated with the test fixture, enabling creation of other loggers for logging purposes.
    /// </summary>
    protected ILoggerFactory LoggerFactory => TestContext.Current.LoggerFactory();

    /// <summary>
    /// The time provider used for simulating and controlling time in tests.
    /// </summary>
    protected FakeTimeProvider TimeProvider { get; private set; } = null!;

    /// <summary>
    /// The service provider instance configured for the test fixture.<para />
    /// This property provides access to the dependency injection container for managing
    /// services and resolving dependencies during the test lifecycle.
    /// </summary>
    protected IServiceProvider ServiceProvider { get; private set; } = null!;

    public async ValueTask OnTestStart(BeforeTestContext beforeTestContext) {
        await TestingToolkitAutoWireUp
	        .TestSetUp(beforeTestContext.TestContext)
	        .ConfigureAwait(false);

        FixtureName   = beforeTestContext.TestContext.TestDetails.TestClass.Name;
        TimeProvider  = new FakeTimeProvider(DateTimeOffset.UtcNow);

        var services = new ServiceCollection()
	        .AddSingleton(LoggerFactory)
	        .AddSingleton(Faker)
	        .AddSingleton<TimeProvider>(TimeProvider);

        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

        try {
            await OnSetUp(services).ConfigureAwait(false);
        }
        catch (Exception ex) {
            Log.Error(ex, "An error occurred during the manual setup of {FixtureName}", FixtureName);
            throw new TUnitException($"An error occurred during the manual setup of {FixtureName}:{Environment.NewLine}{ex}", ex);
        }

        ServiceProvider = services.BuildServiceProvider();
    }

    public async ValueTask OnTestEnd(AfterTestContext afterTestContext)  {
        try {
            await OnCleanUp().ConfigureAwait(false);
        }
        catch (Exception ex) {
            Log.Error(ex, "An error occurred during the manual cleanup of {FixtureName}", FixtureName);
        }

        try {
            if (ServiceProvider is IAsyncDisposable disposable)
                await disposable.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            throw new TUnitException($"Impossibru!!! An error occurred during the manual cleanup of {FixtureName}", ex);
        }

        await TestingToolkitAutoWireUp
	        .TestCleanUp(afterTestContext)
	        .ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the setup operations for the test fixture. <para />This method allows
    /// the addition of services to the service collection used within the test fixture.<para />
    /// Override this method to configure specific services for derived test fixtures.
    /// </summary>
    /// <param name="services">An instance of <see cref="IServiceCollection"/> that can be used
    /// to register services and dependencies required by the test fixture.</param>
    public virtual ValueTask OnSetUp(IServiceCollection services) =>
        ValueTask.CompletedTask;

    /// <summary>
    /// Performs the clean-up operations for the test fixture. <para />This method is called to
    /// release resources and perform any finalization tasks after the test execution
    /// is completed. <para />Override this method to implement specific clean-up logic for derived
    /// test fixtures.
    /// </summary>
    public virtual ValueTask OnCleanUp() =>
        ValueTask.CompletedTask;
}
