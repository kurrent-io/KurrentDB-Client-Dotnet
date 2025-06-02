using System.Runtime.CompilerServices;
using Kurrent.Client.Testing;
using Kurrent.Client.Testing.Fixtures;
using KurrentDB.Client;
using Serilog;
using Serilog.Extensions.Logging;

namespace Kurrent.Client.Tests.Next;

public class KurrentClientTestFixture : TestFixture {
	readonly Lazy<KurrentDB.Client.KurrentClient> _lazyClient = new(() => {
		var settings = KurrentDBClientSettings
			.Create(KurrentDBContainerAutoWireUp.Container.AuthenticatedConnectionString)
			.With(x => { x.LoggerFactory = new SerilogLoggerFactory(Log.Logger); });

		return new KurrentDB.Client.KurrentClient(settings);
	});

	protected KurrentDB.Client.KurrentClient Client => _lazyClient.Value;

	public string GetStreamName([CallerMemberName] string? testMethod = null) =>
		$"stream-{testMethod}-{Guid.NewGuid():N}";
}



public class KurrentClientTests : KurrentDBClientTestFixture {


}
