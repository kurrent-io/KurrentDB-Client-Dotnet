using System.Runtime.CompilerServices;
using System.Text;
using Kurrent.Client.Model;
using Kurrent.Client.Testing;
using Kurrent.Client.Testing.Fixtures;
using KurrentDB.Client;
using Serilog;
using Serilog.Extensions.Logging;

namespace Kurrent.Client.Tests;

public class KurrentClientTestFixture : TestFixture {
	readonly Lazy<KurrentClient> _lazyAutomaticClient = new(() => KurrentClientOptions
        .FromConnectionString(KurrentDBContainerAutoWireUp.Container.AuthenticatedConnectionString)
        .WithLoggerFactory(new SerilogLoggerFactory(Log.Logger))
        .WithSchema(KurrentClientSchemaOptions.NoValidation)
        .WithResilience(KurrentClientResilienceOptions.NoResilience)
        .CreateClient()
    );

    /// <summary>
    /// Client with automatic serialization and deserialization of messages and no validation.
    /// </summary>
	protected KurrentClient AutomaticClient => _lazyAutomaticClient.Value;

    readonly Lazy<KurrentClient> _lazyFullValidationClient = new(() => KurrentClientOptions
        .FromConnectionString(KurrentDBContainerAutoWireUp.Container.AuthenticatedConnectionString)
        .WithLoggerFactory(new SerilogLoggerFactory(Log.Logger))
        .WithSchema(KurrentClientSchemaOptions.FullValidation)
        .WithResilience(KurrentClientResilienceOptions.NoResilience)
        .CreateClient()
    );

    /// <summary>
    /// A client configured for full validation during testing.
    /// </summary>
    /// <remarks>
    /// This client performs comprehensive validation and is utilized for scenarios where stricter
    /// verification of interactions with the database or system under test is required.
    /// </remarks>
    protected KurrentClient FullValidationClient => _lazyFullValidationClient.Value;

    readonly Lazy<KurrentDBClient> _lazyLegacyClient = new(() => {
        var settings = KurrentDBClientSettings
            .Create(KurrentDBContainerAutoWireUp.Container.AuthenticatedConnectionString)
            .With(x => { x.LoggerFactory = new SerilogLoggerFactory(Log.Logger); });

        return new KurrentDBClient(settings);
    });

    /// <summary>
    /// The legacy client used for interacting with KurrentDB streams.
    /// </summary>
    protected KurrentDBClient LegacyClient => _lazyLegacyClient.Value;

	public string GetStreamName([CallerMemberName] string? testMethod = null) =>
		$"stream-{testMethod}-{Guid.NewGuid():N}";


    #region Legacy Client Helpers

    // public IEnumerable<EventData> CreateTestEvents(
    //     int count = 1, string? type = null, ReadOnlyMemory<byte>? metadata = null, string? contentType = null
    // ) =>
    //     Enumerable.Range(0, count)
    //         .Select(index => CreateTestEvent(index, type ?? TestEventType, metadata, contentType));
    //
    // public EventData CreateTestEvent(
    //     string? type = null, ReadOnlyMemory<byte>? metadata = null, string? contentType = null
    // ) =>
    //     CreateTestEvent(0, type ?? TestEventType, metadata, contentType);
    //
    // public IEnumerable<EventData> CreateTestEventsThatThrowsException() {
    //     // Ensure initial IEnumerator.Current does not throw
    //     yield return CreateTestEvent(1);
    //
    //     // Throw after enumerator advances
    //     throw new Exception();
    // }
    //
    // protected static EventData CreateTestEvent(int index) => CreateTestEvent(index, TestEventType);
    //
    // protected static Message CreateTestEvent<T>(T message, Metadata metadata = default, string? type = null, string? contentType = null
    // ) =>
    //     new(
    //         eventId: Uuid.NewUuid(),
    //         type: type,
    //         data: Encoding.UTF8.GetBytes(s: $$"""{"x":{{index}}}"""),
    //         metadata: metadata,
    //         contentType: contentType ?? "application/json"
    //     );

    // protected static EventData CreateTestEvent(
    //     int index, string type, ReadOnlyMemory<byte>? metadata = null, string? contentType = null
    // ) =>
    //     new(
    //         eventId: Uuid.NewUuid(),
    //         type: type,
    //         data: Encoding.UTF8.GetBytes(s: $$"""{"x":{{index}}}"""),
    //         metadata: metadata,
    //         contentType: contentType ?? "application/json"
    //     );


    #endregion
}

// TODO: Remove this when we have a better way to handle exceptions in tests.
static class ShouldThrowAsyncExtensions {
	public static Task<TException> ShouldThrowAsync<TException>(this KurrentDBClient.ReadStreamResult source) where TException : Exception =>
		source.ToArrayAsync().AsTask().ShouldThrowAsync<TException>();

	public static async Task ShouldThrowAsync<TException>(this KurrentDBClient.ReadStreamResult source, Action<TException> handler) where TException : Exception {
		var ex = await source.ShouldThrowAsync<TException>();
		handler(ex);
	}
}
