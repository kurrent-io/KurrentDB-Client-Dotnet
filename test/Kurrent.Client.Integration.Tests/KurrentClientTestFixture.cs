#pragma warning disable CA1822 // Mark members as static

using JetBrains.Annotations;
using Kurrent.Client.Model;
using Kurrent.Client.Streams;
using Kurrent.Client.Testing;
using Kurrent.Client.Testing.Containers.KurrentDB;
using Kurrent.Client.Testing.Fixtures;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Extensions.Logging;

namespace Kurrent.Client.Tests;


[PublicAPI]
public partial class KurrentClientTestFixture : TestFixture {
    [Before(Assembly)]
    public static async Task AssemblySetUp(AssemblyHookContext context, CancellationToken cancellationToken) {

        // "TESTCONTAINER_KURRENTDB_IMAGE"

        var options = ApplicationContext.Configuration
            .GetOptionsOrDefault<KurrentClientTestFixtureOptions>(KurrentClientTestFixtureOptions.SectionName);

        if (options.AutoWireUpContainers) {
            Log.Information("KurrentDB test container auto wire up enabled, starting container...");

            Container = new KurrentDBTestContainer(KurrentDBConfiguration.Insecure.ToDictionary());
            await Container.Start().ConfigureAwait(false);

            AuthenticatedConnectionString = Container.AuthenticatedConnectionString;
            AnonymousConnectionString     = Container.AnonymousConnectionString;
        }
        else {
            Log.Warning("KurrentDB test container auto wire up disabled, using default connection strings");

            AuthenticatedConnectionString = "kurrentdb://admin:changeit@localhost:2113/?tls=false";
            AnonymousConnectionString     = "kurrentdb://localhost:2113/?tls=false";
        }
    }

    [After(Assembly)]
    public static async Task AssemblyCleanUp(AssemblyHookContext context, CancellationToken cancellationToken) {
        if (Container is null) {
            Log.Warning("KurrentDB test container was not started, skipping clean up");
            return;
        }

        await Container.DisposeAsync().ConfigureAwait(false);
    }

    [BeforeEvery(Test)]
    public static void TestSetUp(TestContext context) =>
        Container?.ReportStatus();

    /// <summary>
    /// The KurrentDB test container instance used for integration tests.
    /// </summary>
    public static KurrentDBTestContainer? Container { get; private set; }

    /// <summary>
    /// Indicates whether the KurrentDB test container is available for use in tests.
    /// </summary>
    public static bool IsContainerAvailable => Container is not null;

    /// <summary>
    /// The connection string for authenticated access to the KurrentDB test container.
    /// </summary>
    public static string AuthenticatedConnectionString { get; private set; } = null!;

    /// <summary>
    /// The connection string for anonymous access to the KurrentDB test container.
    /// </summary>
    public static string AnonymousConnectionString { get; private set; } = null!;

    readonly Lazy<KurrentClient> _lazyLightweightClient = new(() => CreateClient());
    readonly Lazy<KurrentClient> _lazyAutomaticClient   = new(() => CreateClient(options => options.WithSchema(KurrentClientSchemaOptions.NoValidation)));
    readonly Lazy<KurrentClient> _lazyCorpoClient       = new(() => CreateClient(options => options.WithSchema(KurrentClientSchemaOptions.FullValidation)));

    /// <summary>
    /// Lightweight client with automatic serde and without any schema integration configured
    /// </summary>
    protected KurrentClient LightweightClient => _lazyAutomaticClient.Value;

    /// <summary>
    /// Lightweight client with automatic schema registration enabled but without validation on consumption.
    /// </summary>
	protected KurrentClient AutomaticClient => _lazyAutomaticClient.Value;

    /// <summary>
    /// Client with full schema integration enabled, which includes schema auto registration and validation.
    /// </summary>
    protected KurrentClient CorpoClient => _lazyCorpoClient.Value;

    /// <summary>
    /// Creates a new instance of <see cref="KurrentClient"/> with the specified configuration options.
    /// </summary>
    /// <param name="configure">
    /// An optional action to configure the <see cref="KurrentClientOptionsBuilder"/>. If not provided, default options will be used.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="KurrentClient"/> configured with the specified options.
    /// </returns>
    public static KurrentClient CreateClient(Action<KurrentClientOptionsBuilder>? configure = null) {
        var builder = KurrentClientOptions.Build
            .WithConnectionString(AuthenticatedConnectionString)
            .WithLoggerFactory(new SerilogLoggerFactory(Log.Logger))
            // .WithSchema(KurrentClientSchemaOptions.Disabled)
            .WithResilience(KurrentClientResilienceOptions.NoResilience)
            .WithMessages(options => options.Map<StreamMetadata>("$metadata"));

        if (configure is not null)
            builder.With(configure);

        var options = builder.Build();

        return KurrentClient.Create(options);
    }
}

public record KurrentClientTestFixtureOptions {
    public static string SectionName => "Kurrent:Client:Tests";

    /// <summary>
    /// If true, the test fixture will automatically wire up KurrentDB test containers
    /// </summary>
    [ConfigurationKeyName("AutoWireUpContainers")]
    public bool AutoWireUpContainers { get; init; } = true;

    /// <summary>
    /// The connection string for authenticated access to the KurrentDB test container.
    /// </summary>
    public static string AuthenticatedConnectionString { get; private set; } = "kurrentdb://admin:changeit@localhost:2113/?tls=false";

    /// <summary>
    /// The connection string for anonymous access to the KurrentDB test container.
    /// </summary>
    public static string AnonymousConnectionString { get; private set; } = "kurrentdb://localhost:2113/?tls=false";
}
