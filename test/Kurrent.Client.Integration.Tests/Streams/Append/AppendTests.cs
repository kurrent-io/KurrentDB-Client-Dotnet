using Kurrent.Client.Streams;
using Kurrent.Client.Testing.Sample;
using Kurrent.Client.Testing.TUnit;

namespace Kurrent.Client.Tests.Streams;

[Category("Streams"), Category("Append")]
public class AppendTests : KurrentClientTestFixture {
    [Test]
    public async Task creates_stream(CancellationToken ct) {
        var simulatedGame = TrySimulateGame(GamesAvailable.TicTacToe);

        var expectedRevision = StreamRevision.From(simulatedGame.GameEvents.Count - 1);

        await AutomaticClient.Streams
            .Append(simulatedGame.Stream, ExpectedStreamState.NoStream, simulatedGame.GameEvents, ct) // this could be a Create Stream operation
            .ShouldNotThrowOrFailAsync(success => {
                success.Stream.ShouldBe(simulatedGame.Stream);
                success.StreamRevision.ShouldBe(expectedRevision);
                success.Position.ShouldBeGreaterThan(LogPosition.Earliest);
            });
    }

    [Test]
    public async Task appends_to_existing_stream(CancellationToken ct) {
        var simulatedGame = TrySimulateGame(GamesAvailable.TicTacToe);

        // create stream
        var result = await AutomaticClient.Streams
            .Append(simulatedGame.Stream, ExpectedStreamState.NoStream, simulatedGame.GameEvents.Take(3), ct)
            .ShouldNotThrowOrFailAsync();

        // append to existing stream
        await AutomaticClient.Streams
            .Append(simulatedGame.Stream, result.StreamRevision, simulatedGame.GameEvents.Skip(3), ct)
            .ShouldNotThrowOrFailAsync(success => {
                success.Stream.ShouldBe(simulatedGame.Stream);
                success.StreamRevision.ShouldBe(simulatedGame.GameEvents.Count - 1);
                success.Position.ShouldBeGreaterThan(LogPosition.Earliest);
            });
    }

    public class AppendToStreamInAnyState : TestCaseGenerator<ExpectedStreamState> {
        protected override IEnumerable<ExpectedStreamState> Data() => [
            ExpectedStreamState.NoStream,
            ExpectedStreamState.StreamExists
        ];
    }

    [Test]
    [AppendToStreamInAnyState]
    public async Task appends_to_stream_in_any_state(ExpectedStreamState expectedStreamState, CancellationToken ct) {
        var simulatedGame = TrySimulateGame(GamesAvailable.TicTacToe);

        if (ExpectedStreamState.NoStream == expectedStreamState) {
            var expectedRevision = StreamRevision.From(simulatedGame.GameEvents.Count - 1);

            // should create the stream
            await AutomaticClient.Streams
                .Append(simulatedGame.Stream, ExpectedStreamState.Any, simulatedGame.GameEvents, ct)
                .ShouldNotThrowOrFailAsync(success => {
                    success.Stream.ShouldBe(simulatedGame.Stream);
                    success.StreamRevision.ShouldBe(expectedRevision);
                    success.Position.ShouldBeGreaterThan(LogPosition.Earliest);
                });
        }
        else if(ExpectedStreamState.StreamExists == expectedStreamState) {
            // create stream
            await AutomaticClient.Streams
                .Append(simulatedGame.Stream, ExpectedStreamState.NoStream, simulatedGame.GameEvents.Take(3), ct)
                .ShouldNotThrowOrFailAsync();

            // append to existing stream
            await AutomaticClient.Streams
                .Append(simulatedGame.Stream, ExpectedStreamState.StreamExists, simulatedGame.GameEvents.Skip(3), ct)
                .ShouldNotThrowOrFailAsync();
        }
    }


    public class CreatesMultipleStreamsTransactionallyState : TestCaseGenerator<int> {
        protected override IEnumerable<int> Data() => [
            2, 5, 6, 8, 10, 15, 20, 25, 30, 50, 100
        ];
    }

    [Test]
    [CreatesMultipleStreamsTransactionallyState]
    public async Task creates_multiple_streams_transactionally(int streams, CancellationToken ct) {
        var requests = GenerateMultipleCreateGameRequests(streams);

        await AutomaticClient.Streams
            .Append(requests, ct)
            .ShouldNotThrowAsync()
            .OnFailureAsync(failures => {
                failures.Count.ShouldBeGreaterThan(0);
                Should.NotThrow(() => failures.FirstOrDefault().Error.Throw());
            })
            .OnSuccessAsync(success => {
                success.Count.ShouldBe(requests.Count);

                for (var i = 0; i < requests.Count; i++) {
                    var request = requests[i];
                    var result  = success[i];

                    var expectedRevision = StreamRevision.From(request.Messages.Count() - 1);

                    result.Stream.ShouldBe(request.Stream);
                    result.StreamRevision.ShouldBe(expectedRevision);
                    result.Position.ShouldBeGreaterThan(LogPosition.Earliest);
                }
            });
    }
}
