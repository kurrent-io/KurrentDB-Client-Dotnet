#pragma warning disable CA1822 // Mark members as static

using JetBrains.Annotations;
using Kurrent.Client.Model;
using Kurrent.Client.Testing.Fixtures;
using KurrentDB.Client;
using Serilog;
using Serilog.Extensions.Logging;
using TicTacToe;

namespace Kurrent.Client.Tests;

[PublicAPI]
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

    // readonly Lazy<KurrentDBClient> _lazyLegacyClient = new(() => {
    //     var settings = KurrentDBClientSettings
    //         .Create(KurrentDBContainerAutoWireUp.Container.AuthenticatedConnectionString)
    //         .With(x => { x.LoggerFactory = new SerilogLoggerFactory(Log.Logger); });
    //
    //     return new KurrentDBClient(settings);
    // });
    //
    // /// <summary>
    // /// The legacy client used for interacting with KurrentDB streams.
    // /// </summary>
    // protected KurrentDBClient LegacyClient => _lazyLegacyClient.Value;

    /// <summary>
    /// Creates a unique stream name for KurrentDB operations, combining the given category with the last part of a randomly generated guid.
    /// </summary>
    /// <param name="category">The category associated with the stream name.</param>
    /// <returns>A StreamName instance containing the generated stream name.</returns>

    public StreamName CreateStreamName(string category) =>
        StreamName.From($"{category}-{Guid.NewGuid().ToString().Substring(24, 12)}");

    #region Helpers

    public async ValueTask<(LogPosition Position, StreamRevision Revision, List<Message> Messages)> SeedTestMessagesAsync(
        string streamName,
        Action<Metadata> transformMetadata,
        SchemaDataFormat dataFormat = SchemaDataFormat.Json,
        CancellationToken cancellationToken = default
    ) {
        var messages = GenerateTestMessages(-1, transformMetadata, dataFormat).ToList();

        var result = await AutomaticClient.Streams
            .Append(streamName, messages, cancellationToken)
            .ConfigureAwait(false);

        var (stream, logPosition, streamRevision) = result.Match(
            success => success ,
            error =>  throw error.Throw()
        );

        Log.Information(
            "Seeded {Count} messages to stream {StreamName} at position {LogPosition} with revision {StreamRevision}",
            messages.Count, stream, logPosition, streamRevision);

        return (logPosition, streamRevision, messages);
    }

    /// <summary>
    /// Generates a sequence of test messages, simulating events based on a Tic Tac Toe game. The metadata of the messages
    /// can be transformed using a user-provided action. The total number of messages generated can be controlled.
    /// <remarks>
    /// if count is -1, messages will be generated until a game is won or drawn, otherwise it will generate exactly the specified number of messages.
    /// </remarks>
    /// </summary>
    /// <param name="count">The desired number of messages to generate. Pass -1 to generate messages based on game conditions.</param>
    /// <param name="transformMetadata">An action to apply transformations to the metadata for each message.</param>
    /// <param name="dataFormat">The data format of the generated messages, with SchemaDataFormat.Json as the default.</param>
    /// <returns>A sequence of generated messages based on the specified parameters.</returns>
    protected static IEnumerable<Message> GenerateTestMessages(int count, Action<Metadata> transformMetadata, SchemaDataFormat dataFormat = SchemaDataFormat.Json) {
        var metadata = new Metadata().Transform(transformMetadata);

        var generatedMessageCount = 0;

        while (true) {
            var game = TicTacToeSimulator.SimulateGame();

            foreach (var evt in game.Events) {
                var meta = new Metadata(metadata)
                    .With("$tests.game", nameof(TicTacToe))
                    .With("$tests.game.id", game.GameId)
                    .With("$tests.game.move-id", generatedMessageCount)
                    .With("$stream", $"{nameof(TicTacToe)}-{game.GameId:N}");

                yield return Message.New
                    .WithValue(evt)
                    .WithMetadata(meta)
                    .WithDataFormat(dataFormat)
                    .Build();

                generatedMessageCount++;

                // If the game is won or drawn, we stop generating more messages.
                switch (count) {
                    case -1  when evt is GameWon or GameDraw:
                    case > 0 when generatedMessageCount >= count:
                        yield break;
                }
            }
        }
    }

    protected static IEnumerable<Message> GenerateTestMessages(int count, SchemaDataFormat dataFormat = SchemaDataFormat.Json) =>
        GenerateTestMessages(count, _ => { }, dataFormat);

    protected static IEnumerable<Message> GenerateTestMessages(SchemaDataFormat dataFormat = SchemaDataFormat.Json) =>
        GenerateTestMessages(-1, _ => { }, dataFormat);

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
