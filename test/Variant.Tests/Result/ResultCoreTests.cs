using TicTacToe;

namespace Kurrent.Variant.Tests.Result;

public class ResultCoreTests {
    Faker Faker { get; } = new();

    [Test]
    public void creates_success_result_when_using_success_factory_method() {
        // Arrange
        var gameId           = new GameId(Faker.Random.Guid());
        var gameStartedEvent = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());

        // Act
        var resultGameId    = Kurrent.Result.Success<GameId, InvalidMoveError>(gameId);
        var resultTicTacToe = Kurrent.Result.Success<GameStarted, InvalidMoveError>(gameStartedEvent);

        // Assert
        resultGameId.IsSuccess.ShouldBeTrue();
        resultGameId.Value.ShouldBe(gameId);

        resultTicTacToe.IsSuccess.ShouldBeTrue();
        resultTicTacToe.Value.ShouldBe(gameStartedEvent);
    }

    [Test]
    public void creates_error_result_when_using_error_factory_method() {
        // Arrange
        var gameEndedError = new GameEndedError(Faker.Random.Guid(), Faker.PickRandom<GameStatus>());
        var ticTacToeError = new InvalidMoveError(Faker.Random.Guid(), "Invalid board position");

        // Act
        var resultGameEnded = Kurrent.Result.Failure<GameStarted, GameEndedError>(gameEndedError);
        var resultTicTacToe = Kurrent.Result.Failure<GameStarted, InvalidMoveError>(ticTacToeError);

        // Assert
        resultGameEnded.IsFailure.ShouldBeTrue();
        resultGameEnded.Error.ShouldBe(gameEndedError);

        resultTicTacToe.IsFailure.ShouldBeTrue();
        resultTicTacToe.Error.ShouldBe(ticTacToeError);
    }

    [Test]
    public void returns_true_for_is_success_when_result_contains_success_value() {
        // Arrange
        var successResult = Kurrent.Result.Success<GameStarted, InvalidMoveError>(new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>()));

        // Act & Assert
        successResult.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void returns_false_for_is_success_when_result_contains_error_value() {
        // Arrange
        var errorResult = Kurrent.Result.Failure<GameStarted, InvalidMoveError>(new InvalidMoveError(Faker.Random.Guid(), "Test reason"));

        // Act & Assert
        errorResult.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public void returns_true_for_is_failure_when_result_contains_error_value() {
        // Arrange
        var errorResult = Kurrent.Result.Failure<GameStarted, InvalidMoveError>(new InvalidMoveError(Faker.Random.Guid(), "Test reason"));

        // Act & Assert
        errorResult.IsFailure.ShouldBeTrue();
    }

    [Test]
    public void returns_false_for_is_failure_when_result_contains_success_value() {
        // Arrange
        var successResult = Kurrent.Result.Success<GameStarted, InvalidMoveError>(new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>()));

        // Act & Assert
        successResult.IsFailure.ShouldBeFalse();
    }

    [Test]
    public void returns_success_value_when_accessing_as_success_on_success_result() {
        // Arrange
        var successValue  = new GameId(Faker.Random.Guid());
        var successResult = Kurrent.Result.Success<GameId, InvalidMoveError>(successValue);

        // Act & Assert
        successResult.Value.ShouldBe(successValue);
    }

    [Test]
    public void throws_invalid_operation_exception_when_accessing_as_success_on_error_result() {
        // Arrange
        var errorResult = Kurrent.Result.Failure<GameId, InvalidMoveError>(new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence()));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => errorResult.Value);
    }

    [Test]
    public void returns_error_value_when_accessing_as_error_on_error_result() {
        // Arrange
        var errorValue  = new Position(Faker.Random.Int(0, 2), Faker.Random.Int(0, 2));
        var errorResult = Kurrent.Result.Failure<GameStarted, Position>(errorValue);

        // Act & Assert
        errorResult.Error.ShouldBe(errorValue);
    }

    [Test]
    public void throws_invalid_operation_exception_when_accessing_as_error_on_success_result() {
        // Arrange
        var successResult = Kurrent.Result.Success<GameStarted, Position>(new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>()));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => successResult.Error);
    }

    [Test]
    public void implicitly_converts_success_value_to_result() {
        // Arrange
        var successValue = new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>());

        // Act
        Result<GameStarted, InvalidMoveError> result = successValue;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(successValue);
    }

    [Test]
    public void implicitly_converts_error_value_to_result() {
        // Arrange
        var errorValue = new InvalidMoveError(Faker.Random.Guid(), "Cell occupied");

        // Act
        Result<GameStarted, InvalidMoveError> result = errorValue;

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(errorValue);
    }

    [Test]
    public void explicitly_converts_result_to_success_value_when_success() {
        // Arrange
        var successValue  = new GameId(Faker.Random.Guid());
        var successResult = Kurrent.Result.Success<GameId, InvalidMoveError>(successValue);

        // Act
        var extractedValue = (GameId)successResult;

        // Assert
        extractedValue.ShouldBe(successValue);
    }

    [Test]
    public void throws_invalid_operation_exception_when_explicitly_converting_error_result_to_success_value() {
        // Arrange
        var errorResult = Kurrent.Result.Failure<GameId, InvalidMoveError>(new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence()));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => (GameId)errorResult);
    }

    [Test]
    public void explicitly_converts_result_to_error_value_when_error() {
        // Arrange
        var errorValue  = new InvalidMoveError(Faker.Random.Guid(), Faker.Lorem.Sentence());
        var errorResult = Kurrent.Result.Failure<GameStarted, InvalidMoveError>(errorValue);

        // Act
        var extractedValue = (InvalidMoveError)errorResult;

        // Assert
        extractedValue.ShouldBe(errorValue);
    }

    [Test]
    public void throws_invalid_operation_exception_when_explicitly_converting_success_result_to_error_value() {
        // Arrange
        var successResult = Kurrent.Result.Success<GameStarted, InvalidMoveError>(new GameStarted(Faker.Random.Guid(), Faker.PickRandom<Player>()));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => (InvalidMoveError)successResult);
    }
}
