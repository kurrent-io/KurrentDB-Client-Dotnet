#pragma warning disable CA1822 // Mark members as static

// ReSharper disable InconsistentNaming

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Kurrent.Client.Streams;
using Kurrent.Client.Testing.Sample;
using RockPaperScissors;
using Serilog;
using TicTacToe;
using GameDraw = TicTacToe.GameDraw;
using GameWon = TicTacToe.GameWon;

namespace Kurrent.Client.Tests;

public partial class KurrentClientTestFixture {
    /// <summary>
    /// Generates a new short ID, which is a 12-character string derived from a GUID.
    /// This ID is suitable for use in test scenarios where a unique identifier is needed without the
    /// full length of a standard GUID.
    /// </summary>
    public string NewShortID() => Guid.NewGuid().ToString().Substring(24, 12);

    /// <summary>
    /// Generates a new entity ID, which is a unique identifier for an entity in the system.
    /// The ID is generated using a version 7 GUID, which is suitable for use in
    /// distributed systems and ensures uniqueness across different entities.
    /// <remarks>
    /// The entity ID is prefixed with the category name, which helps in organizing and identifying
    /// entities based on their category.
    /// </remarks>
    /// <param name="category">The category of the entity, used to prefix the generated ID.</param>
    /// <returns>A unique entity ID string formatted as "{category}-{version7-GUID}"</returns>
    /// </summary>
    public string NewEntityID([CallerMemberName] string category = "") => $"{category}-{Guid.NewGuid()}";

    /// <summary>
    /// Creates a unique stream name for KurrentDB operations, combining the given category with the last part of a randomly generated guid.
    /// </summary>
    /// <param name="category">The category associated with the stream name.</param>
    /// <returns>A StreamName instance containing the generated stream name.</returns>
    public StreamName NewStreamName([CallerMemberName] string category = "") =>
        StreamName.From($"{category}-{NewShortID()}");

    /// <summary>
    /// Creates a unique stream name for KurrentDB operations, combining the given category with the last part of a randomly generated guid.
    /// </summary>
    /// <param name="game">
    /// The game for which the stream name is being created. This will be used to generate a stream name specific to the game type.
    /// </param>
    /// <returns>A StreamName instance containing the generated stream name.</returns>
    public StreamName NewGameStreamName(GamesAvailable game) =>
        NewStreamName(game.ToString());

    public async ValueTask<(LogPosition Position, StreamRevision Revision, List<Message> Messages)> SeedTestMessages(
        string streamName,
        Action<Metadata> transformMetadata,
        SchemaDataFormat dataFormat = SchemaDataFormat.Json,
        CancellationToken cancellationToken = default
    ) {
        var messages = GenerateTestMessages(-1, transformMetadata, dataFormat).ToList();

        var result = await AutomaticClient.Streams
            .Append(streamName, messages, cancellationToken)
            .OnFailureAsync(err => err.Throw())
            .MatchAsync(
                 Result.Success<AppendStreamSuccess, AppendStreamFailure>,
                 Result.Failure<AppendStreamSuccess, AppendStreamFailure>)
            .ConfigureAwait(false);

        var (stream, logPosition, streamRevision) = result.Match(
            success => success ,
            error   => throw error.CreateException()
        );

        Log.Verbose(
            "Seeded {Count} messages to stream {StreamName} at position {LogPosition} with revision {StreamRevision}",
            messages.Count, stream, logPosition, streamRevision);

        return (logPosition, streamRevision, messages);
    }

    public async IAsyncEnumerable<SeededGame> SeedGameSimulations(
        int simulationsCount, Action<Metadata>? transformMetadata = null, SchemaDataFormat dataFormat = SchemaDataFormat.Json, [EnumeratorCancellation] CancellationToken ct = default
    ) {
        transformMetadata ??= _ => { };

        foreach (var i in Enumerable.Range(0, simulationsCount)) {
            // Uncomment the following lines to alternate between different games for simulation.
            // RockPaperScissors is currently broken and not supported in the tests.
            // var simulatedGame = (i % 2) switch {
            //     0 => SimulateGame(GamesAvailable.TicTacToe, transformMetadata, dataFormat),
            //     _ => SimulateGame(GamesAvailable.RockPaperScissors, transformMetadata, dataFormat)
            // };

            var simulatedGame = SimulateGame(GamesAvailable.TicTacToe, transformMetadata, dataFormat);

            var (stream, logPosition, streamRevision) = await AutomaticClient.Streams
                .Append(simulatedGame.Stream, ExpectedStreamState.NoStream, simulatedGame.GameEvents, ct)
                .ShouldNotThrowOrFailAsync()
                .ConfigureAwait(false);

            Log.Verbose(
                "Simulated game {GameId} and with {Count} events to stream {StreamName} at position {LogPosition} with revision {StreamRevision}",
                simulatedGame.GameId, simulatedGame.GameEvents.Count, simulatedGame.Stream, logPosition, streamRevision);

            yield return new(
                new SimulatedGame(simulatedGame.GameId, simulatedGame.Stream, simulatedGame.GameEvents),
                logPosition, streamRevision
            );
        }

        // the following code is an attempt to simulate multiple games and append them in a single request.
        // This is currently commented out because it is not used in the tests, but it can be useful for future scenarios.

        // List<AppendStreamRequest> requests = [];
        //
        // foreach (var i in Enumerable.Range(0, simulationsCount)) {
        //     var simulatedGame = TrySimulateGame(GamesAvailable.TicTacToe, transformMetadata, dataFormat);
        //
        //     var request = new AppendStreamRequest(
        //         simulatedGame.Stream,
        //         ExpectedStreamState.NoStream,
        //         simulatedGame.GameEvents
        //     );
        //
        //     requests.Add(request);
        // }
        //
        // var result = await AutomaticClient.Streams
        //     .Append(requests, ct)
        //     .ShouldNotThrowAsync()
        //     .OnFailureAsync(failures => {
        //         failures.Count.ShouldBeGreaterThan(0);
        //         Should.NotThrow(() => failures.FirstOrDefault().Error.Throw());
        //     })
        //     .MatchAsync(
        //         success => success.Traverse(map: x => {
        //             return (
        //                 // GameId,
        //                 // simulatedGame.Stream,
        //                 // simulatedGame.GameEvents,
        //                 x.Position,
        //                 x.StreamRevision
        //             );
        //
        //         }),
        //         Result.Failure<List<AppendStreamSuccess>, List<AppendStreamFailure>>
        //     )
        //     .ConfigureAwait(false);
    }

