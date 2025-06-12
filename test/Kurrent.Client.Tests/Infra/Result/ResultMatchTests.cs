using Bogus;
using TicTacToe;

namespace Kurrent.Client.Tests.Infra.Result;

public class ResultMatchTests {
    Faker Faker { get; } = new();

    #region . Sync .

    [Test]
    public void match_executes_success_function_when_matching_success_result() {
        // Arrange
        GameStarted successValue = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        Result<GameStarted, InvalidMoveError> result = Result<GameStarted, InvalidMoveError>.Success(successValue);

        // Act
        string matchOutput = result.Match(
            gs => $"Started: {gs.GameId} by {gs.StartingPlayer}",
            err => $"Error: {err.Reason}"
        );

        // Assert
        matchOutput.ShouldBe($"Started: {successValue.GameId} by {successValue.StartingPlayer}");
    }

    [Test]
    public void match_executes_error_function_when_matching_error_result() {
        // Arrange
        InvalidMoveError errorValue = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        Result<GameStarted, InvalidMoveError> result = Result<GameStarted, InvalidMoveError>.Error(errorValue);

        // Act
        string matchOutput = result.Match(
            gs => $"Started: {gs.GameId} by {gs.StartingPlayer}",
            err => $"Error: {err.Reason} for game {err.GameId}"
        );

        // Assert
        matchOutput.ShouldBe($"Error: {errorValue.Reason} for game {errorValue.GameId}");
    }

    [Test]
    public void match_passes_state_to_success_function_when_using_stateful_variant() {
        // Arrange
        GameStarted successValue = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        Result<GameStarted, InvalidMoveError> result = Result<GameStarted, InvalidMoveError>.Success(successValue);
        string additionalInfo = "High stakes game";

        // Act
        string matchOutput = result.Match(
            (gs, info) => $"Started: {gs.GameId} by {gs.StartingPlayer}. Info: {info}",
            (err, info) => $"Error: {err.Reason}. Info: {info}",
            additionalInfo
        );

        // Assert
        matchOutput.ShouldBe($"Started: {successValue.GameId} by {successValue.StartingPlayer}. Info: {additionalInfo}");
    }

    [Test]
    public void match_passes_state_to_error_function_when_using_stateful_variant() {
        // Arrange
        InvalidMoveError errorValue = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        Result<GameStarted, InvalidMoveError> result = Result<GameStarted, InvalidMoveError>.Error(errorValue);
        string additionalInfo = "During critical phase";

        // Act
        string matchOutput = result.Match(
            (gs, info) => $"Started: {gs.GameId}. Info: {info}",
            (err, info) => $"Error: {err.Reason} for game {err.GameId}. Info: {info}",
            additionalInfo
        );

        // Assert
        matchOutput.ShouldBe($"Error: {errorValue.Reason} for game {errorValue.GameId}. Info: {additionalInfo}");
    }

    #endregion

    #region . Async .

    [Test]
    public async Task match_async_executes_success_function_with_value_task() {
        // Arrange
        GameStarted successValue = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        Result<GameStarted, InvalidMoveError> result = Result<GameStarted, InvalidMoveError>.Success(successValue);

        // Act
        string matchOutput = await result.MatchAsync(
            gs => ValueTask.FromResult($"Started: {gs.GameId} by {gs.StartingPlayer}"),
            err => ValueTask.FromResult($"Error: {err.Reason}")
        );

        // Assert
        matchOutput.ShouldBe($"Started: {successValue.GameId} by {successValue.StartingPlayer}");
    }

    [Test]
    public async Task match_async_executes_error_function_with_value_task() {
        // Arrange
        InvalidMoveError errorValue = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        Result<GameStarted, InvalidMoveError> result = Result<GameStarted, InvalidMoveError>.Error(errorValue);

        // Act
        string matchOutput = await result.MatchAsync(
            gs => ValueTask.FromResult($"Started: {gs.GameId} by {gs.StartingPlayer}"),
            err => ValueTask.FromResult($"Error: {err.Reason} for game {err.GameId}")
        );

        // Assert
        matchOutput.ShouldBe($"Error: {errorValue.Reason} for game {errorValue.GameId}");
    }

    [Test]
    public async Task match_async_handles_mixed_sync_async_scenarios() {
        // Arrange
        GameStarted successValue = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        Result<GameStarted, InvalidMoveError> successResult = Result<GameStarted, InvalidMoveError>.Success(successValue);
        InvalidMoveError errorValue = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        Result<GameStarted, InvalidMoveError> errorResult = Result<GameStarted, InvalidMoveError>.Error(errorValue);

        // Act
        string successMatch = await successResult.MatchAsync(
            gs => $"Success: {gs.GameId}",
            err => ValueTask.FromResult($"Error: {err.Reason}")
        );
        string errorMatch = await errorResult.MatchAsync(
            gs => ValueTask.FromResult($"Success: {gs.GameId}"),
            err => $"Error: {err.Reason}"
        );

        // Assert
        successMatch.ShouldBe($"Success: {successValue.GameId}");
        errorMatch.ShouldBe($"Error: {errorValue.Reason}");
    }

    [Test]
    public async Task match_async_passes_state_to_functions_when_using_stateful_variant() {
        // Arrange
        GameStarted successValue = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        Result<GameStarted, InvalidMoveError> result = Result<GameStarted, InvalidMoveError>.Success(successValue);
        string additionalInfo = "High stakes game";

        // Act
        string matchOutput = await result.MatchAsync(
            (gs, info) => ValueTask.FromResult($"Started: {gs.GameId} by {gs.StartingPlayer}. Info: {info}"),
            (err, info) => ValueTask.FromResult($"Error: {err.Reason}. Info: {info}"),
            additionalInfo
        );

        // Assert
        matchOutput.ShouldBe($"Started: {successValue.GameId} by {successValue.StartingPlayer}. Info: {additionalInfo}");
    }

    #endregion
}
