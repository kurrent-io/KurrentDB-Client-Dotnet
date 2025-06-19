using TicTacToe;

namespace Kurrent.Variant.Tests.Result;

public class ResultMatchTests {
    Faker Faker { get; } = new();

    #region . Sync .

    [Test]
    public void match_executes_success_function_when_matching_success_result() {
        // Arrange
        GameStarted successValue = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        Result<GameStarted, InvalidMoveError> result = Kurrent.Result.Success<GameStarted, InvalidMoveError>(successValue);

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
        Result<GameStarted, InvalidMoveError> result = Kurrent.Result.Failure<GameStarted, InvalidMoveError>(errorValue);

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
        Result<GameStarted, InvalidMoveError> result = Kurrent.Result.Success<GameStarted, InvalidMoveError>(successValue);
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
        Result<GameStarted, InvalidMoveError> result = Kurrent.Result.Failure<GameStarted, InvalidMoveError>(errorValue);
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
        Result<GameStarted, InvalidMoveError> result = Kurrent.Result.Success<GameStarted, InvalidMoveError>(successValue);

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
        Result<GameStarted, InvalidMoveError> result = Kurrent.Result.Failure<GameStarted, InvalidMoveError>(errorValue);

        // Act
        string matchOutput = await result.MatchAsync(
            gs => ValueTask.FromResult($"Started: {gs.GameId} by {gs.StartingPlayer}"),
            err => ValueTask.FromResult($"Error: {err.Reason} for game {err.GameId}")
        );

        // Assert
        matchOutput.ShouldBe($"Error: {errorValue.Reason} for game {errorValue.GameId}");
    }

    [Test]
    public async Task match_async_passes_state_to_functions_when_using_stateful_variant() {
        // Arrange
        GameStarted successValue = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());
        Result<GameStarted, InvalidMoveError> result = Kurrent.Result.Success<GameStarted, InvalidMoveError>(successValue);
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