    public async ValueTask<SeededGame> SeedGame(
        Action<Metadata>? transformMetadata = null, SchemaDataFormat dataFormat = SchemaDataFormat.Json, CancellationToken ct = default
    ) => await SeedGameSimulations(1, transformMetadata, dataFormat, ct).SingleAsync(ct).ConfigureAwait(false);

    public ValueTask<SeededGame> SeedGame(SchemaDataFormat dataFormat = SchemaDataFormat.Json, CancellationToken ct = default) =>
         SeedGame(null, dataFormat, ct);

    public ValueTask<SeededGame> SeedGame(CancellationToken ct = default) =>
         SeedGame(SchemaDataFormat.Json, ct);

    public record SeededGame(SimulatedGame Game, LogPosition Position, StreamRevision Revision);

    public record SimulatedGame(Guid GameId, StreamName Stream, List<Message> GameEvents);

    public SimulatedGame TrySimulateGame(GamesAvailable game, Action<Metadata>? transformMetadata = null, SchemaDataFormat dataFormat = SchemaDataFormat.Json) =>
        Result.Try(() => SimulateGame(GamesAvailable.TicTacToe)).ShouldNotThrow();

    SimulatedGame SimulateGame(GamesAvailable game, Action<Metadata>? transformMetadata = null, SchemaDataFormat dataFormat = SchemaDataFormat.Json) {
        var simulatedGame = SimulatedGame();
        var metadata = new Metadata().Transform(transformMetadata ?? (_ => { }));

        var messages = simulatedGame.GameEvents.Aggregate(new List<Message>(), (acc, evt) => {
            var moveId = Guids.CreateVersion7();
            acc.Add(Message.New
                .WithRecordId(moveId)
                .WithValue(evt)
                .WithMetadata(metadata
                    .With("tests.game.id", simulatedGame.GameId)
                    .With("tests.game.name", nameof(TicTacToe))
                    .With("tests.game.stream", simulatedGame.Stream)
                    .With("tests.game.move-id", moveId)
                    .With("tests.game.move-sequence", acc.Count + 1))
                .WithDataFormat(dataFormat)
                .Build());

            return acc;
        });

        return new(simulatedGame.GameId, simulatedGame.Stream, messages);

        (Guid GameId, StreamName Stream, List<object> GameEvents) SimulatedGame() {
            if (game == GamesAvailable.TicTacToe) {
                var (id, events, _) = TicTacToeSimulator.SimulateGame();
                simulatedGame       = (id, NewGameStreamName(game), events);
            }
            else if (game == GamesAvailable.RockPaperScissors) {
                var (id, events, _) = RockPaperScissorsSimulator.SimulateGame();
                simulatedGame       = (id, NewGameStreamName(game), events);
            }
            else
                throw new UnreachableException($"The game '{game}' is not recognized or supported.");

            return simulatedGame;
        }
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
                    .With("tests.game", nameof(TicTacToe))
                    .With("tests.game.id", game.GameId)
                    .With("tests.game.move-id", generatedMessageCount)
                    .With("stream", $"{nameof(TicTacToe)}-{game.GameId:N}");

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

    /// <summary>
    /// Generates multiple append requests for a specified number of streams, all with different simulated games.
    /// </summary>
    public List<AppendStreamRequest> GenerateMultipleCreateGameRequests(int streams) =>
        Enumerable.Range(0, streams).Select(sequence => GenerateSingleGameAppendRequest(sequence, ExpectedStreamState.NoStream)).ToList();

    /// <summary>
    /// Generates a single append request for a stream with a simulated game.
    /// </summary>
    public AppendStreamRequest GenerateSingleGameAppendRequest(int sequence, ExpectedStreamState expectedState) {
        var simulatedGame = TrySimulateGame(GamesAvailable.TicTacToe);

        return new AppendStreamRequest(
            $"{simulatedGame.Stream}-{sequence:000}",
            expectedState,
            simulatedGame.GameEvents
        );
    }

	public static TestUserFaker Users => TestUserFaker.Instance;
}
