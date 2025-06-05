using System.Runtime.CompilerServices;
using Kurrent.Client.Testing;
using Kurrent.Client.Testing.Fixtures;
using KurrentDB.Client;
using Serilog;
using Serilog.Extensions.Logging;

namespace Kurrent.Client.Tests;

public class KurrentClientTestFixture : TestFixture {
	readonly Lazy<KurrentClient> _lazyClient = new(() => {
		var settings = KurrentDBClientSettings
			.Create(KurrentDBContainerAutoWireUp.Container.AuthenticatedConnectionString)
			.With(x => { x.LoggerFactory = new SerilogLoggerFactory(Log.Logger); });

		return new KurrentClient(settings);
	});

	protected KurrentClient Client => _lazyClient.Value;

	public string GetStreamName([CallerMemberName] string? testMethod = null) =>
		$"stream-{testMethod}-{Guid.NewGuid():N}";
}

// TODO: Remove this when we have a better way to handle exceptions in tests.
static class ShouldThrowAsyncExtensions {
	public static Task<TException> ShouldThrowAsync<TException>(this KurrentDBClient.ReadStreamResult source) where TException : Exception =>
		source
			.ToArrayAsync()
			.AsTask()
			.ShouldThrowAsync<TException>();

	public static async Task ShouldThrowAsync<TException>(this KurrentDBClient.ReadStreamResult source, Action<TException> handler) where TException : Exception {
		var ex = await source.ShouldThrowAsync<TException>();
		handler(ex);
	}
}
