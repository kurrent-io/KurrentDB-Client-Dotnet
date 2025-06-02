using System.Runtime.CompilerServices;
using KurrentDB.Client.Testing;
using KurrentDB.Client.Testing.Fixtures;
using Serilog;
using Serilog.Extensions.Logging;

namespace KurrentDB.Client.Tests;

public class KurrentClientTestFixture : TestFixture {
	readonly Lazy<Client.KurrentClient> _lazyClient = new(() => {
		var settings = KurrentDBClientSettings
			.Create(KurrentDBContainerAutoWireUp.Container.AuthenticatedConnectionString)
			.With(x => { x.LoggerFactory = new SerilogLoggerFactory(Log.Logger); });

		return new Client.KurrentClient(settings);
	});

	protected Client.KurrentClient Client => _lazyClient.Value;

	public string GetStreamName([CallerMemberName] string? testMethod = null) =>
		$"stream-{testMethod}-{Guid.NewGuid():N}";
}



public class KurrentClientTests : KurrentDBClientTestFixture {


}
