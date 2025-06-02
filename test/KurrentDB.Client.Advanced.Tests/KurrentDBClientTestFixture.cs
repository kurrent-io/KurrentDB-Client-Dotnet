using System.Runtime.CompilerServices;
using KurrentDB.Client.Testing;
using KurrentDB.Client.Testing.Fixtures;
using Serilog;
using Serilog.Extensions.Logging;

namespace KurrentDB.Client.Tests;

public class KurrentDBClientTestFixture : TestFixture {
	readonly Lazy<KurrentDBClient> _lazyClient = new(() => {
		var settings = KurrentDBClientSettings
			.Create(KurrentDBContainerAutoWireUp.Container.AuthenticatedConnectionString)
			.With(x => { x.LoggerFactory = new SerilogLoggerFactory(Log.Logger); });

		return new KurrentDBClient(settings);
	});

	protected KurrentDBClient Client => _lazyClient.Value;

	public string GetStreamName([CallerMemberName] string? testMethod = null) =>
		$"stream-{testMethod}-{Guid.NewGuid():N}";
}
