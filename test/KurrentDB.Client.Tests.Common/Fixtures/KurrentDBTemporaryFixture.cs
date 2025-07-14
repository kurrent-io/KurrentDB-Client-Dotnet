// ReSharper disable InconsistentNaming

using KurrentDB.Client.Tests.FluentDocker;
using Serilog;
using Serilog.Extensions.Logging;
using static System.TimeSpan;

namespace KurrentDB.Client.Tests.TestNode;

[PublicAPI]
public partial class KurrentDBTemporaryFixture : IAsyncLifetime, IAsyncDisposable {
	static readonly ILogger Logger;

	static KurrentDBTemporaryFixture() {
		Logging.Initialize();
		Logger = Serilog.Log.ForContext<KurrentDBTemporaryFixture>();

		var httpClientHandler = new HttpClientHandler();
		httpClientHandler.ServerCertificateCustomValidationCallback = delegate { return true; };
	}

	public KurrentDBTemporaryFixture() : this(options => options) { }

	protected KurrentDBTemporaryFixture(ConfigureFixture configure) {
		Options = configure(KurrentDBTemporaryTestNode.DefaultOptions());
		Service = new KurrentDBTemporaryTestNode(Options);

		Options.DBClientSettings.LoggerFactory = new SerilogLoggerFactory(Logger);
	}

	List<Guid> TestRuns { get; } = new();

	public ILogger Log => Logger;

	public ITestService            Service { get; }
	public KurrentDBFixtureOptions Options { get; }
	public Faker                   Faker   { get; } = new Faker();

	public Version EventStoreVersion               { get; private set; } = null!;
	public bool    EventStoreHasLastStreamPosition { get; private set; }

	public KurrentDBClient                        Streams       { get; private set; } = null!;
	public KurrentDBUserManagementClient          DBUsers       { get; private set; } = null!;
	public KurrentDBProjectionManagementClient    DBProjections { get; private set; } = null!;
	public KurrentDBPersistentSubscriptionsClient Subscriptions { get; private set; } = null!;
	public KurrentDBOperationsClient              DBOperations  { get; private set; } = null!;

	public bool SkipPsWarmUp { get; set; }

	public Func<Task> OnSetup    { get; init; } = () => Task.CompletedTask;
	public Func<Task> OnTearDown { get; init; } = () => Task.CompletedTask;

	/// <summary>
	/// must test this
	/// </summary>
	public KurrentDBClientSettings DBClientSettings =>
		new() {
			Interceptors             = Options.DBClientSettings.Interceptors,
			ConnectionName           = Options.DBClientSettings.ConnectionName,
			CreateHttpMessageHandler = Options.DBClientSettings.CreateHttpMessageHandler,
			LoggerFactory            = Options.DBClientSettings.LoggerFactory,
			ChannelCredentials       = Options.DBClientSettings.ChannelCredentials,
			OperationOptions         = Options.DBClientSettings.OperationOptions,
			ConnectivitySettings     = Options.DBClientSettings.ConnectivitySettings,
			DefaultCredentials       = Options.DBClientSettings.DefaultCredentials,
			DefaultDeadline          = Options.DBClientSettings.DefaultDeadline
		};

	InterlockedBoolean            WarmUpCompleted { get; } = new InterlockedBoolean();
	static readonly SemaphoreSlim WarmUpGatekeeper = new(1, 1);

	public void CaptureTestRun(ITestOutputHelper outputHelper) {
		var testRunId = Logging.CaptureLogs(outputHelper);
		TestRuns.Add(testRunId);
		Logger.Information(">>> Test Run {TestRunId} {Operation} <<<", testRunId, "starting");
		Service.ReportStatus();
	}

	public async Task InitializeAsync() {
		await WarmUpGatekeeper.WaitAsync();

		try {
			await Service.Start();
			EventStoreVersion               = KurrentDBTemporaryTestNode.Version;
			EventStoreHasLastStreamPosition = (EventStoreVersion?.Major ?? int.MaxValue) >= 21;

			if (!WarmUpCompleted.CurrentValue) {
				Logger.Warning("*** Warmup started ***");

				await Task.WhenAll(
					InitClient<KurrentDBUserManagementClient>(async x => DBUsers = await Task.FromResult(x)),
					InitClient<KurrentDBClient>(async x => Streams = await Task.FromResult(x)),
					InitClient<KurrentDBProjectionManagementClient>(
						async x => DBProjections = await Task.FromResult(x),
						Options.Environment["KURRENTDB_RUN_PROJECTIONS"] != "None"
					),
					InitClient<KurrentDBPersistentSubscriptionsClient>(async x => Subscriptions = SkipPsWarmUp ? x : await Task.FromResult(x)),
					InitClient<KurrentDBOperationsClient>(async x => DBOperations = await Task.FromResult(x))
				);

				WarmUpCompleted.EnsureCalledOnce();

				Logger.Warning("*** Warmup completed ***");
			} else {
				Logger.Information("*** Warmup skipped ***");
			}
		} finally {
			WarmUpGatekeeper.Release();
		}

		await OnSetup();

		return;

		async Task<T> InitClient<T>(Func<T, Task> action, bool execute = true) where T : KurrentDBClientBase {
			if (!execute) return default(T)!;

			var client = (Activator.CreateInstance(typeof(T), DBClientSettings) as T)!;
			await action(client);
			return client;
		}
	}

	public async Task DisposeAsync() {
		try {
			await OnTearDown();
		} catch {
			// ignored
		}

		await Service.DisposeAsync().AsTask().WithTimeout(FromMinutes(5));

		foreach (var testRunId in TestRuns)
			Logging.ReleaseLogs(testRunId);
	}

	async ValueTask IAsyncDisposable.DisposeAsync() => await DisposeAsync();
}

public abstract class KurrentTemporaryTests<TFixture> : IClassFixture<TFixture> where TFixture : KurrentDBTemporaryFixture {
	protected KurrentTemporaryTests(ITestOutputHelper output, TFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	protected TFixture Fixture { get; }
}
